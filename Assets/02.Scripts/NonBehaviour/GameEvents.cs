
// 게임에서 작동되는 여러 이벤트들을 담당하는 클래스.
// 굳이 상속받을 필요 없이 그대로 둬도 됨

using System;

public static class GameEvents
{
    public static event Action PlayerMoved;
    public static event Action StageCleared;
    public static event Action PlayerDied;

    public static event Action<bool> InputLockChanged;

    public static void RaisePlayerMoved()
    {
        PlayerMoved?.Invoke();
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
    
}
