using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{    
    //싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.
    private void Start()
    {
        GameManager.Instance.ResetStage();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
