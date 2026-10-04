#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class Editor_ColorPicker
{
    public static void Open(Action<Color> onColorChanged, Color initialColor, bool showAlpha = true)
    {
        Assembly editorAssembly = typeof(EditorWindow).Assembly;
        Type colorPickerType = editorAssembly.GetType("UnityEditor.ColorPicker");

        if (colorPickerType == null) return;

        // 1. 현재 active/current GUIView 가져오기 (Null 방지용)
        Type guiViewType = editorAssembly.GetType("UnityEditor.GUIView");
        PropertyInfo currentViewProp = guiViewType?.GetProperty("current", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        object currentView = currentViewProp?.GetValue(null);

        MethodInfo[] methods = colorPickerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var method in methods)
        {
            if (method.Name != "Show") continue;

            ParameterInfo[] parameters = method.GetParameters();

            // Unity 6 Show(...) 파라미터 구성 자동 매칭 및 호출
            for (int i = 0; i < parameters.Length; i++)
            {
                Type pType = parameters[i].ParameterType;

                // Action<Color> 또는 내부 Delegate를 받아주는 Show 메서드 찾기
                if (pType == typeof(Action<Color>) || typeof(Delegate).IsAssignableFrom(pType))
                {
                    object[] args = new object[parameters.Length];

                    for (int j = 0; j < parameters.Length; j++)
                    {
                        Type targetType = parameters[j].ParameterType;

                        if (targetType == guiViewType)
                        {
                            args[j] = currentView;
                        }
                        else if (targetType == typeof(Action<Color>))
                        {
                            args[j] = onColorChanged;
                        }
                        else if (typeof(Delegate).IsAssignableFrom(targetType))
                        {
                            args[j] = Delegate.CreateDelegate(targetType, onColorChanged.Target, onColorChanged.Method);
                        }
                        else if (targetType == typeof(Color))
                        {
                            args[j] = initialColor;
                        }
                        else if (targetType == typeof(bool))
                        {
                            // 첫 번째 bool은 showAlpha, 두 번째 bool은 hdr(false)
                            args[j] = (j == i + 1) ? showAlpha : false;
                        }
                        else
                        {
                            args[j] = parameters[j].HasDefaultValue ? parameters[j].DefaultValue : null;
                        }
                    }

                    try
                    {
                        method.Invoke(null, args);
                        return;
                    }
                    catch
                    {
                        // 호출 실패 시 다음 Show 오버로드 시도
                        continue;
                    }
                }
            }
        }
    }
}
#endif