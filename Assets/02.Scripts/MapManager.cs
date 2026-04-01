using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    private struct MapState
    {
        public Vector3 pivotPosition;
        public float   zRotation;
        public Vector3 firstRootPosition;
        public Vector3 secondRootPosition;
        public float   tileIconZRotation;
        public float   accumulatedRotation;
        public bool isFirst;
        public RenderTexture renderTexture;
    }

    private Stack<MapState> _undoMapHistory = new Stack<MapState>();

    private Camera _mainCamera;

    private bool _isFirst    = true;
    private bool _isRotating = false;
    public  bool IsRotating => _isRotating;

    private float _accumulatedRotation = 0f;
    private float _tileIconZRotation   = 0f;

    private bool _preChangeIsFirst;
    private bool _mapChangedSinceLastSave;

    // ── 리팩터링 추가: pb.isUndo/isRedo 직접 접근 대신 Bridge 사용 ──
    private PlayerUndoStateBridge _undoState;

    [Header("Rotation Logic")]
    [SerializeField] private Transform mapPivot;
    [SerializeField] private float     rotateDuration;
    [SerializeField] private GameObject mapFirstRoot;
    [SerializeField] private GameObject mapSecondRoot;

    [SerializeField] private RawImage screen;
    [SerializeField] private RenderTexture mapFirstRenderTexture;
    [SerializeField] private RenderTexture mapSecondRenderTexture;
    private RenderTexture _currentRenderTexture;
    
    private GameObject _currentRoot;
    private Transform  _activatedRoot;
    private Transform  _deactivatedRoot;

    [SerializeField] private PlayerBehaviour player;

    public bool IsFirstRoot() => _currentRoot == mapFirstRoot;

    private void Awake()
    {
        _mainCamera   = Camera.main;
        player        = FindObjectOfType<PlayerBehaviour>();
        _undoState = FindObjectOfType<PlayerUndoStateBridge>(); // Bridge 자동 탐색
    }

    public void InitializeNewStage(GameObject stageRoot)
    {
        StageLinker linker = stageRoot.GetComponent<StageLinker>();
        if (linker != null)
        {
            mapPivot       = linker.mapPivot;
            mapFirstRoot   = linker.mapFirstRoot;
            mapSecondRoot  = linker.mapSecondRoot;
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

        _accumulatedRotation = 0f;
        _tileIconZRotation   = 0f;
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

        Vector3 offset = snappedPivot - mapPivot.position;
        mapPivot.position = snappedPivot;
        foreach (Transform child in mapPivot)
            child.position -= offset;

        _accumulatedRotation += angle;
        Vector3 targetRotation = new Vector3(0, 0, _accumulatedRotation);
        
        Debug.Log("현재 회전 발생");

        mapPivot
            .DORotate(targetRotation, rotateDuration, RotateMode.Fast)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                _tileIconZRotation += -angle;
                GameEvents.RaiseTileIconRotated(-angle);

                DOVirtual.DelayedCall(0.55f, () =>
                {
                    _isRotating = false;
                    GameEvents.RaiseInputLockChanged(false);
                    GameEvents.RaiseAfterMapRotated(false);
                });
            });
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
            pivotPosition       = mapPivot.position,
            zRotation           = mapPivot.eulerAngles.z,
            firstRootPosition   = mapFirstRoot.transform.position,
            secondRootPosition  = mapSecondRoot.transform.position,
            tileIconZRotation   = _tileIconZRotation,
            accumulatedRotation = _accumulatedRotation,
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
        
        mapPivot.position = lastState.pivotPosition;
        mapPivot.rotation = Quaternion.Euler(0, 0, lastState.zRotation);
        mapFirstRoot.transform.position  = lastState.firstRootPosition;
        mapSecondRoot.transform.position = lastState.secondRootPosition;

        SnapTileIcons(lastState.tileIconZRotation);
        _tileIconZRotation   = lastState.tileIconZRotation;
        _accumulatedRotation = lastState.accumulatedRotation;

        // 맵 전환 상태 복원
        _isFirst = lastState.isFirst;
        if (_isFirst) ActivateFirst();
        else          ActivateSecond();
        
        // RenderTexture 복원
        screen.texture = lastState.renderTexture;
        _currentRenderTexture = lastState.renderTexture;
        
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