using UnityEngine;

public class Enemy_ChangeLayerByParent : MonoBehaviour
{
    public void ApplyParentLayer()
    {
        if (this == null) return; // delayCall 시점에 오브젝트가 파괴됐을 수 있음

        if (transform.parent != null)
        {
            int parentLayer = transform.parent.gameObject.layer;
            SetLayerRecursively(this.gameObject, parentLayer);
        }
    }

    public void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
