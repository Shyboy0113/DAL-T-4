using UnityEngine;
using DG.Tweening;

public class MapManager : MonoBehaviour
{
    
    private Camera _mainCamera;

    private bool _isFirst = true;
    private bool _isRotating = false;
    

    [Header ("Rotation Logic")]    
    [SerializeField] private Transform mapPivot;
    [SerializeField] private float rotateDuration = 0.6f;
    
    [SerializeField] private GameObject mapFirstRoot;
    [SerializeField] private GameObject mapSecondRoot;

    private GameObject _currentRoot;
    
    private Transform _activatedRoot;
    private Transform _deactivatedRoot;

    public bool IsFirstRoot()
    {
        if (_currentRoot == mapFirstRoot) return true;
        
        return false;
    }
    
    private void Awake()
    {
        _mainCamera = Camera.main;
        Initialize();
    }

    public void ResetData()
    {
        mapPivot = GameObject.Find("MapPivot").transform;
        mapFirstRoot = GameObject.Find("Tilemap_First");
        mapSecondRoot = GameObject.Find("Tilemap_Second");
    }

    private void Initialize()
    {
        ResetData();
        
        _isFirst = true; // 명시적 초기화
        _currentRoot = mapFirstRoot; // 초기 Root 설정
        
        _activatedRoot = mapFirstRoot.transform;
        _deactivatedRoot = mapSecondRoot.transform;

        SetCameraLayer();
    }

    private void ChangeTileMap()
    {
        _isFirst = !_isFirst;
        
        if (_isFirst)
            ActivateFirst();
        else
            ActivateSecond();
        
    }

    private void ActivateFirst()
    {
        //mapFirstRoot.SetActive(true);
        //mapSecondRoot.SetActive(false);
        
        _activatedRoot = mapFirstRoot.transform;
        _deactivatedRoot = mapSecondRoot.transform;

        _currentRoot = mapFirstRoot;
        
        SetCameraLayer();
    }

    private void ActivateSecond()
    {
        //mapFirstRoot.SetActive(false);
        //mapSecondRoot.SetActive(true);
        
        _activatedRoot = mapSecondRoot.transform;
        _deactivatedRoot = mapFirstRoot.transform;

        _currentRoot = mapSecondRoot;
        
        SetCameraLayer();
    }

    // 최적화된 SetCameraLayer
    private void SetCameraLayer()
    {
        if (_mainCamera == null) return;

        int map1 = LayerMask.NameToLayer("Map 1");
        int map2 = LayerMask.NameToLayer("Map 2");

        // 레이어가 존재하지 않을 경우를 대비한 방어 코드
        if (map1 == -1 || map2 == -1) {
            Debug.LogError("Map 1 또는 Map 2 레이어가 유니티 에디터에 설정되지 않았습니다!");
            return;
        }

        if (_isFirst)
        {
            _mainCamera.cullingMask |= (1 << map1);
            _mainCamera.cullingMask &= ~(1 << map2);
        }
        else
        {
            _mainCamera.cullingMask |= (1 << map2);
            _mainCamera.cullingMask &= ~(1 << map1);
        }
    }

    public Transform GetActiveMapRoot() => _activatedRoot;
    public Transform GetInactiveMapRoot() => _deactivatedRoot;

    private void OnEnable()
    {
        GameEvents.TileMapChanged += ChangeTileMap;
        GameEvents.TileMapRotated += RotateAroundCell;
    }

    private void OnDisable()
    {
        GameEvents.TileMapChanged -= ChangeTileMap;
        GameEvents.TileMapRotated -= RotateAroundCell;
    }

    public void RotateAroundCell(Vector3Int cellPosition, float angle)
    {
        if (_isRotating) return;

        _isRotating = true;
        GameEvents.RaiseInputLockChanged(true);

        StackManager player = FindObjectOfType<StackManager>();
        if (player == null) return;

        // 1. 피벗 위치 계산 및 플레이어 위치 강제 정렬 (Snapping)
        // 회전 전 플레이어를 타일 정중앙으로 옮겨 '애매하게 걸친 상태'를 방지합니다.
        Vector3 snappedPivot = new Vector3(
            Mathf.Floor(player.transform.position.x) + 0.5f,
            Mathf.Floor(player.transform.position.y) + 0.5f,
            0
        );
        player.transform.position = snappedPivot; 

        // 2. 계층 구조를 깨지 않고 피벗만 이동 (중요!)
        // SetParent(null)을 쓰지 않아야 OnTrigger 재발동을 막을 수 있습니다.
        Vector3 offset = snappedPivot - mapPivot.position;
        mapPivot.position = snappedPivot;
        foreach (Transform child in mapPivot)
        {
            child.position -= offset;
        }

        // 3. 물리 고정 (자식으로 넣지 않음!)
        player.FreezePlayerPhysics(true);
    
        Vector3 targetRotation = mapPivot.eulerAngles + new Vector3(0, 0, angle);

        mapPivot
            .DORotate(targetRotation, rotateDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                // 4. 회전 완료 후 방향 동기화
                // 맵이 돌아간 만큼 플레이어의 내부 방향 데이터도 업데이트합니다.
                int rotationIndexOffset = Mathf.RoundToInt(-angle / 90f);
                player.UpdateDirection(rotationIndexOffset);
            
                // 화살표 방향 즉시 갱신
                player.RotateArrow(); 

                player.FreezePlayerPhysics(false);
            
                // 5. 물리 엔진이 안정화될 시간을 아주 짧게 준 뒤 회전 잠금을 풉니다.
                DOVirtual.DelayedCall(0.05f, () => {
                    _isRotating = false;
                    GameEvents.RaiseInputLockChanged(false);
                });
            });
    }
}
