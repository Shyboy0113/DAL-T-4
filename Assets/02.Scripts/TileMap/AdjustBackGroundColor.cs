using UnityEngine;
using System.Collections.Generic;

public class AdjustBackGroundColor : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField] private Color tileColor = Color.white;

    [SerializeField] private List<SpriteRenderer> backGroundSprites = new List<SpriteRenderer>();
    
    // 인스펙터에서 값이 바뀔 때마다 실행됨
    private void OnValidate()
    {
        AdjustColorToChildren();
    }

    private void Awake()
    {
        AdjustColorToChildren();
    }

    // 인스펙터 우클릭 메뉴로 수동 실행 가능
    [ContextMenu("Apply Colors Now")]
    public void AdjustColorToChildren()
    {
        backGroundSprites.Clear();
        
        // Tilemap_First의 직계 자식들을 순회 (TilePrefab들)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "BackGround Sprite")
            {
                if (child.TryGetComponent(out SpriteRenderer sr))
                {
                    sr.color = tileColor;
                    backGroundSprites.Add(sr);
                }
            }
        }
    }
}
