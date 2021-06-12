#if UNITY_EDITOR && UNITY_STANDALONE_WIN
/**
 * Copyright (c) Pixisoft Corporations. All rights reserved.
 * 
 * Licensed under MIT. See LICENSE.txt in the asset root for license informtaion.
 */
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

            //IniUtil.BeginVertical(() => { RecursiveDrawField(xmlDoc.Root); });

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
            iniFile.OpenFromString(rawText);
        }

        private void WriteToIni(bool useRaw = false)
        {
            if (!File.Exists(Path))
                return;

            if (iniFile != null)
            {
                if (!useRaw)
                    rawText = iniFile.iniString;
            }

            File.WriteAllText(Path, rawText);
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
                uniqueName = string.Format("{0}_{1}", orignalName, suffix.ToString());
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
            menu.AddSeparator("");

            menu.ShowAsContext();
        }
    }
}
#endif
