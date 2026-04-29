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
public class EnemyShadow : MonoBehaviour
{
    [SerializeField] private MapManager     mapManager;
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] private SpriteRenderer enemyBodyRenderer; // 플레이어 바디 스프라이트 동기화용

    private void OnEnable()
    {
        GameEvents.StageCleared += Hide;
    }

    private void OnDisable()
    {
        GameEvents.StageCleared -= Hide;
    }
    
    private void LateUpdate()
    {
        if (!shadowRenderer.enabled) return;
        if (!(GameManager.Instance.currentStageData?.hasSecondMap ?? false))
        {
            shadowRenderer.enabled = false; 
            return;
        }

        UpdateLayerAndZ();
        SyncSprite();
    }

    // X,Y는 부모에서 자동으로 받으므로 Z와 레이어만 갱신
    private void UpdateLayerAndZ()
    {
        int map1Layer = LayerMask.NameToLayer("Map 1");
        int map2Layer = LayerMask.NameToLayer("Map 2");
        
        int enemyLayer = transform.parent.gameObject.layer;
        bool enemyOnMap1 = enemyLayer == map1Layer;
        
        Transform oppositeRoot = enemyOnMap1 ?
            mapManager.GetSecondMapRoot() : mapManager.GetFirstMapRoot();
        
        if (oppositeRoot == null) return;

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, oppositeRoot.position.z);

        gameObject.layer = enemyOnMap1 ? map2Layer : map1Layer;
    }

    private void SyncSprite()
    {
        if (enemyBodyRenderer == null) return;
        shadowRenderer.sprite = enemyBodyRenderer.sprite;
    }

    public void Show()
    {
        shadowRenderer.enabled = GameManager.Instance.currentStageData?.hasSecondMap ?? false;
    }

    public void Hide()
    {
        shadowRenderer.enabled = false;
    }
}
