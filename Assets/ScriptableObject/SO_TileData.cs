using UnityEngine;

[CreateAssetMenu(fileName = "SO_TileType", menuName = "ScriptableObject/TileData")]
public class SO_TileData : ScriptableObject
{

    [Header("Base Identity")]
    public TileType tileType;

    [Header("Base Stats")]
    public int baseMaxBreakCount = 4;
    public int baseBreakHitCount = 0;
    public float baseBreakDelay = 0.5f;

    [Header("Base Toggle Settings")]
    public int baseToggleActivationCount = 2;
    public TileColor baseColor = TileColor.White;

    [Header("Teleport Settings")]
    public int baseTeleportID = 0; // ID가 같은 Start/End Teleport 타일끼리 연결
    

}
