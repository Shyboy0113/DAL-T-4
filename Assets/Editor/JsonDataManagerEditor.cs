using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(JsonDataManager))]
public class JsonDataManagerEditor : Editor
{
    private int stagesPerChapter = 15;

    // 모든 스테이지 해금
    private int minChapter = 1;
    private int maxChapter = 4;

    // 지정 스테이지 해금
    private int unlockStartChapter = 1, unlockStartStage = 1;
    private int unlockEndChapter   = 4, unlockEndStage   = 15;

    // 지정 스테이지 잠금
    private int lockStartChapter = 1, lockStartStage = 1;
    private int lockEndChapter   = 4, lockEndStage   = 15;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── 에디터 테스트 도구 ──", EditorStyles.boldLabel);
        stagesPerChapter = EditorGUILayout.IntField("챕터당 스테이지 수", stagesPerChapter);

        // ── 1. 모든 스테이지 잠금 ──────────────────────────────
        Section("1. 모든 스테이지 잠금", () =>
        {
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("모든 스테이지 잠금") &&
                Confirm("모든 스테이지 잠금", "모든 진행 데이터가 삭제됩니다. 되돌릴 수 없습니다."))
            {
                Jdm.ResetAllData();
                RefreshNodes();
            }
            GUI.backgroundColor = Color.white;
        });

        // ── 2. 모든 스테이지 해금 ──────────────────────────────
        Section("2. 모든 스테이지 해금", () =>
        {
            minChapter = EditorGUILayout.IntField("최소 챕터", minChapter);
            maxChapter = EditorGUILayout.IntField("최대 챕터", maxChapter);
            if (GUILayout.Button("모든 스테이지 해금") &&
                Confirm("모든 스테이지 해금", $"{minChapter}-1 ~ {maxChapter}-{stagesPerChapter} 전부 해금합니다."))
            {
                Jdm.UnlockStageRange(minChapter, maxChapter, stagesPerChapter);
                RefreshNodes();
            }
        });

        // ── 3. 지정 스테이지 해금 ──────────────────────────────
        Section("3. 지정 스테이지 해금", () =>
        {
            RangeField("시작  ", ref unlockStartChapter, ref unlockStartStage);
            RangeField("마무리", ref unlockEndChapter,   ref unlockEndStage);
            if (GUILayout.Button("지정 스테이지 해금") &&
                Confirm("지정 스테이지 해금", $"{unlockStartChapter}-{unlockStartStage} ~ {unlockEndChapter}-{unlockEndStage} 해금합니다."))
            {
                Jdm.UnlockSpecificRange(unlockStartChapter, unlockStartStage, unlockEndChapter, unlockEndStage, stagesPerChapter);
                RefreshNodes();
            }
        });

        // ── 4. 지정 스테이지 잠금 ──────────────────────────────
        Section("4. 지정 스테이지 잠금", () =>
        {
            RangeField("시작  ", ref lockStartChapter, ref lockStartStage);
            RangeField("마무리", ref lockEndChapter,   ref lockEndStage);
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("지정 스테이지 잠금") &&
                Confirm("지정 스테이지 잠금", $"{lockStartChapter}-{lockStartStage} ~ {lockEndChapter}-{lockEndStage} 잠금합니다.\n이전 스테이지가 클리어됐으면 자동 해금됩니다."))
            {
                Jdm.LockSpecificRange(lockStartChapter, lockStartStage, lockEndChapter, lockEndStage, stagesPerChapter);
                RefreshNodes();
            }
            GUI.backgroundColor = Color.white;
        });
    }

    // ── 헬퍼 ────────────────────────────────────────────────────

    private JsonDataManager Jdm => (JsonDataManager)target;

    private static void Section(string title, System.Action draw)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        draw();
        EditorGUI.indentLevel--;
    }

    private static void RangeField(string label, ref int chapter, ref int stage)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(44));
        chapter = EditorGUILayout.IntField(chapter, GUILayout.Width(36));
        EditorGUILayout.LabelField("챕터", GUILayout.Width(28));
        stage = EditorGUILayout.IntField(stage, GUILayout.Width(36));
        EditorGUILayout.LabelField("스테이지");
        EditorGUILayout.EndHorizontal();
    }

    private static bool Confirm(string title, string msg) =>
        EditorUtility.DisplayDialog(title, msg, "확인", "취소");

    private static void RefreshNodes()
    {
        var nodes = Object.FindObjectsByType<StageNode>(FindObjectsSortMode.None);
        foreach (var node in nodes)
            node.RefreshVisuals();
    }
}
