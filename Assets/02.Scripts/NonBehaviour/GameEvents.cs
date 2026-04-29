using System;
using UnityEngine;

/// <summary>
/// 게임 전역 이벤트 버스 (정적 클래스)
///
/// 네이밍 컨벤션
///   이벤트    : PascalCase, 과거형(~ed) · 진행형(~ing) · 상태변화형(~Changed/~Started/~Completed)
///   발행 함수 : Raise + 이벤트명
///
/// 주석 구조
///   [발행] 호출 스크립트 — 발행 조건/시점
///   [수신] 구독 스크립트 — 처리 내용
/// </summary>
public static class GameEvents
{
    #region Game State
    // 스테이지 단위 생명주기 (클리어 · 재시작 · 사망)

    // [발행] TileBehaviour.cs — First/SecondDestination 타일 도달 시
    // [수신] GameManager.cs — 클리어 처리 / UIManager.cs — 결과 UI 표시
    public static event Action StageCleared;
    public static void RaiseStageCleared() => StageCleared?.Invoke();

    // [발행] GameManager.cs / UIManager.cs — 재시작 버튼 클릭 시
    // [수신] StageLoader.cs / GameManager.cs — 스테이지 재로드
    public static event Action StageRestarted;
    public static void RaiseStageRestarted() => StageRestarted?.Invoke();

    // [발행] PlayerBehaviour.cs — PlayExplosion() 발동 시 (낙사 · 충돌 · 함정)
    // [수신] GameManager.cs — 사망 처리 / UIManager.cs — 게임오버 UI
    public static event Action PlayerDied;
    public static void RaisePlayerDied() => PlayerDied?.Invoke();

    // [발행] EnemyBehaviour.cs — PlayExplosion() 발동 시
    // [수신] EnemyManager.cs / BehaviourManager.cs — 적 제거 처리
    public static event Action EnemyDied;
    public static void RaiseEnemyDied() => EnemyDied?.Invoke();

    #endregion

    #region Input Control
    // 플레이어 입력 잠금 상태 변경

    // [발행] BehaviourManager.cs — 턴 전환 처리 시
    //        MapManager.cs — 맵 회전 애니메이션 진행 중
    // [수신] PlayerInputHandler.cs — 입력 수락/차단 전환
    public static event Action<bool> InputLockChanged; // bool: isLocked
    public static void RaiseInputLockChanged(bool isLocked) => InputLockChanged?.Invoke(isLocked);

    #endregion

    #region Turn Flow
    // 플레이어 · 적 · 물리 판정의 턴 순서 제어

    // [발행] PlayerBehaviour.cs — 이동/회전 완료 직후
    // [수신] TileBehaviour.cs — OnTriggerEnter에 등록한 타일 효과(pending) 실행
    public static event Action TileLogicTurnStarted;
    public static void RaiseTileLogicTurnStarted() => TileLogicTurnStarted?.Invoke();

    // [발행] PlayerBehaviour.cs — 얼음 미끄러짐 코루틴의 매 FixedUpdate
    // [수신] TileBehaviour.cs — Stop/StartTeleport 타일만 수신, 슬라이딩 도중 타일 반응 처리
    public static event Action IceTileLogicTurnStarted;
    public static void RaiseIceTileLogicTurnStarted() => IceTileLogicTurnStarted?.Invoke();

    // [발행] PlayerBehaviour.cs — TileLogicTurn 처리 완료 후
    // [수신] PlayerBehaviour.cs / EnemyBehaviour.cs — 낙사 등 물리 사망 판정
    public static event Action PhysicsTurnStarted;
    public static void RaisePhysicsTurnStarted() => PhysicsTurnStarted?.Invoke();

    // [발행] PlayerBehaviour.cs — 물리 턴까지 완전히 종료된 시점
    //        PlayerAnimator.cs — 회전 애니메이션 완료 시
    // [수신] BehaviourManager.cs — 액션 카운트 증가 및 적 턴 전환 처리
    public static event Action<int> PlayerActionFinished; // int: 플레이어 레이어
    public static void RaisePlayerActionFinished(int playerLayer) => PlayerActionFinished?.Invoke(playerLayer);

    // [발행] BehaviourManager.cs — 플레이어 턴 종료 후 적 턴으로 전환 시
    // [수신] EnemyBehaviour.cs / EnemyManager.cs — 적 이동/행동 실행
    public static event Action<Vector3> EnemyTurnStarted; // Vector3: 플레이어 현재 위치
    public static void RaiseEnemyTurnStarted(Vector3 playerPosition) => EnemyTurnStarted?.Invoke(playerPosition);

    // [발행] BehaviourManager.cs — 적 턴 종료 후 플레이어 턴으로 복귀 시
    // [수신] PlayerBehaviour.cs — _isEnemyActing 플래그 해제
    public static event Action PlayerTurnStarted;
    public static void RaisePlayerTurnStarted() => PlayerTurnStarted?.Invoke();

    #endregion

    #region Player Action Counter
    // 타일 토글 카운터 판정 (layer 파라미터로 같은 레이어 토글 타일만 반응)

    // [발행] PlayerBehaviour.cs — 이동+회전 합산 액션 카운트 갱신 시
    // [수신] TileBehaviour.cs — ActiveToggle 카운터 판정
    public static event Action<int, int> PlayerActed; // (count, layer)
    public static void RaisePlayerActed(int count, int layer) => PlayerActed?.Invoke(count, layer);

    // [발행] PlayerBehaviour.cs — 이동 액션 카운트 갱신 시
    // [수신] TileBehaviour.cs — MoveToggle 카운터 판정
    public static event Action<int, int> PlayerMoved; // (count, layer)
    public static void RaisePlayerMoved(int count, int layer) => PlayerMoved?.Invoke(count, layer);

    // [발행] PlayerBehaviour.cs — 회전 액션 카운트 갱신 시
    // [수신] TileBehaviour.cs — RotationToggle 카운터 판정
    public static event Action<int, int> PlayerRotated; // (count, layer)
    public static void RaisePlayerRotated(int count, int layer) => PlayerRotated?.Invoke(count, layer);

    #endregion

    #region Undo / Command Pattern
    // 되돌리기 실행 · 실행 전 상태 스냅샷 요청 · UI 카운터 갱신

    // [발행] BehaviourManager.cs — UndoTurn() 실행 시
    //        PlayerInputHandler.cs — Ctrl+Z 입력 시
    // [수신] MapManager.cs — 맵/회전 상태 복원
    //        TileBehaviour.cs(BehaviourManager 경유) — 타일 상태 복원
    public static event Action UndoTriggered;
    public static void RaiseUndoTriggered() => UndoTriggered?.Invoke();

    // [발행] BehaviourManager.cs — ExecuteCommand() 내부, 플레이어 커맨드 실행 직전
    // [수신] MapManager.cs — 맵/회전 상태 스냅샷 저장 (Undo용)
    public static event Action<PlayerBehaviour> PreActionStateSaveRequested;
    public static void RaisePreActionStateSaveRequested(PlayerBehaviour pb) => PreActionStateSaveRequested?.Invoke(pb);

    // [발행] BehaviourManager.cs — UpdateUndoUI() 갱신 시
    // [수신] UIManager.cs 또는 UndoUI 컴포넌트 — 남은 Undo 횟수 표시
    public static event Action<int, int> UndoCountChanged; // (undoCount, maxCount)
    public static void RaiseUndoCountChanged(int undoCount, int count) => UndoCountChanged?.Invoke(undoCount, count);

    #endregion

    #region TileMap
    // 맵 전환 · 회전 · 활성화 · 초기화 이벤트

    // [발행] TileMapChangeCommand.cs — Execute() 내부 (ALT+TAB 맵 전환 시)
    //        TileBehaviour.cs — 크로스맵 텔레포트 도착 시
    // [수신] MapManager.cs — _isFirst 토글 및 활성 맵 전환
    public static event Action TileMapChanged;
    public static void RaiseTileMapChanged() => TileMapChanged?.Invoke();

    // [발행] TileBehaviour.cs — Quarter/Half Rotation 타일을 밟았을 때
    // [수신] MapManager.cs — RotateAroundCell() 애니메이션 실행
    public static event Action<PlayerBehaviour, float> TileMapRotated; // float: 회전 각도(도)
    public static void RaiseTileMapRotated(PlayerBehaviour pb, float angle) => TileMapRotated?.Invoke(pb, angle);

    // [발행] MapManager.cs — RotateAroundCell() 애니메이션 시작 직전
    // [수신] PlayerBehaviour.cs — 회전 중 플레이어 물리 로직 동결
    //        EnemyBehaviour.cs — 회전 중 적 물리 로직 동결
    public static event Action<bool> MapRotationStarted; // bool: freeze 여부
    public static void RaiseMapRotationStarted(bool freeze) => MapRotationStarted?.Invoke(freeze);

    // [발행] MapManager.cs — RotateAroundCell() 애니메이션 완료 후 0.55s 딜레이 후
    // [수신] PlayerBehaviour.cs — 플레이어 물리 로직 재개
    //        EnemyBehaviour.cs — 적 물리 로직 재개
    //        TileBehaviour.cs — 회전 후 타일 레이어 재설정 (stop/fall 판정 등)
    public static event Action<bool> MapRotationCompleted; // bool: freeze 해제 여부
    public static void RaiseMapRotationCompleted(bool freeze) => MapRotationCompleted?.Invoke(freeze);

    // [발행] MapManager.cs — 맵 회전 완료 후 타일 아이콘 역회전 보정 시
    // [수신] TileBehaviour.cs — 각 타일 아이콘을 역방향으로 회전하여 원래 시점 유지
    public static event Action<float> TileIconRotated; // float: 역회전 보정 각도
    public static void RaiseTileIconRotated(float angle) => TileIconRotated?.Invoke(angle);

    // [발행] MapManager.cs — Init() · ActivateFirst() · ActivateSecond() (Undo 복원 포함)
    // [수신] SecondMapScreenPanel.cs — screen/firstMap/secondMap RenderTexture 및 레이블 일괄 갱신
    public static event Action<bool> MapActivated; // bool: isFirst (Map 1 활성 여부)
    public static void RaiseMapActivated(bool isFirst) => MapActivated?.Invoke(isFirst);

    // [발행] MapManager.cs — InitializeNewStage() 완료 직후
    // [수신] EnemyManager.cs — MapManager의 타일맵을 참조하여 적 스폰
    public static event Action MapInitialized;
    public static void RaiseMapInitialized() => MapInitialized?.Invoke();

    #endregion

    #region Tile Toggle
    // 타일 토글 트리거 및 색상 토글 처리

    // [발행] TileBehaviour.cs — StepOn 타일 위에서 ApplyTileCommand 실행 시
    // [수신] TileBehaviour.cs — ToggleTargeted/TrapToggle 타일이 count · layer 일치 여부로 반응
    public static event Action<int, int> ToggleTriggered; // (count, layer)
    public static void RaiseToggleTriggered(int count = -1, int layer = 0) => ToggleTriggered?.Invoke(count, layer);

    // [발행] TileBehaviour.cs — ColorToggle 타일 위에서 ApplyTileCommand 실행 시
    // [수신] TileBehaviour.cs — 같은 TileColor를 가진 타일들이 토글 반응
    public static event Action<TileColor, int> ColorToggleTriggered; // (color, layer)
    public static void RaiseColorToggleTriggered(TileColor color, int layer = 0) => ColorToggleTriggered?.Invoke(color, layer);

    #endregion

    #region Stage Lifecycle
    // 스테이지 로드 완료 및 플레이 기록 추적 (Steam 도전과제 · 이탈율 로그)

    // [발행] StageLoader.cs — 스테이지 타일맵 로드 완료 시
    // [수신] TileBehaviour.cs — continueIceModeAfterTeleport 초기값 수신
    public static event Action<bool> StageLoaded;
    public static void RaiseStageLoaded() => StageLoaded?.Invoke(true);

    // [발행] GameManager.cs / StageManager.cs — 스테이지 진입 시
    // [수신] StageRecorder.cs — 플레이 시간 · 이탈율 기록 시작
    public static event Action<int, int> StageRecordStarted; // (chapter, stage)
    public static void RaiseStageRecordStarted(int ch, int st) => StageRecordStarted?.Invoke(ch, st);

    // [발행] GameManager.cs / StageManager.cs — 스테이지 클리어 후 결과창 출력 시
    // [수신] StageRecorder.cs — 플레이 세션 종료 처리
    public static event Action StageRecordEnded;
    public static void RaiseStageRecordEnded() => StageRecordEnded?.Invoke();

    // [발행] UIManager.cs — 일시정지 후 스테이지 선택 · 로비로 나갈 때
    // [수신] StageRecorder.cs — 비정상 종료(이탈) 로그 기록
    public static event Action<int, int> StageAbandoned; // (chapter, stage)
    public static void RaiseStageAbandoned(int ch, int st) => StageAbandoned?.Invoke(ch, st);

    #endregion

    #region Mission Tracking
    // Star 수집 · 키 입력 누적 · 맵 전환 횟수 추적 (Steam 도전과제)

    // [발행] TileBehaviour.cs — Star 타일 위에서 ApplyTileCommand 실행 시
    // [수신] StageRecorder.cs / UIManager.cs — 별 수집 카운트 갱신 및 도전과제 처리
    public static event Action StarCollected;
    public static void RaiseStarCollected() => StarCollected?.Invoke();

    // [발행] PlayerInputHandler.cs — EnqueueCommand() 실행 시마다
    // [수신] StageRecorder.cs — ALT/F4/TAB 키 누적 사용 횟수 기록 (도전과제)
    public static event Action<KeyType> KeyUsed;
    public static void RaiseKeyUsed(KeyType keyType) => KeyUsed?.Invoke(keyType);

    // [발행] PlayerInputHandler.cs / TileMapChangeCommand.cs — ALT+TAB 맵 전환 입력 시 (Undo 제외)
    // [수신] StageRecorder.cs — 실제 맵 전환 횟수 카운트 (도전과제)
    public static event Action MapSwitched;
    public static void RaiseMapSwitched() => MapSwitched?.Invoke();

    #endregion

    #region Chat Commands
    // 트위치/유튜브 채팅 연동 — 채팅 키워드를 게임 내 커맨드로 변환

    // [발행] ChatManager.cs — "suicide" 채팅 감지 시
    // [수신] PlayerBehaviour.cs — PlayExplosion() 호출 (F4와 동일 효과)
    public static event Action ChatCommandSuicide;
    public static void RaiseChatCommandSuicide() => ChatCommandSuicide?.Invoke();

    // [발행] ChatManager.cs — "rotate" 채팅 감지 시
    // [수신] PlayerInputHandler.cs — 시계 방향 회전 커맨드 (LeftALT와 동일)
    public static event Action ChatCommandRotateCW;
    public static void RaiseChatCommandRotateCW() => ChatCommandRotateCW?.Invoke();

    // [발행] ChatManager.cs — "counterrotate" 채팅 감지 시
    // [수신] PlayerInputHandler.cs — 반시계 방향 회전 커맨드 (TAB과 동일)
    public static event Action ChatCommandRotateCCW;
    public static void RaiseChatCommandRotateCCW() => ChatCommandRotateCCW?.Invoke();

    // [발행] ChatManager.cs — "move" 채팅 감지 시
    // [수신] PlayerInputHandler.cs — 이동 커맨드 (F4와 동일)
    public static event Action ChatCommandMove;
    public static void RaiseChatCommandMove() => ChatCommandMove?.Invoke();

    // [발행] ChatManager.cs — "dance" 채팅 감지 시 (이스터에그)
    // [수신] EnemyBehaviour.cs — Dance 애니메이션 재생
    public static event Action ChatCommandDance;
    public static void RaiseChatCommandDance() => ChatCommandDance?.Invoke();

    // [발행] ChatManager.cs — "i love you" 채팅 감지 시 (이스터에그)
    // [수신] EnemyBehaviour.cs — Love 애니메이션 재생
    public static event Action ChatCommandLove;
    public static void RaiseChatCommandLove() => ChatCommandLove?.Invoke();

    // [발행] ChatManager.cs — "whistle" 채팅 감지 시 (이스터에그)
    // [수신] 오디오 매니저 — 휘파람 효과음 재생
    public static event Action ChatCommandWhistle;
    public static void RaiseChatCommandWhistle() => ChatCommandWhistle?.Invoke();

    #endregion

    #region Visual Effects

    // [발행] CanvasShake.cs / 특정 타일 기믹 · 게임오버 연출
    // [수신] CanvasShake.cs — 화면 글리치 연출 실행
    public static event Action GlitchTriggered;
    public static void RaiseGlitchTriggered() => GlitchTriggered?.Invoke();

    #endregion


}