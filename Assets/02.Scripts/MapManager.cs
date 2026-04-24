using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    private struct MapState
    {
        // [공통 상태]
        public bool isFirst;
        public RenderTexture renderTexture;

        // [Map 1 전용 상태]
        public Vector3 firstRootPosition;           // Map1 오브젝트 위치
        public Vector3 firstPivotPosition;          // Map1이 마지막으로 회전한 축의 위치
        public float   firstZRotation;              // Map1(또는 Map1 Pivot)의 Z 회전값
        public float   firstAccumulatedRotation;    // Map1의 누적 회전각
        public float   firstTileIconZRotation;      // Map1 타일 아이콘들의 역회전 보정값

        // [Map 2 전용 상태]
        public Vector3 secondRootPosition;          // Map2 오브젝트 위치
        public Vector3 secondPivotPosition;         // Map2가 마지막으로 회전한 축의 위치
        public float   secondZRotation;             // Map2(또는 Map2 Pivot)의 Z 회전값
        public float   secondAccumulatedRotation;   // Map2의 누적 회전각
        public float   secondTileIconZRotation;     // Map2 타일 아이콘들의 역회전 보정값
    }

    private Stack<MapState> _undoMapHistory = new Stack<MapState>();

    private Camera _mainCamera;

    private bool _isFirst    = true;
    private bool _isRotating = false;
    public  bool IsRotating => _isRotating;

    
    // 맵 회전 시, 타일의 누적된 회전 각도를 저장함
    private float _firstAccumulatedRotation = 0f;
    private float _secondAccumulatedRotation = 0f;
    
    // 맵 회전 시, 타일들의 아이콘을 원래 시점으로 보정
    private float _firstTileIconZRotation   = 0f;
    private float _secondTileIconZRotation   = 0f;

    private bool _preChangeIsFirst;
    private bool _mapChangedSinceLastSave;

    // ── 리팩터링 추가: pb.isUndo/isRedo 직접 접근 대신 Bridge 사용 ──
    private PlayerUndoStateBridge _undoState;

    [Header("Rotation Logic")]
    [SerializeField] private Transform mapFirstPivot;
    [SerializeField] private Transform mapSecondPivot;
    
    [SerializeField] private float     rotateDuration;
    [SerializeField] private GameObject mapFirstRoot;
    [SerializeField] private GameObject mapSecondRoot;
    [SerializeField] private GameObject mapStaticRoot;
    
    [SerializeField] private RawImage screen;
    [SerializeField] private RenderTexture mapFirstRenderTexture;
    [SerializeField] private RenderTexture mapSecondRenderTexture;
    private RenderTexture _currentRenderTexture;
    
    private GameObject _currentRoot;
    private Transform  _activatedRoot;
    private Transform  _deactivatedRoot;
    private Transform _staticRoot;

    [SerializeField] private PlayerBehaviour player;

    public bool IsFirstRoot() => _currentRoot == mapFirstRoot;

    private void Awake()
    {
        _mainCamera   = Camera.main;
        player        = FindFirstObjectByType<PlayerBehaviour>();
        _undoState = FindFirstObjectByType<PlayerUndoStateBridge>(); // Bridge 자동 탐색
    }

    public void InitializeNewStage(GameObject stageRoot)
    {
        StageLinker linker = stageRoot.GetComponent<StageLinker>();
        if (linker != null)
        {
            mapFirstPivot       = linker.mapFirstPivot;
            mapSecondPivot       = linker.mapSecondPivot;
            mapFirstRoot   = linker.mapFirstRoot;
            mapSecondRoot  = linker.mapSecondRoot;
            mapStaticRoot  = linker.mapStaticRoot;
            
            Init();
            
            GameEvents.RaiseMapInitialized();
            
        }
    }

    public void Init()
    {
        _isFirst      = true;
        _currentRoot  = mapFirstRoot;

        _activatedRoot   = mapFirstRoot.transform;
        _deactivatedRoot = mapSecondRoot.transform;
        _staticRoot = mapStaticRoot.transform;

        _firstAccumulatedRotation = 0f;
        _secondAccumulatedRotation = 0f;
    
        _firstTileIconZRotation   = 0f;
        _secondTileIconZRotation   = 0f;
        
        _isRotating          = false;
        _undoMapHistory.Clear();

        //Raw Image의 RenderTexture 변화
        screen.texture = mapFirstRenderTexture;
        _currentRenderTexture = mapFirstRenderTexture;
        
        player.ChangePlayerTransform(new Vector3(player.transform.position.x,player.transform.position.y,mapFirstRoot.transform.position.z));
        
        SetCameraLayer();
    }
    
    private void ChangeTileMap()
    {
        _preChangeIsFirst        = _isFirst;
        _mapChangedSinceLastSave = true;

        _isFirst = !_isFirst;
        if (_isFirst) ActivateFirst();
        else          ActivateSecond();
    }

    private void ActivateFirst()
    {
        _activatedRoot   = mapFirstRoot.transform;
        _deactivatedRoot = mapSecondRoot.transform;
        _currentRoot     = mapFirstRoot;
        
        //Raw Image의 RenderTexture 변화
        screen.texture = mapFirstRenderTexture;
        _currentRenderTexture = mapFirstRenderTexture;
        
        player.ChangePlayerTransform(new Vector3(player.transform.position.x,player.transform.position.y,mapFirstRoot.transform.position.z));
        
        SetCameraLayer();
    }

    private void ActivateSecond()
    {
        _activatedRoot   = mapSecondRoot.transform;
        _deactivatedRoot = mapFirstRoot.transform;
        _currentRoot     = mapSecondRoot;
        
        //Raw Image의 RenderTexture 변화
        screen.texture = mapSecondRenderTexture;
        _currentRenderTexture = mapSecondRenderTexture;
        
        player.ChangePlayerTransform(new Vector3(player.transform.position.x,player.transform.position.y,mapSecondRoot.transform.position.z));
        
        SetCameraLayer();
    }

    private void SetCameraLayer()
    {
        if (_mainCamera == null) return;

        int map1 = LayerMask.NameToLayer("Map 1");
        int map2 = LayerMask.NameToLayer("Map 2");

        if (map1 == -1 || map2 == -1)
        {
            Debug.LogError("Map 1 또는 Map 2 레이어가 유니티 에디터에 설정되지 않았습니다!");
            return;
        }

        if (_isFirst)
        {
            _mainCamera.cullingMask |=  (1 << map1);
            _mainCamera.cullingMask &= ~(1 << map2);
        }
        else
        {
            _mainCamera.cullingMask |=  (1 << map2);
            _mainCamera.cullingMask &= ~(1 << map1);
        }
    }

    public Transform GetActiveMapRoot()   => _activatedRoot;
    public Transform GetInactiveMapRoot() => _deactivatedRoot;
    public Transform GetStaticRoot() => _staticRoot;

    private void OnEnable()
    {
        GameEvents.TileMapChanged        += ChangeTileMap;
        GameEvents.TileMapRotated        += RotateAroundCell;
        GameEvents.SaveStateBeforeAction += SaveMapState;
        GameEvents.UndoTriggered         += RestoreMapState;
    }

    private void OnDisable()
    {
        GameEvents.TileMapChanged        -= ChangeTileMap;
        GameEvents.TileMapRotated        -= RotateAroundCell;
        GameEvents.SaveStateBeforeAction -= SaveMapState;
        GameEvents.UndoTriggered         -= RestoreMapState;
    }

    // ── 변경: pb.isUndo/isRedo → _undoState.IsUndo/IsRedo ──
    public void RotateAroundCell(PlayerBehaviour pb, float angle)
    {
        bool isUndo = _undoState != null && _undoState.IsUndo;
        if (_isRotating || pb == null || isUndo) return;

        _isRotating = true;
        GameEvents.RaiseInputLockChanged(true);
        GameEvents.RaiseBeforeMapRotated(true);

        Vector3 snappedPivot = new Vector3(
            Mathf.Floor(pb.transform.position.x) + 0.5f,
            Mathf.Floor(pb.transform.position.y) + 0.5f,
            0
        );
        pb.transform.position = snappedPivot;

        if (_isFirst)
        {
            Vector3 offset = snappedPivot - mapFirstPivot.position;
            mapFirstPivot.position = snappedPivot;
            foreach (Transform child in mapFirstPivot)
                child.position -= offset;

            _firstAccumulatedRotation += angle;
            Vector3 targetRotation = new Vector3(0, 0, _firstAccumulatedRotation);
        
            Debug.Log("현재 회전 발생");

            mapFirstPivot
                .DORotate(targetRotation, rotateDuration, RotateMode.Fast)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    _firstTileIconZRotation += -angle;
                    GameEvents.RaiseTileIconRotated(-angle);

                    DOVirtual.DelayedCall(0.55f, () =>
                    {
                        _isRotating = false;
                        GameEvents.RaiseInputLockChanged(false);
                        GameEvents.RaiseAfterMapRotated(false);
                    });
                }); 
        }
        else
        {
            Vector3 offset = snappedPivot - mapSecondPivot.position;
            mapSecondPivot.position = snappedPivot;
            foreach (Transform child in mapSecondPivot)
                child.position -= offset;

            _secondAccumulatedRotation += angle;
            Vector3 targetRotation = new Vector3(0, 0, _secondAccumulatedRotation);
        
            Debug.Log("현재 회전 발생");

            mapSecondPivot
                .DORotate(targetRotation, rotateDuration, RotateMode.Fast)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    _secondTileIconZRotation += -angle;
                    GameEvents.RaiseTileIconRotated(-angle);

                    DOVirtual.DelayedCall(0.55f, () =>
                    {
                        _isRotating = false;
                        GameEvents.RaiseInputLockChanged(false);
                        GameEvents.RaiseAfterMapRotated(false);
                    });
                }); 
        }
    }

    // ── 변경: pb.isUndo/isRedo → _undoState.IsUndo/IsRedo ──
    private void SaveMapState(PlayerBehaviour pb)
    {
        bool isUndo = _undoState != null && _undoState.IsUndo;
        if (pb == null || isUndo) return;

        // 직전에 맵 전환이 있었다면 전환 전 isFirst 값을 사용
        bool isFirstSnapshot     = _mapChangedSinceLastSave ? _preChangeIsFirst : _isFirst;
        _mapChangedSinceLastSave = false;

        // isFirst와 renderTexture가 항상 일치하도록 isFirstSnapshot에서 파생
        RenderTexture renderTextureSnapshot = isFirstSnapshot ? mapFirstRenderTexture : mapSecondRenderTexture;

        var state = new MapState
        {
            firstPivotPosition  = mapFirstPivot.position,
            firstZRotation      = mapFirstPivot.eulerAngles.z,
            secondPivotPosition  = mapSecondPivot.position,
            secondZRotation      = mapSecondPivot.eulerAngles.z,
            
            firstRootPosition   = mapFirstRoot.transform.position,
            secondRootPosition  = mapSecondRoot.transform.position,
            
            firstTileIconZRotation = _firstTileIconZRotation,
            firstAccumulatedRotation = _firstAccumulatedRotation,
            secondTileIconZRotation = _secondTileIconZRotation,
            secondAccumulatedRotation =  _secondAccumulatedRotation,
            
            isFirst             = isFirstSnapshot,
            renderTexture       = renderTextureSnapshot
        };

        _undoMapHistory.Push(state);
    }

    private void RestoreMapState()
    {
        if (_undoMapHistory.Count <= 0)
        {
            return;
        }

        MapState lastState = _undoMapHistory.Pop();
        
        mapFirstPivot.position = lastState.firstPivotPosition;
        mapFirstPivot.rotation = Quaternion.Euler(0, 0, lastState.firstZRotation);
        
        mapSecondPivot.position = lastState.secondPivotPosition;
        mapSecondPivot.rotation = Quaternion.Euler(0, 0, lastState.secondZRotation);

        mapFirstRoot.transform.position  = lastState.firstRootPosition;
        mapSecondRoot.transform.position = lastState.secondRootPosition;

        SnapTileIcons(mapFirstRoot.transform, lastState.firstTileIconZRotation, ref _firstTileIconZRotation);
        SnapTileIcons(mapSecondRoot.transform, lastState.secondTileIconZRotation, ref _secondTileIconZRotation);
        
        _firstAccumulatedRotation = lastState.firstAccumulatedRotation;
        _secondAccumulatedRotation = lastState.secondAccumulatedRotation;

        // 맵 전환 상태 복원
        _isFirst = lastState.isFirst;
        if (_isFirst) ActivateFirst();
        else          ActivateSecond();
        
        // RenderTexture 복원
        screen.texture = lastState.renderTexture;
        _currentRenderTexture = lastState.renderTexture;
        
        Physics2D.SyncTransforms();
    }

    private void SnapTileIcons(Transform targetRoot, float targetZRotation, ref float currentIconRotation)
    {
        float delta = targetZRotation - currentIconRotation;
        if (Mathf.Approximately(delta, 0f)) return;

        // [변경] mapPivot 대신 전달받은 targetRoot의 자식들만 뒤져서 회전시킵니다.
        var tiles = targetRoot.GetComponentsInChildren<TileBehaviour>(includeInactive: true);
        foreach (var tile in tiles)
        {
            tile.transform.DOKill();
            tile.transform.Rotate(0f, 0f, delta, Space.Self);
        }
        
        // [변경] 현재 회전값을 ref를 통해 원본 변수에 업데이트합니다.
        currentIconRotation = targetZRotation;
    }
}