// 게임에서 작동되는 여러 이벤트들을 담당하는 클래스
using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action StageCleared;
    public static event Action PlayerDied;
    public static event Action<bool> InputLockChanged;
    public static event Action TileMapChanged;
    public static event Action<Vector3Int, float> TileMapRotated;

    public static event Action<TileColor> ColorToggleTriggered; // ColorToggle용

    // 플레이어가 타일을 벗어났을 때 작동하는 이벤트

    public static event Action PlayerExitedTile;

    public static event Action<int> ToggleTriggered; // StepOnToggle용
    public static event Action<int> PlayerActed;     // ActiveToggle용 (이동/회전 합산)
    public static event Action<int> PlayerMoved;     // MoveToggle용
    public static event Action<int> PlayerRotated;   // RotationToggle용

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

    public static void RaisePlayerExitedTile()
    {
        PlayerExitedTile?.Invoke();
    }
}