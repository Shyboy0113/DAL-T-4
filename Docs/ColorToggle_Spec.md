# ColorToggle 색상 로직 명세서

작성일: 2026-05-17  
대상: 레벨 디자이너, QA, 플레이어 가이드 작성자

---

## 1. 개요

ColorToggle은 두 가지 타일 유형이 쌍으로 동작합니다.

| 역할        | TileType         | 설명                                         |
| ----------- | ---------------- | -------------------------------------------- |
| **CT 버튼** | `ColorToggle`    | 플레이어가 밟으면 자신의 색상을 브로드캐스트 |
| **CT 타깃** | `ToggleTargeted` | 브로드캐스트된 색상을 받아 토글 여부를 결정  |

---

## 2. TileColor 비트 구조

TileColor는 비트 플래그(Flags) enum입니다. 각 색상은 R(빨강), G(초록), B(파랑) 비트의 조합입니다.

| 색상           | 비트값  | R   | G   | B   |
| -------------- | ------- | --- | --- | --- |
| Black          | 0 (000) | -   | -   | -   |
| Blue           | 1 (001) | -   | -   | ●   |
| Green          | 2 (010) | -   | ●   | -   |
| Cyan           | 3 (011) | -   | ●   | ●   |
| Red            | 4 (100) | ●   | -   | -   |
| Magenta (보라) | 5 (101) | ●   | -   | ●   |
| Yellow         | 6 (110) | ●   | ●   | -   |
| White          | 7 (111) | ●   | ●   | ●   |

---

## 3. 반응 판정 로직 (감산 / 부분집합 체크)

```
타일 색상이 CT 버튼 색상의 부분집합일 때 반응
(타일색 & CT색) == 타일색
```

직관적 해석: **타일의 모든 색 비트가 CT 버튼 색 안에 포함되어 있을 때 반응합니다.**

### Black CT 특수 규칙

Black CT(색상값 0)는 위 공식과 무관하게 **모든 타일**을 토글합니다. "아무 색도 없는 검정 버튼 = 만능 스위치" 개념입니다.

### 예시

- CT 버튼이 **Magenta(5 = R+B)**를 브로드캐스트할 때:
    - Red(4) 타일: `(4 & 5) == 4` → `4 == 4` ✅ 반응 (R만 있음, R⊆{R,B})
    - Blue(1) 타일: `(1 & 5) == 1` → `1 == 1` ✅ 반응 (B만 있음, B⊆{R,B})
    - Magenta(5) 타일: `(5 & 5) == 5` → `5 == 5` ✅ 반응 (R+B, {R,B}⊆{R,B})
    - Yellow(6=R+G) 타일: `(6 & 5) == 6` → `4 == 6` ❌ 비반응 (G 비트가 Magenta에 없음)
    - Cyan(3=G+B) 타일: `(3 & 5) == 3` → `1 == 3` ❌ 비반응 (G 비트가 Magenta에 없음)
    - White(7) 타일: `(7 & 5) == 7` → `5 == 7` ❌ 비반응 (G 비트가 Magenta에 없음)
    - Black(0) 타일: `(0 & 5) == 0` → `0 == 0` ✅ 반응 (비어있는 집합은 모든 집합의 부분집합)

---

## 4. CT 버튼 색상별 반응표

| CT 버튼 색상         | 반응하는 타일 색상        | 특징                                 |
| -------------------- | ------------------------- | ------------------------------------ |
| **Black** (0)        | **모든 타일**             | 특수 규칙 — 범용 스위치              |
| **Blue** (1)         | Black, Blue               | Blue만 성분으로 갖는 타일            |
| **Green** (2)        | Black, Green              | Green만 성분으로 갖는 타일           |
| **Cyan** (3)         | Black, Blue, Green, Cyan  | Blue 또는 Green만 성분으로 갖는 타일 |
| **Red** (4)          | Black, Red                | Red만 성분으로 갖는 타일             |
| **Magenta/보라** (5) | Black, Blue, Red, Magenta | Red 또는 Blue만 성분으로 갖는 타일   |
| **Yellow** (6)       | Black, Red, Green, Yellow | Red 또는 Green만 성분으로 갖는 타일  |
| **White** (7)        | **모든 타일**             | 모든 색의 상위집합 — 범용 스위치     |

### 핵심 원칙

- **Black CT** = 만능 열쇠 (모든 문을 열지만, 가장 '무채색'인 버튼)
- **단색 CT** (Red, Green, Blue) = 자신과 Black 타일만 반응
- **복합색 CT** (Magenta, Cyan, Yellow) = 자신의 성분 색 + 자신 + Black 타일 반응
- **White CT** = Black CT와 동일하게 전체 범용 (모든 색의 상위집합이기 때문)

---

## 5. ToggleTargeted 타일 색상별 반응 조건

반대로, **타일 색상**에 따라 어떤 CT 버튼에 반응하는지 정리합니다.

| 타일 색상       | 반응하는 CT 버튼                                  | 특징                                  |
| --------------- | ------------------------------------------------- | ------------------------------------- |
| **Black** (0)   | 모든 CT 버튼                                      | 범용 타깃 — 어떤 버튼에도 열림        |
| **Blue** (1)    | Black CT, Blue CT, Cyan CT, Magenta CT, White CT  | B 비트를 포함한 모든 CT               |
| **Green** (2)   | Black CT, Green CT, Cyan CT, Yellow CT, White CT  | G 비트를 포함한 모든 CT               |
| **Cyan** (3)    | Black CT, Cyan CT, White CT                       | G+B 둘 다 포함한 CT만                 |
| **Red** (4)     | Black CT, Red CT, Magenta CT, Yellow CT, White CT | R 비트를 포함한 모든 CT               |
| **Magenta** (5) | Black CT, Magenta CT, White CT                    | R+B 둘 다 포함한 CT만                 |
| **Yellow** (6)  | Black CT, Yellow CT, White CT                     | R+G 둘 다 포함한 CT만                 |
| **White** (7)   | Black CT, White CT만                              | 가장 까다로운 타깃 — 범용 CT에만 반응 |

### 핵심 원칙

- **Black 타일** = 가장 쉽게 열리는 문 (어떤 CT에도 반응)
- **White 타일** = 가장 열기 어려운 문 (범용 CT만 반응)
- **단색 타일** (Red, Green, Blue) = 자신과 상위복합색 CT에 반응
- **복합색 타일** (Magenta, Cyan, Yellow) = Black CT와 White CT에만 반응

---

## 6. 특수 규칙: White sentinel

`TileBehaviour`의 `overrideColor` 필드에서 `TileColor.White`는 두 가지 의미를 갖습니다.

| 값                | 동작                                             |
| ----------------- | ------------------------------------------------ |
| `TileColor.White` | 오버라이드 없음 → `SO_TileData.baseColor`를 사용 |
| 그 외 모든 색상   | 해당 색상으로 오버라이드                         |

실제 흰색 타일을 원하는 경우 `SO_TileData.baseColor`를 White로 설정하세요.

---

## 7. ColorToggle 버튼 기본값 규칙

`OnValidate`에 의해 자동 적용됩니다.

- **ColorToggle 버튼**을 처음 추가하면 `overrideColor`가 자동으로 **Black**으로 설정됩니다.
- Black = 범용 토글이므로, 특정 색상 버튼이 필요한 경우 Inspector에서 직접 변경하세요.
- **White CT 버튼은 설정 불가**: OnValidate가 White → Black으로 자동 복원합니다.  
  (White CT는 논리적으로 Black CT와 동일한 범용 효과이므로 별도 설계 불필요)

---

## 8. 레이어 필터링

ColorToggle은 레이어 단위로도 필터링됩니다.

- CT 버튼과 CT 타깃이 **같은 레이어**에 있어야 반응합니다.
- **Static 레이어** 타일은 예외 — 모든 CT 버튼에 반응합니다 (맵1/맵2 공유 타일).

챕터 4 듀얼 맵 구조에서는 Map 1 CT 버튼이 Map 2 타일을 열 수 없습니다 (단, Static 레이어 타일은 예외).

---

## 9. 레벨 디자인 가이드라인

| 의도                                             | 사용 방법                                               |
| ------------------------------------------------ | ------------------------------------------------------- |
| "버튼 하나로 맵 전체 열기"                       | Black CT 버튼 사용                                      |
| "특정 색 계열 타일만 선택적으로 열기"            | 해당 색 CT 버튼 + 열 타일에 그 색 이하의 색 설정        |
| "Magenta(보라) 버튼으로 Red/Blue/보라 타일 열기" | Magenta CT 버튼, 타깃은 Red/Blue/Magenta ToggleTargeted |
| "어떤 CT에도 반응하지 않는 타일"                 | White ToggleTargeted 타일 (Black/White CT에만 반응)     |
| "모든 CT에 반응하는 타일"                        | Black ToggleTargeted 타일                               |
| "보라 버튼으로만 열리는 타일"                    | Magenta CT만 배치, Magenta ToggleTargeted 타일 사용     |
