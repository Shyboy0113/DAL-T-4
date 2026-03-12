using System.Collections;
using UnityEngine;
using DG.Tweening;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool canKnowTrap = true; // true : 함정을 피함
    
    private bool _isDead = false;
    public bool IsDead => _isDead;

    [SerializeField] private Vector3 startPosition;
    [SerializeField] private BehaviourManager behaviourManager;
    
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    [SerializeField] private AudioClip explosionSound;

    [Header("Physics Stats")]
    [SerializeField] private float slideSpeed = 5f;

    [SerializeField] private MapManager mapManager;

    private Coroutine _slideCoroutine = null;
    private Vector2 _lastMoveDirection;
    
    private Animator _animator;
    private Collider2D _collider2D;
    private Rigidbody2D _rigidbody2D;


    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _collider2D = GetComponent<Collider2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // 맵 회전 시 플레이어와 적의 물리 로직을 활성화/비활성화 (여기선 적의 물리 상태만)
        GameEvents.BeforeMapRotated += FreezeEnemyPhysicalLogic;
        GameEvents.AfterMapRotated  += FreezeEnemyPhysicalLogic;
    }

    private void OnDisable()
    {
        // 맵 회전 시 플레이어와 적의 물리 로직을 활성화/비활성화 (여기선 적의 물리 상태만)
        GameEvents.BeforeMapRotated -= FreezeEnemyPhysicalLogic;
        GameEvents.AfterMapRotated  -= FreezeEnemyPhysicalLogic;
    }

    public void FreezeEnemyPhysicalLogic(bool freeze)
    {
        if(IsDead) return; // 적이 죽어있으면 무시

        if (freeze)
        {
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
        }

        _collider2D.enabled = !freeze;        
        _rigidbody2D.simulated = !freeze;

        if (!freeze) // 재활성화 됐을 시 물리 적용
        {
            Physics2D.SyncTransforms();
            Invoke(nameof(CheckForGround), 0.05f);
        }    
    }

    #region Ice & Slide Logic

    private bool _isOnIce = false;
    public bool IsOnIce() => _isOnIce;

    public void EnableIceMode(bool enable)
    {
        _isOnIce = enable;

        if (enable)
        {
            // 얼음 타일에 '닿자마자' 마지막 이동 방향으로 미끄러짐을 시작합니다
            if (_slideCoroutine == null)
            {
                _slideCoroutine = StartCoroutine(Slide(_lastMoveDirection));
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
            _rigidbody2D.velocity = Vector2.zero; // 타일 위에서 딱 멈추게 함
        }
    }
    
    private IEnumerator Slide(Vector2 direction)
    {
        while (_isOnIce)
        {
            Vector2 nextPos = _rigidbody2D.position + (direction * slideSpeed * Time.fixedDeltaTime);
            _rigidbody2D.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();

            Physics2D.SyncTransforms(); // 물리 엔진에 변경된 트랜스폼 정보를 즉시 반영

            CheckForGround(); // 타일을 벗어났는지 확인

            // 4. 게임 오버(폭발) 혹은 클리어 상태가 되면 미끄러짐 루프를 즉시 탈출

            if (GameManager.Instance.isGameOver || GameManager.Instance.isCleared || IsDead)
            {
                _rigidbody2D.velocity = Vector2.zero; // 물리적 움직임 완전 정지
                yield break; // 코루틴 종료
            }
        }
    }
    
    #endregion

    
    private void CheckForGround()
    {
        Physics2D.SyncTransforms();

        Transform activeMapRoot = mapManager.GetActiveMapRoot();

        if (activeMapRoot == null)
        {
            PlayExplosion();
            return;
        }

        bool hasGround = false;

        Collider2D[] hitColliders = Physics2D.OverlapPointAll(transform.position);

        foreach (var col in hitColliders)
        {
            // 반드시 activeMapRoot의 자식 타일인지 먼저 확인
            // 이전: IsChildOf 블록 바깥에서 hasGround=true를 설정해
            //        적/플레이어 등 다른 콜라이더에도 hasGround=true가 되던 버그 수정
            if (!col.transform.IsChildOf(activeMapRoot)) continue;

            if (col.TryGetComponent(out TileBehaviour tile))
            {
                if (IsTrap(tile))
                {
                    PlayExplosion();
                    return;
                }
            }

            hasGround = true;
            break;
        }

        if (!hasGround)
        {
            PlayExplosion();
        }
    }
    
    public void PlayExplosion()
    {
        // 이미 폭발 애니메이션이 재생 중이거나, 죽어있다면 무시
        if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Explosion") || IsDead) return;

        SetDeadState(true);
        
        // 효과음 실행
        soundEffectPlayer.PlaySoundEffect(explosionSound);

        ICommand deathCommand = new EnemyDeathCommand(this);
        
        behaviourManager.ExecuteCommand(deathCommand);

    }    

    public void Init()
    {
        _isDead = false;
        _isOnIce = false;
        
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = null;
        
        transform.position = startPosition;

        // 위치 초기화 후 물리 엔진에 즉시 반영
        // 이전: SyncTransforms 없어서 Retry 시 적이 타일 OnTriggerEnter를 받지 못하던 버그 수정
        Physics2D.SyncTransforms();

        SetDeadState(false);
    }

    public void SetDeadState(bool isDead)
    {
        _isDead = isDead;
        _collider2D.enabled = !isDead;
        
        if (isDead)
        {
            _rigidbody2D.velocity = Vector2.zero;
            _animator.Play("Explosion");
        }
        else
        {
            _animator.Play("Idle");
        }

    }
    
    public void TakeTurn(Vector3 playerPosition)
    {
        if (_isDead) return;
        
        // 1. AI 로직: playerPosition을 향해 이동할 Vector3 계산
        Vector3 nextPosition = CalculateMove(playerPosition);
        // 2. 적 이동 명령 생성 및 매니저를 통한 실행
        ICommand moveCommand = new EnemyMoveCommand(this, nextPosition);
        
        if (behaviourManager != null)
        {
            behaviourManager.ExecuteCommand(moveCommand);
        }
    }

    #region EnemyAI
    
    private Vector3 CalculateMove(Vector3 targetPosition)
    {
        Vector3Int startCoordination = Vector3Int.FloorToInt(transform.position);
        
        Vector3Int[] directions = {Vector3Int.up , Vector3Int.down , Vector3Int.left , Vector3Int.right };

        Vector3Int bestMove = startCoordination;
        float minDistance = Vector3.Distance(transform.position, targetPosition);

        foreach (var dir in directions)
        {
            Vector3Int nextCoordination = startCoordination + dir;
            
            // 이동 가능한 타일인지 체크
            if (IsWalkable(nextCoordination))
            {
                Vector3 nextPosition =
                    new Vector3(nextCoordination.x + 0.5f, nextCoordination.y + 0.5f, 0);
                
                float dist = Vector3.Distance(nextPosition, targetPosition);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestMove = nextCoordination;
                }
            }
        }
        return new Vector3(bestMove.x + 0.5f, bestMove.y + 0.5f , transform.position.z); 
    }

    
    // ICommand 중 MoveCommand를 위한 메서드
    public void MoveEnemy(Vector3 targetPosition)
    {
        Vector2 moveDirection = (targetPosition - transform.position).normalized;
        _lastMoveDirection = moveDirection; // 방향 기억

        //Ice타일 반영
        if (_isOnIce)
        {
            // 이미 얼음 위에서 다시 이동 명령을 내린 경우 (방향 전환 등), 기존 미끄러짐을 교체
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(Slide(moveDirection));
        }
        else
        {
            transform.DOMove(targetPosition, 0.25f).SetEase(Ease.OutBounce);
        }
    }

    private bool IsWalkable(Vector3Int cellPos)
    {
        Vector2 checkPosition = new Vector2(cellPos.x + 0.5f, cellPos.y + 0.5f);
        // 해당 위치의 모든 콜라이더를 체크
        Collider2D[] hits = Physics2D.OverlapPointAll(checkPosition);

        if (hits.Length == 0) return false; // 타일 자체가 없으면 (낭떠러지) 이동 불가

        bool hasGround = false;
        TileBehaviour targetTile = null;
        
        foreach (var hit in hits)
        {
            // 적 겹침 방지 - 이미 다른 적이 해당 칸에 서있는지 확인
            if (hit.TryGetComponent(out EnemyBehaviour otherEnemy))
            {

                // 자기 자신 제외, 죽은 적(시체)은 무시하고 통과
                if (otherEnemy != this && !otherEnemy.IsDead)
                {
                    // 다른 적이 길을 막고 있음
                    return false;
                }
            }
            
            // 발 밑에 타일이 존재하는지 확인
            if (hit.TryGetComponent(out TileBehaviour tile))
            {
                targetTile = tile;
                hasGround = true;
            }
        }
        
        // 낙사 체크 - 땅이 아예 없거나, 파괴돼서 사라진 타일인가?
        if (!hasGround) return false;
        
        // Breakable 타일의 경우, SpriteRenderer가 꺼져있을 경우 파괴된 것으로 간주
        SpriteRenderer sr = targetTile.GetComponentInChildren<SpriteRenderer>();

        if (sr != null && !sr.enabled) return false;
        
        // 함정을 피하는 AI 토글이 켜져있을 경우 (canKnowTrap)
        if (canKnowTrap)
        {
            if (IsTrap(targetTile))
            {
                return false;
            }
        }
        
        return true; // 갈 수 있는 타일
    }

    private bool IsTrap(TileBehaviour tile)
    {
        // TrapToggle: false(가시 등 함정 이미지)일 때 위험
        if (tile.currentTileType == TileType.TrapToggle)
        {
            return !tile.IsToggled; 
        }
    
        // 일반 토글 함정들: true(활성화)일 때 위험
        if (tile.currentTileType == TileType.ToggleTargeted ||
            tile.currentTileType == TileType.ActiveToggle ||
            tile.currentTileType == TileType.MoveToggle ||
            tile.currentTileType == TileType.RotationToggle)
        {
            return tile.IsToggled;
        }

        return false; // 그 외 일반 타일은 안전함
    }

    #endregion
    
}