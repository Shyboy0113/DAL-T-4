using UnityEngine;

[System.Serializable]
public class TileStateSnapshot
{
    public int hitCount;
    public bool isToggled;
    public bool playerIsMap1;
    public int playerLayer;
    public int playerMap1MoveCount;
    public int playerMap1RotationCount;
    public int playerMap1ActionCount;
    public int playerMap2MoveCount;
    public int playerMap2RotationCount;
    public int playerMap2ActionCount;
    public int playerTotalActionCount;

    public Quaternion rotation;
    public bool isVisible;   // Breakable 타일의 파괴 여부 체크용
    public bool isShaking;   // Breakable 타일의 흔들림 상태

    public Vector3 localPosition;
    
    public TileStateSnapshot(int hit, bool toggled, bool isMap1, int layer,
        int map1MoveCount, int map1RotationCount, int map1ActionCount,
        int map2MoveCount, int map2RotationCount, int map2ActionCount,
        int totalActionCount,
        Quaternion rot, bool visible, bool shaking, Vector3 snapLocalPosition)
    {
        hitCount = hit;
        isToggled = toggled;
        playerIsMap1 = isMap1;
        playerLayer = layer;
        playerMap1MoveCount = map1MoveCount;
        playerMap1RotationCount = map1RotationCount;
        playerMap1ActionCount = map1ActionCount;
        playerMap2MoveCount = map2MoveCount;
        playerMap2RotationCount = map2RotationCount;
        playerMap2ActionCount = map2ActionCount;
        playerTotalActionCount = totalActionCount;
        rotation = rot;
        isVisible = visible;
        isShaking = shaking;
        localPosition = snapLocalPosition;
    }
}