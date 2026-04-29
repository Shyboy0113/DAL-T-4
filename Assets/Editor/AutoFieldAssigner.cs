#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class AutoFieldAssigner : EditorWindow
{
    private string prefabFolder = "";
    private string componentName = "";

    [Serializable]
    private struct FieldMapping
    {
        public string fieldName;     // 스크립트의 변수명
        public string childName;     // Find할 자식 오브젝트 이름
    }

    private List<FieldMapping> mappings = new();

    private Vector2 scrollPos;

    [MenuItem("Tools/Auto Field Assigner")]
    private static void Open() => GetWindow<AutoFieldAssigner>("Auto Field Assigner");

    private void OnGUI()
    {
        GUILayout.Label("대상 설정", EditorStyles.boldLabel);
        prefabFolder  = EditorGUILayout.TextField("프리팹 폴더", prefabFolder);
        componentName = EditorGUILayout.TextField("컴포넌트 이름", componentName);

        GUILayout.Space(10);
        GUILayout.Label("필드 매핑 (변수명 → 자식 오브젝트 이름)", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < mappings.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            var m = mappings[i];
            m.fieldName = EditorGUILayout.TextField(m.fieldName);
            EditorGUILayout.LabelField("→", GUILayout.Width(20));
            m.childName = EditorGUILayout.TextField(m.childName);
            mappings[i] = m;

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                mappings.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("+ 매핑 추가"))
            mappings.Add(new FieldMapping());

        GUILayout.Space(15);
        if (GUILayout.Button("일괄 할당", GUILayout.Height(30)))
            AssignAll();
    }

    private void AssignAll()
    {
        // 컴포넌트 타입 찾기
        Type compType = null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            compType = asm.GetType(componentName);
            if (compType != null) break;
        }

        if (compType == null)
        {
            Debug.LogError($"컴포넌트 '{componentName}'를 찾을 수 없습니다.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
        int count = 0;

        foreach (string guid in guids)
        {
            string     path   = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            var        comp   = prefab.GetComponent(compType);

            if (comp == null) { PrefabUtility.UnloadPrefabContents(prefab); continue; }

            bool changed = false;

            foreach (var m in mappings)
            {
                if (string.IsNullOrEmpty(m.fieldName) || string.IsNullOrEmpty(m.childName))
                    continue;

                FieldInfo field = compType.GetField(m.fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    Debug.LogWarning($"{prefab.name}: 필드 '{m.fieldName}' 없음");
                    continue;
                }

                Transform child = FindDeep(prefab.transform, m.childName);
                if (child == null)
                {
                    Debug.LogWarning($"{prefab.name}: 자식 '{m.childName}' 없음");
                    continue;
                }

                // Transform 필드인지 GameObject 필드인지 자동 판별
                if (field.FieldType == typeof(Transform))
                    field.SetValue(comp, child);
                else if (field.FieldType == typeof(GameObject))
                    field.SetValue(comp, child.gameObject);

                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                count++;
            }

            PrefabUtility.UnloadPrefabContents(prefab);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"완료! {count}개 프리팹 할당됨");
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (child.name == name) return child;
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif