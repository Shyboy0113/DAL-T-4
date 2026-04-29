# TileBehaviour 상호작용 명세서

---

## 1. Ice 타일

**진입 시 동작**
- 플레이어/적 모두 `EnableIceMode(true)` → `Slide()` 코루틴 시작
- `Slide()`는 FixedUpdate마다 `lastMoveDirection`으로 위치를 밀고, 매 물리 스텝에 `RaiseIceTileLogicTurnStarted()` 발화

**Ice 모드 해제 조건**

| 조건 | 호출 경로 |
|------|----------|
| Stop 타일 진입 | `OnIceTileLogicTurn` → `ApplyTileCommand` → `StopIceAndFinish()` |
| StartTeleport 진입 (`continueIceAfterTeleport = false`) | 동일 경로, teleport 후 `StopIceAndFinish()` |
| 낙사 | `PlayExplosion()` → `EnableIceMode(false)` |
| Undo | `MoveCommand.Undo()` → `EnableIceMode(false)` |

**이벤트 발화 타이밍 (Ice 진입 시)**
- `PlayerMoved` → Ice 진입 직후 `MoveSequence()`에서 **즉시 발화** (토글 카운터 갱신용)
- `PlayerActionFinished` → **보류**. `StopIceAndFinish()` 호출 시에만 발화
- 즉, Ice 슬라이딩 중에는 적 턴이 발생하지 않음

---

## 2. Ice + Stop 상호작용 흐름

```
[Ice 타일 진입]
  └─ EnableIceMode(true) → Slide() 시작
       └─ 매 FixedUpdate: RaiseIceTileLogicTurnStarted()
            └─ Stop 타일의 OnIceTileLogicTurn() 발화 (Stop, StartTeleport만 구독)
                 └─ _pendingPlayer 있을 때: ExecuteCommand(new TileCommand(this, pb))
                      └─ 이 시점에 GetSnapShot() → beforeState 저장 (Apply 이전)
                      └─ ApplyTileCommand() → StopIceAndFinish()
                           └─ EnableIceMode(false) + RaisePlayerActionFinished()
```

**Stop 타일에서 Snapshot이 찍히는 시점**: `TileCommand` 생성자 안에서 `GetSnapShot()` 호출 → `ApplyTileCommand()` 실행 **직전** 상태

---

## 3. Snapshot 타이밍 공통 규칙

`TileCommand` 생성 → `GetSnapShot()` → `ExecuteCommand()` → `command.Execute()` → `ApplyTileCommand()` 순서이므로:

> **Snapshot은 항상 타일 효과 적용 이전 상태를 저장한다.**

`TileStateSnapshot`이 보관하는 필드:

| 필드 | 목적 |
|------|------|
| `hitCount` | Breakable 피격 횟수 |
| `isToggled` | Toggle 상태 |
| `playerLayer` | 크로스맵 텔레포트 후 레이어 복원 |
| `playerMap1/2 MoveCount / RotationCount` | 카운트 텍스트 복원 |
| `rotation` | Rotation 타일의 맵 회전 복원 |
| `isVisible` | Breakable/Star 파괴·수집 복원 |
| `isShaking` | Breakable 흔들림 상태 복원 |

---

## 4. Toggle 계열 타일

**이벤트 구독 분류**

| 타일 | 구독 이벤트 | 발화 조건 |
|------|------------|----------|
| `ToggleTargeted` | `ToggleTriggered` | StepOn 타일 진입 시 (`count == -1`) |
| `TrapToggle` | `ToggleTriggered` | 동일 |
| `ActiveToggle` | `PlayerActed` | 액션 횟수 % `toggleActivationCount == 0` |
| `MoveToggle` | `PlayerMoved` | 이동 횟수 % `toggleActivationCount == 0` |
| `RotationToggle` | `PlayerRotated` | 회전 횟수 % `toggleActivationCount == 0` |

**Collider 활성화 규칙**

| 타입 | 규칙 |
|------|------|
| `ToggleTargeted`, `ActiveToggle`, `MoveToggle`, `RotationToggle` | `isToggled = true` → collider 비활성화 (통과 가능) |
| `TrapToggle` | 항상 collider 활성화. `isToggled = false`일 때 진입하면 폭발 |

**Toggle 후 점유자 처리**: `CheckOccupantsAfterToggle()` → 현재 타일 위에 플레이어/적이 있으면 즉시 `PlayExplosion()`

---

## 5. ColorToggle

```
ColorToggle 타일 진입
  └─ RaiseColorToggleTriggered(CurrentTileColor, layer)
       └─ ToggleTargeted 타일의 HandleColorToggle() 발화
            └─ (CurrentTileColor & receivedColor) != 0 이면 ExecuteCommand(new TileCommand(this))
```

- 동일 레이어(`gameObject.layer`)의 ToggleTargeted 타일 중 색상 비트가 겹치는 것만 반응
- ColorToggle 자신은 `isToggled` 변경 없이 이벤트만 발화

---

## 6. Teleport

**StartTeleport → EndTeleport 매칭**: `AutoLinkTeleport()`가 Awake/OnValidate에서 `CurrentTeleportID`로 자동 연결

**크로스맵 텔레포트**
- `this.gameObject.layer != teleportTarget.gameObject.layer` → `isCrossMap = true`
- 플레이어 layer를 EndTeleport의 layer로 변경 + `RaiseTileMapChanged()`

**Ice 슬라이딩 중 텔레포트**

| `continueIceAfterTeleport` | 동작 |
|---------------------------|------|
| `false` (기본) | `StopIceAndFinish()` → Stop 타일과 동일하게 슬라이딩 종료 |
| `true` | Ice 슬라이딩 유지, 같은 방향으로 계속 이동 |

---

## 7. Breakable

```
진입할 때마다: _currentHit++
  └─ _currentHit >= CurrentMaxBreakCount → ShakeUntilBreak() 코루틴 시작

OnTriggerExit2D (플레이어가 떠날 때):
  └─ _currentHit >= CurrentMaxBreakCount → BreakTile() 코루틴
       └─ WaitForSeconds(breakDelay)
       └─ iconRenderer/backgroundRenderer/collider 모두 비활성화
```

- Undo 시: `isVisible`이 `false`면 렌더러·collider 모두 꺼진 채 복원, `isShaking`이 `true`면 ShakeUntilBreak 재개

---

## 8. Rotation 타일

`QuarterClockwise`, `HalfClockwise`, `QuarterCounterClockwise`, `HalfCounterClockwise` 4종

- 진입 시 플레이어/적 위치를 타일 위치로 **강제 스냅** 후 `RaiseTileMapRotated(player, angle)` 발화
- `RotateTile()`은 `IsUndoOr || mapManager.IsRotating`이면 실행 안 함
- Snapshot의 `rotation` 필드로 맵 회전 이전 Quaternion 복원

---

## 9. Destination (클리어 판정)

| 타입 | 조건 |
|------|------|
| `FirstDestination` | `pb.IsFirstTile()` (Map 1 레이어) + `!isCleared` |
| `SecondDestination` | `!pb.IsFirstTile()` (Map 2 레이어) + `!isCleared` |

- 조건 미충족 시 완전히 무시됨 (두 맵이 있을 때 Map 2 전용 목적지를 Map 1에서 밟아도 반응 없음)

---

## 10. StepOn

- 진입 시 `RaiseToggleTriggered(-1, layer)` 발화
- `count == -1`은 `HandleToggle()`에서 활성화 카운트 무관하게 즉시 Toggle 발동하는 특수값

---

## 11. Star

- `iconRenderer.enabled`가 `true`일 때만 수집 처리 (중복 발동 방지)
- 수집 시: iconRenderer + collider 비활성화, `RaiseStarCollected()` 발화
- Undo 복원: `TileStateSnapshot.isVisible`로 렌더러 상태 복원

---

## 이벤트 흐름 요약 (일반 이동 기준)

```
입력 → ExecuteCommand(MoveCommand)
  └─ GetSnapShot() on all relevant tiles already tracked
  └─ MovePlayer() → 물리 이동

MoveSequence():
  0.075s 후 → RaiseTileLogicTurnStarted()
    └─ OnTileLogicTurn(): _pendingPlayer 있는 타일들 TileCommand 실행
         └─ TileCommand 생성 시 GetSnapShot() (Apply 이전)
         └─ ApplyTileCommand()
  yield null → RaisePhysicsTurnStarted() → 낙사 판정
  yield null → RaisePlayerMoved() → 토글 카운터 갱신
             → RaisePlayerActionFinished() (Ice가 아닐 때)
               └─ 3회마다 TurnSequence() → 적 턴
```
