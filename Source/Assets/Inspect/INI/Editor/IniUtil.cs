#if UNITY_EDITOR
/**
 * Copyright (c) Pixisoft Corporations. All rights reserved.
 * 
 * Licensed under MIT. See LICENSE.txt in the asset root for license informtaion.
 */
using IniParser.Model;
using IniParser.Parser;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.GenericMenu;

namespace Inspect.Ini
{
    public delegate void EmptyFunction();

    public static class IniUtil
    {
        /* Variables */

        private const string NAME = "Inspect_Ini";

        /* Setter & Getter */

        /* Functions */

        public static bool IsValidIni(string ini)
        {
            try
            {
                var parser = new IniDataParser();
                parser.Parse(ini);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
                return false;
            }
        }

        public static void BeginHorizontal(EmptyFunction func, bool flexibleSpace = false)
        {
            GUILayout.BeginHorizontal();
            if (flexibleSpace) GUILayout.FlexibleSpace();
            func.Invoke();
            GUILayout.EndHorizontal();
        }

        public static void BeginVertical(EmptyFunction func, string style = "box")
        {
            if (style == "")
                GUILayout.BeginVertical();
            else
                GUILayout.BeginVertical("box");
            func.Invoke();
            GUILayout.EndVertical();
        }

        public static Vector2 CalcSize(string text)
        {
            return GUI.skin.label.CalcSize(new GUIContent(text));
        }

        public static T WithoutSelectAll<T>(Func<T> guiCall)
        {
            bool preventSelection = (Event.current.type == EventType.MouseDown);

            Color oldCursorColor = GUI.skin.settings.cursorColor;

            if (preventSelection)
                GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);

            T value = guiCall();

            if (preventSelection)
                GUI.skin.settings.cursorColor = oldCursorColor;

            return value;
        }

        public static void Label(string label, float border = 0.0f)
        {
            float width = CalcSize(label).x + border;
            GUILayout.Label(label, GUILayout.Width(width));
        }

        public static bool Button(string label, float border = 0.0f)
        {
            float width = CalcSize(label).x + border;
            return GUILayout.Button(label, GUILayout.Width(width));
        }

        public static GUILayoutOption TextWidth(string text)
        {
            float width = CalcSize(text).x;
            return GUILayout.Width(width);
        }

        public static void AddItem(GenericMenu menu, string name, MenuFunction fnc, bool cond = true)
        {
            var content = new GUIContent(name);
            if (cond)
                menu.AddItem(content, false, fnc);
            else
                menu.AddDisabledItem(content, false);
        }

        public static bool Foldout(bool foldout, string content, float border = 0.0f)
        {
            GUIStyle style = new GUIStyle(EditorStyles.foldout);
            style.fixedWidth = IniUtil.CalcSize(content).x + border;
            return EditorGUILayout.Foldout(foldout, content, style);
        }

        public static string GetUniqueName(IniData iniFile, string orignalName)
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

        public static string GetUniqueName(IniData iniFile, string section, string orignalName)
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

        private static SectionData SectionDataFrom(IniData data, string section, bool up)
        {
            SectionData node = null;
            if (up)
            {
                foreach (SectionData sd in data.Sections)
                {
                    if (sd.SectionName == section)
                        return node;
                    node = sd;
                }
            }
            else
            {
                foreach (SectionData sd in data.Sections)
                {
                    if (node != null)
                        return sd;

                    if (sd.SectionName == section)
                        node = sd;
                }
            }
            return null;
        }
        private static KeyData PairDataFrom(IniData data, string section, string key, bool up)
        {
            var sectionData = data[section];
            KeyData node = null;
            if (up)
            {
                foreach (KeyData kd in sectionData)
                {
                    if (kd.KeyName == key)
                        return node;
                    node = kd;
                }
            }
            else
            {
                foreach (KeyData kd in sectionData)
                {
                    if (node != null)
                        return kd;

                    if (kd.KeyName == key)
                        node = kd;
                }
            }
            return null;
        }

        private static List<SectionData> SectionDataBeforeSelf(IniData data, SectionData section)
        {
            var lst = new List<SectionData>();

            foreach (SectionData sd in data.Sections)
            {
                if (sd == section)
                    break;

                lst.Add(sd);
            }

            return lst;
        }
        private static List<SectionData> SectionDataAfterSelf(IniData data, SectionData section)
        {
            var lst = new List<SectionData>();
            bool found = false;

            foreach (SectionData kd in data.Sections)
            {
                if (found)
                {
                    lst.Add(kd);
                }
                else
                {
                    found = (kd == section);
                }
            }

            return lst;
        }

        private static List<KeyData> KeyDataBeforeSelf(IniData data, string section, KeyData keyData)
        {
            var lst = new List<KeyData>();
            var sectionData = data[section];

            foreach (KeyData kd in sectionData)
            {
                if (kd == keyData)
                    break;

                lst.Add(kd);
            }

            return lst;
        }
        private static List<KeyData> KeyDataAfterSelf(IniData data, string section, KeyData keyData)
        {
            var lst = new List<KeyData>();
            var sectionData = data[section];
            bool found = false;

            foreach (KeyData kd in sectionData)
            {
                if (found)
                {
                    lst.Add(kd);
                }
                else
                {
                    found = (kd == keyData);
                }
            }

            return lst;
        }

        private static void SectionDataAddBeforeSelf(IniData data, SectionData current, SectionData newData)
        {
            SectionDataAddBeforeOrAfterSelf(data, current, newData, true);
        }
        private static void SectionDataAddAfterSelf(IniData data, SectionData current, SectionData newData)
        {
            SectionDataAddBeforeOrAfterSelf(data, current, newData, false);
        }

        private static void KeyDataAddBeforeSelf(IniData data, string section, KeyData keyData, KeyData newData)
        {
            KeyDataAddBeforeOrAfterSelf(data, section, keyData, newData, true);
        }
        private static void KeyDataAddAfterSelf(IniData data, string section, KeyData keyData, KeyData newData)
        {
            KeyDataAddBeforeOrAfterSelf(data, section, keyData, newData, false);
        }

        private static void SectionDataAddBeforeOrAfterSelf(IniData data, SectionData current, SectionData newData, bool before)
        {
            var beforeSelf = SectionDataBeforeSelf(data, current);
            var afterSelf = SectionDataAfterSelf(data, current);

            data.Sections.Clear();

            foreach (var sd in beforeSelf)
                data.Sections.Add(sd);

            if (before)
            {
                data.Sections.Add(newData);
                data.Sections.Add(current);
            }
            else
            {
                data.Sections.Add(current);
                data.Sections.Add(newData);
            }

            foreach (var sd in afterSelf)
                data.Sections.Add(sd);
        }
        private static void KeyDataAddBeforeOrAfterSelf(IniData data, string section, KeyData keyData, KeyData newData, bool before)
        {
            var sectionData = data[section];

            var beforeSelf = KeyDataBeforeSelf(data, section, keyData);
            var afterSelf = KeyDataAfterSelf(data, section, keyData);

            sectionData.RemoveAllKeys();

            foreach (var kd in beforeSelf)
                sectionData.AddKey(kd);

            if (before)
            {
                sectionData.AddKey(newData);
                sectionData.AddKey(keyData);
            }
            else
            {
                sectionData.AddKey(keyData);
                sectionData.AddKey(newData);
            }

            foreach (var kd in afterSelf)
                sectionData.AddKey(kd);
        }

        public static void SectionMove(IniData data, string section, bool up)
        {
            SectionData target = SectionDataFrom(data, section, up);
            SectionData current = data.Sections.GetSectionData(section);

            data.Sections.RemoveSection(target.SectionName);

            if (up)
            {
                SectionDataAddAfterSelf(data, current, target);
            }
            else
            {
                SectionDataAddBeforeSelf(data, current, target);
            }
        }

        public static void PairMove(IniData data, string section, string key, bool up)
        {
            KeyData target = PairDataFrom(data, section, key, up);
            KeyData current = data[section].GetKeyData(key);

            data[section].RemoveKey(target.KeyName);

            if (up)
            {
                KeyDataAddAfterSelf(data, section, current, target);
            }
            else
            {
                KeyDataAddBeforeSelf(data, section, current, target);
            }
        }

        public static bool CanSectionMove(IniData data, string section, bool up)
        {
            return SectionDataFrom(data, section, up) != null;
        }

        public static bool CanPairMove(IniData data, string section, string key, bool up)
        {
            return PairDataFrom(data, section, key, up) != null;
        }
    }
}
#endif
