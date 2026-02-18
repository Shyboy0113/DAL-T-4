using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public enum TileType
{
    //일반 로직
    None, //아무것도 적용 안 돼 있음
    Trap, //닿으면 사망
    
    //회전 로직
    QuarterClockwiseRotation, //맵이 시계 방향으로 90도 회전
    HalfClockRotation, //맵이 시계 방향으로 180도 회전
    QuarterCounterClockwiseRotation, //맵이 반시계 방향으로 90도 회전
    HalfCounterClockRotation, //맵이 반시계 방향으로 180도 회전
    
    //이동 로직
    Ice, //플레이어가 이동할 경우, 화살표 방향 기준으로 미끄러짐
    Stop, //Ice 상태에서, 플레이어가 타일에 도착할 경우 이동이 멈춤
    
    StartTeleport, //텔레포트 출발지점
    EndTeleport, //텔레포트 도착지점
    
    Breakable, //파괴될 수 있음
    
    FirstDestination,
    SecondDestination
}

public class TileBehaviour : BaseTile
{
    [Header("Tile Settings")]
    [SerializeField] private TileType tileType;
    
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] tileSprites;

    #region Activation Count
    [Header("Activation Settings")]
    [SerializeField] private int maxActivationCount = -1; // -1 : 무제한
    private int _currentActivationCount = 0; // 0부터 시작하도록 수정
    
    private bool CanActivate()
    {
        if (maxActivationCount < 0) return true;
        return _currentActivationCount < maxActivationCount;
    }
    #endregion

    // 무한 회전을 막기 위한 상태 변수
    private bool _isWaitExit = false; 

    [Header("Rotation Settings")]
    [SerializeField] private float customRotationAngle = 90f;

    [Header("SFX Settings")]
    private AudioSource _effectSound;
    [SerializeField] private AudioClip rotationSound; // 회전 효과음
    [SerializeField] private AudioClip crackSound;      // Breakable 타일 밟을 때 (콰직)
    [SerializeField] private AudioClip breakSound;      // 타일이 완전히 파괴될 때
    
    [Header("Breakable Settings")]
    [SerializeField] private Sprite[] breakableSprites; // 파괴 단계별 스프라이트
    [SerializeField] private int breakHitCount = 2;
    [SerializeField] private float breakDelay = 0.5f;
    private int _currentHit = 0;

    [Header("Teleport")]
    [SerializeField] private TileBehaviour teleportTarget;

    private Tilemap _tilemap;
    
    private void Awake()
    {
        _tilemap = GetComponentInParent<Tilemap>();
        _effectSound = GetComponentInParent<AudioSource>();
        
        UpdateSprite();
    }

    private void OnValidate()
    {
        UpdateSprite();
    }
    
// 스프라이트 업데이트 로직
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        // Breakable 타일일 경우 전용 배열에서 _currentHit에 맞는 스프라이트 선택
        if (tileType == TileType.Breakable && breakableSprites != null && breakableSprites.Length > 0)
        {
            int spriteIndex = Mathf.Clamp(_currentHit, 0, breakableSprites.Length - 1);
            spriteRenderer.sprite = breakableSprites[spriteIndex];
        }
        else if (tileSprites != null && (int)tileType < tileSprites.Length)
        {
            spriteRenderer.sprite = tileSprites[(int)tileType];
        }

        if (tileType == TileType.StartTeleport)
        {
            teleportTarget = GameObject.FindObjectOfType<TileBehaviour>(); //씬에 존재하는 타일 중에서 EndTeleport 타입을 찾아서 할당
        }
        else
        {
            teleportTarget = null; // StartTeleport가 아닐 경우, 타겟 초기화
        }

    }

    // 플레이어가 타일을 나갈 때 상태 리셋
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //회전중일 때는 무시
            StackManager player = other.GetComponent<StackManager>();
            if (player != null && player.IsRotating()) return;
            
            _isWaitExit = false; // 타일을 나갔으므로 다시 작동 가능하게 리셋
            
            //플레이어가 타일을 벗어났을 때, 타격 횟수가 충족되었다면 파괴 시작
            if (tileType == TileType.Breakable && _currentHit >= breakHitCount)
            {
                StartCoroutine(BreakTile());
            }
        }
    }

    protected override void OnPlayerEnter(StackManager player)
    {
        // 1. 활성화 횟수 체크
        if (!CanActivate()) return;
        
        // 2. 무한 회전 방지: 아직 이 타일을 나가지 않았다면 로직 실행 안 함
        if (_isWaitExit) return;
        
        _currentActivationCount++;

        // 3. 위치 교정 (Snapping): 회전 타일이거나 멈춰야 하는 타일일 때 정중앙으로 강제 고정
        if (IsRotationTile() || tileType == TileType.Ice || tileType == TileType.Stop)
        {
            // 물리 엔진의 오차를 없애기 위해 플레이어 위치를 타일 중앙으로 스냅
            player.transform.position = new Vector3(transform.position.x, transform.position.y, player.transform.position.z);
        }

        switch (tileType)
        {
            case TileType.None:
                break;

            case TileType.Trap:
                player.PlayExplosion();
                break;

            #region Rotation Logic
            case TileType.QuarterClockwiseRotation:
                _isWaitExit = true; // 회전 시작 전 중복 방지 설정
                RotateTile(-90f);
                if (rotationSound != null) _effectSound.PlayOneShot(rotationSound);
                break;

            case TileType.HalfClockRotation:
                _isWaitExit = true;
                RotateTile(-180f);
                if (rotationSound != null) _effectSound.PlayOneShot(rotationSound);
                break;

            case TileType.QuarterCounterClockwiseRotation:
                _isWaitExit = true;
                RotateTile(90f);
                if (rotationSound != null) _effectSound.PlayOneShot(rotationSound);
                break;

            case TileType.HalfCounterClockRotation:
                _isWaitExit = true;
                RotateTile(180f);
                if (rotationSound != null) _effectSound.PlayOneShot(rotationSound);
                break;
            #endregion

            case TileType.StartTeleport:
                if (teleportTarget != null)
                {
                    player.FreezePlayerPhysics(true);
                    player.transform.position = teleportTarget.transform.position;
                    player.FreezePlayerPhysics(false);
                }
                break;
            case TileType.EndTeleport: //별도로 구현할 로직은 없는데, 미관상 추가함
                break;
            case TileType.Breakable:
                _currentHit++;
                UpdateSprite(); // 밟을 때마다 스프라이트 갱신
                if (crackSound != null) _effectSound.PlayOneShot(crackSound); // 콰직 소리
                break;
                
            case TileType.Ice:
                player.EnableIceMode(true);
                break;

            case TileType.Stop:
                player.EnableIceMode(false);
                break;
            case TileType.FirstDestination:
                player.ReachedDestination();
                break;
            case TileType.SecondDestination:
                player.ReachedDestination();
                break;
        }
    }

    // 현재 타일이 회전 관련 타일인지 판별
    private bool IsRotationTile()
    {
        return tileType == TileType.QuarterClockwiseRotation ||
               tileType == TileType.HalfClockRotation ||
               tileType == TileType.QuarterCounterClockwiseRotation ||
               tileType == TileType.HalfCounterClockRotation;
    }

    private void RotateTile(float angle)
    {
        if (_tilemap == null) return;
        Vector3Int cell = _tilemap.WorldToCell(transform.position);
        
        GameEvents.RaiseTileMapRotated(cell, angle);
    }

    private IEnumerator BreakTile()
    {
        yield return new WaitForSeconds(breakDelay);

        if (breakSound != null) _effectSound.PlayOneShot(breakSound);
        
        if (_tilemap != null)
        {
            Vector3Int cell = _tilemap.WorldToCell(transform.position);
            _tilemap.SetTile(cell, null);
        }
        Destroy(gameObject);
    }
}
