#if UNITY_EDITOR && UNITY_STANDALONE_WIN
/**
 * Copyright (c) Pixisoft Corporations. All rights reserved.
 * 
 * Licensed under MIT. See LICENSE.txt in the asset root for license informtaion.
 */
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Inspect.Ini
{
    [CustomEditor(typeof(DefaultAsset), true)]
    public class IniEditor : Editor
    {
        /* Variables */

        private const int INDENT_SPACE = 15;
        private string Path => AssetDatabase.GetAssetPath(target);
        private bool isCompatible => Path.EndsWith(".ini");
        private bool unableToParse => !IniUtil.IsValidIni(rawText);

        private bool isTextMode;

        private string rawText;
        private INIParser iniFile;

        private string sectionToRename;
        private string pairToRename;
        private string renameValue;

        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
        private const bool DEFAULT_FOLD = false;

        private Dictionary<string, Dictionary<string, string>> cache = new Dictionary<string, Dictionary<string, string>>();

        /* Setter & Getter */

        /* Functions */

        private void OnEnable()
        {
            if (isCompatible)
                LoadFromIni();
        }

        private void OnDisable()
        {
            if (isCompatible)
                WriteToIni();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (isCompatible)
                IniInspectorGUI();
        }

        private void IniInspectorGUI()
        {
            GUI.enabled = true;

            const string info = "You edit raw text if the INI editor isn't enough by clicking the button to the right";

            Rect subHeaderRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 1.8f);
            Rect helpBoxRect = new Rect(subHeaderRect.x, subHeaderRect.y, subHeaderRect.width - subHeaderRect.width / 6 - 5f, subHeaderRect.height);
            Rect rawTextModeButtonRect = new Rect(subHeaderRect.x + subHeaderRect.width / 6 * 5, subHeaderRect.y, subHeaderRect.width / 6, subHeaderRect.height);
            EditorGUI.HelpBox(helpBoxRect, info, MessageType.Info);

            GUIStyle wrappedButton = new GUIStyle("Button");
            wrappedButton.wordWrap = true;
            EditorGUI.BeginChangeCheck();
            isTextMode = GUI.Toggle(rawTextModeButtonRect, isTextMode || unableToParse, "Edit Text", wrappedButton);

            if (EditorGUI.EndChangeCheck() && !unableToParse)
            {
                WriteToIni(!isTextMode);
                GUI.FocusControl("");
                LoadFromIni();
            }

            if (isTextMode || unableToParse)
            {
                OnTextMode();
            }
            else
            {
                OnGUIMode();
            }
        }

        private void OnGUIMode()
        {
            if (iniFile == null)
                return;

            GUILayout.Space(5);

            IniUtil.BeginVertical(() => { RecursiveDrawField(); });

            GUILayout.Space(5);

            IniUtil.BeginHorizontal(() =>
            {
                const float border = 20.0f;
                GUILayout.Space(border / 2);

                if (GUILayout.Button("Add New Property", GUILayout.Width(EditorGUIUtility.currentViewWidth - border)))
                    DrawNewProperty();
            });
        }

        private void OnTextMode()
        {
            rawText = IniUtil.WithoutSelectAll(() => EditorGUILayout.TextArea(rawText));

            GUIStyle helpBoxRichText = new GUIStyle(EditorStyles.helpBox);
            Texture errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;

            helpBoxRichText.richText = true;

            if (unableToParse)
            {
                var content = new GUIContent("Unable to parse text into XML. Make sure there are no mistakes!", errorIcon);
                GUILayout.Label(content, helpBoxRichText);
            }
            else
            {
                WriteToIni(true);
            }
        }

        private void LoadFromIni()
        {
            if (string.IsNullOrWhiteSpace(Path) || !File.Exists(Path))
                return;

            rawText = File.ReadAllText(Path);

            iniFile = new INIParser();
            iniFile.Open(Path);
        }

        private void WriteToIni(bool useRaw = false)
        {
            if (!File.Exists(Path))
                return;

            if (iniFile != null)
            {
                if (!useRaw)
                    rawText = iniFile.iniString;

                if (rawText != iniFile.iniString)
                {
                    File.WriteAllText(Path, rawText);
                    return;
                }
            }

            iniFile.Close();  // when you close it, it writes to the disk
        }

        private string GetUniqueName(string orignalName)
        {
            string uniqueName = orignalName;
            int suffix = 0;

            while (iniFile.IsSectionExists(uniqueName) && suffix < 100)
            {
                ++suffix;
                if (suffix >= 100)
                {
                    Debug.LogError("Stop calling all your fields the same thing! Isn't it confusing?");
                }
                uniqueName = string.Format("{0} {1}", orignalName, suffix.ToString());
            }
            return uniqueName;
        }

        private string GetUniqueName(string section, string orignalName)
        {
            string uniqueName = orignalName;
            int suffix = 0;

            while (iniFile.IsKeyExists(section, uniqueName) && suffix < 100)
            {
                ++suffix;
                if (suffix >= 100)
                {
                    Debug.LogError("Stop calling all your fields the same thing! Isn't it confusing?");
                }
                uniqueName = string.Format("{0}_{1}", orignalName, suffix.ToString());
            }
            return uniqueName;
        }

        [MenuItem("Assets/Create/INI File", priority = 81)]
        public static void CreateNewXmlFile()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
                path = "Assets";
            else if (System.IO.Path.GetExtension(path) != "")
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

            path = System.IO.Path.Combine(path, "New INI File.ini");

            var newIni = new INIParser();
            newIni.Open(path);
            newIni.Close();

            AssetDatabase.Refresh();
        }

        private void DrawNewProperty()
        {
            GenericMenu menu = new GenericMenu();

            IniUtil.AddItem(menu, "Add", () => 
            {
                string section = GetUniqueName("New Section");
                string key = GetUniqueName(section, "NewKey");

                iniFile.WriteValue(section, key, "");
            });

            menu.ShowAsContext();
        }

        private void ApplyModifiedData()
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> section_entry in cache)
            {
                string section = section_entry.Key;
                Dictionary<string, string> pairs = section_entry.Value;

                foreach (KeyValuePair<string, string> pair_entry in pairs)
                {
                    string key = pair_entry.Key;
                    string val = pair_entry.Value;

                    iniFile.WriteValue(section, key, val);
                }
            }
        }

        private void RecursiveDrawField()
        {
            Dictionary<string, Dictionary<string, string>> sections = iniFile.Sections;

            cache.Clear();

            foreach (KeyValuePair<string, Dictionary<string, string>> section_entry in sections)
            {
                string section = section_entry.Key;
                Dictionary<string, string> pairs = section_entry.Value;

                DrawSection(section, pairs);
            }

            ApplyModifiedData();
        }

        private void DrawSection(string name, Dictionary<string, string> pairs)
        {
            bool renaming = (name == sectionToRename && pairToRename == null);

            IniUtil.BeginHorizontal(() =>
            {
                if (renaming)
                {
                    DrawRenameField(name);
                }
                else
                {
                    string txt = "Σ";
                    const float border = 4;
                    if (IniUtil.Button(txt, border))
                        DrawSectionMenu(name);
                }

                GUILayout.Space(13);

                if (!foldouts.ContainsKey(name)) foldouts.Add(name, DEFAULT_FOLD);
                foldouts[name] = IniUtil.Foldout(foldouts[name], name);
            });

            if (!foldouts[name])
                return;

            foreach (KeyValuePair<string, string> pair_entry in pairs)
            {
                string pair_name = pair_entry.Key;
                string pair_value = pair_entry.Value;
                DrawPair(name, pair_name, pair_value);
            }
        }

        private void DrawPair(string section, string name, string val)
        {
            const int level = 1;

            bool renaming = (section == sectionToRename && name == pairToRename);

            IniUtil.BeginHorizontal(() =>
            {
                GUILayout.Space(INDENT_SPACE * level);

                if (renaming)
                {
                    DrawRenameField(section, name);
                }
                else
                {
                    string txt = "Σ";
                    const float border = 4;
                    if (IniUtil.Button(txt, border))
                        DrawPairMenu(section, name);

                    IniUtil.Label(name);
                }

                string newVal = GUILayout.TextField(val);
                if (newVal != val)
                {
                    if (!cache.ContainsKey(section))
                        cache.Add(section, new Dictionary<string, string>());

                    if (!cache[section].ContainsKey(name))
                        cache[section].Add(name, newVal);
                    else
                        cache[section][name] = newVal;
                }
            });
        }

        private void DrawSectionMenu(string section)
        {
            GenericMenu menu = new GenericMenu();

            IniUtil.AddItem(menu, "Rename", () =>
            {
                sectionToRename = section;
                pairToRename = null;

                renameValue = section;
            });

            menu.AddSeparator("");

            IniUtil.AddItem(menu, "Remove", () => { iniFile.SectionDelete(section); });

            menu.ShowAsContext();
        }

        private void DrawPairMenu(string section, string key)
        {
            GenericMenu menu = new GenericMenu();

            IniUtil.AddItem(menu, "Rename", () =>
            {
                pairToRename = key;
                sectionToRename = section;

                renameValue = key;
            });

            menu.AddSeparator("");

            IniUtil.AddItem(menu, "Remove", () => { iniFile.KeyDelete(section, key); });

            menu.ShowAsContext();
        }

        private void DrawRenameField(string section)
        {
            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = new Color(0, 1, 0);

            if (GUILayout.Button("✔", style, GUILayout.Width(24)))
            {
                if (!iniFile.IsSectionExists(renameValue))
                {
                    Dictionary<string, string> pairs = iniFile.Sections[section];

                    foreach (KeyValuePair<string, string> entry in pairs)
                    {
                        string key = entry.Key;
                        string val = entry.Value;

                        iniFile.WriteValue(renameValue, key, val);
                    }

                    iniFile.SectionDelete(section);
                }
                GUI.FocusControl("");

                sectionToRename = null;
                pairToRename = null;
                renameValue = null;
            }

            GUI.SetNextControlName("RENAME_SECTION");
            string txt = renameValue + "        ";
            renameValue = EditorGUILayout.TextField(renameValue, IniUtil.TextWidth(txt));
        }

        private void DrawRenameField(string section, string key)
        {
            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = new Color(0, 1, 0);

            if (GUILayout.Button("✔", style, GUILayout.Width(24)))
            {
                if (!iniFile.IsKeyExists(section, renameValue))
                {
                    string oldVal = iniFile.Sections[section][key];

                    iniFile.WriteValue(section, renameValue, oldVal);
                    iniFile.KeyDelete(section, key);
                }
                GUI.FocusControl("");

                sectionToRename = null;
                pairToRename = null;
                renameValue = null;
            }

            GUI.SetNextControlName("RENAME_PAIR");
            string txt = renameValue + "        ";
            renameValue = EditorGUILayout.TextField(renameValue, IniUtil.TextWidth(txt));
        }
    }
}
#endif
