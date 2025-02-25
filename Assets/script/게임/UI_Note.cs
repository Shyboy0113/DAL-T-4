using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Note : MonoBehaviour
{
    public float noteSpeed = 5f;

    // Update is called once per frame
    void Update()
    {
        transform.localPosition += Vector3.right * noteSpeed * Time.deltaTime;
    }
}
