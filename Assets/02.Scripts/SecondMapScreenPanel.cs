using TMPro;
using UnityEngine;

/// <summary>
/// Second Map Screen Panel의 현재 맵 표시 텍스트와 맵 없음 오버레이를 관리합니다.
///
/// [씬 설정]
/// - mapLabel     : 패널 중앙 상단 TMP 텍스트 ("Map 1" / "Map 2")
/// - noMapOverlay : Map 2가 없을 때 표시할 X자 이미지 GameObject
/// - StageLoader에서 스테이지 로드 후 Refresh() 호출
/// </summary>
public class SecondMapScreenPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text  mapLabel;
    [SerializeField] private GameObject noMapOverlay; // X자 이미지
    [SerializeField] private MapManager mapManager;

    private void Start()
    {
        UpdatePanel();
    }

    private void OnEnable()
    {
        GameEvents.TileMapChanged += OnMapChanged;
    }

    private void OnDisable()
    {
        GameEvents.TileMapChanged -= OnMapChanged;
    }

    // 스테이지 로드 후 StageLoader에서 호출
    public void Refresh()
    {
        UpdatePanel();
    }

    private void OnMapChanged()
    {
        UpdatePanel();
    }

    private void UpdatePanel()
    {
        bool hasSecondMap = GameManager.Instance.currentStageData?.hasSecondMap ?? false;

        noMapOverlay.SetActive(!hasSecondMap);

        if (!hasSecondMap)
        {
            mapLabel.text = "";
            return;
        }

        // 이 패널은 플레이어가 없는 반대 맵을 비춤
        mapLabel.text = mapManager.IsFirstRoot() ? "Map 2" : "Map 1";
    }
}
