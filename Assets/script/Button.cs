using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Button : MonoBehaviour
{
    public AudioSource[] Sound_Effect;
    float cnt = 0;
    void Start()
    {

        Sound_Effect = GetComponents<AudioSource>();
        
    }
    
    void Update()
    {


        if (Input.GetKeyDown(KeyCode.F4))
        {
            Sound_Effect[0].Play();
        }

        if (Input.GetKey(KeyCode.LeftAlt)) { cnt += Time.deltaTime; }
        if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            Debug.Log(cnt);
            if (cnt >= 0.3f)
                Sound_Effect[2].Play();
            else
                Sound_Effect[1].Play();
            cnt = 0;

        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Sound_Effect[3].Play();
        }


        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("f를 눌렀습니다.");
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("4를 눌렀습니다.");
        }

    }
}
