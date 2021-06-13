#if UNITY_EDITOR && UNITY_STANDALONE_WIN
/**
 * Copyright (c) Pixisoft Corporations. All rights reserved.
 * 
 * Licensed under MIT. See LICENSE.txt in the asset root for license informtaion.
 */
using IniParser;
using IniParser.Model;
using System;
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
        private FileIniDataParser parser;
        private IniData iniFile;

        private string sectionToRename;
        private string pairToRename;
        private string renameValue;

        private bool modifiedDataThisFrame;

        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
        private const bool DEFAULT_FOLD = false;

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
                var content = new GUIContent("Unable to parse text into INI. Make sure there are no mistakes!", errorIcon);
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

            parser = new FileIniDataParser();
            iniFile = parser.ReadFile(Path);
        }

        private void WriteToIni(bool useRaw = false)
        {
            if (!File.Exists(Path))
                return;

            if (iniFile != null)
            {
                if (!useRaw)
                    rawText = iniFile.ToString();
            }

            parser.WriteFile(Path, iniFile);
        }

        private string GetUniqueName(string orignalName)
        {
            string uniqueName = orignalName;
            int suffix = 0;

            while (iniFile.Sections.ContainsSection(uniqueName) && suffix < 100)
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

            while (iniFile[section].ContainsKey(uniqueName) && suffix < 100)
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
        public static void CreateNewIniFile()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
                path = "Assets";
            else if (System.IO.Path.GetExtension(path) != "")
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

            path = System.IO.Path.Combine(path, "New INI File.ini");

            var parser = new FileIniDataParser();
            parser.WriteFile(path, new IniData());

            AssetDatabase.Refresh();
        }

        private void DrawNewProperty()
        {
            GenericMenu menu = new GenericMenu();

            IniUtil.AddItem(menu, "Add", () =>
            {
                string section = GetUniqueName("New Section");
                string key = GetUniqueName(section, "NewKey");

                iniFile[section][key] = "";
            });

            menu.AddSeparator("");

            IniUtil.AddItem(menu, "Clear", () => { iniFile.Sections.Clear(); });

            menu.ShowAsContext();
        }

        private void DrawSectionMenu(string section)
        {
            GenericMenu menu = new GenericMenu();

            IniUtil.AddItem(menu, "Add/Pair", () =>
            {
                string key = GetUniqueName(section, "NewKey");
                iniFile[section][key] = "";
            });

            IniUtil.AddItem(menu, "Duplicate", () =>
            {
                string dupSection = GetUniqueName(section + " (duplicate)");

                iniFile.Sections.AddSection(dupSection);
                KeyData keyData = iniFile[section].GetKeyData(section);
                foreach (KeyData data in iniFile[section])
                {
                    iniFile[dupSection][data.KeyName] = data.Value;
                }
            });

            IniUtil.AddItem(menu, "Rename", () =>
            {
                sectionToRename = section;
                pairToRename = null;

                renameValue = section;
            });

            menu.AddSeparator("");

            IniUtil.AddItem(menu, "Move Up", () =>
            {
                // TODO: ..
            }, false);

            IniUtil.AddItem(menu, "Move Down", () =>
            {
                // TODO: ..
            }, false);

            menu.AddSeparator("");

            IniUtil.AddItem(menu, "Remove", () => { iniFile.Sections.RemoveSection(section); });

            menu.ShowAsContext();
        }

        private void DrawPairMenu(string section, string key)
        {
            GenericMenu menu = new GenericMenu();

            IniUtil.AddItem(menu, "Duplicate", () =>
            {
                string dupKey = GetUniqueName(section + " (duplicate)", key);
                string val = iniFile[section][key];
                iniFile[section][dupKey] = val;
            });

            IniUtil.AddItem(menu, "Rename", () =>
            {
                pairToRename = key;
                sectionToRename = section;

                renameValue = key;
            });

            menu.AddSeparator("");

            IniUtil.AddItem(menu, "Move Up", () =>
            {
                // TODO: ..
            }, false);

            IniUtil.AddItem(menu, "Move Down", () =>
            {
                // TODO: ..
            }, false);

            menu.AddSeparator("");

            IniUtil.AddItem(menu, "Remove", () => { iniFile[section].RemoveKey(key); });

            menu.ShowAsContext();
        }

        private void RecursiveDrawField()
        {
            modifiedDataThisFrame = false;

            foreach (var section in iniFile.Sections)
            {
                DrawSection(section, section.Keys);
                if (modifiedDataThisFrame) break;
            }
        }

        private void DrawSection(SectionData section, KeyDataCollection keys)
        {
            string name = section.SectionName;
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
                string displayName = (renaming) ? "" : name;
                foldouts[name] = IniUtil.Foldout(foldouts[name], displayName);
            });

            if (!foldouts[name])
                return;

            foreach (KeyData keyData in keys)
            {
                DrawPair(section, keyData);
                if (modifiedDataThisFrame) break;
            }
        }

        private void DrawPair(SectionData section, KeyData keyData)
        {
            string sectionName = section.SectionName;
            string keyName = keyData.KeyName;
            string keyVal = keyData.Value;

            const int level = 1;

            bool renaming = (sectionName == sectionToRename && keyName == pairToRename);

            IniUtil.BeginHorizontal(() =>
            {
                GUILayout.Space(INDENT_SPACE * level);

                if (renaming)
                {
                    DrawRenameField(sectionName, keyName);
                }
                else
                {
                    string txt = "Σ";
                    const float border = 4;
                    if (IniUtil.Button(txt, border))
                        DrawPairMenu(sectionName, keyName);

                    IniUtil.Label(keyName);
                }

                keyData.Value = GUILayout.TextField(keyVal);
            });
        }

        private void DrawRenameField(string section)
        {
            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = new Color(0, 1, 0);

            if (GUILayout.Button("✔", style, GUILayout.Width(24)))
            {
                if (!iniFile.Sections.ContainsSection(renameValue))
                {
                    SectionData data = iniFile.Sections.GetSectionData(section);
                    data.SectionName = renameValue;

                    iniFile.Sections.RemoveSection(section);
                    iniFile.Sections.Add(data);

                    if (!foldouts.ContainsKey(renameValue))
                    {
                        foldouts.Add(renameValue, foldouts[section]);
                    }

                    modifiedDataThisFrame = true;
                }
                else
                {
                    if (renameValue != section)
                        Debug.LogWarning("Section key exists, please try another key: " + section);
                }
                
                GUI.FocusControl("");

                sectionToRename = null;
                pairToRename = null;
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
                if (!iniFile[section].ContainsKey(renameValue))
                {
                    iniFile[section].GetKeyData(key).KeyName = renameValue;

                    modifiedDataThisFrame = true;
                }
                else
                {
                    if (renameValue != key)
                        Debug.LogWarning("Pair key exists, please try another key: " + key);
                }
                
                GUI.FocusControl("");

                sectionToRename = null;
                pairToRename = null;
            }

            GUI.SetNextControlName("RENAME_PAIR");
            string txt = renameValue + "        ";
            renameValue = EditorGUILayout.TextField(renameValue, IniUtil.TextWidth(txt));
        }
    }
}
#endif
