using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LetterboxCamera : MonoBehaviour
{
    private const float BaseAspect = 16f / 9f;
    private const int   BaseWidth  = 1920;
    private const int   BaseHeight = 1080;

    private Camera _cam;

    private int  _lastW = -1;
    private int  _lastH = -1;
    private bool _lastFs;
    private bool _initialized;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Update()
    {
        int  curW  = Screen.width;
        int  curH  = Screen.height;
        bool curFs = Screen.fullScreen;

        if (!_initialized || curW != _lastW || curH != _lastH || curFs != _lastFs)
        {
            _lastW       = curW;
            _lastH       = curH;
            _lastFs      = curFs;
            _initialized = true;

            int selectedW = PlayerPrefs.GetInt("ResolutionWidth",  1920);
            int selectedH = PlayerPrefs.GetInt("ResolutionHeight", 1080);
            Apply(selectedW, selectedH, curFs);
        }
    }

    public void Apply(int selectedWidth, int selectedHeight, bool isFullScreen)
    {
        // 창모드면 뷰포트 풀로
        if (!isFullScreen)
        {
            _cam.rect = new Rect(0, 0, 1, 1);
            return;
        }

        // 전체화면: Outer = 모니터 네이티브
        int outerW = Screen.currentResolution.width;
        int outerH = Screen.currentResolution.height;

        int innerW, innerH;

        if (selectedWidth <= BaseWidth && selectedHeight <= BaseHeight)
        {
            // 선택 해상도가 기준(1920x1080) 이하 → 선택 크기 그대로 가운데 배치
            innerW = selectedWidth;
            innerH = selectedHeight;
        }
        else
        {
            // 선택 해상도가 기준 초과 → 네이티브 안에서 16:9 최대 영역 계산
            float outerAspect = (float)outerW / outerH;

            if (outerAspect >= BaseAspect)
            {
                // 모니터가 16:9보다 넓다 → 세로를 채우고 좌우 레터박스
                innerH = outerH;
                innerW = Mathf.RoundToInt(outerH * BaseAspect);
            }
            else
            {
                // 모니터가 16:9보다 좁다 (예: 2560x1600) → 가로를 채우고 위아래 레터박스
                innerW = outerW;
                innerH = Mathf.RoundToInt(outerW / BaseAspect);
            }
        }

        float vpW = (float)innerW / outerW;
        float vpH = (float)innerH / outerH;

        _cam.rect = new Rect(
            (1f - vpW) / 2f,
            (1f - vpH) / 2f,
            vpW,
            vpH
        );
    }
}