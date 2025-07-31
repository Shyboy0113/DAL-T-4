using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//DoTween 사용
using DG.Tweening;

//TileMap 사용
using UnityEngine.Tilemaps;

public enum KeyType
{
    None = 0,
    Alt = 1,
    F4 = 2,
    Tab = 3
}

public class StackManager : MonoBehaviour
{
    //플레이어의 회전 방향을 Enum(문자)로 표시
    public enum PlayerDirection
    {
        Right,Down,Left,Up
    }
    
    private PlayerDirection _playerDirection = PlayerDirection.Right;
    
    
    //전역 접근이 가능하도록 하는 이벤트
    public static event Action OnStageCleared; 
    public static event Action OnPlayerDied;
    public event Action OnInputQueueChanged; // SequenceUI의 Update 함수 비용 줄이기
    
    private int _stack = 0;
    
    // 최대로 쌓을 수 있는 큐 스택을 상수로 선언
    private const int MaxQueueSize = 3;
    private List<int> _inputQueue = new List<int>(new int[MaxQueueSize]);

    private Rigidbody2D _rigidbody2D;
    [SerializeField]
    private float forceAmount = 1f;

    [SerializeField]
    private Animator _animatior;
    public GameObject arrow;
    
    //DOTween 전용 변동 속도
    public float DOTweenDuration;
    public Vector3 DOTweenPunch;
    public int DOTweenVibrato;
    
    //Alt + Tab 전용 로직 bool
    [SerializeField]
    private bool _isSwitched;
    
    // 카메라 Layer Culling Mask 전환을 위한 메인 카메라
    private Camera _mainCamera;

    #region TileMap

    // 인스펙터에서 두 타일맵을 연결할 변수
    public Tilemap tilemapFirst;
    public Tilemap tilemapSecond;
    
    private TilemapCollider2D _colliderFirst;
    private TilemapCollider2D _colliderSecond;

    private Tilemap _activeTilemap; // 현재 활성화된 타일맵을 저장할 변수
    
    public GameObject mainCamera; // 메인 카메라
    
    [SerializeField]
    private CanvasGroup changePanelCanvasGroup;

    #endregion
    

    private void Awake()
    {
        _animatior = GetComponent<Animator>();
        _rigidbody2D = GetComponent<Rigidbody2D>();

        _colliderFirst = tilemapFirst.GetComponent<TilemapCollider2D>();
        _colliderSecond = tilemapSecond.GetComponent<TilemapCollider2D>();
        
        _mainCamera = Camera.main;
        Debug.Log(_mainCamera);

    }

    private void Start()
    {
        // 게임 시작 시 첫 번째 맵을 활성화
        _activeTilemap = tilemapFirst;
      
        // 게임 시작 시 CanvasGroup의 알파값을 0으로, 비활성화 상태로 만듭니다.
        changePanelCanvasGroup.alpha = 0;
        changePanelCanvasGroup.interactable = false; // 클릭 등 상호작용 비활성화
        changePanelCanvasGroup.blocksRaycasts = false; // UI 뒤의 오브젝트가 클릭되는 것을 막지 않음

        _isSwitched = false;

        _colliderSecond.enabled = false; //2번째 맵 콜라이더를 끈다.
        
        // 카메라 시야에서 보이지 않도록 규정
        _mainCamera.cullingMask = ~(1 << LayerMask.NameToLayer("Map 2")); // Map 2 Layer를 카메라의 Culling Mask에서 제거
        _mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Map 1"); // Map 1 Layer를 카메라의 Culling Mask에서 추가
    }
    
    private void Update()
    {
        // 게임이 진행 중일 때만 입력을 받도록 GameManager 상태를 확인합니다.
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        if (Input.GetKeyDown(KeyCode.LeftAlt) && GameManager.Instance.currentStageData.canUseAlt)
        {
            ProcessAltInput();
            GameManager.Instance.pushedNumberALT++; // 카운트는 GameManager가 관리
        }

        if (Input.GetKeyDown(KeyCode.F4) && GameManager.Instance.currentStageData.canUseF4)
        {
            ProcessF4Input();
            GameManager.Instance.pushedNumberF4++;
        }

        if (Input.GetKeyDown(KeyCode.Tab) && GameManager.Instance.currentStageData.canUseTab)
        {
            ProcessTabInput();
            GameManager.Instance.pushedNumberTAB++;
        }
    }
    
    // 맵 전환을 처리하는 메서드
    public void SwitchMap()
    {
        Vector3 newPlayerPosition;
    
        // 현재 활성화된 맵을 기준으로 다른 맵으로 전환
        if (_activeTilemap == tilemapFirst)
        {
            _colliderFirst.enabled = false;
            _colliderSecond.enabled = true;
            
            _activeTilemap = tilemapSecond;
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, tilemapSecond.gameObject.transform.position.z - 10f);
        
            // Z 위치만 새로운 타일맵에 맞게 설정
            newPlayerPosition = new Vector3(transform.position.x, transform.position.y, tilemapSecond.gameObject.transform.position.z);
            
            // 카메라 시야에서 보이지 않도록 규정
            _mainCamera.cullingMask = ~(1 << LayerMask.NameToLayer("Map 1")); // Map 1 Layer를 카메라의 Culling Mask에서 제거
            _mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Map 2"); // Map 2 Layer를 카메라의 Culling Mask에서 추가

        }
        else
        {
            _colliderFirst.enabled = true;
            _colliderSecond.enabled = false;
            
            _activeTilemap = tilemapFirst;
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y,tilemapFirst.gameObject.transform.position.z - 10f);
        
            // Z 위치만 새로운 타일맵에 맞게 설정
            newPlayerPosition = new Vector3(transform.position.x, transform.position.y, tilemapFirst.gameObject.transform.position.z);
            
            // 카메라 시야에서 보이지 않도록 규정
            _mainCamera.cullingMask = ~(1 << LayerMask.NameToLayer("Map 2")); // Map 2 Layer를 카메라의 Culling Mask에서 제거
            _mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Map 1"); // Map 1 Layer를 카메라의 Culling Mask에서 추가
            
        }
        
        _rigidbody2D.MovePosition(newPlayerPosition); //transform.position이 아니라, MovePosition으로 이동해야 콜라이더 판정이 작동한다.
        _isSwitched = true;

    }
    
    private void CheckForGroundAfterSwitch()
    {
        // 1. 플레이어의 현재 월드 좌표를 가져옵니다.
        Vector3 playerPosition = transform.position;

        // 2. 월드 좌표를 현재 활성화된 타일맵의 셀(그리드) 좌표로 변환합니다.
        Vector3Int cellPosition = _activeTilemap.WorldToCell(playerPosition);

        // 3. 변환된 셀 좌표에 타일이 존재하는지 확인합니다.
        bool hasGround = _activeTilemap.HasTile(cellPosition);

        // 4. 만약 타일이 없다면, 사망 처리를 합니다.
        if (!hasGround)
        {
            Debug.Log("맵 전환 후 발밑에 타일이 없습니다! 사망 처리!");
            
            PlayExplosion(); // 사망 이벤트 발동
             
        }
    }

    IEnumerator FadeSwitchPanel()
    {
        // 1. CanvasGroup의 알파값을 즉시 1로 만들어 전체를 보이게 함
        changePanelCanvasGroup.alpha = 1f;

        // 0.5초 대기
        yield return new WaitForSeconds(0.5f);
    
        // 2. CanvasGroup에 DOFade를 한 번만 호출하여 전체를 페이드 아웃
        changePanelCanvasGroup.DOFade(0f, 1.0f);
    
        // 1초 대기 (페이드 아웃 완료까지)
        yield return new WaitForSeconds(1.0f);

        // 3. 필요하다면 여기서 상호작용을 막을 수 있습니다.
        // (어차피 알파값이 0이라 보이지 않으므로 필수는 아님)
        changePanelCanvasGroup.interactable = false;
        changePanelCanvasGroup.blocksRaycasts = false;
    }

    public void ProcessAltInput()
    {
        HandleInput(1); // ALT 입력
        _playerDirection = (PlayerDirection)(((int)_playerDirection + 1) % 4);
        RotateArrow(); // 정방향 회전

    }
    public void ProcessTabInput()
    {
        HandleInput(3); // Tab 입력
        
        _playerDirection = (PlayerDirection)(((int)_playerDirection + 3) % 4); // direction -1 + 4 = direction +3
        RotateArrow(); // 역방향 회전
        
    }
    
    public void ProcessF4Input()
    {
        HandleInput(2); // F4 입력
        MovePlayer();
    }

    void HandleInput(int keyCode)
    {
        
        // 1. 여기서 스택을 먼저 체크하고 가득 찼으면 리셋합니다.
        if (_stack >= MaxQueueSize) ResetQueue();
        
        _inputQueue[_stack] = keyCode;
        _stack++;
        
        //이벤트 발생
        OnInputQueueChanged?.Invoke();

        if (CheckGameOver())
        {
            gameObject.GetComponent<BoxCollider2D>().enabled = false;

            Debug.Log("게임 오버!");
            _animatior.Play("Explosion");
            arrow.SetActive(false);
            
            OnPlayerDied?.Invoke(); //플레이어가 죽었다는 방송을 내보냄 
            //GameManager.Instance.isGameOver = true;
        }
        else if (CheckMapChange())
        {
            // 맵 전환
            SwitchMap();
            
            // 맵 전환 직후, 플레이어 위치의 타일 유효성 검사 실행!
            CheckForGroundAfterSwitch();
            
            // 코루틴 시작
            StartCoroutine(FadeSwitchPanel());
        }
    }

    bool CheckGameOver()
    {
        return (_inputQueue[0] == (int)KeyType.Alt && _inputQueue[1] == (int)KeyType.F4)
               || (_inputQueue[1] == (int)KeyType.Alt && _inputQueue[2] == (int)KeyType.F4);
    }
    
    bool CheckMapChange()
    {
        return (_inputQueue[0] == (int)KeyType.Alt && _inputQueue[1] == (int)KeyType.Tab && !_isSwitched)
               || (_inputQueue[1] == (int)KeyType.Alt && _inputQueue[2] == (int)KeyType.Tab && !_isSwitched);
    }

    void ResetQueue()
    {
        _inputQueue = new List<int>(new int[MaxQueueSize]);
        _stack = 0;

        _isSwitched = false;
        
        //큐가 리셋 될 경우에도 발동
        OnInputQueueChanged?.Invoke();
    }

    void MovePlayer()
    {
        Vector2 moveDirection = _playerDirection switch
        {
            PlayerDirection.Right => Vector2.right,
            PlayerDirection.Down => Vector2.down,
            PlayerDirection.Left => Vector2.left,
            PlayerDirection.Up => Vector2.up,
            _ => Vector2.zero
        };
        
        Debug.Log(moveDirection + " 이동");
        
        _rigidbody2D.AddForce(moveDirection * forceAmount, ForceMode2D.Impulse);
    }

    void RotateArrow()
    {
        float angle = _playerDirection switch
        {
            PlayerDirection.Right => 0f,
            PlayerDirection.Down => 270f,
            PlayerDirection.Left => 180f,
            PlayerDirection.Up => 90f,
            _ => 0f
        };

        //arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
        arrow.transform.DORotate(new Vector3(0, 0, angle), DOTweenDuration).SetEase(Ease.OutElastic); //일반 rotation을 Dotween으로 교체
        
        // Z축으로 90도만큼 '펀치'를 날렸다가 돌아옵니다.
        // punch: 펀치의 강도 (회전할 각도)
        // duration: 전체 애니메이션 시간
        // vibrato: 흔들림 횟수 (많을수록 더 덜렁거림)
        // elasticity: 탄성 (0~1 사이 값, 1에 가까울수록 더 많이 튕김)
        transform.DOPunchRotation(punch: DOTweenPunch, duration: DOTweenDuration, vibrato: DOTweenVibrato, elasticity: 0.5f);
        
    }

    public int CheckInputQueue(int slot)
    {
        return _inputQueue[slot];
    }

    public void PlayExplosion()
    {
        Debug.Log("게임 오버!");
        //GameManager.Instance.isGameOver = true;
        _animatior.Play("Explosion");
        arrow.SetActive(false);
        
        OnPlayerDied?.Invoke(); //플레이어가 죽었다는 방송을 내보낸다
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Destination"))
        {
            Debug.Log("게임을 클리어하셨습니다.");
            
            _animatior.Play("Clear");
            arrow.SetActive(false);

            StartCoroutine(StageClear(1.0f));
        }
    }

    IEnumerator StageClear(float time)
    {
        yield return new WaitForSeconds(time);
        
        OnStageCleared?.Invoke(); //게임이 클리어 됐다는 방송을 내보냄
    }

    private void OnDestroy()
    {
        // 유니티 에디터에서 랜덤으로 OnDestroy를 실행해서, 가끔 NullReferenceException 오류가 뜸
        if (GameManager.Instance)
        {
            //파괴시, StackManager의 연결 해제
            GameManager.Instance.UnregisterStackManager();
        } 
    }
}
