using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorWindow_Color : EditorWindow
{
    const string c_key = "EditorWindow_Color";
    ColorPalette m_palette;

    GUIStyle m_invalidBg;
    GUIStyle m_errorMsgStyle;

    string m_search = "";
    Vector2 m_scroll;
    bool m_isValid = false;

    private void OnEnable()
    {
        LoadData();
    }

    private void OnDisable() => EditorPrefs.SetInt(c_key + "_position", 1);

    void LoadData()
    {
        m_palette = ScriptableObject.CreateInstance<ColorPalette>();
        m_palette.SetData(Resources.Load<ColorPalette>("Settings/ColorPalette"));
        Repaint();

        if (EditorPrefs.HasKey(c_key + "_position") == false)
        {
            Rect postion = position;
            postion.width = 400;
            postion.height = 600;
            position = postion;
        }
    }

    private void OnGUI()
    {
        if (m_palette == null)
            return;

        if (m_errorMsgStyle == null)
        {
            m_errorMsgStyle = new GUIStyle(EditorStyles.boldLabel);
            m_errorMsgStyle.normal.textColor = Color.red;
            m_invalidBg = new GUIStyle("RL Element") { normal = { background = Texture2D.redTexture } };
        }

        DrawToolbar();

        using (var scrollView = new EditorGUILayout.ScrollViewScope(m_scroll))
        {
            m_scroll = scrollView.scrollPosition;
            DrawList();
        }

        DrawSave();
    }

    void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("검색");
            m_search = GUILayout.TextField(m_search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.MinWidth(200));
            if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20)))
                m_search = "";
        }
    }

    void DrawList()
    {
        if (m_palette.element == null)
            return;

        // 헤더
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Key", EditorStyles.miniBoldLabel, GUILayout.Width(200));
            GUILayout.Label("Color", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
            GUILayout.Label("", GUILayout.Width(40));
        }
        EditorGUILayout.Space(4);

        for (int i = 0; i < m_palette.element.Count; i++)
        {
            var e = m_palette.element[i];

            if (m_search.IsActive())
            {
                if ((e.key ?? "").IndexOf(m_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
            }

            using (new EditorGUILayout.HorizontalScope(m_invalidBg, GUILayout.Height(20)))
            {
                EditorGUI.BeginChangeCheck();
                e.key = EditorGUILayout.TextField(e.key, GUILayout.Width(200));
                e.color = EditorGUILayout.ColorField(e.color);
                if (EditorGUI.EndChangeCheck())
                    Undo.RecordObject(m_palette, "Edit Color Entry");

                if (GUILayout.Button("−", GUILayout.Width(24)))
                {
                    Undo.RecordObject(m_palette, "Remove Color Entry");
                    m_palette.RemoveAt(i);
                    break;
                }

                if (m_palette.element[i].key != e.key ||
                    m_palette.element[i].color != e.color)
                {
                    m_palette.UpdateData(i, e);
                }
            }
        }
    }
    void DrawSave()
    {
        EditorGUILayout.Space();

        m_isValid = true;
        for (int i = 0; i < m_palette.element.Count; i++)
        {
            var key = m_palette.element[i].key;

            if (key.IsActive() == false ||
                key.Trim().IsActive() == false ||
                m_palette.element.Count(x => key.Equals(x.key)) > 1)
            {
                EditorGUILayout.LabelField("빈 키 또는 중복 키가 있습니다. 수정해야 저장할 수 있습니다.", m_errorMsgStyle);

                m_isValid = false;
                break;
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("ADD", GUILayout.Width(100), GUILayout.Height(25)))
            {
                Undo.RecordObject(m_palette, "Add Color Row");
                m_palette.Add(new ColorPalette.ElementData { key = "", color = Color.white });
            }

            if (GUILayout.Button("SORT", GUILayout.Width(100), GUILayout.Height(25)))
            {
                m_palette.Sort();
                EditorUtility.SetDirty(m_palette);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("SAVE", GUILayout.Width(100), GUILayout.Height(25)))
            {
                if (m_isValid)
                {
                    m_palette.RebuildCache();
                    EditorUtility.SetDirty(m_palette);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }
        }
    }
}
