using System;
using UnityEngine;

public static class GameEvents
{
    #region Game State

    public static event Action StageCleared;
    public static event Action StageRestarted;
    public static event Action PlayerDied;
    public static event Action EnemyDied;

    // 호출 위치: TileBehaviour.cs (First/SecondDestination 타일 도달 시)
    public static void RaiseStageCleared()    => StageCleared?.Invoke();
    
    // 호출 위치: GameManager.cs 또는 UIManager.cs (재시작 버튼 클릭 시)
    public static void RaiseStageRestarted()  => StageRestarted?.Invoke();
    
    // 호출 위치: PlayerBehaviour.cs (PlayExplosion() 발동 시)
    public static void RaisePlayerDied()      => PlayerDied?.Invoke();
    
    // 호출 위치: EnemyBehaviour.cs (PlayExplosion() 발동 시)
    public static void RaiseEnemyDied()       => EnemyDied?.Invoke();

    #endregion

    #region Input Control

    public static event Action<bool> InputLockChanged;

    // 호출 위치: BehaviourManager.cs (턴 전환 시), MapManager.cs (맵 회전 애니메이션 진행 중)
    public static void RaiseInputLockChanged(bool isLocked) => InputLockChanged?.Invoke(isLocked);

    #endregion

    #region Turn Flow
    // 플레이어/적 이동 후, 타일이 OnTriggerEnter에서 등록한 pending 효과를 실행하는 턴
    public static event Action TileLogicTurnStarted;
    // 호출 위치: PlayerBehaviour.cs (이동/회전이 끝난 직후 턴 흐름 제어 중)
    public static void RaiseTileLogicTurnStarted() => TileLogicTurnStarted?.Invoke();

    // Ice 슬라이딩 중 매 물리 스텝 후 발화 — Stop/StartTeleport 타일만 수신
    public static event Action IceTileLogicTurnStarted;
    // 호출 위치: PlayerBehaviour.cs (얼음 미끄러짐 코루틴의 FixedUpdate 내부)
    public static void RaiseIceTileLogicTurnStarted() => IceTileLogicTurnStarted?.Invoke();

    // 타일 로직 턴 후, Player/Enemy가 낙사 등 물리 사망 여부를 판정하는 턴
    public static event Action PhysicsTurnStarted;
    // 호출 위치: PlayerBehaviour.cs (TileLogicTurn 처리 후 바닥 확인을 위해 호출)
    public static void RaisePhysicsTurnStarted() => PhysicsTurnStarted?.Invoke();

    // 플레이어 행동 하나(이동/회전)가 완전히 끝났을 때 발생
    // BehaviourManager가 수신해서 액션 카운트 증가 및 적 턴 전환 처리
    public static event Action PlayerActionFinished;
    // 호출 위치: PlayerBehaviour.cs (물리 턴까지 모두 종료된 시점)
    public static void RaisePlayerActionFinished() => PlayerActionFinished?.Invoke();

    public static event Action<Vector3> OnEnemyTurnStarted;
    public static event Action          OnPlayerTurnStarted;

    // 호출 위치: BehaviourManager.cs (Player 턴 종료 후 적 턴으로 넘길 때)
    public static void RaiseEnemyTurnStarted(Vector3 playerPosition)  => OnEnemyTurnStarted?.Invoke(playerPosition);
    
    // 호출 위치: BehaviourManager.cs (적 턴 종료 후 다시 플레이어 턴으로 돌아올 때)
    public static void RaisePlayerTurnStarted()                       => OnPlayerTurnStarted?.Invoke();

    #endregion

    #region Player Action Counter
    // 각 타일의 토글 카운터 판정에 사용
    // layer: 플레이어가 현재 속한 맵 레이어 — 같은 레이어의 토글 타일만 반응

    public static event Action<int, int> PlayerActed;    // ActiveToggle용  (이동 + 회전 합산)
    public static event Action<int, int> PlayerMoved;    // MoveToggle용
    public static event Action<int, int> PlayerRotated;  // RotationToggle용

    // 호출 위치: PlayerBehaviour.cs (CalculateActionCount / UpdateSequenceCanvas 등 액션 카운트 갱신 시)
    public static void RaisePlayerActed(int count, int layer)   => PlayerActed?.Invoke(count, layer);
    public static void RaisePlayerMoved(int count, int layer)   => PlayerMoved?.Invoke(count, layer);
    public static void RaisePlayerRotated(int count, int layer) => PlayerRotated?.Invoke(count, layer);

    #endregion

    #region Undo / Command Pattern

    public static event Action UndoTriggered;
    // 호출 위치: BehaviourManager.cs (UndoTurn() 실행 시) 또는 PlayerInputHandler.cs (Ctrl+Z)
    public static void RaiseUndoTriggered() => UndoTriggered?.Invoke();
    
    public static event Action<PlayerBehaviour> SaveStateBeforeAction;
    // 호출 위치: BehaviourManager.cs (ExecuteCommand() 내에서 IsPlayerCommand 판정 시)
    public static void RaiseSaveStateBeforeAction(PlayerBehaviour pb) => SaveStateBeforeAction?.Invoke(pb);
    
    public static event Action<int, int> UndoCountChanged;
    // 호출 위치: BehaviourManager.cs (UpdateUndoUI() 갱신 시)
    public static void RaiseUndoCountChanged(int undoCount, int count) => UndoCountChanged?.Invoke(undoCount, count);

    #endregion

    #region TileMap

    public static event Action                        TileMapChanged;
    public static event Action<PlayerBehaviour, float> TileMapRotated;
    public static event Action<bool>                  BeforeMapRotated;  // 회전 직전
    public static event Action<bool>                  AfterMapRotated;   // 회전 직후
    public static event Action<float>                 TileIconRotated;

    // 호출 위치: TileMapChangeCommand.cs (Execute 내부), TileBehaviour.cs (크로스맵 텔레포트 적용 시)
    public static void RaiseTileMapChanged()                                  => TileMapChanged?.Invoke();
    
    // 호출 위치: TileBehaviour.cs (시계/반시계 회전 타일(Quarter/Half Rotation)을 밟았을 때)
    public static void RaiseTileMapRotated(PlayerBehaviour pb, float angle)   => TileMapRotated?.Invoke(pb, angle);
    
    // 호출 위치: MapManager.cs (RotateAroundCell() 애니메이션 시작 직전)
    public static void RaiseBeforeMapRotated(bool freeze)                     => BeforeMapRotated?.Invoke(freeze);
    
    // 호출 위치: MapManager.cs (RotateAroundCell() 애니메이션 완료 직후)
    public static void RaiseAfterMapRotated(bool freeze)                      => AfterMapRotated?.Invoke(freeze);
    
    // 호출 위치: MapManager.cs (맵 회전 후 타일 아이콘들의 역회전 보정 완료 시점)
    public static void RaiseTileIconRotated(float angle)                      => TileIconRotated?.Invoke(angle);

    #endregion

    #region Tile Toggle

    // layer: 이벤트를 발생시킨 타일(= 플레이어가 서 있는 타일)의 레이어 — 같은 레이어의 토글 타일만 반응
    public static event Action<int, int>          ToggleTriggered;       // StepOn → ToggleTargeted/TrapToggle용
    public static event Action<TileColor, int>    ColorToggleTriggered;  // ColorToggle용

    // 호출 위치: TileBehaviour.cs (StepOn 타일을 밟아 ApplyTileCommand가 실행될 때)
    public static void RaiseToggleTriggered(int count = -1, int layer = 0)        => ToggleTriggered?.Invoke(count, layer);
    
    // 호출 위치: TileBehaviour.cs (ColorToggle 타일을 밟아 ApplyTileCommand가 실행될 때)
    public static void RaiseColorToggleTriggered(TileColor color, int layer = 0)  => ColorToggleTriggered?.Invoke(color, layer);

    #endregion

    #region Chat Commands

    // 채팅 입력으로 발동되는 커맨드 이벤트 (키보드 입력과 동일한 효과)
    public static event Action ChatCommandSuicide;    // "suicide"       → 플레이어 PlayExplosion
    public static event Action ChatCommandRotateCW;   // "rotate"        → LeftALT와 동일
    public static event Action ChatCommandRotateCCW;  // "counterrotate" → TAB과 동일
    public static event Action ChatCommandMove;       // "move"          → F4와 동일

    // 채팅 입력으로 발동되는 이스터에그 이벤트
    public static event Action ChatCommandDance;    // "dance"      → 적 Dance 애니메이션
    public static event Action ChatCommandLove;     // "i love you" → 적 Love 애니메이션
    public static event Action ChatCommandWhistle;  // "whistle"    → 휘파람 효과음

    // 호출 위치: 트위치/유튜브 채팅 연동 매니저 스크립트 (ChatManager.cs 등)에서 특정 채팅 감지 시
    public static void RaiseChatCommandSuicide()   => ChatCommandSuicide?.Invoke();
    public static void RaiseChatCommandRotateCW()  => ChatCommandRotateCW?.Invoke();
    public static void RaiseChatCommandRotateCCW() => ChatCommandRotateCCW?.Invoke();
    public static void RaiseChatCommandMove()      => ChatCommandMove?.Invoke();
    public static void RaiseChatCommandDance()     => ChatCommandDance?.Invoke();
    public static void RaiseChatCommandLove()      => ChatCommandLove?.Invoke();
    public static void RaiseChatCommandWhistle()   => ChatCommandWhistle?.Invoke();

    #endregion
    
    // MapManager에서 맵 스폰이 끝난 뒤, EnemyManager에서 MapManager의 타일맵을 참조하여 적을 스폰시키기 위한 이벤트
    public static event Action MapInitialized;
    
    // 호출 위치: MapManager.cs (InitializeNewStage() 실행 완료 직후)
    public static void RaiseMapInitialized() => MapInitialized?.Invoke();
    
    
    // Steam의 도전과제 및 유저 플레이 타임 및 이탈율을 로그로 기록하기 위한 이벤트
    #region StageRecorder
    
    public static event Action<int, int> StageRecordStarted;   // chapter, stage
    public static event Action StageRecordEnded; // 세션 종료 (챕터/스테이지/시간은 StageRecorder 내부에서 추적)
    public static event Action<int, int> StageAbandoned;        // StageSelect이동/종료 시

    // 호출 위치: StageManager.cs 또는 GameManager.cs (스테이지 진입 시)
    public static void RaiseStageRecordStarted(int ch, int st) => StageRecordStarted?.Invoke(ch, st);
    
    // 호출 위치: StageManager.cs 또는 GameManager.cs (스테이지 클리어 후 결과창 출력 시)
    public static void RaiseStageRecordEnded() => StageRecordEnded?.Invoke();
    
    // 호출 위치: UIManager.cs (일시정지 후 로비/스테이지 선택창으로 나갈 때)
    public static void RaiseStageAbandoned(int ch, int st) => StageAbandoned?.Invoke(ch, st);

    #endregion

    #region Mission Tracking

    // Star 타일을 플레이어가 밟아 수집했을 때
    public static event Action StarCollected;
    // 호출 위치: TileBehaviour.cs (Star 타일을 밟아 ApplyTileCommand가 실행될 때)
    public static void RaiseStarCollected() => StarCollected?.Invoke();

    // ALT / F4 / TAB 키를 사용할 때마다 발생 (누적 도전과제 추적용)
    public static event Action<KeyType> KeyUsed;
    // 호출 위치: PlayerInputHandler.cs (EnqueueCommand() 실행될 때마다)
    public static void RaiseKeyUsed(KeyType keyType) => KeyUsed?.Invoke(keyType);

    // ALT+TAB으로 맵이 전환될 때 발생 (Undo 제외, 실제 입력만 카운트)
    public static event Action MapSwitched;
    // 호출 위치: PlayerInputHandler.cs (Map Switch 조작 시) 또는 TileMapChangeCommand.cs
    public static void RaiseMapSwitched() => MapSwitched?.Invoke();

    #endregion

    public static event Action GlitchTriggered;
    // 호출 위치: CanvasShake.cs (또는 특정 타일 기믹/게임오버 연출 등에서 호출)
    public static void RaiseGlitchTriggered() => GlitchTriggered?.Invoke();

}