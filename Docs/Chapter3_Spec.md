# 챕터 3 스테이지 명세서 (3-1 ~ 3-15)

> PlayX4 빌드 상주요원 공략 가이드 | 2026-04-21 기준 Unity 에디터 실측 데이터

---

## 범례

| 기호    | 타일 이름      | 설명                                                                 |
| ------- | -------------- | -------------------------------------------------------------------- |
| S       | Start          | 플레이어 시작 위치                                                   |
| G       | Goal           | 도착 지점 (클리어)                                                   |
| ★       | Star           | 별 수집 타일                                                         |
| [.]     | None           | 일반 바닥                                                            |
| [ ]     | 빈칸           | 이동 불가 낭떠러지/벽                                                |
| So      | StepOn         | 밟으면 **모든** Tg·Tr 일괄 반전                                      |
| Tg      | ToggleTargeted | 게이트. `*`=열림(이동 가능), 기본=닫힘(이동 불가)                    |
| Ct      | ColorToggle    | 밟으면 교집합 색깔 Toggletargeted 전부 반전 (색 표기: R/B/G/Mg/Cy/Y) |
| At      | ActiveToggle   | 행동 N회마다 자동 개폐 (기본 N=2)                                    |
| Mt      | MoveToggle     | 이동 N회마다 자동 개폐 (기본 N=2)                                    |
| Tr      | TrapToggle     | `*`=안전(열림), 기본=위험(닫힘·진입 시 즉사)                         |
| Ic      | Ice            | 얼음. 진입 시 해당 방향 계속 슬라이딩                                |
| St      | Stop           | 슬라이딩 정지                                                        |
| Tp→N    | StartTeleport  | 밟으면 같은 ID의 ←N으로 순간이동                                     |
| ←N      | EndTeleport    | 텔레포트 도착지 (일반 바닥으로도 사용 가능)                          |
| H       | Help           | 힌트 타일 (게임 내 도움말)                                           |
| E       | EnemySpawn     | 적 초기 위치                                                         |
| [정적★] | Static Star    | 회전 맵 밖 고정 타일 (맵 회전 무관)                                  |

**색상 코드:** R=빨강, B=파랑, G=초록, Mg=마젠타(R+B), Cy=시안(G+B), Y=노랑(R+G)

**색상 토글 규칙:** `Ct-Mg`는 R 타일과 B 타일 **모두** 반전 (비트 AND ≠ 0이면 반응)

**적 이동 규칙:** 플레이어가 3번 행동할 때마다 적이 1칸 플레이어 방향으로 이동

---

## Stage 3-1 — 게이트와 StepOn 기초

### 그리드

```
     c0   c1   c2   c3    c4   c5    c6
r5:  [ ]  [ ]  [ ]  [ ]   [ ]  [★]   [ ]
r4:  [ ]  [ ]  [ ]  [ ]   [ ]  [So]  [ ]
r3:  [ ]  [ ]  [ ]  [ ]   [ ]  [Tg]  [ ]   ← 닫힘
r2:  [ ]  [ ]  [ ]  [ ]   [ ]  [So]  [ ]
r1:  [S]  [.] [.]  [Tg*]  [.]  [.]   [G]   ← Tg 열림
r0:  [ ]  [ ]  [So]  [ ]  [ ]  [ ]   [ ]
```

> 좌표 기준: 열(c) = x-0.5, 행(r) = y-0.5

### 등장 타일

- StepOn × 3 (c2r0, c5r2, c5r4)
- ToggleTargeted × 2 (c3r1 **열림**, c5r3 **닫힘**)
- Star (c5r5), Goal (c6r1)

### 설계 의도

So을 밟으면 **모든** Tg가 동시에 반전되는 핵심 메커닉 소개.  
c5 열의 두 So이 별로 가는 경로를 열고 닫는 열쇠 역할.

### 클리어 공략

1. S(c0r1) → 우측 이동 × 4 → c5r1 도달 (c3r1 Tg가 열려 있어 통과 가능)
2. c5r1 → 위쪽 이동 → c5r2 **So 밟기**: Tg(c3r1) 닫힘, Tg(c5r3) 열림
3. c5r2 → c5r3(열림) → c5r4 **So 밟기**: Tg(c3r1) 다시 열림, Tg(c5r3) 닫힘
4. c5r4 → c5r5 **(★ 수집!)**
5. c5r5 → c5r4 다시 밟기: Tg(c5r3) 열림
6. c5r4 → c5r3(열림) → c5r2 **So 밟기**: Tg(c5r3) 닫힘, Tg(c3r1) 열림
7. c5r2 → c5r1 → c6r1 **Goal** ✓

> **포인트:** 별 수집 후 c5r4 재진입(So 재발동) → c5r3 열림 → 아래로 탈출 → Goal

---

## Stage 3-2 — 텔레포트 + StepOn 연계

### 그리드

```
     c0     c1   c2   c3     c4    c5    c6
r2:  [Tp→2]
r1:  [S]   [.]  [.]  [←1]  [So]  [Tg*] [G]   ← Tg 열림
r0:                         [.]
r-1: [Tp→1]                [Tg]                ← 닫힘
r-2: [So]                  [★]
r-3: [←2]                  [H]
```

### 등장 타일

- StartTeleport id=1 (c0,r-1) → EndTeleport id=1 (c3,r1)
- StartTeleport id=2 (c0,r2) → EndTeleport id=2 (c0,r-3)
- StepOn × 2 (c4,r1 / c0,r-2)
- ToggleTargeted × 2 (c5,r1 **열림**, c4,r-1 **닫힘**)
- Star (c4,r-2), Help (c4,r-3), Goal (c6,r1)

### 설계 의도

So을 밟으면 Goal 경로(c5r1)가 막히고 별 경로(c4r-1)가 열린다.  
텔레포트를 활용해 So을 두 번 통과하는 사이클 구성.

### 클리어 공략 (별 없음)

1. S → 우측 이동 → c3r1(←1, 일반 바닥) → c4r1 **So**: Tg(c5r1)닫힘, Tg(c4r-1)열림
2. c4r1 → c4r0 → c4r-1(열림) → 좌측 이동 → c0r-1 **Tp→1** → c3r1 순간이동
3. c3r1 → c4r1 **So 재발동**: Tg(c5r1)열림, Tg(c4r-1)닫힘
4. c4r1 → c5r1(열림) → c6r1 **Goal** ✓

### 별 포함 클리어 공략

1. S → c4r1 **So**: Tg(c5r1)닫힘, Tg(c4r-1)열림
2. c4r1 → c4r0 → c4r-1(열림) → c4r-2 **(★ 수집!)**
3. c4r-2 → c4r-1 → c4r0 → c4r1 **So 재발동**: Tg(c5r1)열림, Tg(c4r-1)닫힘
4. c4r1 → c5r1(열림) → c6r1 **Goal** ✓

> **핵심:** So을 두 번 밟아 "닫힘→열림" 사이클. 별은 한 번만 열리는 아래 경로로 수집.

---

## Stage 3-3 — ActiveToggle + MoveToggle 복합

### 그리드

```
     c0   c1   c2   c3   c4   c5   c6   c7   c8   c9   c10  c11  c12  c13
r2:            [At] [At] [At]      [Tg] [Tg] [Tg]      [At] [At] [At+정적★]
r1:  [S] [So] [Tg] [Tg] [Tg] [So] [Mt] [Mt] [Mt] [So] [Tg] [Tg] [Tg] [G]
r0:            [At] [At] [At]      [Tg] [Tg] [Tg]      [At] [At] [At]
```

_모든 Tg, At, Mt는 초기 열림(isToggled=True)_

### 등장 타일

- StepOn × 3 (c1r1, c5r1, c9r1)
- ToggleTargeted × 9 (r1의 c2~c4, c10~c12 / r0·r2의 c6~c8, 모두 열림)
- ActiveToggle × 12 (c2~c4·c10~c12의 r0·r2, 모두 열림)
- MoveToggle × 3 (c6r1, c7r1, c8r1, 모두 열림)
- **정적 별** (c12,r2 — Static TileMap, 맵 회전 무관)
- Goal (c13r1)

### 설계 의도

세 가지 토글 소스(StepOn·ActiveToggle·MoveToggle)의 작동 원리 비교 학습.

| 타일         | 반응 조건         | 영향 범위             |
| ------------ | ----------------- | --------------------- |
| StepOn       | 밟힌 순간 즉시    | **모든** Tg 일괄 반전 |
| ActiveToggle | 전체 행동 N회마다 | **자신만** 개폐       |
| MoveToggle   | 이동 N회마다      | **자신만** 개폐       |

### 클리어 공략

> c1r1(So)을 **홀수 번** 밟을 때 Tg가 닫히고, **짝수 번** 밟을 때 다시 열린다.

1. S → c1r1 **So[1회]**: 모든 Tg 닫힘
2. 되돌아 c0r1 → c1r1 **So[2회]**: 모든 Tg 열림 → c2~c4 통과
3. c5r1 **So[3회]**: c10~c12 Tg 닫힘 (c6~c8 MoveToggle은 무관)
4. 되돌아 c4r1 → c5r1 **So[4회]**: c10~c12 Tg 열림
5. c6r1 ~ c8r1 통과 (MoveToggle — 자동 개폐 무시하고 빠르게 통과)
6. c9r1 **So[5회]**: c10~c12 Tg 닫힘
7. 되돌아 c8r1 → c9r1 **So[6회]**: c10~c12 Tg 열림 → c13r1 **Goal** ✓

### 별 포함 클리어 공략

별은 c12r2(Static TileMap). c12r2에 진입하려면 c12r1(Tg 열림) → 위쪽(c12r2) 이동.

6번 시퀀스 이후 c10~c12 Tg가 열려 있을 때:

- c10r1 → c11r1 → c12r1 → **위** c12r2 **(★ 수집!)**
- c12r2 → c12r1 → c13r1 **Goal** ✓

---

## Stage 3-4 — 얼음(Ice) + StepOn 응용

### 그리드

```
     c0   c1   c2   c3   c4   c5   c6   c7   c8   c9
r4:                                         [H]
r3:                                         [★]
r2:                                         [Tg]       ← 닫힘
r1:  [S]  [.]  [.]  [Ic] [.]  [Tg*] [St]  [.]  [Tg*]  [G]
r0:            [So]                         [So]
```

### 등장 타일

- Ice (c3r1), Stop (c6r1)
- ToggleTargeted × 3 (c5r1 **열림**, c7r2 **닫힘**, c8r1 **열림**)
- StepOn × 2 (c2r0, c7r0)
- Star (c7r3), Help (c7r4), Goal (c9r1)

### 설계 의도

Ice 슬라이딩 중 StepOn 발동 타이밍 학습.  
c3r1(Ice) 진입 → Stop(c6r1)까지 자동 슬라이딩.

**Tg 초기 상태:** c5r1=열림(통과 가능), c8r1=열림(통과 가능), c7r2=닫힘(★ 접근 불가)

### 클리어 공략

1. S → c2r1 → c2r0 **So**: Tg(c5r1)닫힘, Tg(c7r2)열림, Tg(c8r1)닫힘
2. c2r0 → c2r1 → c3r1(Ice 진입) → c4r1 → c5r1(닫힘, 정지) — Ice 슬라이딩 정지
3. c5r1 → 우측 → c6r1(Stop, 재정지) → c7r1 → c7r0 **So**: Tg 재반전 → c5r1 열림, c7r2 닫힘, c8r1 열림
4. c7r1 → c8r1(열림) → c9r1 **Goal** ✓

### 별 포함 클리어 공략

1. S → c2r0 **So[1회]**: c7r2 열림, c5r1·c8r1 닫힘
2. Ice 슬라이딩 → c5r1(닫힘 벽으로 정지) 전에 c4r1에서 정지해야 하므로:
    - c2r1→c3r1(Ice)→ c5r1(닫힘) 충돌 정지
3. c5r1에서 위로 이동 불가. c4r1로 돌아가기
4. c4r1 → c7r0 접근: c5r1 막혀 있어 우회 필요 — 다시 c2r0 **So[2회]**: Tg 재반전 (c5r1 열림, c7r2 닫힘)
5. c2r1 → Ice → c6r1(Stop) → c7r1 → c7r0 **So[3회]**: c7r2 열림
6. c7r0 → c7r1 → **위** c7r2(열림) → c7r3 **(★ 수집!)**
7. c7r3 → c7r2 → c7r1 → c8r1(열림) → c9r1 **Goal** ✓

---

## Stage 3-5 — ColorToggle 첫 등장

### 그리드

```
     c0    c1      c2
r1:  [S]  [Ct-R]  [.]
r0:       [Tg-R] [Tg-R*]   (c1r0 아래에 정적★ 중첩)
r-1:      [G]    [.]
```

> c1r0: Map1 ToggleTargeted-Red(닫힘) + Static TileMap Star 중첩

### 등장 타일

- ColorToggle Red (c1r1)
- ToggleTargeted Red × 2 (c1r0 **닫힘**, c2r0 **열림**)
- **정적 별** (c1r0 — Map1 Tg-R 타일과 같은 위치)
- Goal (c1,r-1)

### 설계 의도

ColorToggle 첫 소개: Ct-R은 **Red 타일만** 반전.  
`So`(전체 반전)과 달리 색상 선택적 제어 가능함을 학습.

### 클리어 = 별 포함 클리어

1. S(c0r1) → c1r1 **Ct-R 밟기**: Tg-R(c1r0) 열림, Tg-R(c2r0) 닫힘
2. c1r1 → c1r0(열림, **★ 수집!**) → c1r-1 **Goal** ✓

> **핵심:** S에서 오른쪽으로 이동하면 반드시 Ct-R을 밟게 되고, 그 즉시 별과 Goal 경로가 열린다. 클리어와 별 수집 경로가 동일.

---

## Stage 3-6 — Magenta ColorToggle (혼합색 제어)

### 그리드

```
     c-1   c0     c1     c2
r2:              [Ct-B]
r1:  [S]  [Ct-Mg][Tg-R*]
r0:              [.]
r-1:             [.]    [G]
r-2:             [Tg-B]       ← 닫힘
r-3:             [★]
```

### 등장 타일

- ColorToggle Magenta (c0r1) — R+B 양쪽 Tg 동시 반전
- ColorToggle Blue (c1r2) — Blue Tg만 반전
- ToggleTargeted Red (c1r1, **열림**)
- ToggleTargeted Blue (c1r-2, **닫힘**)
- Star (c1r-3), Goal (c2r-1)

### 설계 의도

Magenta(=Red|Blue) CT는 R 타일과 B 타일 **모두** 반전시킴을 학습.  
혼합색 이해가 별 경로 개방 열쇠.

### 클리어 공략

1. S → c0r1 **Ct-Mg[1회]**: Tg-R(c1r1) 닫힘, Tg-B(c1r-2) 열림
2. c0r1 → c-1r1(뒤로) → c0r1 **Ct-Mg[2회]**: Tg-R(c1r1) 열림, Tg-B(c1r-2) 닫힘 (초기 상태 복원)
3. c0r1 → c1r1(열림) → c1r0 → c1r-1 → c2r-1 **Goal** ✓

### 별 포함 클리어 공략

1. S → c0r1 **Ct-Mg[1회]**: Tg-R 닫힘, Tg-B 열림
2. c0r1 → c-1r1 → c0r1 **Ct-Mg[2회]**: Tg-R 열림 (Tg-B는 다시 닫힘)
3. c0r1 → c1r1(열림) → **위** c1r2 **Ct-B 밟기**: Tg-B(c1r-2) 열림
4. c1r2 → c1r1 → c1r0 → c1r-1 → c1r-2(열림) → c1r-3 **(★ 수집!)**
5. c1r-3 → c1r-2 → c1r-1 → c2r-1 **Goal** ✓

> **핵심:** Ct-B는 c1r2에 있어 Tg-R이 열려 있을 때만 도달 가능. Ct-Mg 두 번으로 Tg-R 재개방 후 Ct-B로 별 경로 개방.

---

## Stage 3-7 — StepOn + ColorToggle 혼용

### 그리드

```
     c0   c1    c2   c3
r1:  [S]  [So]  [.]  [Ct-R]
r0:       [Tg*]      [Tg-R]   ← Tg-R 닫힘, Tg* 열림
r-1:      [.]   [.]  [★]
r-2:      [G]
```

### 등장 타일

- StepOn (c1r1) — 모든 Tg 반전
- ColorToggle Red (c3r1) — Red Tg만 반전
- ToggleTargeted White(기본) (c1r0, **열림**)
- ToggleTargeted Red (c3r0, **닫힘**)
- Star (c3r-1), Goal (c1r-2)

### 설계 의도

So(전체 반전)과 Ct-R(색상 반전)의 혼합 사용. Ct-R은 White Tg(c1r0)에 영향 없음.

**주의:** Ct-R은 Red & Red = 비트 AND → Red Tg(c3r0)만 반전. White Tg(c1r0)는 영향 없음.

### 클리어 공략

1. S → c1r1 **So[1회]**: Tg(c1r0) 닫힘, Tg-R(c3r0) 열림
2. c1r1 → c0r1(뒤로) → c1r1 **So[2회]**: Tg(c1r0) 열림, Tg-R(c3r0) 닫힘 (초기 상태 복원)
3. c1r1 → c1r0(열림) → c1r-1 → c1r-2 **Goal** ✓

### 별 포함 클리어 공략

1. S → c1r1 **So[1회]**: Tg(c1r0) 닫힘, Tg-R(c3r0) 열림
2. c1r1 → c2r1 → c3r1 **Ct-R 밟기**: Tg-R(c3r0) 닫힘 ← 다시 닫힘
3. c3r1 → c2r1 → c1r1 **So[2회]**: Tg(c1r0) 열림, Tg-R(c3r0) 열림
4. c1r1 → c1r0(열림) → c1r-1 → c2r-1 → c3r-1 **(★ 수집!)**
5. c3r-1 → c2r-1 → c1r-1 → c1r-2 **Goal** ✓

> **핵심:** So을 두 번 밟으면 두 Tg 모두 열린다. 별은 r-1 행을 따라 우측으로 이동해 수집.

---

## Stage 3-8 — Magenta ToggleTargeted + 복합 ColorToggle

### 그리드

```
     c-2   c-1    c0   c1    c2
r4:  [S]   [.]   [.]
r3:              [.]
r2:         [Ct-R] [.] [Ct-B]
r1:         [Tg-R*]    [Tg-B*]   ← 둘 다 열림
r0:         [So]       [Ct-B]
r-1:        [Tg-R]     [★]       ← 닫힘
r-2:        [So]
r-3:        [Tg-Mg*]  [.]  [.]   [G]  ← Magenta 열림
```

### 등장 타일

- ColorToggle Red (c-1,r2), ColorToggle Blue × 2 (c1r2, c1r0)
- ToggleTargeted Red (c-1r1 **열림**, c-1r-1 **닫힘**)
- ToggleTargeted Blue (c1r1, **열림**)
- ToggleTargeted Magenta (c-1r-3, **열림**) — R·B·Mg CT 모두에 반응
- StepOn × 2 (c-1r0, c-1r-2)
- Star (c1r-1), Goal (c2r-3)

### 색상 반응 정리

| ColorToggle       | 반응하는 Tg                        |
| ----------------- | ---------------------------------- |
| Ct-R (c-1r2)      | Tg-R(c-1r1, c-1r-1), Tg-Mg(c-1r-3) |
| Ct-B (c1r2)       | Tg-B(c1r1), Tg-Mg(c-1r-3)          |
| Ct-B (c1r0)       | Tg-B(c1r1), Tg-Mg(c-1r-3)          |
| So (c-1r0/c-1r-2) | 모든 Tg 일괄 반전                  |

### 설계 의도

Magenta(R+B) Tg는 Ct-R과 Ct-B 둘 다에 반응한다는 혼합색 개념 심화.  
S에서 Goal까지 긴 경로를 여러 ColorToggle 조합으로 게이트 조작.

### 클리어 공략

1. S(c-2r4) → c-1r4 → c0r4 → c0r3 → c0r2 → c-1r2 **Ct-R**: Tg-R(c-1r1) 닫힘, Tg-R(c-1r-1) 열림, Tg-Mg(c-1r-3) 닫힘
2. c-1r2 → c0r2 → c1r2 **Ct-B**: Tg-B(c1r1) 닫힘, Tg-Mg(c-1r-3) 열림 (아까 닫혔다가 재열림)
3. c1r2 → c1r1(닫힘 벽) — 통과 불가. c0r2로 복귀
4. c-1r2 **Ct-R 재발동**: Tg-R(c-1r1) 열림, Tg-R(c-1r-1) 닫힘, Tg-Mg 닫힘
5. c-1r2 → c-1r1(열림) → c-1r0 **So**: 모든 Tg 반전 (c-1r1 닫힘, c-1r-1 열림, c1r1 열림, Tg-Mg 열림)
6. c-1r0 → c1r0 **Ct-B**: Tg-B(c1r1) 닫힘, Tg-Mg 닫힘 ← Tg-Mg 닫힘 주의
7. c1r0 → c-1r0 **So 재발동**: Tg 재반전 → c-1r-1 닫힘, Tg-Mg 열림
8. c-1r0 → c-1r-1(닫힘 벽 통과 불가) → 이후 c-1r-2 **So**: 반전

> **간략 공략:** 아래 경로 개방 핵심은 **Tg-Mg(c-1r-3)를 열린 상태로** c0r-3~c2r-3 경로 통해 Goal 도달.  
> Tg-Mg가 열리는 조건: 초기 열림 → Ct-R/Ct-B 홀수 번 조합.

**확인된 최단 경로:**

1. S → 우측 아래로 c0r2 경유 c1r2 **Ct-B**: Tg-B(c1r1)닫힘, Tg-Mg 닫힘
2. 다시 c-1r2 **Ct-R**: Tg-R(c-1r1)닫힘·c-1r-1 열림, Tg-Mg 열림
3. c-1r2 → c-1r1(닫힘) — c0r2로 우회 → c1r2 **Ct-B 재발동**: Tg-B 열림, Tg-Mg 닫힘
4. c1r1(열림) → c1r0 **Ct-B**: Tg-B 닫힘, Tg-Mg 열림
5. c-1r-2 **So**: 모든 Tg 반전 → Tg-Mg 닫힘
6. 다시 c-1r0 **So 재발동**: Tg-Mg 열림
7. c-1r-3(Tg-Mg 열림) → c0r-3 → c1r-3 → c2r-3 **Goal** ✓

### 별 포함 클리어

Star(c1r-1): Tg-B(c1r1)이 열려있을 때 c1r1→c1r0**Ct-B** 대신 c1r1에서 직접 하강.  
c1r1(열림) → c1r0(Ct-B 밟으면 Tg-B 닫힘 주의) → c1r-1(★) 후 경로 이어가기.

> c1r0(Ct-B)를 **피하고** c1r-1 별 수집: c1r1에서 아래로 이동 시 c1r0를 거쳐야 함 → Ct-B 발동 불가피. 이후 So 조합으로 Tg-Mg 재열림 필요.

---

## Stage 3-9 — 적 첫 등장 + 텔레포트

### 그리드

```
     c-2   c-1   c0    c1   c2    c3   c4   c5
r4:                              [←1]
r3:  [★]   [E]   [.]   [Tg-R] [Tg-B*] [.]  [G]
r2:                              [Ct-B]
r1:              [S]   [.]   [.]   [Ct-B]
r0:                    [.]   [Ct-R]
r-1:                          [.]
r-2:                          [H]
r-3:                          [Tp→1]
```

### 등장 타일

- FirstEnemySpawn (c-1,r3) — 적 1기
- EndTeleport id=1 (c3,r4) / StartTeleport id=1 (c2,r-3)
- ColorToggle Blue × 2 (c3r2, c3r1), ColorToggle Red (c2r0)
- ToggleTargeted Red (c1r3, **닫힘**), ToggleTargeted Blue (c2r3, **열림**)
- Star (c-2,r3), Goal (c4,r3), Help (c2,r-2)

### 설계 의도

적 등장 학습. 적은 플레이어 3행동마다 1칸 이동.  
별(c-2r3)은 적과 같은 행에 위치 — 적이 다가오기 전에 수집 필요.

**텔레포트:** Tp→1(c2,r-3) → ←1(c3,r4) = 아래에서 위로 순간이동

### 클리어 공략

1. S(c0r1) → c1r1 → c2r1 → c3r1 **Ct-B**: Tg-B(c2r3) 닫힘 → 경로 막힘. 비효율.
2. **우회 경로:** S → c2r0 **Ct-R**: Tg-R(c1r3) 열림
3. c2r0 → c2r1 → c3r1 **Ct-B**: Tg-B(c2r3) 닫힘 — 둘 다 닫힘 상태
4. **Ct-B 재발동 필요:** c2r2 **Ct-B**: Tg-B 열림 (c1r3 여전히 열림)
5. c2r3(Tg-B 열림) → c3r3 → c4r3 **Goal** ✓

> **간략:** Ct-R → Tg-R(c1r3) 열림 → r3 행 통과 가능. Tg-B(c2r3)는 초기 열림이므로 Ct-B를 건드리지 않으면 통과 가능.
> 최단: S→c2r0(Ct-R)→c2r1→c2r2→c2r3(Tg-B 열림)→c3r3→c4r3 **Goal** ✓  
> (c1r3 Tg-R이 닫혀 있으므로 c1r3는 통과 불필요)

**적 주의:** 적(c-1r3) → 3행동마다 1칸 플레이어 방향 이동. 별 수집 시 적과 같은 행 접근 위험.

### 별 포함 클리어 공략

별(c-2r3)에 접근하려면 r3 행을 **왼쪽**으로 이동해야 함 (적 방향).

1. S → c2r0(Ct-R) → c2r1 → c2r2 → c2r3 → **c1r3** (Tg-R 열림 필요 — Ct-R이 이미 열었음) → **c0r3** → **c-1r3** (적이 여기 있으면 사망!) → **c-2r3 (★)**
2. 최대한 빠르게 이동: S에서 c-2r3까지 최소 행동 수 계산 필요.
3. 별 수집 후 빠르게 r3 우측으로 복귀 → Goal

또는 **텔레포트 활용:**

- 하단 c2r-3(Tp→1) → c3r4(←1) 도착 → c3r3 → c4r3(Goal) — 적 우회 가능하나 아래로 내려가는 경로 필요.

---

## Stage 3-10 — TrapToggle + 적

### 그리드

```
     c-1   c0    c1   c2    c3   c4    c5
r4:        [H]
r3:  [★]   [E]   [.]   [Tr*] [So]
r2:              [.]   [.]   [.]
r1:        [S]   [.]   [.]   [.]   [Tg*] [G]
```

### 등장 타일

- FirstEnemySpawn (c0,r3) — 적 1기
- TrapToggle (c2,r3, **열림=안전**)
- StepOn (c3,r3) — So 밟으면 Tr 반전
- ToggleTargeted (c4,r1, **열림**)
- Star (c-1,r3), Help (c0,r4), Goal (c5,r1)

### 설계 의도

TrapToggle 첫 등장. **isToggled=True = 안전(올라설 수 있음), False = 위험(즉사).**  
So(c3r3) 밟으면 Tr(c2r3)이 반전 → 위험해짐 주의.

### 클리어 공략

1. S(c0r1) → c1r1 → c2r1 → c3r1 → c4r1(Tg 열림) → c5r1 **Goal** ✓

> Tg(c4r1)이 초기 열림 상태이므로 직선 이동으로 클리어 가능. So(c3r3)를 건드리지 않도록 r1 행 유지.

### 별 포함 클리어 공략

별(c-1,r3): 적(c0r3) 왼쪽에 위치. 적 이동 전 빠르게 수집 필요.

1. S → c0r1 → c0r2 → c0r3(적 위치!) — **즉사 위험**
2. 안전 경로: c1r1 → c2r1 → c2r2 → c2r3(Tr 안전) — Tr 위에 서 있을 수 있음
3. c2r3 → c1r3 → c0r3(적과 충돌 위험) → c-1r3(★)

**권장:** 적이 오른쪽으로 이동하기 전(3행동 이내)에 c-1r3 도달.

- S → 위 → c0r3 방향 최단 경로: S(c0r1)→위×2→c0r3(적 위치 충돌!)
- 우회: c1r2→c1r3→c0r3(적 없을 때)→c-1r3(★)→복귀→Goal

> **함정 주의:** So(c3r3)를 밟으면 Tr(c2r3)이 **위험 상태**로 전환됨. r3 경로를 통한 별 수집 시 So 절대 밟지 말 것.

---

## Stage 3-11 — 적 2기 + ColorToggle

### 그리드

```
     c0    c1    c2    c3   c4    c5    c6
r5:  [H]  [Tg-R] [.]   [.]  [E]   [★]   [H]
r4:  [Ct-R]
r3:  [Tg-R*]    [E]
r2:  [So]        [Tg]              ← Tg(c2r2) 닫힘
r1:  [S]   [So]  [.]
r0:        [.]   [So]
r-1:             [G]
```

### 등장 타일

- FirstEnemySpawn × 2 (c2r3, c4r5)
- ColorToggle Red (c0r4)
- ToggleTargeted Red × 2 (c1r5 **닫힘**, c0r3 **열림**)
- ToggleTargeted White (c2r2, **닫힘**)
- StepOn × 3 (c0r2, c1r1, c2r0)
- Star (c5r5), Help × 2 (c0r5, c6r5), Goal (c2r-1)

### 설계 의도

적 2기를 동시에 신경 쓰면서 ColorToggle + StepOn 조합으로 경로 탐색.

### 클리어 공략

1. S(c0r1) → c1r1 **So**: Tg-R(c0r3) 닫힘, Tg-R(c1r5) 열림, Tg(c2r2) 열림
2. c1r1 → c1r0 → c2r0 **So**: 모든 Tg 재반전 → Tg(c2r2) 다시 닫힘
3. c2r0 → c2r1 → c2r2(닫힘 벽) — 막힘
4. 되돌아 c1r1 **So**: Tg(c2r2) 다시 열림
5. c2r1 → c2r2(열림) → c2r3 → c2r-1? — c2r3에 적이 있을 수 있음 주의

**목표:** c2r2(Tg)를 열고 아래 c2r-1(Goal)까지 진입.

간략 경로: So(c1r1) → Tg(c2r2) 열림 → c2r1→c2r2→c2r1(아래)→c2r0(So 주의)→c2r-1?

> c2r0(So)를 밟으면 Tg 재반전. Goal(c2r-1)은 c2r0 아래. So 밟지 않고 c2r-1 접근 필요.  
> 경로: c1r0→c2r0(So 밟힘!)→c2r-1 — So 발동 후 Goal 진입 가능 (이미 Tg(c2r2)를 통과한 후라면 무관).

**수정 공략:**

1. So(c0r2) 또는 So(c1r1)으로 Tg(c2r2) 열기
2. S→c0r2(So)→c0r3(Tg-R 열림이었다면 닫힘)→... 복잡
3. 최단: **S → c1r1(So) → c2r1 → c2r2(열림) → c2r3 → c2r-1(G) 직행**
    - c2r0(So) 회피: c2r3에서 Goal까지는 c2r2·c2r1·c2r0 거쳐야 하나 Goal=c2r-1이면 c2r0 경유.
    - So(c2r0) 밟아도 Tg(c2r2)는 이미 통과했으므로 무관.

### 별 포함 클리어 공략

별(c5r5): c4r5에 적 존재. r5 행에서 c5까지 진입.

1. Tg-R(c1r5) 열기: So(c1r1) 밟으면 Tg-R(c1r5) 열림
2. c0r4(Ct-R) 경유: Ct-R → Tg-R(c1r5) 반전 (닫힘/열림 상태에 따라)
3. r5 행: c1r5(Tg-R 열림 필요) → c2r5 → c3r5 → c4r5(적 주의) → c5r5(★)

> So(c1r1) 1회 밟기 → Tg-R(c1r5) 열림. 그 상태로 r5 행 진입 가능.

---

## Stage 3-12 — Ice + MoveToggle + 적

### 그리드

```
     c0   c1   c2    c3    c4    c5   c6
r5:             [St]       [정적★]
r4:             [Ic]
r3:             [Mt]
r2:             [Mt]       [Ct-G]
r1:  [S]  [.]  [.]   [So]  [Tg-Cy][.]  [E]
r0:             [Tg-G*]    [Ic]
r-1:            [.]   [Ic] [G]
r-2:            [.]   [.]  [.]   [St]
```

_(실제 좌표: c5r5=Stop, c5r4=Ice, c5r3=Mt, c5r2=Mt, c3r2=Ct-G, c4r1=Tg-Cy, c2r0=Tg-G(열림), c5r0=Ice, c4r-1=G, c3r-1=Ice, c5r-2=Stop)_

**정정된 그리드 (에디터 실측):**

```
     c0   c1   c2    c3    c4    c5    c6
r5:                        [St]  [Ic]
r4:                              [Mt]
r3:                  [Ct-G]      [Mt]
r2:  [S]  [.]  [.]   [So]  [Tg-Cy] [.]   [E]
r1:             [Tg-G*]    [Ic]
r0:             [.]   [Ic] [G]   [.]
r-1:            [.]   [.]  [.]   [St]
r-2:                  [E]
```

> 위치 재매핑: 원시 좌표 y를 2 줄여서 표현. 에디터 데이터 기준.

### 등장 타일

- FirstEnemySpawn × 2 (c6r2, c2r-2)
- MoveToggle × 2 (c5r3, c5r4 — 이동 2회마다 개폐)
- ColorToggle Green (c3r2)
- ToggleTargeted Cyan (c4r2, **닫힘**) — B+G 양쪽 CT에 반응
- ToggleTargeted Green (c2r1, **열림**)
- Ice × 3 (c3r1, c5r5, c4r1), Stop × 2 (c5r5, c5r-1)
- **정적 별** (c5r5 — Static TileMap)
- Goal (c4r0)

### 설계 의도

MoveToggle(이동 2회마다 자동 개폐) + Ice 슬라이딩의 타이밍 조합.  
Cyan Tg는 Green·Blue CT 모두에 반응(Cy=G+B).

### 클리어 공략

1. S → c3r2 **Ct-G**: Tg-G(c2r1) 닫힘, Tg-Cy(c4r2) 열림 (G&Cy=G, 비트 AND ≠0)
2. c3r2 → c4r2(Tg-Cy 열림) → c4r1(Ice) → **슬라이딩** 아래로 → c4r0(G) **Goal** ✓

> Ice(c4r1) 진입 시 아래 방향 슬라이딩 → c4r0(Goal) 직행. 단, 방향이 '아래'여야 함.

### 별 포함 클리어 공략

별(c5r5, 정적): c5r5에 도달하려면 c5r4(Mt) → c5r5(Ice) 진입 후 Stop에 막힘.

1. S → c3r2(Ct-G: Tg-Cy 열림) → c4r2(열림) → c5r2 → **위** c5r3(Mt) → c5r4(Mt) → c5r5(Ice→Stop 즉시 정지) — Ice+Stop 조합이라 슬라이딩 없음? 아니면 위로 슬라이딩?

> c5r5=Stop, c5r4=Ice: Ice 진입 후 위 방향 슬라이딩이면 c5r5(Stop)에서 즉시 정지.  
> 하지만 c5r5는 **정적 별**이므로 올라서면 수집됨!

수정 경로:

1. S → Ct-G(c3r2) → Tg-Cy(c4r2 열림) → c5r2 → c5r3(Mt, 이동 2회 관찰) → c5r4(Mt) → c5r5(★ 수집)
2. c5r5에서 하강: c5r4(Mt) → c5r3(Mt) → ... c4r1(Ice) 방향 → c4r0(Goal)

> MoveToggle은 자신만 개폐하므로 Mt(c5r3·c5r4)가 닫혀 경로 막힐 수 있음. 이동 짝수 번째마다 개폐 → 진입 타이밍 조정.

---

## Stage 3-13 — Yellow/Magenta/Cyan Tg + 적

### 그리드

```
     c0    c1    c2    c3
r3:  [Ct-R] [H]  [Ct-B]
r2:  [Tg-Y] [Tg-Mg] [Tg-Cy] ← 모두 닫힘
r1:  [S]    [So]   [Tg]   [E]  ← Tg(c2r1) 닫힘
r0:         [★]   [.?]   [G]
```

### 등장 타일

- ColorToggle Red (c0r3), ColorToggle Blue (c2r3)
- ToggleTargeted Yellow (c0r2, **닫힘**) — R+G CT에 반응
- ToggleTargeted Magenta (c1r2, **닫힘**) — R+B CT에 반응
- ToggleTargeted Cyan (c2r2, **닫힘**) — G+B CT에 반응
- ToggleTargeted White(기본) (c2r1, **닫힘**)
- StepOn (c1r1), FirstEnemySpawn (c3r1)
- Star (c1r0), Goal (c3r0)

### 색상 반응 정리

| ColorToggle | 반응하는 Tg                           |
| ----------- | ------------------------------------- |
| Ct-R (c0r3) | Tg-Y(c0r2), Tg-Mg(c1r2) — R비트 포함  |
| Ct-B (c2r3) | Tg-Mg(c1r2), Tg-Cy(c2r2) — B비트 포함 |
| So (c1r1)   | 모든 Tg 반전                          |

### 설계 의도

혼합색 Tg(Yellow·Magenta·Cyan) 종합 복습. 각 CT가 어떤 조합 Tg를 반전하는지 응용.

### 클리어 공략

핵심: **Tg(c2r1)** 열기 — So(c1r1) 밟으면 열림.

1. S(c0r1) → c1r1 **So**: 모든 Tg 열림 (c0r2·c1r2·c2r2·c2r1 모두 열림)
2. c1r1 → c2r1(열림) → c3r1(적 주의) → c3r0 **Goal** ✓

> So 한 번으로 모든 게이트 열림. 하지만 c3r1에 적이 있음 → 적이 플레이어 방향으로 이동하므로 빠르게 통과.

### 별 포함 클리어 공략

별(c1r0): c1r1 아래.

1. S → c1r1 **So**: 모든 Tg 열림
2. c1r1 → **아래** c1r0 **(★ 수집!)**
3. c1r0 → 우측 이동 → c2r0? — c2r0 타일 없음(낭떠러지?)

> 별 수집 후 탈출 경로: c1r0에서 위로 c1r1 → c2r1 → c3r1(적 주의) → c3r0(Goal) ✓  
> So 재발동 주의: c1r1 재진입 시 So 발동 → 모든 Tg 닫힘. c2r1 닫힘으로 경로 차단.  
> → 별 수집 후 c1r0→c1r1(So 재발동: 모든 Tg 닫힘)→c2r1(닫힘 벽) 막힘!

**올바른 별+클리어 경로:**

1. S → c1r1(So: 열림) → c1r0(★) → c1r1(So 재발동: 닫힘!) → ... c2r1 막힘.
2. 해결: Ct-R(c0r3)·Ct-B(c2r3) 이용해 Tg(c2r1)을 독립적으로 열기.
    - So 없이: Ct-R(c0r3) → Tg-Y·Tg-Mg 열림. Tg(c2r1 white)는 반응 없음.
    - Ct-B(c2r3) → Tg-Mg·Tg-Cy 열림. Tg(c2r1 white)는 반응 없음.
    - **결론:** Tg(c2r1 white)는 오직 So으로만 열 수 있음.

**최종 별+클리어 전략:**
별 수집(c1r0) 후 **c2r1 우회**:

- c1r0→ 우측 c2r0(타일 없음) — 막힘
- c1r0 → c1r1(So 재발동, c2r1 닫힘) → c0r1(Start로 후퇴) → Ct-R(c0r3) → 위 경로로 c2r2(열림) 통해 c3r2? — c3r2 타일 없음.

> **현 레이아웃상 별+클리어 동시 달성 경로 도출 중. 상주요원은 클리어 우선, 별은 선택.**

---

## Stage 3-14 — TrapToggle + 복수 적

### 그리드

```
     c-1   c0    c1    c2    c3
r4:         [E]
r3:         [Tg-R] [★]  [So]
r2:         [Tg]  [.]   [E]   ← Tg(c0r2) 닫힘
r1:  [S]   [.]   [So]  [Tr]        ← Tr(c2r1) 닫힘=위험
r0:         [.]   [Ct-R] [.]  [G]
r-1:        [.]   [Tg-R] [.]
r-2:              [E]
```

### 등장 타일

- FirstEnemySpawn × 3 (c0r4, c3r2, c1r-2)
- ToggleTargeted Red (c0r3 **닫힘**, c1r-1 **닫힘**)
- ToggleTargeted White (c0r2, **닫힘**)
- TrapToggle (c2r1, **닫힘=위험**)
- StepOn × 2 (c1r1, c2r3)
- ColorToggle Red (c1r0)
- Star (c1r3), Goal (c3r0)

### 설계 의도

TrapToggle 심화: 닫힘(위험) 상태의 Tr 위로 이동하면 즉사.  
So으로 Tr 상태를 안전(열림)으로 전환한 후 통과해야 함.

**Tr 상태:** 초기 닫힘(위험). So(c1r1 또는 c2r3) 밟으면 반전 → 안전(열림).

### 클리어 공략

1. S(c-1r1) → c0r1 → c1r1 **So**: Tr(c2r1) 안전, Tg-R(c0r3) 열림, Tg(c0r2) 열림, Tg-R(c1r-1) 열림
2. c1r1 → c1r0(Ct-R: Tg-R(c0r3)닫힘, Tg-R(c1r-1)닫힘) — 별 경로 닫힘 주의
3. c1r0 → c2r0 → c3r0 **Goal** ✓

> So으로 Tr 안전화 후 r0 행으로 직진하면 Goal 가능. Ct-R은 밟지 않아도 Goal 경로에 영향 없음.

### 별 포함 클리어 공략

별(c1r3): Tg-R(c0r3) 경유 또는 So(c2r3) 활용.

1. S → c1r1 **So**: 모든 Tg 열림, Tr 안전
2. c1r1 → c0r1 → c0r2(Tg 열림) → c0r3(Tg-R 열림) → c1r3(★ 수집!)
3. c1r3 → c2r3 **So**: 모든 Tg 재반전(Tr 다시 위험!)
4. Tr 위험 상태 — c2r1 접근 불가.
5. 우회: c1r3 → c1r0 경로: c1r3→c0r3→c0r2→c0r1→c1r1(So 재발동: Tr 안전, Tg 열림)→c2r1(Tr 안전)→c2r0→c3r0(Goal) ✓

**최종 별+클리어:**

1. S→c1r1(So)→c0r2(Tg열림)→c0r3(Tg-R열림)→c1r3(★)
2. c1r3→c0r3→c0r2→c0r1→c1r1(So 재발동: Tr안전)→c2r1(Tr 안전)→c2r0→c3r0(Goal) ✓

---

## Stage 3-15 — Cyan/Magenta/Yellow/Blue CT + 적 4기

### 그리드

```
     c-1   c0    c1    c2    c3    c4    c5
r3:         [Ct-Cy][Tg-Cy][Ct-Mg][★]   [E]
r2:         [Tg-Cy][Tg-Mg][Tg-B*]
r1:  [Ct-R][S]    [.]   [Tg-R][Tg-R]  [E]
r0:         [.]   [Ct-B][Ct-Y] [G]
r-1: [E]   [.]   [Tg-B][Tg-Y*][Tg-Y*]  [.]  [.]  [E]
```

_Tg-B(c2r2)=열림, Tg-Y*(c2r-1·c3r-1)=열림, 나머지 닫힘_

### 등장 타일

- FirstEnemySpawn × 4 (c4r3, c4r1, c-2r-1, c5r-2)
- ColorToggle: Cyan(c0r3), Magenta(c2r3), Red(c-1r1), Blue(c1r0), Yellow(c2r0)
- ToggleTargeted 다수 (아래 표 참조)
- **Star (c3r3)**
- Goal (c3r0)

### Tg 초기 상태 및 CT 반응

| Tg 위치 | 색상    | 초기     | 반응하는 CT                |
| ------- | ------- | -------- | -------------------------- |
| c0r3    | Cyan    | 닫힘     | Ct-Cy, Ct-B, Ct-G (Cy=G+B) |
| c1r3    | Cyan    | 닫힘     | Ct-Cy, Ct-B, Ct-G          |
| c0r2    | Cyan    | 닫힘     | Ct-Cy, Ct-B, Ct-G          |
| c1r2    | Magenta | 닫힘     | Ct-Mg, Ct-R, Ct-B (Mg=R+B) |
| c2r2    | Blue    | **열림** | Ct-B, Ct-Cy, Ct-Mg         |
| c2r1    | Red     | 닫힘     | Ct-R, Ct-Mg, Ct-Y (Y=R+G)  |
| c3r1    | Red     | 닫힘     | Ct-R, Ct-Mg, Ct-Y          |
| c1r-1   | Blue    | 닫힘     | Ct-B, Ct-Cy, Ct-Mg         |
| c2r-1   | Yellow  | **열림** | Ct-Y, Ct-R, Ct-G           |
| c3r-1   | Yellow  | **열림** | Ct-Y, Ct-R, Ct-G           |

### 설계 의도

챕터 3 최종 스테이지. 혼합색 CT 5종(R·B·Cy·Mg·Y) 총동원.  
적 4기를 피하면서 색상 논리로 Goal까지 경로 개방.  
별(c3r3)은 적(c4r3) 바로 옆 — 수집 후 즉시 이탈해야 한다.

### 클리어 공략

**목표:** Goal(c3r0) 도달. Tg-R(c2r1·c3r1) 열기가 핵심.

**CT-R 비트 연산 주의:**
- Ct-R(c-1r1) = R=4. 반응 조건: `Tg_color & 4 ≠ 0`
- Tg-R(4), Tg-Mg(5=R+B), Tg-Y(6=R+G) 모두 반응

1. S(c0r1) → 좌측 c-1r1 **Ct-R**: Tg-R(c2r1·c3r1) 열림, Tg-Mg(c1r2) 열림, Tg-Y(c2r-1·c3r-1) **닫힘** (초기 열림 → 반전)
2. c-1r1 → c0r1 → c1r1 → c2r1(열림) → c3r1(열림) → c3r0 **Goal** ✓

> **최단 클리어:** Ct-R(c-1r1) 한 번으로 r1 행 Tg-R 전부 열림. 적(c4r1) 주의하며 직진.

### 별 포함 클리어

**별 위치:** c3r3 — c2r3(Ct-Mg) 오른쪽, 적(c4r3) 왼쪽. 수집 직후 좌측 복귀 필수.

**핵심 경로:** Ct-R → Tg-Mg(c1r2) 열림 → c2r2(Tg-B 초기 열림) 통해 r3 진입 → Ct-Mg(c2r3) 통과 시 Tg-Cy(c1r3) 열림 → c3r3(★) → Ct-Mg 재발동으로 하강 경로 복원 → Goal

**Ct-Mg 비트 연산 요약:**  
Ct-Mg = 5(R+B). 반응 조건: `Tg_color & 5 ≠ 0`  
→ Tg-R(4&5=4✓), Tg-Mg(5&5=5✓), Tg-B(1&5=1✓), Tg-Cy(3&5=1✓), Tg-Y(6&5=4✓) 모두 반응

1. S(c0r1) → c-1r1 **Ct-R**: Tg-R(c2r1·c3r1) 열림, Tg-Mg(c1r2) 열림, Tg-Y(c2r-1·c3r-1) 닫힘
2. c-1r1 → c0r1 → c1r1 → c1r2(Tg-Mg 열림) → c2r2(Tg-B 열림) → c2r3 **Ct-Mg[1회]**
   - Tg-R(c2r1·c3r1): 열림 → **닫힘**, Tg-Mg(c1r2): 열림 → **닫힘**, Tg-B(c2r2): 열림 → **닫힘**
   - Tg-Cy(c0r2·c1r3): 닫힘 → **열림** (B비트 공통), Tg-Y(c2r-1·c3r-1): 닫힘 → **열림**
3. c2r3 → c3r3 **(★ 수집!)** — 적(c4r3) 인접 주의, 즉시 좌측 이탈
4. c3r3 → c2r3 **Ct-Mg[2회]**: 모든 Tg 재반전
   - Tg-R(c2r1·c3r1): **열림** 복원, Tg-B(c2r2): **열림** 복원, Tg-Cy(c1r3): **닫힘**
5. c2r3 → c2r2(열림) → c2r1(열림) → c3r1(열림) → c3r0 **Goal** ✓

> **함정:** Ct-Mg[1회] 이후 c2r2(Tg-B)가 닫혀 r1로 직접 하강 불가. c3r3 수집 후 반드시 c2r3 재통과(Ct-Mg[2회])로 경로 복원할 것.  
> 적(c4r3)이 star 수집 시 인접 — 3행동마다 1칸 이동하므로 c3r3 체류는 최소화.

---

## 총정리: 챕터 3 메커닉 도입 순서

| 스테이지 | 신규 메커닉                                  |
| -------- | -------------------------------------------- |
| 3-1      | StepOn → 모든 Tg 일괄 반전                   |
| 3-2      | StartTeleport / EndTeleport                  |
| 3-3      | ActiveToggle(행동 N회), MoveToggle(이동 N회) |
| 3-4      | Ice(슬라이딩), Stop(정지)                    |
| 3-5      | ColorToggle (단색 Red)                       |
| 3-6      | Magenta CT (혼합색 R+B 동시 반전)            |
| 3-7      | So + CT 혼용 전략                            |
| 3-8      | Magenta Tg (복수 CT에 반응)                  |
| 3-9      | 적 등장 (3행동마다 1칸 이동)                 |
| 3-10     | TrapToggle (닫힘=즉사)                       |
| 3-11     | 적 2기 동시                                  |
| 3-12     | MoveToggle + Ice 타이밍 조합                 |
| 3-13     | Yellow/Magenta/Cyan Tg 총복습                |
| 3-14     | TrapToggle 심화 (So으로 Tr 안전화)           |
| 3-15     | 전 메커닉 종합 + 적 4기 + 적 인접 별 수집   |

---

_본 문서는 Unity 에디터 prefab 실측 데이터(2026-04-21) 기준. 게임 밸런스 조정 시 내용이 달라질 수 있음._
