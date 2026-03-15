using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public class MapManager : MonoBehaviour
{
    private struct MapState
    {
        public Vector3 pivotPosition;
        public float zRotation;
        
        public Vector3 firstRootPosition;
        public Vector3 secondRootPosition;

        public float tileIconZRotation;

        // eulerAngles는 0~360으로 정규화되므로 누적값을 별도 저장합니다.
        public float accumulatedRotation;
    }
    
    private Stack<MapState> _undoMapHistory = new Stack<MapState>();
    private Stack<MapState> _redoMapHistory = new Stack<MapState>();
    
    private Camera _mainCamera;

    private bool _isFirst = true;
    private bool _isRotating = false;
    public bool IsRotating => _isRotating;

    // eulerAngles는 0~360으로 정규화되므로 누적값으로 별도 관리합니다.
    // 예: -90 후 -180 → _accumulatedRotation = -270 (올바른 방향 유지)
    private float _accumulatedRotation = 0f;

    // 타일 아이콘 누적 로컬 Z 회전값
    private float _tileIconZRotation = 0f;
    

    [Header ("Rotation Logic")]    
    [SerializeField] private Transform mapPivot;
    [SerializeField] private float rotateDuration;
    
    [SerializeField] private GameObject mapFirstRoot;
    [SerializeField] private GameObject mapSecondRoot;

    private GameObject _currentRoot;
    
    private Transform _activatedRoot;
    private Transform _deactivatedRoot;

    [SerializeField] private PlayerBehaviour player;

    public bool IsFirstRoot()
    {
        if (_currentRoot == mapFirstRoot) return true;
        return false;
    }
    
    private void Awake()
    {
        _mainCamera = Camera.main;
        player = FindObjectOfType<PlayerBehaviour>();
    }

    public void InitializeNewStage(GameObject stageRoot)
    {
        StageLinker linker = stageRoot.GetComponent<StageLinker>();

        if (linker != null)
        {
            mapPivot = linker.mapPivot;
            mapFirstRoot = linker.mapFirstRoot;
            mapSecondRoot = linker.mapSecondRoot;
        
            Init();
        }
    }

    public void Init()
    {
        _isFirst = true;
        _currentRoot = mapFirstRoot;
        
        _activatedRoot = mapFirstRoot.transform;
        _deactivatedRoot = mapSecondRoot.transform;
        
        _accumulatedRotation = 0f;      // 누적 회전각 초기화
        _tileIconZRotation = 0f;        // 타일 아이콘 회전각 초기화
        _isRotating = false;            // 회전 중 플래그 초기화
        _undoMapHistory.Clear();        // Undo 스택 비우기
        _redoMapHistory.Clear();        // Redo 스택 비우기

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

    private void SetCameraLayer()
    {
        if (_mainCamera == null) return;

        int map1 = LayerMask.NameToLayer("Map 1");
        int map2 = LayerMask.NameToLayer("Map 2");

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

     public void RotateAroundCell(PlayerBehaviour pb, float angle)
    {
        if (_isRotating || pb == null || pb.isUndo|| pb.isRedo) return;
        
        _isRotating = true;
        GameEvents.RaiseInputLockChanged(true);
        GameEvents.RaiseBeforeMapRotated(true);

        // 1. 피벗 위치 계산 및 플레이어 위치 강제 정렬 (Snapping)
        Vector3 snappedPivot = new Vector3(
            Mathf.Floor(pb.transform.position.x) + 0.5f,
            Mathf.Floor(pb.transform.position.y) + 0.5f,
            0
        );
        pb.transform.position = snappedPivot;

        // 2. 계층 구조를 깨지 않고 피벗만 이동
        Vector3 offset = snappedPivot - mapPivot.position;
        mapPivot.position = snappedPivot;
        foreach (Transform child in mapPivot)
        {
            child.position -= offset;
        }

        // [FIX] eulerAngles 대신 누적값을 사용합니다.
        // eulerAngles는 0~360으로 정규화되어 회전 방향이 뒤집히는 버그가 있었습니다.
        _accumulatedRotation += angle;
        Vector3 targetRotation = new Vector3(0, 0, _accumulatedRotation);

        mapPivot
            .DORotate(targetRotation, rotateDuration, RotateMode.Fast)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                _tileIconZRotation += -angle;
                GameEvents.RaiseTileIconRotated(-angle);

                DOVirtual.DelayedCall(0.55f, () => {
                    _isRotating = false;
                    GameEvents.RaiseInputLockChanged(false);
                    GameEvents.RaiseAfterMapRotated(false);
                });
            });
    }

    private void SaveMapState(PlayerBehaviour pb)
    {
        if (pb == null || pb.isUndo|| pb.isRedo) return;
        
        _undoMapHistory.Push(new MapState
        {
            pivotPosition = mapPivot.position,
            zRotation = mapPivot.eulerAngles.z,
            firstRootPosition = mapFirstRoot.transform.position,
            secondRootPosition = mapSecondRoot.transform.position,
            tileIconZRotation = _tileIconZRotation,
            accumulatedRotation = _accumulatedRotation
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
            secondRootPosition = mapSecondRoot.transform.position,
            tileIconZRotation = _tileIconZRotation,
            accumulatedRotation = _accumulatedRotation
        });
        
        MapState lastState = _undoMapHistory.Pop();
        
        mapPivot.position = lastState.pivotPosition;
        mapPivot.rotation = Quaternion.Euler(0, 0, lastState.zRotation);
        
        mapFirstRoot.transform.position = lastState.firstRootPosition;
        mapSecondRoot.transform.position = lastState.secondRootPosition;

        SnapTileIcons(lastState.tileIconZRotation);
        _tileIconZRotation = lastState.tileIconZRotation;
        _accumulatedRotation = lastState.accumulatedRotation;
        
        Physics2D.SyncTransforms();
    }
    
    private void ApplyRedoMapState()
    {
        if (_redoMapHistory.Count <= 0) return;

        _undoMapHistory.Push(new MapState
        {
            pivotPosition = mapPivot.position,
            zRotation = mapPivot.eulerAngles.z,
            firstRootPosition = mapFirstRoot.transform.position,
            secondRootPosition = mapSecondRoot.transform.position,
            tileIconZRotation = _tileIconZRotation,
            accumulatedRotation = _accumulatedRotation
        });
        
        MapState state = _redoMapHistory.Pop();
        
        mapPivot.position = state.pivotPosition;
        mapPivot.rotation = Quaternion.Euler(0, 0, state.zRotation);
        mapFirstRoot.transform.position = state.firstRootPosition;
        mapSecondRoot.transform.position = state.secondRootPosition;

        SnapTileIcons(state.tileIconZRotation);
        _tileIconZRotation = state.tileIconZRotation;
        _accumulatedRotation = state.accumulatedRotation;
        
        Physics2D.SyncTransforms();
    }

    private void SnapTileIcons(float targetZRotation)
    {
        float delta = targetZRotation - _tileIconZRotation;
        if (Mathf.Approximately(delta, 0f)) return;

        var tiles = mapPivot.GetComponentsInChildren<TileBehaviour>(includeInactive: true);
        foreach (var tile in tiles)
        {
            tile.transform.DOKill();
            tile.transform.Rotate(0f, 0f, delta, Space.Self);
        }
    }
}