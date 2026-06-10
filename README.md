# DAL-T-4 (달-T-4)

> **"Alt+F4로 죽고, Alt+Tab으로 세계를 넘나드는 퍼즐 게임"**

Unity 기반 1인 개발 퍼즐 게임입니다.  
PC 키보드의 단축키 조작 체계를 게임 메커닉으로 재해석하여, `F4`, `ALT`, `TAB` 세 키만으로 이동·회전·맵 전환을 수행합니다.  
4개 챕터 × 15스테이지 = **총 60개 스테이지**, Steam 출시를 목표로 개발되었습니다.

---

## 목차

- [게임 개요](#게임-개요)
- [개발 정보](#개발-정보)
- [핵심 조작 체계](#핵심-조작-체계)
- [아키텍처 및 설계 패턴](#아키텍처-및-설계-패턴)
- [타일 시스템](#타일-시스템)
- [턴 시스템](#턴-시스템)
- [Undo 시스템](#undo-시스템)
- [듀얼 맵 시스템 (챕터 4)](#듀얼-맵-시스템-챕터-4)
- [스테이지 미션 시스템](#스테이지-미션-시스템)
- [Steam 연동](#steam-연동)
- [전시 분석 시스템 (PlayX4 2026)](#전시-분석-시스템-playx4-2026)
- [스트리밍 채팅 연동](#스트리밍-채팅-연동)
- [기술 스택 및 외부 패키지](#기술-스택-및-외부-패키지)
- [프로젝트 구조](#프로젝트-구조)

---

## 게임 개요

| 항목 | 내용 |
|------|------|
| 장르 | 2D 퍼즐 (턴제) |
| 엔진 | Unity 6.000.3.12f1 |
| 플랫폼 | Windows PC (Steam) |
| 조작 키 | `F4` (이동) · `Left ALT` (시계방향 회전) · `TAB` (반시계방향 회전) |
| 핵심 개념 | 단축키 남용 → 퍼즐 메커닉으로 전환 |

플레이어는 화살표 방향으로 이동(`F4`)하고, 방향을 회전시킵니다(`ALT`/`TAB`).  
`ALT + F4`를 입력하면 **자살(게임 오버)** 이 되며, 챕터 4에서는 `ALT + TAB`으로 **두 번째 맵으로 전환**할 수 있습니다.  

또한, 25종류의 다양한 타일이 존재하여, 각 챕터마다 색다른 경험을 얻으실 수 있습니다.

---

## 개발 정보

| 항목 | 내용 |
|------|------|
| 개발 기간 | 2023년 1월 ~ 2026년 6월 (약 3년 6개월) |
| 개발 인원 | 1인 (솔로 개발) |
| 커밋 수 | 410+ commits |
| 총 스테이지 | 4챕터 × 15스테이지 = 60스테이지 |
| Steam App ID | 4624670 |

### 개발 마일스톤

- **2023**: 프로젝트 초기 설계 및 기본 타일·이동 시스템 구현
- **2024**: 챕터 1~2 스테이지 제작, Command Pattern 기반 Undo 시스템 완성
- **2025**: 챕터 3~4 스테이지 제작, 듀얼 맵·색상 토글·Steam 연동 구현
- **2026**: Steam 업적·미션 시스템·클리어 연출·PlayX4 전시 대응

---

## 핵심 조작 체계

```
[ F4 ]       → 플레이어가 바라보는 방향으로 한 칸 이동
[ Left ALT ] → 방향을 시계방향으로 90° 회전
[ TAB ]      → 방향을 반시계방향으로 90° 회전

[ ALT + F4 ] → 자살 (게임 오버 유도 메커닉)
[ ALT + TAB ]→ 맵 전환 (챕터 4, 듀얼 맵 스테이지에서만 활성)

[ Ctrl+Z ]   → Undo (직전 행동 되돌리기)
[ R ]        → 스테이지 재시작
[ ESC ]      → 일시정지 메뉴
[ H ]        → 조작법 패널 토글
[ M ]        → 미션 패널 토글
[ / ]        → 채팅 입력창 열기 (스트리밍 연동)
```

### 입력 큐 (Sequence UI)

플레이어의 입력은 3슬롯 링 버퍼(`_inputQueue`)로 관리됩니다.  
3번의 행동이 모일 때마다 **적의 턴**이 발동되며, 화면 상단 UI에 현재 입력 순서가 시각적으로 표시됩니다.

```
슬롯: [ ALT ][ F4 ][ TAB ] → 3번째 입력 완료 시 적 이동
```

각 스테이지는 키별 사용 가능 여부(`canUseLeftALT`, `canUseF4`, `canUseTAB`)와  
사용 횟수 제한(`limitNumberALT`, `limitNumberF4`, `limitNumberTAB`)을 ScriptableObject로 독립 설정합니다.

---

## 아키텍처 및 설계 패턴

### 1. Command Pattern (커맨드 패턴)

모든 플레이어·적·타일 상태 변경은 `ICommand` 인터페이스로 추상화되어 있습니다.  
이로 인해 **무제한 Undo** 기능과 **턴 기반 재현** 이 가능합니다.

```csharp
public interface ICommand
{
    void Execute();
    void Undo();
}
```

구현된 커맨드 목록:

| 커맨드 클래스 | 역할 |
|---------------|------|
| `MoveCommand` | 플레이어 이동 (위치 스냅샷 저장) |
| `ClockwiseRotateCommand` | 시계방향 회전 |
| `CounterClockwiseRotateCommand` | 반시계방향 회전 |
| `SuicideCommand` | ALT+F4 자살 |
| `EnemyMoveCommand` | 적 이동 |
| `EnemyDeathCommand` | 적 사망 |
| `TileCommand` | 타일 효과 발동 (타일 상태 스냅샷 저장) |
| `TileBreakCommand` | 파괴 타일 제거 |
| `TileMapChangeCommand` | 맵 전환 (ALT+TAB) |

모든 커맨드는 `BehaviourManager`의 `CommandHistory` 스택에 누적되며,  
Undo 시에는 플레이어 커맨드 단위로 팝하고 관련 타일/적 상태를 함께 복원합니다.

### 2. Event Bus Pattern (옵저버 패턴)

`GameEvents` 정적 클래스가 전역 이벤트 버스 역할을 합니다.  
컴포넌트 간 직접 참조 없이 느슨하게 결합된 통신을 제공합니다.

```csharp
// 발행 예시
GameEvents.RaiseTileLogicTurnStarted();
GameEvents.RaisePlayerMoved(map1MoveCount, LayerMask.NameToLayer("Map 1"));

// 구독 예시 (TileBehaviour)
GameEvents.PlayerMoved += HandleToggle;       // MoveToggle 카운터 판정
GameEvents.PlayerActed += HandleToggle;       // ActiveToggle 카운터 판정
GameEvents.ColorToggleTriggered += HandleColorToggle;
```

이벤트 카테고리:

| 카테고리 | 주요 이벤트 |
|----------|-------------|
| 게임 상태 | `StageCleared`, `PlayerDied`, `EnemyDied` |
| 턴 흐름 | `TileLogicTurnStarted`, `EnemyTurnStarted`, `PlayerActionFinished` |
| 플레이어 카운터 | `PlayerMoved`, `PlayerRotated`, `PlayerActed` |
| Undo | `UndoTriggered`, `PreActionStateSaveRequested` |
| 타일맵 | `TileMapChanged`, `TileMapRotated`, `MapRotationCompleted` |
| 채팅 커맨드 | `ChatCommandSuicide`, `ChatCommandMove`, `ChatCommandRotateCW` |
| 미션 추적 | `StarCollected`, `KeyUsed`, `MapSwitched` |

### 3. Singleton Pattern

씬 간 영속 오브젝트에 제네릭 싱글턴이 적용되어 있습니다.

```csharp
// GameManager, SoundManager, DevelopmentManager 등
public class GameManager : Singleton<GameManager> { ... }
```

### 4. ScriptableObject 기반 데이터 분리

스테이지 설계 데이터는 모두 `SO_StageData`로 분리되어, 코드 변경 없이 에디터에서 조작 가능합니다.

`SO_StageData` 주요 필드:
- 스테이지 프리팹 레퍼런스
- 키 사용 허용 여부 및 횟수 제한
- 듀얼 맵 여부 (`hasSecondMap`)
- 챕터별 BGM 클립
- 미션 타입 및 조건 (3개 미션)
- 텔레포트 아이스 모드 지속 여부

### 5. Snapshot 기반 상태 복원

타일과 플레이어 상태는 커맨드 실행 직전 `TileStateSnapshot` 구조체로 스냅샷됩니다.

```csharp
public struct TileStateSnapshot
{
    public int   hitCount;
    public bool  isToggled;
    public bool  playerIsMap1;
    public int   playerLayer;
    public int   playerMap1MoveCount, playerMap1RotationCount, playerMap1ActionCount;
    public int   playerMap2MoveCount, playerMap2RotationCount, playerMap2ActionCount;
    public int   totalActionCount;
    public Quaternion rotation;
    public bool  isVisible;
    public bool  isShaking;
    public Vector3 localPosition;
}
```

---

## 타일 시스템

총 **25종의 TileType** 이 `TileBehaviour` 단일 컴포넌트로 처리됩니다.

### 타일 타입 일람

| 타입 | 역할 |
|------|------|
| `QuarterClockwiseRotation` | 플레이어/맵을 시계방향 90° 회전 |
| `HalfClockwiseRotation` | 플레이어/맵을 시계방향 180° 회전 |
| `QuarterCounterClockwiseRotation` | 반시계방향 90° 회전 |
| `HalfCounterClockwiseRotation` | 반시계방향 180° 회전 |
| `StartTeleport` | 텔레포트 출발지 (ID로 EndTeleport와 자동 연결) |
| `EndTeleport` | 텔레포트 도착지 |
| `Breakable` | 일정 횟수 밟으면 파괴되는 타일 |
| `Ice` | 플레이어가 해당 방향으로 계속 미끄러짐 |
| `Stop` | 아이스 슬라이딩 즉시 중단 |
| `FirstDestination` | Map1 플레이어용 목적지 (클리어) |
| `SecondDestination` | Map2 플레이어용 목적지 (챕터 4 클리어) |
| `StepOn` | 밟으면 ToggleTargeted/TrapToggle을 발동 |
| `ToggleTargeted` | StepOn 또는 색상 일치 시 나타나거나 사라지는 타일 |
| `TrapToggle` | 꺼진 상태에서 플레이어가 올라오면 사망 |
| `ActiveToggle` | 이동+회전 합산 N회마다 토글 |
| `MoveToggle` | 이동 N회마다 토글 |
| `RotationToggle` | 회전 N회마다 토글 |
| `ColorToggle` | 특정 색상의 ToggleTargeted를 일괄 토글 |
| `ConditionalToggle` | 조건부 토글 (확장 슬롯) |
| `Help` | 힌트 텍스트 표시 전용 타일 |
| `Star` | 수집 가능한 별 (미션 달성 조건) |
| `Start` | 플레이어 스폰 위치 |
| `FirstEnemySpawn` | Map1 적 스폰 위치 |
| `SecondEnemySpawn` | Map2 적 스폰 위치 |

### 색상 토글 시스템

`TileColor`는 RGB 비트 플래그로 7가지 색상 조합을 표현합니다.

```csharp
[System.Flags]
public enum TileColor
{
    Black   = 0,
    Blue    = 1 << 0,   // 001
    Green   = 1 << 1,   // 010
    Red     = 1 << 2,   // 100
    Yellow  = Red | Green,      // 110
    Cyan    = Green | Blue,     // 011
    Magenta = Red | Blue,       // 101
    White   = Red | Green | Blue // 111
}
```

`ColorToggle` 타일을 밟으면 해당 색상을 포함하는 모든 `ToggleTargeted` 타일이 반응합니다.  
교집합(`(CurrentTileColor & color) == CurrentTileColor`) 판정으로 White는 모든 색상을 활성화합니다.

### 타일 로직 처리 순서

타일은 `OnTriggerEnter2D`에서 플레이어를 `_pendingPlayer`로 등록만 하고,  
실제 로직은 `BehaviourManager`가 제어하는 **타일 로직 턴** 이벤트(`TileLogicTurnStarted`)에서 처리됩니다.  
이는 같은 프레임에 여러 타일이 동시에 반응하는 물리 레이스 컨디션을 방지합니다.

---

## 턴 시스템

```
[플레이어 입력]
    │
    ▼
[MoveSequence / RotateSequence]
    │
    ├─ TileLogicTurnStarted  → 타일 효과 적용
    ├─ PhysicsTurnStarted    → 낙사 판정
    └─ PlayerActionFinished  → 3회마다 적 턴 개시
                                      │
                                      ▼
                              [EnemyTurnStarted]
                                      │
                              EnemyManager가 모든 적 이동
                                      │
                              [PlayerTurnStarted] → 입력 잠금 해제
```

`BehaviourManager`가 `TurnState` (Player / Tile / Enemy) 상태를 관리하고,  
`CommandHistory`에 모든 커맨드를 푸시하며, 적 턴 완료 후 플레이어 턴을 복귀시킵니다.

---

## Undo 시스템

`Ctrl+Z` 또는 채팅 커맨드 `undo` 입력 시:

1. `CommandHistory`에서 **비플레이어 커맨드**(타일, 적)를 먼저 Undo
2. 가장 최근 **플레이어 커맨드** 하나를 Undo
3. 키 카운터(`pushedNumberALT` 등) 역산
4. `UndoTriggered` 이벤트 발행 → `MapManager`가 맵 회전 상태 복원
5. 플레이어 위치·방향·입력 히스토리 복원
6. 관련 타일의 `TileStateSnapshot` 복원

`MapManager`는 `Stack<MapState>`로 맵 회전 이력을 관리하고,  
타일 아이콘 역회전 보정값(`_firstTileIconZRotation`)도 함께 복원합니다.

---

## 듀얼 맵 시스템 (챕터 4)

챕터 4 스테이지는 `hasSecondMap = true`로 설정된 두 개의 독립된 타일맵을 가집니다.

- **Map 1** / **Map 2**: Unity Layer로 분리 (`"Map 1"`, `"Map 2"`)
- **Static Layer**: 양쪽 맵에 공유되는 타일 (크로스맵 판정 제외)
- `ALT + TAB` 입력 시 `TileMapChangeCommand` 실행 → `MapManager`가 활성 맵 전환
- 서브 카메라 2대(`map1Camera`, `map2Camera`)가 `RenderTexture`로 화면 내 미니뷰를 렌더링
- 각 맵의 이동·회전 카운터(`map1MoveCount`, `map2MoveCount`)는 독립 집계
- **크로스맵 텔레포트**: `StartTeleport` 레이어 ≠ `EndTeleport` 레이어일 때, 플레이어 레이어를 전환하여 맵 이동 효과 구현

플레이어는 자신이 속한 맵의 타일에만 반응하며(`isSameLayer` 검사),  
비활성 맵의 `Destination` 타일 위에 서 있으면 파티클 이펙트로 시각적으로 알려줍니다.

---

## 스테이지 미션 시스템

각 스테이지는 최대 3개의 미션을 가집니다.

### 미션 타입 (MissionType)

| 타입 | 조건 |
|------|------|
| `StageClear` | 스테이지 클리어만으로 달성 |
| `MoveCountLimit` | 전체 행동 횟수 N회 이하 클리어 |
| `TimeLimit` | N초 이내 클리어 |
| `KillAllEnemies` | 모든 적 처치 후 클리어 |
| `CollectStar` | 맵 내 모든 Star 타일 수집 |
| `NoSpecificFeature` | 특정 키(ALT/F4/TAB) N회 이하 사용 |

### 3번째 미션 (ThirdMissionCondition Flags)

3번째 미션은 비트 플래그 조합으로 **여러 조건을 동시에 요구**할 수 있습니다.

```csharp
[System.Flags]
public enum ThirdMissionCondition
{
    None              = 0,
    TimeLimit         = 1 << 0,
    MoveCountLimit    = 1 << 1,
    KillAllEnemies    = 1 << 2,
    NoSpecificFeature = 1 << 3
}
```

### 진행 데이터 저장

클리어·미션 달성 여부는 `JsonDataManager`를 통해 JSON 파일로 영속 저장됩니다.  
이 데이터는 스테이지 선택 화면의 노드 해금, Steam 업적 판정, 미션 패널 표시에 활용됩니다.

---

## Steam 연동

Steamworks.NET을 사용하며 Steam App ID `4624670`로 등록되어 있습니다.

### 업적 구조

```
ACH_CH{1~4}_COMPLETE  → 해당 챕터 전 스테이지 클리어
ACH_CH{1~4}_PERFECT   → 해당 챕터 전 미션 달성
ACH_ALL_CLEAR         → 전체 60스테이지 클리어
ACH_ALL_PERFECT       → 전체 미션 달성
```

`StageAchievementHandler`가 `StageProgressSaved` 이벤트를 수신하여 달성 조건을 판정하고,  
`SteamUserStats.SetAchievement()` → `SteamUserStats.StoreStats()` 순서로 잠금 해제합니다.

---

## 전시 분석 시스템 (PlayX4 2026)

**PlayX4 2026** 인디게임 전시 대응을 위해 플레이어 행동 분석 파이프라인이 구축되어 있습니다.

### 수집 데이터

스테이지별로 다음 통계를 실시간 기록합니다:

| 항목 | 설명 |
|------|------|
| `entryCount` | 스테이지 진입 횟수 |
| `clearCount` | 클리어 횟수 |
| `deathCount` | 사망 횟수 |
| `abandonCount` | 이탈 횟수 |
| `retryCount` | 재시도 횟수 |
| `totalClearTime` / `minClearTime` / `maxClearTime` | 클리어 시간 통계 |
| `totalPlayTime` | 총 플레이 시간 |
| `totalAltCount` / `totalTabCount` / `totalF4Count` / `totalUndoCount` | 키 사용 빈도 |

### 출력 형식

`ExhibitionExporter`를 통해 세션 종료 후 CSV 파일로 내보낼 수 있습니다.  
Unity Editor 메뉴 `Tools > Exhibition > Export CSV` 에서도 즉시 내보내기 가능합니다.

### Google Forms 연동

`ExhibitionFormsReporter`를 통해 전시 세션 데이터를 Google Forms로 자동 제출하는 기능을 포함합니다.

### 개발자 패널 (F12)

인게임 F12로 개발자 패널을 열 수 있습니다. 기능 10종:

- 스테이지 직접 이동
- 키 사용 횟수 무제한 해제
- 미션 강제 달성
- 게임 상태 수동 조작 등

---

## 스트리밍 채팅 연동

트위치/유튜브 라이브 스트리밍 중 채팅 메시지로 게임 조작이 가능합니다.

| 채팅 키워드 | 게임 내 동작 |
|------------|-------------|
| `move` | 플레이어 이동 (F4 동일) |
| `rotate` | 시계방향 회전 (ALT 동일) |
| `counterrotate` | 반시계방향 회전 (TAB 동일) |
| `suicide` | 플레이어 자살 (ALT+F4 동일) |
| `undo` | 행동 되돌리기 |
| `restart` | 스테이지 재시작 |
| `pause` | 일시정지 토글 |
| `dance` | 적 댄스 애니메이션 (이스터에그) |
| `i love you` | 적 하트 애니메이션 (이스터에그) |
| `whistle` | 휘파람 효과음 (이스터에그) |

---

## 기술 스택 및 외부 패키지

| 패키지 | 용도 |
|--------|------|
| **Unity 2022+** | 게임 엔진 |
| **DOTween** | 트윈 애니메이션 (타일 연출, UI 바운스, 맵 회전) |
| **Steamworks.NET** | Steam API 연동 (업적, 초기화) |
| **Unity Visual Effect Graph (VFX)** | 텔레포트 파티클 이펙트 |
| **TextMeshPro** | 카운트 텍스트, 스테이지 번호 UI |
| **Unity Localization** | 다국어 지원 (한국어/영어 등) |
| **Unity Addressables** | 에셋 주소 기반 관리 |
| **Eflatun.SceneReference** | 타입 안전한 씬 레퍼런스 |
| **MCP For Unity** | Claude Code와 Unity Editor 간 MCP 통신 |

---

## 프로젝트 구조

```
Assets/
├── 02.Scripts/
│   ├── BootStrapper&SceneLoader/   # 씬 초기화 및 전환
│   ├── Camera/                     # 카메라 컨트롤러 및 레터박스
│   ├── Clear Scene/                # 전체 클리어 씬 연출
│   ├── Enemy/                      # EnemyBehaviour, EnemyManager
│   ├── EventSystem/                # Canvas 포커스 유지
│   ├── Fade In&Out/                # 씬 전환 페이드 이펙트
│   ├── Game(UI)/                   # 인게임 UI (키 제한, Undo 버튼, 시퀀스)
│   │   └── StageInfoPanel/         # 미션·클리어·일시정지 패널 (상속 계층)
│   ├── GamePlay/                   # BehaviourManager, GameStateManagement
│   ├── HowToPlay/                  # 튜토리얼 플레이어 로직
│   ├── Intro/                      # 인트로 씬, 게임 일시정지 캔버스
│   ├── NonBehaviour/               # Command, CommandHistory, GameEvents
│   ├── Option(UI)/                 # 해상도·사운드·언어 설정
│   ├── Player/                     # PlayerBehaviour, PlayerInputHandler, PlayerAnimator
│   ├── Singleton.cs                # 제네릭 싱글턴
│   ├── SoundManager.cs             # BGM/SFX 관리
│   ├── StageSelect/                # 스테이지 선택 화면 노드 시스템
│   ├── Steam&Achievement/          # SteamManager, JsonDataManager, 업적 핸들러
│   ├── TileMap/                    # MapManager, StageLoader, TileBehaviour
│   └── 2026 PlayX4/                # 전시용 데이터 수집·내보내기 시스템
│
├── ScriptableObject/
│   ├── SO_StageData                # 스테이지 설계 데이터 (챕터 1~4)
│   ├── SO_TileData                 # 타일 기본값 데이터
│   └── SO_UIEvent / SO_SceneGroup  # UI 이벤트·씬 그룹 참조
│
└── Plugins/
    └── Demigiant/DOTween/          # DOTween 플러그인
```

---

## 개발 특이사항

### 물리 기반 이동 + 턴제 퍼즐의 결합

Unity 2D Physics(`Rigidbody2D` + `AddForce`)를 사용하지만, 퍼즐의 일관성을 위해  
`OnTriggerEnter2D`에서 즉각 처리하지 않고 **명시적인 턴 이벤트**에서 로직을 실행합니다.  
이 방식으로 같은 프레임 내 다중 타일 발동에 의한 레이스 컨디션을 방지합니다.

### 맵 회전 시 타일 아이콘 역회전 보정

`Rotation` 타일을 밟으면 타일맵 전체가 DOTween으로 회전합니다.  
이때 각 타일의 아이콘이 함께 회전하지 않도록 `TileIconRotated` 이벤트로  
모든 타일 아이콘에 역방향 보정 회전을 별도 적용합니다.

### 아이스 슬라이딩 중 텔레포트

`Ice` 타일 위에서 슬라이딩 중 `StartTeleport`에 진입하면  
`continueIceModeAfterTeleport` 플래그에 따라 도착 후 슬라이딩을 유지하거나 정지시킬 수 있습니다.  
이 옵션은 ScriptableObject에서 스테이지별로 독립 설정됩니다.

---

## 라이선스

본 프로젝트는 개인 포트폴리오 및 Steam 상업 출시를 목적으로 개발된 1인 창작물입니다.  
코드 및 에셋의 무단 복제·배포를 금지합니다.
