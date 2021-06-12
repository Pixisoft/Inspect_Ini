#if UNITY_EDITOR && UNITY_STANDALONE_WIN
/**
 * Copyright (c) Pixisoft Corporations. All rights reserved.
 * 
  * Licensed under MIT. See LICENSE.txt in the asset root for license informtaion.
 */
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
                var parser = new INIParser();
                parser.OpenFromString(ini);
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
    }
}
#endif
