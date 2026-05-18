# 챕터 3·4 도전과제(미션) 명세서

작성일: 2026-05-16  
대상 스테이지: Stage 3-1 ~ 4-15 (총 30개)

---

## 개요

### 문제

모든 챕터 3·4 스테이지의 `SO_StageData` 미션 값이 동일한 기본값으로 설정되어 있음.

```
firstMissionType  : 1  (StageClear)
secondMissionType : 6  (NoSpecificFeature)
thirdMissionType  : 5  (CollectStar)
forbiddenFeature  : 3  (TAB)
```

### 설계 원칙

| 미션 슬롯 | 고정값 | 설명 |
|----------|--------|------|
| **1st** | StageClear (1) | 단순 클리어 |
| **2nd** | 스테이지별 상이 | 핵심 메커닉을 시험하는 도전 |
| **3rd** | CollectStar (5) | 별 수집 + 클리어 (최고 도전) |

### 2nd 미션 선택 기준

| 조건 | 채택 미션 타입 |
|------|--------------|
| 적(Enemy) + TrapToggle 연계 가능 스테이지 | `KillAllEnemies (4)` |
| 타이밍·속도가 핵심 (적 압박, 자동 토글) | `TimeLimit (2)` |
| 경로 효율·최적 순서가 핵심 | `MoveCountLimit (3)` |
| 특정 키 없이도 클리어 가능한 스테이지 | `NoSpecificFeature (6)` |
| G2(SecondDestination) 필수 → TAB 없인 불가 | `MoveCountLimit (3)` |

### MissionType 열거형 참조

```csharp
public enum MissionType
{
    None             = 0,
    StageClear       = 1,
    TimeLimit        = 2,  // → limitTime 필드 사용
    MoveCountLimit   = 3,  // → missionActionCount 필드 사용
    KillAllEnemies   = 4,
    CollectStar      = 5,
    NoSpecificFeature= 6,  // → forbiddenFeature 필드 사용
}

public enum ForbiddenFeature { None=0, ALT=1, F4=2, TAB=3 }
```

---

## 챕터 3 미션 명세

### 공통

```
firstMissionType  : 1 (StageClear)
thirdMissionType  : 5 (CollectStar)
hasSecondMap      : 0
```

---

### Stage 3-1 — StepOn 기초

```
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 10
```

**설계 근거**  
직선 6칸 레이아웃. So 없이 직진(최적 6회)이 가능하나, 별 경로는 So 타이밍 조절 필수.  
count=10 → 최적 경로 여유 4회. So 낭비 없는 최적 시퀀스를 유도.

---

### Stage 3-2 — Teleport + StepOn 연계

```
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 18
```

**설계 근거**  
So 2회 발동 + 텔레포트 사이클 필수(최적 ≈ 14회).  
count=18 → 여유 4회. 텔레포트 왕복 낭비 방지.

---

### Stage 3-3 — ActiveToggle + MoveToggle 복합

```
secondMissionType : 2  (TimeLimit)
limitTime         : 60
```

**설계 근거**  
At/Mt 자동 개폐 주기에 맞춰 이동해야 하므로 타이밍이 핵심.  
기다리는 행동 자체가 패널티가 되도록 시간 제한 채택.

---

### Stage 3-4 — Ice + StepOn 응용

```
secondMissionType : 6  (NoSpecificFeature)
forbiddenFeature  : 2  (F4 금지)
```

**설계 근거**  
Ice 슬라이딩 퍼즐은 맵 회전 없이 풀 수 있음.  
F4 없이 So + Ice 조합만으로 클리어하는 도전.

---

### Stage 3-5 — ColorToggle 첫 등장

```
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 4
```

**설계 근거**  
S→Ct-R→Tg열림→G = 최적 3회. count=4 → 여유 1회.  
CT 한 번으로 별+Goal 동시 해결하는 최단 경로 유도.

---

### Stage 3-6 — Magenta CT (혼합색 제어)

```
secondMissionType : 6  (NoSpecificFeature)
forbiddenFeature  : 1  (ALT 금지)
```

**설계 근거**  
Ct-Mg 논리(R+B 비트 AND)만으로 클리어 가능.  
ALT 없이 색상 논리만으로 Tg 열고 닫는 도전.

---

### Stage 3-7 — StepOn + ColorToggle 혼용

```
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 16
```

**설계 근거**  
So(전체 반전)와 Ct-R(색상 반전) 혼합 사용 최적 경로 약 12회.  
count=16 → 두 토글 소스를 효율적으로 조합하는 순서 유도.

---

### Stage 3-8 — Magenta ToggleTargeted + 복합 CT

```
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 26
```

**설계 근거**  
CT 4종(Ct-R, Ct-B×2, So×2) 조합으로 Tg-Mg 상태 제어. 최적 ≈ 20회.  
count=26 → 복잡한 색상 비트 연산 최적 순서 탐색 유도.

---

### Stage 3-9 — 적 첫 등장 + 텔레포트

```
secondMissionType : 2  (TimeLimit)
limitTime         : 40
```

**설계 근거**  
적이 3행동마다 1칸 이동. 느린 플레이는 별 경로(c-2r3)에서 즉사.  
40초 제한 → 빠른 CT 조합 선택과 적 회피 압박.

---

### Stage 3-10 — TrapToggle 첫 등장

```
secondMissionType : 4  (KillAllEnemies)
```

**설계 근거**  
Tr(c2r3) 초기=안전. So(c3r3) 밟으면 위험 전환.  
E1(c0r3)이 플레이어 추적 시 c1r3→c2r3(위험) 경유 → 격멸 가능.  
TrapToggle로 적을 제거하는 첫 학습 미션.

> ⚠️ **구현 확인 필요:** 적이 StepOn 타일을 밟을 경우 Tr 상태가 되돌아올 수 있음. 적의 So 발동 여부 실측 검증 권장.

---

### Stage 3-11 — 적 2기 + ColorToggle

```
secondMissionType : 2  (TimeLimit)
limitTime         : 40
```

**설계 근거**  
적 2기(c2r3, c4r5) 동시 회피. So(c1r1) 밟아 Tg(c2r2) 열어야 탈출 가능.  
적이 접근하기 전 So 발동 후 Goal 직진 요구 → 40초 압박.

---

### Stage 3-12 — Ice + MoveToggle 타이밍

```
secondMissionType : 2  (TimeLimit)
limitTime         : 60
```

**설계 근거**  
Mt(이동 2회마다 개폐) + Ice 슬라이딩 타이밍을 동시에 맞춰야 함.  
Mt 닫힌 순간 Ice 진입하면 경로 막힘 → 타이밍 최적화 압박.

---

### Stage 3-13 — Y/Mg/Cy ToggleTargeted 총복습

```
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 12
```

**설계 근거**  
So(c1r1) 1회로 모든 Tg 열림 → Goal 직진 최적 ≈ 7회.  
count=12 → 불필요한 CT 발동 없이 So 1회+직진 유도.

---

### Stage 3-14 — TrapToggle 심화

```
secondMissionType : 2  (TimeLimit)
limitTime         : 50
```

**설계 근거**  
적 3기(c0r4, c3r2, c1r-2) 동시 + Tr 안전화(So 필요) 조합.  
So 발동 → Tr 안전 → 적 회피하며 Goal → 빠른 판단 필요.

---

### Stage 3-15 — 전 메커닉 종합 + 적 4기

```
secondMissionType : 2  (TimeLimit)
limitTime         : 75
```

**설계 근거**  
혼합색 CT 5종(R·B·Cy·Mg·Y) + 적 4기(c4r3, c4r1, c-2r-1, c5r-2).  
최단 클리어: Ct-R(c-1r1) → Tg-R 열림 → Goal 직진.  
적 인접 환경에서 최적 경로 실행 → 75초 스피드런.

---

## 챕터 4 미션 명세

### 공통

```
firstMissionType : 1 (StageClear)
thirdMissionType : 5 (CollectStar)
```

> **G2 필수 스테이지** (SecondDestination 있음, TAB 없이 클리어 불가)  
> → secondMission을 NoSpecificFeature(TAB)으로 설정 불가. MoveCountLimit 채택.

---

### Stage 4-1 — TAB 입문 (듀얼맵 첫 등장)

```
hasSecondMap      : 1
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 10
```

**설계 근거**  
선형 레이아웃. Start(c0r1)→G2(c6r1) 최적 ≈ 7회 (TAB 포함).  
count=10 → TAB 불필요한 우회 없이 Map2로 직행하는 경로 유도.

---

### Stage 4-2 — EnemySpawn + 듀얼맵

```
hasSecondMap      : 1
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 14
```

**설계 근거**  
수직 레이아웃. E1(c3r3)이 Goal(c3r5) 경로를 막음.  
TAB으로 Map2 우회하거나 E1 이동 전 선점 → 최적 ≈ 10회.  
count=14 → 적 회피 전략과 TAB 활용 병행.

---

### Stage 4-3 — Teleport + StepOn + 듀얼맵

```
hasSecondMap      : 1
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 18
```

**설계 근거**  
StartTp/EndTp + So + Tg + E1 + TAB 복합. 최적 ≈ 14회.  
count=18 → 텔레포트 왕복 낭비 없이 So→Tg→Tp 순서 최적화.

---

### Stage 4-4 — 회전 타일 + 듀얼맵

```
hasSecondMap      : 1
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 22
```

**설계 근거**  
HCW×3 + QCW×1 회전 타일이 r1 행에 분산된 광폭 레이아웃.  
StartTp(c15r15)→EndTp(c15r0) 텔레포트로 수직 이동.  
최적 ≈ 16회. count=22 → 회전 타일 활용 순서 최적화.

---

### Stage 4-5 — EnemySpawn 시험 (E1+E2)

```
hasSecondMap      : 1
secondMissionType : 2  (TimeLimit)
limitTime         : 45
```

**설계 근거**  
Map1 E1 + Map2 E2 동시 존재 → TAB만으로 회피 불가.  
양쪽 맵 적 위치 판단+TAB 타이밍을 빠르게 결정해야 함.  
limitTAB=3, limitALT=8 적용 시 추가 압박.

---

### Stage 4-6 — EnemySpawn + StepOn 복합

```
hasSecondMap      : 1
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 12
```

**설계 근거**  
So(c1r2) → Tg(c1r1) 열림 → E1 추적 시작 → 하단 경로로 빠른 이동.  
기본 클리어 최적 ≈ 8회. count=12 → So 발동 후 지체 없이 Goal 직행.

---

### Stage 4-7 — TAB + Enemy + TrapToggle 복합

```
hasSecondMap      : 1  (G2 필수)
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 12
```

**설계 근거**  
CoR(c1r1)으로 TrR(Map2) ON → TAB → G2(c3r1) → TAB → G(c3r0).  
최적 경로 ≈ 8회(TAB 2회 포함). count=12 → 여유 4회.  
E1 회피+TrR 안전화+두 Goal 순서 최적화.

---

### Stage 4-8 — Enemy + QuarterCW 회전

```
hasSecondMap      : 1
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 14
```

**설계 근거**  
Q>(c1r1) 밟으면 E1 위치 변동 → 안전 경로 생성.  
또는 TAB으로 Map2 임시 대피 후 Goal 접근. 최적 ≈ 10회.  
count=14 → 회전 전/후 E1 위치 예측 최소 이동 유도.

---

### Stage 4-9 — 브릿지 (TAB·적 패턴 정리)

```
hasSecondMap      : 1  (G2 있음)
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 14
```

**설계 근거**  
경로 A (상단 우회, TAB 없음): 최적 4회.  
경로 B (Map2 G2 포함, TAB 2회): 최적 9회.  
count=14 → G2까지 포함한 효율 경로 탐색. 여유 5회.

---

### Stage 4-10 — ConditionalToggle 첫 소개

```
hasSecondMap      : 1  (G2 필수)
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 10
```

**설계 근거**  
So→Tg→Ct(Map2 Tg2 열림)→G→TAB→G2. 직선 최적 ≈ 6회.  
count=10 → Ct 인과 학습 + TAB 1회 최소 사용 유도.

---

### Stage 4-11 — Ice + ColorToggle + Breakable

```
hasSecondMap      : 1
secondMissionType : 2  (TimeLimit)
limitTime         : 60
```

**설계 근거**  
Ice 슬라이딩 + Breakable(균열 후 소멸) + CT 조합.  
Breakable 타일 소멸 전에 통과해야 하는 타이밍 의존 구조.  
E1 존재로 추가 압박. 60초 제한.

---

### Stage 4-12 — TrapToggle + StepOn + ColorToggle (싱글맵)

```
hasSecondMap      : 0
secondMissionType : 4  (KillAllEnemies)
```

**설계 근거**  
TrapToggle×4 + So×2 + CT + E1(c0r-1).  
So로 Tr 위험 전환 → E1을 TrapToggle 위치로 유인하면 격멸 가능.  
TrapToggle 복합 구조를 공격적으로 활용하는 심화 도전.

> ⚠️ **구현 확인 필요:** E1의 최단 경로가 TrapToggle을 통과하는지, 경로 회피 AI가 Tr를 우회하는지 실측 검증 필요.

---

### Stage 4-13 — E1+E2 + Teleport + ColorToggle

```
hasSecondMap      : 1
secondMissionType : 3  (MoveCountLimit)
missionActionCount: 18
```

**설계 근거**  
E1(c2r0, Map1) + E2(c0r1, Map2) 동시 + Tp + CT 조합.  
별 위치가 두 곳(c3r1, c4r-1)으로 분산. 최적 ≈ 14회.  
count=18 → 두 맵 적 회피 + 텔레포트 경로 최적화.

---

### Stage 4-14 — ColorToggle×3 + ToggleTargeted×3 (싱글맵)

```
hasSecondMap      : 0
secondMissionType : 6  (NoSpecificFeature)
forbiddenFeature  : 3  (TAB 금지)
```

**설계 근거**  
싱글맵이므로 TAB 없이 클리어 가능(TAB 필수 경로 없음).  
CT×3(c2r1, c1r-1, c3r-1) + Tg×3 색상 논리만으로 Goal 도달.  
TAB 없이 CT 조합 최적 순서를 찾는 도전.

---

### Stage 4-15 — 최종 종합

```
hasSecondMap      : 1
secondMissionType : 2  (TimeLimit)
limitTime         : 75
```

**설계 근거**  
Ice + Breakable + E1(Map1) + E2(Map2) + ActiveToggle + CT + Tp 전부 등장.  
복잡도 최고. 75초 내 두 맵을 오가며 전 메커닉 실행 요구.  
챕터 4 최종 스피드런.

---

## SO_StageData 필드 입력 요약표

### 챕터 3

| 스테이지 | secondMissionType | missionActionCount | limitTime | forbiddenFeature |
|---------|:-----------------:|:------------------:|:---------:|:----------------:|
| 3-1 | 3 | **10** | 0 | 0 |
| 3-2 | 3 | **18** | 0 | 0 |
| 3-3 | 2 | 0 | **60** | 0 |
| 3-4 | 6 | 0 | 0 | **2** (F4) |
| 3-5 | 3 | **4** | 0 | 0 |
| 3-6 | 6 | 0 | 0 | **1** (ALT) |
| 3-7 | 3 | **16** | 0 | 0 |
| 3-8 | 3 | **26** | 0 | 0 |
| 3-9 | 2 | 0 | **40** | 0 |
| 3-10 | 4 | 0 | 0 | 0 |
| 3-11 | 2 | 0 | **40** | 0 |
| 3-12 | 2 | 0 | **60** | 0 |
| 3-13 | 3 | **12** | 0 | 0 |
| 3-14 | 2 | 0 | **50** | 0 |
| 3-15 | 2 | 0 | **75** | 0 |

### 챕터 4

| 스테이지 | secondMissionType | missionActionCount | limitTime | forbiddenFeature |
|---------|:-----------------:|:------------------:|:---------:|:----------------:|
| 4-1 | 3 | **10** | 0 | 0 |
| 4-2 | 3 | **14** | 0 | 0 |
| 4-3 | 3 | **18** | 0 | 0 |
| 4-4 | 3 | **22** | 0 | 0 |
| 4-5 | 2 | 0 | **45** | 0 |
| 4-6 | 3 | **12** | 0 | 0 |
| 4-7 | 3 | **12** | 0 | 0 |
| 4-8 | 3 | **14** | 0 | 0 |
| 4-9 | 3 | **14** | 0 | 0 |
| 4-10 | 3 | **10** | 0 | 0 |
| 4-11 | 2 | 0 | **60** | 0 |
| 4-12 | 4 | 0 | 0 | 0 |
| 4-13 | 3 | **18** | 0 | 0 |
| 4-14 | 6 | 0 | 0 | **3** (TAB) |
| 4-15 | 2 | 0 | **75** | 0 |

---

## 주의 사항

### missionActionCount 직렬화 문제

현재 `SO_StageData` 에셋 YAML에 `missionActionCount` 필드가 직렬화되어 있지 않음.  
Unity 에디터에서 Inspector로 직접 값을 입력하고 저장해야 에셋에 반영됨.

### count 값 조정 권장

표의 `missionActionCount` 값은 **추정 최적 경로 × 1.3~1.5** 기준.  
실제 플레이 테스트 후 너무 쉽거나 어려우면 아래 기준으로 조정:

- 쉬움 (80% 이상 성공): count 2~3 감소
- 어려움 (클리어 불가): count 3~5 증가

### KillAllEnemies 적용 스테이지 검증 필요

- **3-10:** 적이 StepOn(c3r3)을 밟아 TrapToggle 상태가 되돌아오는지 확인
- **4-12:** E1의 AI 최단 경로가 TrapToggle 위치를 통과하는지 확인

---

_본 명세서는 Unity 에디터 프리팹 실측 + 챕터 3 스펙 문서(Chapter3_Spec.md) + 챕터 4 스펙 문서(Stage4_5to4_10_Spec.md) 기반으로 작성. 게임 밸런스 조정 시 count/time 값 재조정 권장._
