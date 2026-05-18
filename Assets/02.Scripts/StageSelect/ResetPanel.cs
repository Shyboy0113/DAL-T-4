using UnityEngine;

/// <summary>
/// StageSelect 씬의 세이브 데이터 초기화 패널.
/// Reset Button → Open() → YES 클릭 → OnYesClicked() / NO 클릭 → OnNoClicked()
/// </summary>
using UnityEngine;

public class ResetPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    public void OpenResetPanel()
    {
        panel.SetActive(true);
    }

    public void OnYesClicked()
    {
        var jdm = GameManager.Instance?.jsonDataManager;
        if (jdm == null)
        {
            Debug.LogWarning("ResetPanel: JsonDataManager를 찾을 수 없습니다.");
            panel.SetActive(false);
            return;
        }

        jdm.ResetAllData();

        // 노드 비주얼 갱신
        var nodes = FindObjectsByType<StageNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var node in nodes)
            node.RefreshVisuals();

        // 연결선 색상 갱신 (노드 상태가 바뀌었으므로 IsUnlocked 재계산)
        var paths = FindObjectsByType<StagePathRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var path in paths)
            path.Refresh();

        panel.SetActive(false);
    }

    public void OnNoClicked()
    {
        panel.SetActive(false);
    }
}