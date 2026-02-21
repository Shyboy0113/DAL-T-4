using UnityEngine;

[CreateAssetMenu(fileName = "NewTileData", menuName = "ScriptableObject/TileData")]
public class SOTileData : ScriptableObject
{

    [Header("Base Identity")]
    public TileType tileType;

    [Header("Base Stats")]
    public int baseMaxActivationCount = -1; // -1은 무제한
    public int baseBreakHitCount = 2;
    public float baseBreakDelay = 0.5f;

    [Header("Base Toggle Settings")]
    public int baseToggleActivationCount = 2;
    public TileColor baseColor = TileColor.White;

}
