using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SecondMapScreenPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text   mapLabel;
    [SerializeField] private GameObject noMapOverlay;
    [SerializeField] private MapManager mapManager;

    [Header("Raw Images")]
    [SerializeField] private RawImage screen;     // 메인 게임 뷰 (전체화면)
    [SerializeField] private RawImage firstMap;   // 패널 첫 번째 슬롯
    [SerializeField] private RawImage secondMap;  // 패널 두 번째 슬롯

    [Header("Render Textures")]
    [SerializeField] private RenderTexture firstTexture;
    [SerializeField] private RenderTexture secondTexture;
    
    private void OnEnable()
    {
        GameEvents.MapActivated += OnMapActivated;
    }

    private void OnDisable()
    {
        GameEvents.MapActivated -= OnMapActivated;
    }

    // MapManager.ActivateFirst/Second 및 Init에서 발행 — Undo 복원 포함
    private void OnMapActivated(bool isFirst)
    {
        screen.texture    = isFirst ? firstTexture  : secondTexture;
        firstMap.texture  = isFirst ? secondTexture : firstTexture;
        secondMap.texture = isFirst ? firstTexture  : secondTexture;
        UpdatePanel();
    }

    public void UpdatePanel()
    {
        bool hasSecondMap = GameManager.Instance.currentStageData?.hasSecondMap ?? false;
        Debug.Log($"UpdatePanel called — hasSecondMap: {hasSecondMap}");

        noMapOverlay.SetActive(!hasSecondMap);
        secondMap.enabled = hasSecondMap;

        if (!hasSecondMap)
        {
            mapLabel.text = "";
            return;
        }

        // 이 패널은 플레이어가 없는 반대 맵을 비춤
        mapLabel.text = mapManager.IsFirstRoot() ? "Map 2" : "Map 1";
    }
}