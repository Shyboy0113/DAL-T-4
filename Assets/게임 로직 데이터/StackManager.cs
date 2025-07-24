using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//DoTween 사용
using DG.Tweening;

//TileMap 사용
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public enum KeyType
{
    None = 0,
    Alt = 1,
    F4 = 2,
    Tab = 3
}

public class StackManager : MonoBehaviour
{
    //전역 접근이 가능하도록 하는 이벤트
    public static event Action OnStageCleared; 
    public static event Action OnPlayerDied;
    public event Action OnInputQueueChanged; // SequenceUI의 Update 함수 비용 줄이기
    
    private int direction = 0;
    private int stack = 0;
    private List<int> inputQueue = new List<int> { 0, 0, 0 };

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

        }
        else
        {
            _colliderFirst.enabled = true;
            _colliderSecond.enabled = false;
            
            _activeTilemap = tilemapFirst;
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y,tilemapFirst.gameObject.transform.position.z - 10f);
        
            // Z 위치만 새로운 타일맵에 맞게 설정
            newPlayerPosition = new Vector3(transform.position.x, transform.position.y, tilemapFirst.gameObject.transform.position.z);
        }
        
        transform.position  = newPlayerPosition;
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
        if (stack >= 3) ResetQueue();
        HandleInput(1); // ALT 입력
        direction = (direction + 1) % 4;
        RotateArrow();

    }

    public void ProcessF4Input()
    {
        if (stack >= 3) ResetQueue();
        HandleInput(2); // F4 입력
        MovePlayer();
    }

    public void ProcessTabInput()
    {
        if (stack >= 3) ResetQueue();
        HandleInput(3); // Tab 입력
        
        direction = (direction + 3) % 4; // direction -1 + 4 = direction +3
        RotateArrow(); // 
        
    }

    void HandleInput(int keyCode)
    {
        inputQueue[stack] = keyCode;
        stack++;
        
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
        return (inputQueue[0] == (int)KeyType.Alt && inputQueue[1] == (int)KeyType.F4)
               || (inputQueue[1] == (int)KeyType.Alt && inputQueue[2] == (int)KeyType.F4);
    }
    
    bool CheckMapChange()
    {
        return (inputQueue[0] == (int)KeyType.Alt && inputQueue[1] == (int)KeyType.Tab && !_isSwitched)
               || (inputQueue[1] == (int)KeyType.Alt && inputQueue[2] == (int)KeyType.Tab && !_isSwitched);
    }

    void ResetQueue()
    {
        inputQueue = new List<int> { 0, 0, 0 };
        stack = 0;

        _isSwitched = false;
        
        //큐가 리셋 될 경우에도 발동
        OnInputQueueChanged?.Invoke();
    }

    void MovePlayer()
    {
        Vector2 moveDirection = direction switch
        {
            0 => Vector2.right,
            1 => Vector2.down,
            2 => Vector2.left,
            3 => Vector2.up,
            _ => Vector2.zero
        };
        Debug.Log(moveDirection + " 이동");
        _rigidbody2D.AddForce(moveDirection * forceAmount, ForceMode2D.Impulse);
    }

    void RotateArrow()
    {
        float angle = direction switch
        {
            0 => 0f,
            1 => 270f,
            2 => 180f,
            3 => 90f,
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
        return inputQueue[slot];
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
            
            OnStageCleared?.Invoke(); //게임이 클리어 됐다는 방송을 내보냄
        }
    }

}
