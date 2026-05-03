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

    // [수정 포인트 1] 현재 타겟으로 삼고 있는 해상도를 기억할 변수 추가
    private int _targetWidth;
    private int _targetHeight;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        
        // [수정 포인트 2] 처음에만 PlayerPrefs에서 저장된 값을 가져와 초기 타겟으로 설정합니다.
        _targetWidth  = PlayerPrefs.GetInt("ResolutionWidth", BaseWidth);
        _targetHeight = PlayerPrefs.GetInt("ResolutionHeight", BaseHeight);
    }

    private void Update()
    {
        int  curW  = Screen.width;
        int  curH  = Screen.height;
        bool curFs = Screen.fullScreen;

        // 화면 크기나 전체화면 여부가 변했다면 (창 드래그, 옵션 조작 등)
        if (!_initialized || curW != _lastW || curH != _lastH || curFs != _lastFs)
        {
            _lastW       = curW;
            _lastH       = curH;
            _lastFs      = curFs;
            _initialized = true;

            // [수정 포인트 3] PlayerPrefs를 다시 읽지 않고, 기억해둔 타겟 해상도로 Apply를 호출합니다.
            Apply(_targetWidth, _targetHeight, curFs);
        }
    }

    public void Apply(int selectedWidth, int selectedHeight, bool isFullScreen)
    {
        // [수정 포인트 4] 외부(옵션 창 등)에서 Apply가 호출되면, 타겟 해상도를 새 값으로 갱신해 줍니다.
        _targetWidth = selectedWidth;
        _targetHeight = selectedHeight;

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
            innerW = selectedWidth;
            innerH = selectedHeight;
        }
        else
        {
            float outerAspect = (float)outerW / outerH;

            if (outerAspect >= BaseAspect)
            {
                innerH = outerH;
                innerW = Mathf.RoundToInt(outerH * BaseAspect);
            }
            else
            {
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