using System;
using UnityEngine;

public static class GameEvents
{
    #region Game State

    public static event Action StageCleared;
    public static event Action StageRestarted;
    public static event Action PlayerDied;
    public static event Action EnemyDied;

    public static void RaiseStageCleared()    => StageCleared?.Invoke();
    public static void RaiseStageRestarted()  => StageRestarted?.Invoke();
    public static void RaisePlayerDied()      => PlayerDied?.Invoke();
    public static void RaiseEnemyDied()       => EnemyDied?.Invoke();

    #endregion

    #region Input Control

    public static event Action<bool> InputLockChanged;

    public static void RaiseInputLockChanged(bool isLocked) => InputLockChanged?.Invoke(isLocked);

    #endregion

    #region Turn Flow
    // 플레이어/적 이동 후, 타일이 OnTriggerEnter에서 등록한 pending 효과를 실행하는 턴
    public static event Action TileLogicTurnStarted;
    public static void RaiseTileLogicTurnStarted() => TileLogicTurnStarted?.Invoke();

    // Ice 슬라이딩 중 매 물리 스텝 후 발화 — Stop/StartTeleport 타일만 수신
    public static event Action IceTileLogicTurnStarted;
    public static void RaiseIceTileLogicTurnStarted() => IceTileLogicTurnStarted?.Invoke();

    // 타일 로직 턴 후, Player/Enemy가 낙사 등 물리 사망 여부를 판정하는 턴
    public static event Action PhysicsTurnStarted;
    public static void RaisePhysicsTurnStarted() => PhysicsTurnStarted?.Invoke();

    // 플레이어 행동 하나(이동/회전)가 완전히 끝났을 때 발생
    // BehaviourManager가 수신해서 액션 카운트 증가 및 적 턴 전환 처리
    public static event Action PlayerActionFinished;
    public static void RaisePlayerActionFinished() => PlayerActionFinished?.Invoke();

    public static event Action<Vector3> OnEnemyTurnStarted;
    public static event Action           OnPlayerTurnStarted;

    public static void RaiseEnemyTurnStarted(Vector3 playerPosition) => OnEnemyTurnStarted?.Invoke(playerPosition);
    public static void RaisePlayerTurnStarted()                       => OnPlayerTurnStarted?.Invoke();

    #endregion

    #region Player Action Counter
    // 각 타일의 토글 카운터 판정에 사용

    public static event Action<int> PlayerActed;    // ActiveToggle용  (이동 + 회전 합산)
    public static event Action<int> PlayerMoved;    // MoveToggle용
    public static event Action<int> PlayerRotated;  // RotationToggle용

    public static void RaisePlayerActed(int count)   => PlayerActed?.Invoke(count);
    public static void RaisePlayerMoved(int count)   => PlayerMoved?.Invoke(count);
    public static void RaisePlayerRotated(int count) => PlayerRotated?.Invoke(count);

    #endregion

    #region Undo / Command Pattern

    public static event Action                    UndoTriggered;
    public static event Action<PlayerBehaviour>   SaveStateBeforeAction;
    public static event Action<int, int>          UndoCountChanged;

    public static void RaiseUndoTriggered()                              => UndoTriggered?.Invoke();
    public static void RaiseSaveStateBeforeAction(PlayerBehaviour pb)    => SaveStateBeforeAction?.Invoke(pb);
    public static void RaiseUndoCountChanged(int undoCount, int count)   => UndoCountChanged?.Invoke(undoCount, count);

    #endregion

    #region TileMap

    public static event Action                        TileMapChanged;
    public static event Action<PlayerBehaviour, float> TileMapRotated;
    public static event Action<bool>                  BeforeMapRotated;  // 회전 직전
    public static event Action<bool>                  AfterMapRotated;   // 회전 직후
    public static event Action<float>                 TileIconRotated;

    public static void RaiseTileMapChanged()                                  => TileMapChanged?.Invoke();
    public static void RaiseTileMapRotated(PlayerBehaviour pb, float angle)   => TileMapRotated?.Invoke(pb, angle);
    public static void RaiseBeforeMapRotated(bool freeze)                     => BeforeMapRotated?.Invoke(freeze);
    public static void RaiseAfterMapRotated(bool freeze)                      => AfterMapRotated?.Invoke(freeze);
    public static void RaiseTileIconRotated(float angle)                      => TileIconRotated?.Invoke(angle);

    #endregion

    #region Tile Toggle

    public static event Action<int>       ToggleTriggered;       // StepOn → ToggleTargeted/TrapToggle용
    public static event Action<TileColor> ColorToggleTriggered;  // ColorToggle용

    public static void RaiseToggleTriggered(int count = -1)        => ToggleTriggered?.Invoke(count);
    public static void RaiseColorToggleTriggered(TileColor color)  => ColorToggleTriggered?.Invoke(color);

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
    public static void RaiseMapInitialized() => MapInitialized?.Invoke();
    
    
    
    
    // Steam의 도전과제 및 유저 플레이 타임 및 이탈율을 로그로 기록하기 위한 이벤트
    #region StageRecorder
    
    public static event Action<int, int> StageRecordStarted;   // chapter, stage
    public static event Action StageRecordEnded; // 세션 종료 (챕터/스테이지/시간은 StageRecorder 내부에서 추적)
    public static event Action<int, int> StageAbandoned;        // StageSelect이동/종료 시

    public static void RaiseStageRecordStarted(int ch, int st) =>
        StageRecordStarted?.Invoke(ch, st);
    public static void RaiseStageRecordEnded() => StageRecordEnded?.Invoke();
    public static void RaiseStageAbandoned(int ch, int st) => StageAbandoned?.Invoke(ch, st);

    #endregion

    #region Mission Tracking

    // Star 타일을 플레이어가 밟아 수집했을 때
    public static event Action StarCollected;
    public static void RaiseStarCollected() => StarCollected?.Invoke();

    // ALT / F4 / TAB 키를 사용할 때마다 발생 (누적 도전과제 추적용)
    public static event Action<KeyType> KeyUsed;
    public static void RaiseKeyUsed(KeyType keyType) => KeyUsed?.Invoke(keyType);

    #endregion
}
