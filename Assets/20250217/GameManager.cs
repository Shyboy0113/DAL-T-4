using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    //싱글톤 패턴화
    public static GameManager Instance { get; private set; }

    [SerializeField]
    private MapDataLoader _mapDataLoader;
    [SerializeField]
    private StackManager _stackManager;
    [SerializeField]
    private PlayerPrefsManager _playerPrefsManager;

    //선택한 맵의 정보
    public StageData currentStageData;

    public int chapter;
    public int stage;

    //NullReferenceException 방지용 토글
    private bool _ismapDataLoaded = false;
    [SerializeField]
    private bool _isStackManagerLoaded = false;

    //게임 상태
    public bool isGameOver = false;
    public bool isCleared = false;

    //도전 과제용 데이터
    public float currentTime;
    public float shortestClearTime;
        
    public int pushedNumberALT;
    public int pushedNumberF4;
    public int pushedNumberTAB;


    void Awake()
    {
        //싱글톤 구현
        if (Instance == null) Instance = this;
        else Destroy(gameObject);


        //NullReferenceException 방지용 확인 토글 및 코드
        if (_mapDataLoader is null)
        {
            Debug.Log("Can't find the mapData!!");
            _ismapDataLoaded = false;
        }
    }

    private void Start()
    {
        //추후에 에디터 작업 끝나고나면, 삭제해야 합니다.
        currentStageData = _mapDataLoader.GetStageData(1, 1);
    }

    void Update()
    {
        if (isGameOver || isCleared ) return;

        currentTime += Time.deltaTime;

        if (_isStackManagerLoaded)
        {

            if (Input.GetKeyDown(KeyCode.LeftAlt) && currentStageData.canUseF4)
            {
                _stackManager.ProcessAltInput();
                pushedNumberALT += 1;
            }

            if (Input.GetKeyDown(KeyCode.F4) && currentStageData.canUseF4)
            {
                _stackManager.ProcessF4Input();
                pushedNumberF4 += 1;
            }

            if (Input.GetKeyDown(KeyCode.Tab) && currentStageData.canUseTab)
            {
                _stackManager.ProcessTabInput();
                pushedNumberTAB += 1;
            }
        }
    }
    
    public void GameClear()
    {
        isCleared = true;
        _playerPrefsManager.ReportData(_mapDataLoader, currentStageData);
    }

    public void TileOut()
    {
        if (_isStackManagerLoaded)
        {
            _stackManager.PlayExplosion();
        }
    }
    public void ConnectStackManager()
    {
        _stackManager = FindObjectOfType<StackManager>();

        if(_stackManager is null)
        {
            Debug.Log("Failed To Connect StackManager!");
        }
        else
        {
            Debug.Log("Succeed To Connect StackManager!");

            _isStackManagerLoaded = true;
        }
    }

    public void DisconnectStackManager()
    {
        _stackManager = null;
        _isStackManagerLoaded = false;
    }

    private void OnDestroy()
    {
        Instance = null;
    }
}

