using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class InputManager : MonoBehaviour
{
    [SerializeField]
    private MapDataLoader _mapDataLoader;
    [SerializeField]
    private ResetStackManager resetStackManager;

    public int chapter;
    public int stage;

    //NullReferenceException 방지용 토글
    private bool _ismapDataLoaded = true;
    private bool _isResetStack = true;

    void Awake()
    {       
        //SerialziedFireld를 쓰면 GetComponent 빼도 됨
        //외부 오브젝트에서 클래스를 가져오고, 내부에 해당 클래스가 존재하지 않으면 GetComponent를 쓸 경우 Null Error가 뜸

        //NullReferenceException 방지용 확인 토글 및 코드
        if (_mapDataLoader is null)
        {
            Debug.Log("Can't find the gameStateManager!!");
            _ismapDataLoaded = false;
        }

        if (resetStackManager is null)
        {
            Debug.Log("Can't find the resetStackManager!!");
            _isResetStack = false;
        }
    }
    private void Start()
    {
        _mapDataLoader.GetStageData(chapter,stage);
    }

    void Update()
    {
        if (_ismapDataLoaded && resetStackManager.IsGameOver()) return;

        if (_isResetStack)
        {

            if (Input.GetKeyDown(KeyCode.LeftAlt) && _mapDataLoader.CanUseF4())
            {
                resetStackManager.ProcessAltInput();
            }

            if (Input.GetKeyDown(KeyCode.F4) && _mapDataLoader.CanUseF4())
            {
                resetStackManager.ProcessF4Input();
            }

            if (Input.GetKeyDown(KeyCode.Tab) && _mapDataLoader.CanUseTab())
            {
                resetStackManager.ProcessTabInput();
            }
        }
    }
}

