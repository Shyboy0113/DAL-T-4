#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyHiderWindow : EditorWindow
{
    private static List<string> hideNames = new();
    private static bool hideInHierarchy = false;
    private static bool hideInScene     = false;
    private Vector2 scrollPos;

    static HierarchyHiderWindow()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    [MenuItem("Tools/Hierarchy Hider")]
    private static void Open() => GetWindow<HierarchyHiderWindow>("Hierarchy Hider");

    private void OnGUI()
    {
        GUILayout.Label("숨길 오브젝트 이름", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < hideNames.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            hideNames[i] = EditorGUILayout.TextField(hideNames[i]);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                hideNames.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ 추가"))
            hideNames.Add("");

        GUILayout.Space(15);
        GUILayout.Label("숨기기 옵션", EditorStyles.boldLabel);

        // ── 하이어라키 토글 ──
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("하이어라키에서 숨기기", GUILayout.Width(160));
        bool newHideHierarchy = GUILayout.Toggle(hideInHierarchy, hideInHierarchy ? "ON" : "OFF");
        EditorGUILayout.EndHorizontal();

        if (newHideHierarchy != hideInHierarchy)
        {
            hideInHierarchy = newHideHierarchy;
            if (hideInHierarchy) ApplyHideInHierarchy();
            else                 ShowInHierarchy();
        }

        // ── 씬 뷰 토글 ──
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("씬 뷰에서 숨기기", GUILayout.Width(160));
        bool newHideScene = GUILayout.Toggle(hideInScene, hideInScene ? "ON" : "OFF");
        EditorGUILayout.EndHorizontal();

        if (newHideScene != hideInScene)
        {
            hideInScene = newHideScene;
            if (hideInScene) ApplyHideInScene();
            else             ShowInScene();
        }

        GUILayout.Space(10);
        if (GUILayout.Button("전체 해제", GUILayout.Height(25)))
        {
            hideInHierarchy = false;
            hideInScene     = false;
            ShowInHierarchy();
            ShowInScene();
        }
    }

    // ── 하이어라키 변경 콜백 ──
    private static void OnHierarchyChanged()
    {
        if (hideInHierarchy) ApplyHideInHierarchy();
        if (hideInScene)     ApplyHideInScene();
    }

    // ── 하이어라키 숨기기 ──
    private static void ApplyHideInHierarchy()
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!obj.scene.isLoaded) continue;
            if (hideNames.Contains(obj.name))
                obj.hideFlags = HideFlags.HideInHierarchy;
        }
        EditorApplication.RepaintHierarchyWindow();
    }

    private static void ShowInHierarchy()
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!obj.scene.isLoaded) continue;
            if (hideNames.Contains(obj.name))
                obj.hideFlags = HideFlags.None;
        }
        EditorApplication.RepaintHierarchyWindow();
    }

    // ── 씬 뷰 숨기기 ──
    private static void ApplyHideInScene()
    {
        var svm = SceneVisibilityManager.instance;
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!obj.scene.isLoaded) continue;
            if (hideNames.Contains(obj.name))
                svm.Hide(obj, true);
        }
    }

    private static void ShowInScene()
    {
        var svm = SceneVisibilityManager.instance;
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!obj.scene.isLoaded) continue;
            if (hideNames.Contains(obj.name))
                svm.Show(obj, true);
        }
    }
}
#endif