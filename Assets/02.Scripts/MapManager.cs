using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public class MapManager : MonoBehaviour
{
    
    private struct MapState
    {
        public Vector3 pivotPosition;
        public float zRotation;
        
        //자식들의 월드 좌표 저장
        public Vector3 firstRootPosition;
        public Vector3 secondRootPosition;
    }
    
    private Stack<MapState> _undoMapHistory = new Stack<MapState>();
    private Stack<MapState> _redoMapHistory = new Stack<MapState>();
    
    private Camera _mainCamera;

    private bool _isFirst = true;
    private bool _isRotating = false;
    

    [Header ("Rotation Logic")]    
    [SerializeField] private Transform mapPivot;
    [SerializeField] private float rotateDuration;
    
    [SerializeField] private GameObject mapFirstRoot;
    [SerializeField] private GameObject mapSecondRoot;

    private GameObject _currentRoot;
    
    private Transform _activatedRoot;
    private Transform _deactivatedRoot;

    [SerializeField] private StackManager player;

    public bool IsFirstRoot()
    {
        if (_currentRoot == mapFirstRoot) return true;
        
        return false;
    }
    
    private void Awake()
    {
        _mainCamera = Camera.main;
        player = FindObjectOfType<StackManager>();
    }

    public void InitializeNewStage(GameObject stageRoot)
    {
        StageLinker linker = stageRoot.GetComponent<StageLinker>();

        if (linker != null)
        {
            mapPivot = linker.mapPivot;
            mapFirstRoot = linker.mapFirstRoot;
            mapSecondRoot = linker.mapSecondRoot;
        
            Initialize();
        }
    }

    private void Initialize() // StackLoader에서 InitializeNewStage와 Initialize를 호출함
    {
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
        _activatedRoot = mapFirstRoot.transform;
        _deactivatedRoot = mapSecondRoot.transform;

        _currentRoot = mapFirstRoot;
        
        SetCameraLayer();
    }

    private void ActivateSecond()
    {
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

        GameEvents.SaveStateBeforeAction += SaveMapState;
        GameEvents.UndoTriggered += RestoreMapState;
        GameEvents.RedoTriggered += ApplyRedoMapState;
    }

    private void OnDisable()
    {
        GameEvents.TileMapChanged -= ChangeTileMap;
        GameEvents.TileMapRotated -= RotateAroundCell;
        
        GameEvents.SaveStateBeforeAction -= SaveMapState;
        GameEvents.UndoTriggered -= RestoreMapState;
        GameEvents.RedoTriggered -= ApplyRedoMapState;
    }

    public void RotateAroundCell(Vector3Int cellPosition, float angle)
    {
        if (_isRotating || player == null || player.isUndoRedo) return;
        
        _isRotating = true;
        GameEvents.RaiseIsRotating(true);
        GameEvents.RaiseInputLockChanged(true);

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
                GameEvents.RaiseTileIconRotated(-angle);

                player.FreezePlayerPhysics(false);
            
                // 5. 물리 엔진이 안정화될 시간을 아주 짧게 준 뒤 회전 잠금을 풉니다.
                DOVirtual.DelayedCall(0.05f, () => {
                    _isRotating = false;
                    GameEvents.RaiseInputLockChanged(false);
                    GameEvents.RaiseIsRotating(false);
                });
            });
    }

    private void SaveMapState()
    {
        if (player == null || player.isUndoRedo) return;
        
        _undoMapHistory.Push(new MapState
        {
            pivotPosition = mapPivot.position,
            zRotation = mapPivot.eulerAngles.z,
            firstRootPosition = mapFirstRoot.transform.position,
            secondRootPosition = mapSecondRoot.transform.position
        });

        _redoMapHistory.Clear();
        
    }

    private void RestoreMapState()
    {
        if (_undoMapHistory.Count <= 0) return;
        
        _redoMapHistory.Push(new MapState
        {
            pivotPosition = mapPivot.position,
            zRotation = mapPivot.eulerAngles.z,
            firstRootPosition = mapFirstRoot.transform.position,
            secondRootPosition = mapSecondRoot.transform.position
        });
        
        MapState lastState = _undoMapHistory.Pop();
        
        mapPivot.position = lastState.pivotPosition;
        mapPivot.rotation = Quaternion.Euler(0,0,lastState.zRotation);
        
        mapFirstRoot.transform.position = lastState.firstRootPosition;
        mapSecondRoot.transform.position = lastState.secondRootPosition;
        
        Physics2D.SyncTransforms(); // 물리 엔진 동기화
        
    }
    
    private void ApplyRedoMapState()
    {
        if (_redoMapHistory.Count <= 0) return;

        // 다시하기 전의 '과거' 상태를 Undo 스택에 다시 넣고, 미래를 적용합니다.
        _undoMapHistory.Push(new MapState
        {
            pivotPosition = mapPivot.position,
            zRotation = mapPivot.eulerAngles.z,
            firstRootPosition = mapFirstRoot.transform.position,
            secondRootPosition = mapSecondRoot.transform.position
        });
        
        MapState state = _redoMapHistory.Pop();
        
        // 실제 트랜스폼 복구 로직 (가장 안정적인 월드 좌표 방식)
        mapPivot.position = state.pivotPosition;
        mapPivot.rotation = Quaternion.Euler(0, 0, state.zRotation);
        mapFirstRoot.transform.position = state.firstRootPosition;
        mapSecondRoot.transform.position = state.secondRootPosition;
        
        Physics2D.SyncTransforms();
    }
    
}
