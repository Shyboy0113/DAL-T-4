using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//DoTween 사용
using DG.Tweening;

public enum KeyType
{
    None = 0,
    Alt = 1,
    F4 = 2,
    Tab = 3
}

public class StackManager : MonoBehaviour
{
    #region Enum
    
    //플레이어의 회전 방향을 Enum(문자)로 표시
    public enum PlayerDirection
    {
        Right,Down,Left,Up
    }
    
    #endregion
    
    #region Fields
    
    private PlayerDirection _playerDirection = PlayerDirection.Right;
    
    // UI 이벤트
    public event Action OnInputQueueChanged; // SequenceUI의 Update 함수 비용 줄이기
    
    private int _stack = 0;
    
    // 최대로 쌓을 수 있는 큐 스택을 상수로 선언
    private const int MaxQueueSize = 3;
    private List<int> _inputQueue = new List<int>(new int[MaxQueueSize]);

    private Rigidbody2D _rigidbody2D;
    
    [SerializeField] private float forceAmount = 1f;
    [SerializeField] private Animator _animatior;
    public GameObject arrow;
    
    //방향 전환 및 이동시 효과음 발동
    [SerializeField] private AudioClip rotateSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioClip triggerSound; // 2번째 맵에 발판이 있어 Alt + Tab 로직을 쓸 수 있는 경우
    [SerializeField] private AudioClip cancelSound; // Alt + Tab 트리거에서 벗어날 때

    private bool _isTriggerd = false;
    
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    
    //DOTween 전용 변동 속도
    public float DOTweenDuration;
    public Vector3 DOTweenPunch;
    public int DOTweenVibrato;
    
    // 파티클 시스템
    [SerializeField] private ParticleSystem particle; // 파티클 시스템
    
    // 회전중인지를 판단하는 bool 값
    private bool _isRotating = false;
    
    #endregion

    #region InputLock

    private bool _isInputLocked = false;
    
    private void SetInputLock(bool isLocked)
    {
        _isInputLocked = isLocked;
    }
    
    #endregion
    
    [SerializeField]
    private MapManager mapManager;
    
    [SerializeField]
    private CanvasGroup changePanelCanvasGroup;

    #region IceMode

    private float _slideSpeed = 15f;
    
    private Vector2 _lastMoveDirection; // 마지막으로 움직인 방향을 기록
    private bool _isOnIce = false;

    private Coroutine _slideCoroutine;

    public void EnableIceMode(bool enable)
    {
        _isOnIce = enable;

        if (enable)
        {
            // 얼음 타일에 '닿자마자' 마지막 이동 방향으로 미끄러짐을 시작합니다
            if (_slideCoroutine == null)
            {
                _slideCoroutine = StartCoroutine(Slide(_lastMoveDirection));
                SetInputLock(true); // 이동 중 입력 잠금
            }
        }
        else
        {
            // STOP 타일에 닿으면 미끄러짐을 즉시 멈추고 속도를 0으로 만듭니다
            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }
            _rigidbody2D.velocity = Vector2.zero; // 선단시티처럼 타일 위에서 딱 멈추게 함
            SetInputLock(false); // Stop에 닿았을 경우 입력 가능해짐
        }
    }

    
    private IEnumerator Slide(Vector2 direction)
    {
        while (_isOnIce)
        {
            _rigidbody2D.velocity = direction * _slideSpeed;
            yield return new WaitForFixedUpdate();
            
            // 타일을 벗어났는지 확인
            CheckForGround();

            // 4. 게임 오버(폭발) 혹은 클리어 상태가 되면 미끄러짐 루프를 즉시 탈출
            if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared)
            {
                _rigidbody2D.velocity = Vector2.zero; // 물리적 움직임 완전 정지
                yield break; // 코루틴 종료
            }
        }
    }
        
    private void UnlockInputAfterMove()
    {
        if (!_isOnIce) SetInputLock(false);
    }
    
    #endregion
    
    #region Command Pattern

    //ICommand 인터페이스를 상속받은 커맨드를 전부 모아놓은 버퍼
    private Queue<ICommand> _commandBuffer = new Queue<ICommand>();

    #endregion

    #region Lifecycle
    
    private void Awake()
    {
        // GameManager에 자신을 등록합니다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterStackManager(this);
        }
        else
        {
            Debug.LogError("GameManager instance isn't registered!");
        }
        
        _animatior = GetComponent<Animator>();
        _rigidbody2D = GetComponent<Rigidbody2D>();

        soundEffectPlayer = GetComponent<SoundEffectPlayer>();
        
    }

    private void OnEnable()
    {
        //이벤트 추가
        GameEvents.PlayerDied += StopParticle;
        GameEvents.StageCleared += StopParticle;
        GameEvents.InputLockChanged += SetInputLock;
    }

    private void Start()
    {
        // 파티클 일단 끄기
        particle.Stop();
    
        // 트리거도 false
        _isTriggerd = false;
    
        // 게임 시작 시 CanvasGroup의 알파값을 0으로, 비활성화 상태로 만듭니다.
        changePanelCanvasGroup.alpha = 0;
        changePanelCanvasGroup.interactable = false; // 클릭 등 상호작용 비활성화
        changePanelCanvasGroup.blocksRaycasts = false; // UI 뒤의 오브젝트가 클릭되는 것을 막지 않음
        
    }
    
    private void Update()
    {
        // 회전 애니메이션 작동중일 땐 스킵
        if (_isRotating || _isInputLocked) return;
        
        // 게임이 진행 중일 때만 입력을 받도록 GameManager 상태를 확인합니다.
        if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared) return;

        // 1. 입력 수집 (Producer)
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            _commandBuffer.Enqueue(new ClockwiseRotateCommand(this));
            soundEffectPlayer.PlaySoundEffect(rotateSound);
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _commandBuffer.Enqueue(new CounterClockwiseRotateCommand(this));
            soundEffectPlayer.PlaySoundEffect(rotateSound);
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            _commandBuffer.Enqueue(new MoveCommand(this));
            soundEffectPlayer.PlaySoundEffect(moveSound);
        }

        // 2. 명령 실행 (Consumer)
        // 예: 큐에 명령이 있고, 현재 애니메이션 중이 아닐 때만 하나씩 꺼내서 실행
        if (_commandBuffer.Count > 0 && !_isRotating && !_isInputLocked)
        {
            ICommand cmd = _commandBuffer.Dequeue();
            cmd.Execute();
        }
    
        // 3. Alt + Tab 트리거 체크
        if (CheckBackTile()) 
        {
            // 파티클의 Main 모듈을 가져옴
            var main = particle.main;
            
            if ( mapManager.IsFirstRoot()) main.startColor = new Color(144/255f,57/255f,205/255f); //보라색
            else main.startColor = Color.white;

            // 3. 파티클이 재생 중이 아닐 때만 Play 호출
            if (!particle.isPlaying)
            {
                // 트리거 됐을 경우, 효과음 재생
                if(!_isTriggerd) soundEffectPlayer.PlaySoundEffect(triggerSound);
                
                particle.Play();
                _isTriggerd = true;
            }
        }
        else
        {
            // 파티클이 현재 멈춰있지 않고, 트리거 상태였다면 Stop()을 호출합니다.
            if (!particle.isStopped && _isTriggerd)
            {
                //트리거 취소 효과음 재생
                soundEffectPlayer.PlaySoundEffect(cancelSound);
                
                particle.Stop();
                _isTriggerd = false;
            }
        }
        
    }
    
    private void OnDisable()
    {
        //이벤트 추가
        GameEvents.PlayerDied -= StopParticle;
        GameEvents.StageCleared -= StopParticle;
        GameEvents.InputLockChanged -= SetInputLock;
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
    
    #endregion

    public bool CheckBackTile()
    {
        Transform inactiveMapRoot = mapManager.GetInactiveMapRoot();
        if (inactiveMapRoot == null) return false;

        // 플레이어 위치(transform.position)에 겹쳐 있는 모든 2D 충돌체 감지
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(transform.position);

        foreach (var col in hitColliders)
        {
            // 감지된 충돌체가 '비활성화된 맵(백타일)'의 자식인지 확인
            // 이 방식은 Z축이 달라도, 계층 구조가 깊어도 정확히 찾아냅니다.
            if (col.transform.IsChildOf(inactiveMapRoot))
            {
                return true;
            }
        }
        return false;
    }
    
    private void CheckForGround()
    {
        // 중요: 물리 엔진에 변경된 트랜스폼 정보를 즉시 반영
        Physics2D.SyncTransforms();
        
        Transform activeMapRoot = mapManager.GetActiveMapRoot();

        if (activeMapRoot == null)
        {
            Debug.Log("No active map root found!");
            PlayExplosion();
            return;
        }

        bool hasGround = false;

        // 1. 현재 내 발밑(위치)에 있는 모든 2D 콜라이더를 가져옵니다.
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(transform.position);

        foreach (var col in hitColliders)
        {
            // 2. 이 콜라이더가 '현재 활성화된 맵 루트'의 자식인지 확인합니다.
            // 이 방식은 계층 구조가 아무리 깊어도(Grid 안에 있어도) 찾아냅니다.
            if (col.transform.IsChildOf(activeMapRoot))
            {
                hasGround = true;
                break;
            }
        }

        if (!hasGround)
        {
            Debug.Log("No Ground! Exploding...");
            PlayExplosion();
        }
    }

    IEnumerator FadeSwitchPanel()
    {
        // 1. CanvasGroup의 알파값을 즉시 1로 만들어 전체를 보이게 함
        changePanelCanvasGroup.alpha = 1f;

        // 0.5초 대기
        //yield return new WaitForSeconds(0.5f);
    
        // 2. CanvasGroup에 DOFade를 한 번만 호출하여 전체를 페이드 아웃
        changePanelCanvasGroup.DOFade(0f, 1.0f);
    
        // 1초 대기 (페이드 아웃 완료까지)
        //yield return new WaitForSeconds(1.0f);

        // 3. 필요하다면 여기서 상호작용을 막을 수 있습니다.
        // (어차피 알파값이 0이라 보이지 않으므로 필수는 아님)
        changePanelCanvasGroup.interactable = false;
        changePanelCanvasGroup.blocksRaycasts = false;

        // 입력 가능하게
        GameEvents.RaiseInputLockChanged(false);

        yield return null;

    }

      public void HandleInput(KeyType keyType)
    {
        
        // 1. 여기서 스택을 먼저 체크하고 가득 찼으면 리셋합니다.
        if (_stack >= MaxQueueSize) ResetQueue();
        
        _inputQueue[_stack] = (int)keyType;
        _stack++;
        
        //이벤트 발생
        OnInputQueueChanged?.Invoke();

        if (CheckGameOver())
        {
            gameObject.GetComponent<Collider2D>().enabled = false;

            PlayExplosion();
            
        }
        else if (CheckMapChange())
        {
            // 맵 전환
            GameEvents.RaiseTileMapChanged();

            //_isTriggerd = false;
            
            // Alt + Tab 로직을 작동했을 경우, 큐를 초기화해줘야함
            ResetQueue();
            
            // 맵 전환 직후 아주 미세한 지연 후 바닥 체크 (물리 엔진 갱신 대기)
            Invoke(nameof(CheckForGround), 0.02f);
            
            // 입력 막기
            GameEvents.RaiseInputLockChanged(true);
            
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
        return (_inputQueue[0] == (int)KeyType.Alt && _inputQueue[1] == (int)KeyType.Tab)
               || (_inputQueue[1] == (int)KeyType.Alt && _inputQueue[2] == (int)KeyType.Tab);
    }

    void ResetQueue()
    {
        _inputQueue = new List<int>(new int[MaxQueueSize]);
        _stack = 0;
        
        //큐가 리셋 될 경우에도 발동
        OnInputQueueChanged?.Invoke();
    }

    public void UpdateDirection(int rotation)
    {
        _playerDirection = (PlayerDirection)(((int)_playerDirection + rotation + 4) % 4);
    }

    // ICommand 중 MoveCommand를 위한 메서드
    public void MovePlayer()
    {
        Vector2 moveDirection = _playerDirection switch
        {
            PlayerDirection.Right => Vector2.right,
            PlayerDirection.Down => Vector2.down,
            PlayerDirection.Left => Vector2.left,
            PlayerDirection.Up => Vector2.up,
            _ => Vector2.zero
        };
        
        _lastMoveDirection = moveDirection; // 방향 기억
        
        //Ice타일 반영
        if (_isOnIce)
        {   
            // 이미 얼음 위에서 다시 이동 명령을 내린 경우 (방향 전환 등), 기존 미끄러짐을 교체
            if(_slideCoroutine !=null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(Slide(moveDirection));
            SetInputLock(true);
        }
        else
        {
            _rigidbody2D.AddForce(moveDirection * forceAmount, ForceMode2D.Impulse);
            SetInputLock(true);
            Invoke(nameof(UnlockInputAfterMove),0.2f);
        }
        
        //맵 밖으로 벗어났는지 체크
        Invoke(nameof(CheckForGround), 0.1f);
        
    }

    // ICommand 중 ClockwiseRotateCommand/CounterClockwiseRotateCommand를 위한 메서드
    public void RotateArrow(bool immediate = false)
    {
        float targetAngle = _playerDirection switch
        {
            PlayerDirection.Right => 0f,
            PlayerDirection.Down => 270f,
            PlayerDirection.Left => 180f,
            PlayerDirection.Up => 90f,
            _ => 0f
        };

        if (immediate)
        {
            // 즉시 회전 (맵 회전 직후용)
            arrow.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
        else
        {
            // 기존 애니메이션 방식
            _isRotating = true;
            arrow.transform
                .DORotate(new Vector3(0, 0, targetAngle), DOTweenDuration)
                .SetEase(Ease.OutElastic)
                .OnComplete(() => { _isRotating = false; });
            
            transform.DOPunchRotation(DOTweenPunch, DOTweenDuration, DOTweenVibrato, 0.5f);
        }
    }

    public int CheckInputQueue(int slot)
    {
        return _inputQueue[slot];
    }

    public void PlayExplosion()
    {
        // 이미 폭발 애니메이션이 재생 중이라면 중복 실행 방지
        if (_animatior.GetCurrentAnimatorStateInfo(0).IsName("Explosion")) return;
        
        _rigidbody2D.velocity = Vector2.zero;
        
        _animatior.Play("Explosion");
        arrow.SetActive(false);
        
        // 효과음 실행
        soundEffectPlayer.PlaySoundEffect(explosionSound);
        
        GameEvents.RaisePlayerDied(); //플레이어가 죽었다는 방송을 내보낸다
    }

    public void ReachedDestination()
    {
        if (GameManager.Instance.isCleared) return; // 중복 실행 방지
        
        _animatior.Play("Clear");
        arrow.SetActive(false);
            
        GameManager.Instance.isCleared = true;
        
        // 1초 뒤 코루틴 실행
        StartCoroutine(StageClear(1.0f));
    }

    private void StopParticle()
    {
        // 파티클이 재생 중일 경우
        if (particle.isPlaying)
        {
            // StopEmittingAndClear 옵션을 사용해
            // 새로운 파티클 생성을 막고, 기존 파티클도 즉시 제거합니다.
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        _isTriggerd = false;
    }

    IEnumerator StageClear(float time) // 목적지에 닿자마자 곧바로 Clear!이란 UI 텍스트가 출력되지 않도록 하는 코루틴
    {
        yield return new WaitForSeconds(time);
        
        GameEvents.RaiseStageCleared(); //게임이 클리어 됐다는 방송을 내보냄
    }
    
    public void FreezePlayerPhysics(bool freeze)
    {
        _rigidbody2D.velocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
        _rigidbody2D.simulated = !freeze;
    }

    public bool IsRotating()
    {
        return _isRotating;
    }


}
