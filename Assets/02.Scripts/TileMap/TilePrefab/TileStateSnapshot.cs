using UnityEngine;

[System.Serializable]
public class TileStateSnapshot
{
    public int hitCount;
    public bool isToggled;
    public int playerMoveCount;
    public int playerRotationCount;
    public int playerTotalActionCount;
    public Quaternion rotation;
    public bool isVisible;   // Breakable 타일의 파괴 여부 체크용
    public bool isShaking;   // Breakable 타일의 흔들림 상태

    public TileStateSnapshot(int hit, bool toggled, int moveCount, int rotationCount, int totalActionCount, Quaternion rot, bool visible, bool shaking)
    {
        hitCount = hit;
        isToggled = toggled;
        playerMoveCount = moveCount;
        playerRotationCount = rotationCount;
        playerTotalActionCount = totalActionCount;
        rotation = rot;
        isVisible = visible;
        isShaking = shaking;
    }
}