// 게임에서 작동되는 여러 이벤트들을 담당하는 클래스
using System;
using UnityEngine;

public static class GameEvents
{
    //public static Directory<string, Action> action =
    
    public static event Action StageCleared;
    public static event Action StageRestarted;
    public static event Action PlayerDied;
    public static event Action EnemyDied;
    public static event Action<bool> InputLockChanged;
    public static event Action TileMapChanged;
    public static event Action<TileColor> ColorToggleTriggered; // ColorToggle용

    // 플레이어가 타일을 벗어났을 때 작동하는 이벤트    
    public static event Action<int> ToggleTriggered; // StepOnToggle용
    public static event Action<int> PlayerActed;     // ActiveToggle용 (이동/회전 합산)
    public static event Action<int> PlayerMoved;     // MoveToggle용
    public static event Action<int> PlayerRotated;   // RotationToggle용

    #region Command Pattern

    public static event Action UndoTriggered;
    public static event Action<PlayerBehaviour> SaveStateBeforeAction;
    public static void RaiseSaveStateBeforeAction(PlayerBehaviour pb) => SaveStateBeforeAction?.Invoke(pb);

    public static event Action<int, int> UndoCountChanged;

    public static void RaiseUndoTriggered()
    {
        UndoTriggered?.Invoke();
    }

    public static void RaiseUndoCountChanged(int undoCount, int Count)
    {
        UndoCountChanged?.Invoke(undoCount, Count);
    }

    #endregion

    public static event Action<float> TileIconRotated;


    #region Player/Enemy Turn

    // 플레이어 행동 + 타일 반응(토글/물리)이 모두 끝났을 때 발생
    // MoveCommand → OnTriggerEnter → TileCommand 완료 후 TileBehaviour에서 발생
    // RotateCommand → DOTween OnComplete 후 PlayerBehaviour에서 발생
    // BehaviourManager가 이 이벤트를 수신해서 _actionCount++ 및 적 턴 전환을 처리
    public static event Action PlayerActionFinished;

    public static void RaisePlayerActionFinished()
    {
        PlayerActionFinished?.Invoke();
    }

    public static event Action<Vector3> OnEnemyTurnStarted;
    public static event Action OnPlayerTurnStarted;

    public static void RaiseEnemyTurnStarted(Vector3 playerPosition)
    {
        OnEnemyTurnStarted?.Invoke(playerPosition);
    }
    
    public static void RaisePlayerTurnStarted()
    {
        OnPlayerTurnStarted?.Invoke();
    }
    
    #endregion
    
    
    public static void RaiseTileIconRotated(float angle)
    {
        TileIconRotated?.Invoke(angle);
    }
    
    public static void RaiseColorToggleTriggered(TileColor color)
    {
        ColorToggleTriggered?.Invoke(color);
    }

    public static void RaiseToggleTriggered(int count = -1)
    {
        ToggleTriggered?.Invoke(count);
    }

    public static void RaisePlayerActed(int count)
    {
        PlayerActed?.Invoke(count);
    }

    public static void RaisePlayerMoved(int count)
    {
        PlayerMoved?.Invoke(count);   
    }

    public static void RaisePlayerRotated(int count)
    {    
        PlayerRotated?.Invoke(count);
    }

    public static void RaiseStageCleared()
    {
        StageCleared?.Invoke();
    }

    public static void RaisePlayerDied()
    {
        PlayerDied?.Invoke();
    }

    public static void RaiseEnemyDied()
    {
        EnemyDied?.Invoke();
    }

    public static void RaiseInputLockChanged(bool isLocked)
    {
        Debug.Log($"InputLock: {isLocked}\n{System.Environment.StackTrace}");
        InputLockChanged?.Invoke(isLocked);
    }

    public static void RaiseTileMapChanged()
    {
        TileMapChanged?.Invoke();
    }    

    public static void RaiseStageRestarted()
    {
        StageRestarted?.Invoke();
    }

    #region Chat Commands

    // 채팅 입력으로 발동되는 커맨드 이벤트 (키보드 입력과 동일한 효과)
    public static event Action ChatCommandRotateCW;   // "rotate"   → LeftALT와 동일
    public static event Action ChatCommandRotateCCW;  // "counterrotate" → TAB과 동일
    public static event Action ChatCommandMove;       // "move"     → F4와 동일

    // 채팅 입력으로 발동되는 이스터에그 이벤트
    public static event Action ChatCommandDance;      // "dance"    → 적 Dance 애니메이션
    public static event Action ChatCommandLove;       // "i love you" → 적 Love 애니메이션
    public static event Action ChatCommandWhistle;    // "whistle"  → 휘파람 효과음

    public static void RaiseChatCommandRotateCW()  => ChatCommandRotateCW?.Invoke();
    public static void RaiseChatCommandRotateCCW() => ChatCommandRotateCCW?.Invoke();
    public static void RaiseChatCommandMove()      => ChatCommandMove?.Invoke();
    public static void RaiseChatCommandDance()     => ChatCommandDance?.Invoke();
    public static void RaiseChatCommandLove()      => ChatCommandLove?.Invoke();
    public static void RaiseChatCommandWhistle()   => ChatCommandWhistle?.Invoke();

    #endregion

    #region TileMap
    
    public static event Action<PlayerBehaviour, float> TileMapRotated;
    public static void RaiseTileMapRotated(PlayerBehaviour pb, float angle) => TileMapRotated?.Invoke(pb, angle);
    
    // 맵이 회전하기 전 발생하는 이벤트
    public static event Action<bool> BeforeMapRotated;
    public static void RaiseBeforeMapRotated(bool freeze) => BeforeMapRotated?.Invoke(freeze);

    // 맵이 회전하고 난 뒤에 발생하는 이벤트
    public static event Action<bool> AfterMapRotated;
    public static void RaiseAfterMapRotated(bool freeze) => AfterMapRotated?.Invoke(freeze);

    #endregion

    
}