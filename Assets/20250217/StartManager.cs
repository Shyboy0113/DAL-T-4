using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{    
    //싱글톤에 기존 찌꺼기 데이터들이 남아있지 않게 초기 설정해주는 스크립트입니다.
    private void Start()
    {

        //스택 상태 초기화
        GameManager.Instance.DisconnectStackManager();
        GameManager.Instance.ConnectStackManager();

        //게임 상태 초기화
        GameManager.Instance.isGameOver = false;
        GameManager.Instance.isCleared = false;

        //도전과제 초기화
        GameManager.Instance.currentTime = 0f;
        GameManager.Instance.pushedNumberALT = 0;
        GameManager.Instance.pushedNumberF4 = 0;
        GameManager.Instance.pushedNumberTAB = 0;
}

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
