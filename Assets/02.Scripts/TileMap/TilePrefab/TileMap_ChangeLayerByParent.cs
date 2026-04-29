using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TileMap_ChangeLayerByParent : MonoBehaviour
{
    private void Awake()
    {
        ApplyParentLayer();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // OnValidate 안에서 직접 레이어를 바꾸면 Canvas의 SendMessage가 터지므로 지연 호출
        EditorApplication.delayCall += ApplyParentLayer;
#endif
    }

    private void ApplyParentLayer()
    {
        if (this == null) return; // delayCall 시점에 오브젝트가 파괴됐을 수 있음

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