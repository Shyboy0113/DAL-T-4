// 게임에서 작동되는 여러 이벤트들을 담당하는 클래스
using System;
using UnityEngine;

public static class GameEvents
{
    //public static Directory<string, Action> action =
    
    public static event Action StageCleared;
    public static event Action StageRestarted;
    public static event Action PlayerDied;
    public static event Action<bool> InputLockChanged;
    public static event Action TileMapChanged;
    public static event Action<Vector3Int, float> TileMapRotated;

    public static event Action<TileColor> ColorToggleTriggered; // ColorToggle용

    // 플레이어가 타일을 벗어났을 때 작동하는 이벤트

    public static event Action<bool> IsRotating;
    
    public static event Action<int> ToggleTriggered; // StepOnToggle용
    public static event Action<int> PlayerActed;     // ActiveToggle용 (이동/회전 합산)
    public static event Action<int> PlayerMoved;     // MoveToggle용
    public static event Action<int> PlayerRotated;   // RotationToggle용

    #region Command Pattern

    public static event Action UndoTriggered; // Undo 발생 신호
    public static event Action RedoTriggered;
    public static event Action SaveStateBeforeAction;

    // (undoCount , redoCount)
    public static event Action<int, int> UndoRedoCountChanged;
    
    public static void RaiseUndoTriggered()
    {
        UndoTriggered?.Invoke();
    }

    public static void RaiseRedoTriggered()
    {
        RedoTriggered?.Invoke();
    }

    public static void RaiseSaveStateBeforeAction()
    {
        SaveStateBeforeAction?.Invoke();
    }
    
    public static void RaiseUndoRedoCountChanged(int undoCount, int redoCount)
    {
        UndoRedoCountChanged?.Invoke(undoCount, redoCount);
    }

    #endregion

    public static event Action<float> TileIconRotated;


    #region Player/Enemy Turn

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

    public static void RaiseInputLockChanged(bool isLocked)
    {
        InputLockChanged?.Invoke(isLocked);
    }

    public static void RaiseTileMapChanged()
    {
        TileMapChanged?.Invoke();
    }

    public static void RaiseTileMapRotated(Vector3Int cell, float angle)
    {
        TileMapRotated?.Invoke(cell, angle);
    }

    public static void RaiseStageRestarted()
    {
        StageRestarted?.Invoke();
    }
    
    public static void RaiseIsRotating(bool toggle)
    {
        IsRotating?.Invoke(toggle);
    }

    
}