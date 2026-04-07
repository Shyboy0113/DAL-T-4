using UnityEngine;

/// <summary>
/// StageSelect 씬의 세이브 데이터 초기화 패널.
/// Reset Button → Open() → YES 클릭 → OnYesClicked() / NO 클릭 → OnNoClicked()
/// </summary>
public class ResetPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    /// <summary>Reset Button의 OnClick에 연결합니다.</summary>
    public void OpenResetPanel()
    {
        panel.SetActive(true);
    }

    /// <summary>YES Button의 OnClick에 연결합니다.</summary>
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

        // 모든 StageNode 시각 갱신
        var nodes = FindObjectsByType<StageNode>(FindObjectsSortMode.None);
        foreach (var node in nodes)
            node.RefreshVisuals();

        panel.SetActive(false);
    }

    /// <summary>NO Button의 OnClick에 연결합니다.</summary>
    public void OnNoClicked()
    {
        panel.SetActive(false);
    }
}