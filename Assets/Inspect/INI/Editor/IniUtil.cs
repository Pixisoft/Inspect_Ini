#if UNITY_EDITOR && UNITY_STANDALONE_WIN
/**
 * Copyright (c) Pixisoft Corporations. All rights reserved.
 * 
 * Licensed under the Source EULA. See COPYING in the asset root for license informtaion.
 */
using System;
using UnityEngine;

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
            return true;
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
    }
}
#endif
