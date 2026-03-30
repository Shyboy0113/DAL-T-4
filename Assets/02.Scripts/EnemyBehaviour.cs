using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.VisualScripting;

public class EnemyBehaviour : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private bool canKnowTrap = true; // true : 함정을 피함
    
    private bool _isDead = false;
    public bool IsDead => _isDead;

    private Vector3 startPosition;
    [SerializeField] private BehaviourManager behaviourManager;
    
    [SerializeField] private SoundEffectPlayer soundEffectPlayer;
    [SerializeField] private AudioClip explosionSound;

    [Header("Physics Stats")]
    [SerializeField] private float slideSpeed = 5f;

    [SerializeField] private MapManager mapManager;

    private Coroutine _slideCoroutine = null;
    private Vector2 _lastMoveDirection;
    
    [SerializeField] private Animator animator;
    private Collider2D _collider2D;
    private Rigidbody2D _rigidbody2D;
    private SpriteRenderer _spriteRenderer;
    
    [SerializeField] private SpriteRenderer iconSpriteRenderer;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        _collider2D = GetComponent<Collider2D>();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        GameEvents.BeforeMapRotated  += FreezeEnemyPhysicalLogic;
        GameEvents.AfterMapRotated   += FreezeEnemyPhysicalLogic;
        GameEvents.ChatCommandDance  += OnChatDance;
        GameEvents.ChatCommandLove   += OnChatLove;
    }

    private void OnDisable()
    {
        GameEvents.BeforeMapRotated  -= FreezeEnemyPhysicalLogic;
        GameEvents.AfterMapRotated   -= FreezeEnemyPhysicalLogic;
        GameEvents.ChatCommandDance  -= OnChatDance;
        GameEvents.ChatCommandLove   -= OnChatLove;
    }

    private void OnChatDance()
    {
        if (IsDead) return;
        animator.Play("Dance");
    }

    private void OnChatLove()
    {
        if (IsDead) return;
        animator.Play("Love");
    }

    public void FreezeEnemyPhysicalLogic(bool freeze)
    {
        if(IsDead) return; // 적이 죽어있으면 무시

        if (freeze)
        {
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.angularVelocity = 0f;
            _collider2D.enabled = false;
            _rigidbody2D.simulated = false;
        }

        if (!freeze) // 재활성화 됐을 시 물리 적용
        {
            Physics2D.SyncTransforms();
            
            Invoke(nameof(EnablePhysicsLogic),0.05f);
            Invoke(nameof(CheckForGround), 0.05f);
        }    
    }

    private void EnablePhysicsLogic()
    {
        if (!IsDead) _collider2D.enabled = true;
    }

    #region Ice & Slide Logic

    [SerializeField] private bool _isOnIce = false;
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
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Explosion") || IsDead) return;

        SetDeadState(true);
        GameEvents.RaiseEnemyDied();

        // 효과음 실행
        soundEffectPlayer.PlaySoundEffect(explosionSound);

        ICommand deathCommand = new EnemyDeathCommand(this);
        
        behaviourManager.ExecuteCommand(deathCommand);

    }

    private IEnumerator IDisableSprite()
    {
        yield return new WaitForSeconds(0.75f);

        if(_spriteRenderer != null) _spriteRenderer.enabled = false;
    }

    public void SetStartPosition(Vector3 pos)
    {
        startPosition = pos;
    }

    public void Init()
    {
        _isDead = false;
        _isOnIce = false;
        
        if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
        _slideCoroutine = null;
        
        _rigidbody2D.velocity = Vector2.zero;
        _rigidbody2D.simulated = true;

        _collider2D.enabled = true;
        _spriteRenderer.enabled = true;
        
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
            _isOnIce = false;
            
            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }
            
            _rigidbody2D.velocity = Vector2.zero;
            _rigidbody2D.simulated = false;
            _collider2D.enabled = false;
            
            if (iconSpriteRenderer != null)
                iconSpriteRenderer.enabled = false; // 죽으면 Icon 비활성화
            
            animator.Play("Explosion");
            
            StartCoroutine(IDisableSprite());
            
        }
        else
        {
            _rigidbody2D.simulated = true;
            _collider2D.enabled = true;
            _spriteRenderer.enabled = true;
            
            if (iconSpriteRenderer != null)
                iconSpriteRenderer.enabled = true; // 살아나면 Icon 활성화
            
            animator.Play("Idle");
            
            
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
    // 1. Tilemap 또는 로컬 그리드 좌표를 사용하는 것이 가장 안전합니다.
    // 여기서는 기존 방식을 유지하되, 목적지 도달 가능성을 높이는 로직을 추가합니다.
    Vector3Int startCoord = Vector3Int.RoundToInt(new Vector3(transform.position.x - 0.5f, transform.position.y - 0.5f, 0));
    Vector3Int targetCoord = Vector3Int.RoundToInt(new Vector3(targetPosition.x - 0.5f, targetPosition.y - 0.5f, 0));

    if (startCoord == targetCoord) return transform.position;

    Queue<Vector3Int> queue = new Queue<Vector3Int>();
    Dictionary<Vector3Int, Vector3Int> parentMap = new Dictionary<Vector3Int, Vector3Int>();

    queue.Enqueue(startCoord);
    parentMap[startCoord] = startCoord;

    bool found = false;
    int maxSearchSteps = 200;
    int steps = 0;

    while (queue.Count > 0 && steps < maxSearchSteps)
    {
        Vector3Int current = queue.Dequeue();
        steps++;

        if (current == targetCoord)
        {
            found = true;
            break;
        }

        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (var dir in directions)
        {
            Vector3Int next = current + dir;

            if (!parentMap.ContainsKey(next))
            {
                // 수정: 목적지(플레이어 칸)라면 함정이라도 경로에 포함시킵니다.
                bool isTarget = (next == targetCoord);
                if (isTarget || IsWalkable(next))
                {
                    parentMap[next] = current;
                    queue.Enqueue(next);
                }
            }
        }
    }

    if (found)
    {
        Vector3Int nextStep = targetCoord;
        while (parentMap[nextStep] != startCoord)
        {
            nextStep = parentMap[nextStep];
        }
        
        // 회전된 맵에서도 정확한 위치를 찾기 위해, 
        // 0.5를 더한 '상대적 위치'를 현재 부모(MapRoot)의 로컬 좌표에 맞춰 변환하는 것이 좋습니다.
        return new Vector3(nextStep.x + 0.5f, nextStep.y + 0.5f, transform.position.z);
    }

    // 경로를 못 찾았을 때의 보험: 플레이어 방향으로 한 칸이라도 가려고 시도 (기존 Greedy 방식)
    return CalculateGreedyMove(targetPosition); 
}
    
    private Vector3 CalculateGreedyMove(Vector3 targetPosition)
    {
        //Vector3Int startCoordination = Vector3Int.FloorToInt(transform.position);
        Vector3Int startCoordination = Vector3Int.RoundToInt(
            new Vector3(transform.position.x - 0.5f, transform.position.y - 0.5f, 0));
        
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