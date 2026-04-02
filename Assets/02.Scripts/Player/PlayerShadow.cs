using UnityEngine;

/// <summary>
/// 플레이어의 자식 오브젝트로 배치되어 플레이어와 함께 이동합니다.
/// X,Y는 부모(플레이어)를 자동으로 따라가고, Z와 레이어만 반대 맵으로 오버라이드합니다.
/// 반대 맵의 서브 카메라 RenderTexture에 플레이어 위치를 그림자로 표시합니다.
///
/// - 플레이어가 Map 1에 있을 때 → Map 2 레이어 → Map 2 카메라에 렌더링
/// - 플레이어가 Map 2에 있을 때 → Map 1 레이어 → Map 1 카메라에 렌더링
///
/// [씬 설정]
/// - 이 GameObject를 PlayerBehaviour의 자식으로 배치
/// - SpriteRenderer color/alpha를 인스펙터에서 조정하여 그림자 느낌 연출
/// </summary>
public class PlayerShadow : MonoBehaviour
{
    [SerializeField] private MapManager     mapManager;
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] private SpriteRenderer playerBodyRenderer; // 플레이어 바디 스프라이트 동기화용

    private void OnEnable()
    {
        GameEvents.PlayerDied   += Hide;
        GameEvents.StageCleared += Hide;
    }

    private void OnDisable()
    {
        GameEvents.PlayerDied   -= Hide;
        GameEvents.StageCleared -= Hide;
    }

    private void LateUpdate()
    {
        if (!shadowRenderer.enabled) return;
        if (!(GameManager.Instance.currentStageData?.hasSecondMap ?? false)) { shadowRenderer.enabled = false; return; }

        UpdateLayerAndZ();
        SyncSprite();
    }

    // X,Y는 부모에서 자동으로 받으므로 Z와 레이어만 갱신
    private void UpdateLayerAndZ()
    {
        Transform otherRoot = mapManager.GetInactiveMapRoot();
        if (otherRoot == null) return;

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, otherRoot.position.z);

        int map1Layer = LayerMask.NameToLayer("Map 1");
        int map2Layer = LayerMask.NameToLayer("Map 2");
        gameObject.layer = mapManager.IsFirstRoot() ? map2Layer : map1Layer;
    }

    private void SyncSprite()
    {
        if (playerBodyRenderer == null) return;
        shadowRenderer.sprite = playerBodyRenderer.sprite;
    }

    public void Show()
    {
        shadowRenderer.enabled = GameManager.Instance.currentStageData?.hasSecondMap ?? false;
    }

    private void Hide()
    {
        shadowRenderer.enabled = false;
    }
}
