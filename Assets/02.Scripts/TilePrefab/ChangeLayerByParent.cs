/*
 *  TilePrefab에 붙어서 프리팹과 자식 오브젝트들의 Layer를 부모 TileMap의 Layer로 설정
 */

using UnityEngine;

public class ChangeLayerByParent : MonoBehaviour
{
    private void Awake()
    {
        ApplyParentLayer();
    }

    private void OnValidate()
    {
        ApplyParentLayer();
    }

    private void ApplyParentLayer()
    {
        if (transform.parent != null)
        {
            int parentLayer = transform.parent.gameObject.layer;
            SetLayerRecursively(this.gameObject, parentLayer);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
        
    }
}
