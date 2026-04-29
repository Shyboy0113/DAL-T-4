# 해상도/레터박스 시스템 수정 명세서

## 0. 작업 개요

현재 프로젝트의 해상도 옵션 시스템(`Option_Resolution`, `LetterboxCamera`, `CutoutFade`)을 아래 규칙에 맞게 수정한다.

### 관련 파일
- `Assets/.../Option_Resolution.cs`
- `Assets/.../LetterboxCamera.cs`
- `Assets/.../LetterboxBackground.cs` (변경 없음, 참고만)
- `Assets/.../CutoutFade.cs`
- `Assets/.../Option_UIHandler.cs` (변경 없음, 참고만)

### 기준 상수
```csharp
BaseWidth   = 1920
BaseHeight  = 1080
BaseAspect  = 16f / 9f
```

---

## 1. 핵심 규칙

### 규칙 1. 기준 해상도
- 기준 해상도는 **1920×1080 (16:9)**.
- 모든 UI는 1920×1080 기준으로 작성되어 있음.

### 규칙 2. 해상도 옵션 필터링 (신규)
- `Screen.resolutions`에서 가져온 해상도 중 **16:9 비율만** 드롭다운에 표시한다.
- 비율 허용 오차: `AspectTolerance = 0.01f` (1366×768 같은 근사 케이스 포함).
- **1920×1080 초과 해상도는 제외**한다 (UI 기준 해상도가 상한).
- 결과적으로 드롭다운에는 16:9 & ≤ 1920×1080 해상도만 노출됨.
  - 예: 1280×720, 1366×768, 1600×900, 1920×1080

### 규칙 3. 전체화면 + 선택 해상도 ≤ 1920×1080
- 게임 렌더링 영역(inner) = 선택한 해상도 그대로.
- 외곽 영역(outer) = **모니터 네이티브 해상도**(`Screen.currentResolution`).
- 차이만큼 **상하좌우 중앙 정렬 레터박스** 생성.
- 예시:
  - 모니터 2560×1600, 선택 1280×720 → 게임 1280×720, 좌우 640/640, 상하 440/440.
  - 모니터 1920×1080, 선택 1280×720 → 게임 1280×720, 좌우 320/320, 상하 180/180.

### 규칙 4. 창모드 → 레터박스 절대 금지
- 창모드(`Screen.fullScreen == false`)에서는 어떤 상황에서도 카메라 viewport rect = `(0, 0, 1, 1)` 이어야 한다.
- 이전 풀스크린 상태에서 잔존한 rect가 창모드 진입 시 그대로 남아 게임이 창 안에서 또 축소되는 버그 차단.
- **Update()에서 dirty flag 방식으로 매 프레임 가드**한다 (성능 영향 무시 가능).

### 규칙 5. 1920×1080 초과 처리 (참고용 - 본 작업에서는 비활성)
- 규칙 2에 의해 1920×1080 초과 해상도는 옵션에서 제외되므로, 본 작업 범위에서는 발생하지 않음.
- 단, `LetterboxCamera.Apply()`의 16:9 최대 영역 계산 분기는 **삭제하지 말고 유지**한다 (향후 확장 대비).

---

## 2. 수정 사항

### 2.1 Option_Resolution.cs

**`InitializeResolutions()` 수정** — 16:9 필터링 + 1920×1080 상한 추가.

```csharp
private const float TargetAspect = 16f / 9f;
private const float AspectTolerance = 0.01f;

private void InitializeResolutions()
{
    Dictionary<string, Resolution> uniqueResolutions = new Dictionary<string, Resolution>();
    foreach (Resolution res in Screen.resolutions)
    {
        // [추가] 16:9 비율 필터
        float aspect = (float)res.width / res.height;
        if (Mathf.Abs(aspect - TargetAspect) > AspectTolerance) continue;

        // [추가] 1920×1080 초과 제외
        if (res.width > 1920 || res.height > 1080) continue;

        string key = res.width + "x" + res.height;
        if (!uniqueResolutions.ContainsKey(key) ||
            res.refreshRateRatio.value > uniqueResolutions[key].refreshRateRatio.value)
        {
            uniqueResolutions[key] = res;
        }
    }

    // [추가] 안전장치: 필터 결과가 비어있을 경우 1920×1080 강제 추가
    if (uniqueResolutions.Count == 0)
    {
        Resolution fallback = new Resolution { width = 1920, height = 1080 };
        uniqueResolutions["1920x1080"] = fallback;
    }

    List<Resolution> filteredResolutions = new List<Resolution>(uniqueResolutions.Values);
    filteredResolutions.Sort((a, b) =>
    {
        if (a.width != b.width) return a.width.CompareTo(b.width);
        return a.height.CompareTo(b.height);
    });
    resolutions = filteredResolutions.ToArray();

    resolutionDropdown.ClearOptions();
    List<string> options = new List<string>();
    for (int i = 0; i < resolutions.Length; i++)
    {
        string option = resolutions[i].width + " x " + resolutions[i].height + " @ " +
            Mathf.RoundToInt((float)resolutions[i].refreshRateRatio.value) + "hz";
        options.Add(option);
    }
    resolutionDropdown.AddOptions(options);
}
```

**`OnApplicationFocus()` 보강** — OS 레벨(Alt+Enter) 풀스크린 토글 시 dropdown도 동기화.

```csharp
private void OnApplicationFocus(bool hasFocus)
{
    if (hasFocus)
    {
        _selectedFullScreen = Screen.fullScreen;
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);

        // [추가] 현재 Screen 해상도와 dropdown 동기화
        SyncDropdownToCurrentResolution();
    }
}
```

---

### 2.2 LetterboxCamera.cs

**Update() 추가** — dirty flag 방식으로 해상도/풀스크린 변경 자동 감지.

```csharp
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LetterboxCamera : MonoBehaviour
{
    private const float BaseAspect = 16f / 9f;
    private const int   BaseWidth  = 1920;
    private const int   BaseHeight = 1080;

    private Camera _cam;

    // [추가] dirty flag용
    private int  _lastW = -1;
    private int  _lastH = -1;
    private bool _lastFs;
    private bool _initialized = false;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    // [추가] 매 프레임 OS 레벨 변경까지 감지
    private void Update()
    {
        int curW = Screen.width;
        int curH = Screen.height;
        bool curFs = Screen.fullScreen;

        if (!_initialized || curW != _lastW || curH != _lastH || curFs != _lastFs)
        {
            _lastW = curW;
            _lastH = curH;
            _lastFs = curFs;
            _initialized = true;

            Apply(curW, curH, curFs);
        }
    }

    public void Apply(int selectedWidth, int selectedHeight, bool isFullScreen)
    {
        // [규칙 4] 창모드 → 무조건 풀 뷰포트, 어떤 상태에서도 보장
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
            // [규칙 3] 선택 ≤ 기준 → 선택 그대로 가운데 배치
            innerW = selectedWidth;
            innerH = selectedHeight;
        }
        else
        {
            // [규칙 5, 비활성] 선택 > 기준 → 16:9 최대 영역 (확장 대비 유지)
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
```

**중요:** `Apply()` 메서드의 인자는 그대로 두되, `Update()`에서는 `Screen.width/height`(실제 화면 크기)를 넘긴다. `Option_Resolution.ApplyAndClose()`에서 호출하는 기존 경로는 그대로 유지(즉시 동기 적용 효과).

---

### 2.3 CutoutFade.cs

**현재 문제:** `ResizeResolution()`이 1920×1080 하드코딩.
주석 처리된 `Screen.width/height` 코드가 원래 의도였음.

**수정:** 카메라 뷰포트가 레터박스로 잡히는 구조이므로, **CutoutFade는 1920×1080 고정이 맞다**. 이유는:
- Canvas가 `Screen Space - Camera`이고 카메라 viewport가 게임 영역(16:9)으로 잡혀있음.
- Canvas의 referenceResolution도 1920×1080이라, 페이드 마스크는 1920×1080 기준으로 그리면 정확히 게임 영역을 덮음.

**결론:** CutoutFade.cs는 **수정하지 않음**. 단, `ResizeResolution()`이 사실상 의미 없는 코드가 되므로 메서드 자체를 제거해도 무방.

(정리 차원에서 제거하려면 다음 호출도 함께 제거)
- `Option_Resolution.ApplyAndClose()`의 `if (_cutoutFade != null) _cutoutFade.ResizeResolution();`
- `Option_Resolution.ApplySavedResolution()`의 동일 라인

---

## 3. 테스트 케이스

### 3.1 옵션 드롭다운 (규칙 2 검증)

| # | 모니터 환경 | 기대 드롭다운 항목 |
|---|---|---|
| T1 | 1920×1080 모니터 | 1280×720, 1366×768, 1600×900, 1920×1080 |
| T2 | 2560×1440 (16:9) | 위와 동일 (≤1920×1080만) |
| T3 | 2560×1600 (16:10) | 16:9만 통과, 1920×1080 이하만 |
| T4 | 3440×1440 (21:9) | 위와 동일 |

### 3.2 창모드 (규칙 4 검증, **버그 재현 케이스**)

| # | 진입 상태 | 조작 | 기대 결과 |
|---|---|---|---|
| T5 | 창 1920×1080 | 창 1280×720 선택 → Apply | 창 1280×720, 카메라 rect (0,0,1,1) |
| T6 | 풀스크린 1920×1080 (모니터 2560×1600) | 풀스크린 토글 OFF → Apply | 창 1920×1080, **레터박스 0** |
| T7 | 풀스크린 1280×720 (모니터 2560×1600, 사방 레터박스) | 풀스크린 토글 OFF → Apply | 창 1280×720, **내부 축소 없음** |
| T8 | 풀스크린에서 Alt+Enter (OS 레벨) | — (Update가 자동 처리) | 창모드, 카메라 rect (0,0,1,1) |

### 3.3 전체화면 (규칙 3 검증)

| # | 모니터 | 선택 | 기대 게임 영역 | 기대 레터박스 |
|---|---|---|---|---|
| T9 | 1920×1080 | 1920×1080 | 1920×1080 | 없음 |
| T10 | 1920×1080 | 1280×720 | 1280×720 | 좌우 320/320, 상하 180/180 |
| T11 | 2560×1440 | 1920×1080 | 1920×1080 | 좌우 320/320, 상하 180/180 |
| T12 | 2560×1600 | 1920×1080 | 1920×1080 | 좌우 320/320, 상하 260/260 |
| T13 | 2560×1600 | 1280×720 | 1280×720 | 좌우 640/640, 상하 440/440 |
| T14 | 3440×1440 | 1920×1080 | 1920×1080 | 좌우 760/760, 상하 180/180 |

### 3.4 영속성

| # | 시나리오 | 기대 |
|---|---|---|
| T15 | 1280×720 창모드로 OK → 게임 재시작 | 1280×720 창모드 복원 |
| T16 | 1920×1080 풀스크린으로 OK → 게임 재시작 | 풀스크린 복원, 모니터 네이티브에 맞춰 레터박스 자동 적용 |
| T17 | PlayerPrefs 비어있는 첫 실행 | 1920×1080 창모드 |

### 3.5 Cancel/ESC 롤백

| # | 시나리오 | 기대 |
|---|---|---|
| T18 | 드롭다운/풀스크린 변경 후 Cancel | 변경 전 값으로 복원, 패널 닫힘 |
| T19 | 변경 후 ESC | Cancel과 동일 동작 |

---

## 4. 검증 방법

1. Unity Editor에서 Game View를 1920×1080, 1280×720, 2560×1600, 3440×1440 등으로 바꿔가며 위 케이스 확인.
2. 빌드 후 실제 모니터에서 풀스크린 토글 + Alt+Enter + Alt+Tab 조합으로 규칙 4 보장 확인.
3. Profiler에서 `LetterboxCamera.Update()` 항목이 0.01ms 미만인지 확인 (성능 영향 없음 검증).

---

## 5. 작업 순서 권장

1. `Option_Resolution.InitializeResolutions()`에 필터 추가 → 빌드 후 드롭다운 확인.
2. `LetterboxCamera`에 `Update()` + dirty flag 추가 → T5~T8 (규칙 4 버그) 검증.
3. `Option_Resolution.OnApplicationFocus()`에 `SyncDropdownToCurrentResolution()` 호출 추가.
4. (선택) `CutoutFade.ResizeResolution()` 및 호출처 정리.
5. 전체 회귀 테스트 (T1~T19).

---

## 6. 주의사항

- `LetterboxCamera`의 `Update()`는 메인 카메라에만 붙은 컴포넌트라 부하 없음. `Camera.main` 의존성에 주의.
- `Screen.fullScreen` 변경은 Unity가 다음 프레임에 반영하는 경우가 있어, dirty flag 방식이 동기 호출보다 안정적.
- 16:9 필터로 인해 16:10 노트북(맥북 등)에서 네이티브 해상도 옵션이 사라짐. 의도된 트레이드오프.
- `Screen.resolutions`가 비어있는 환경(일부 Linux/특수 빌드) 대비 안전장치 추가됨.
