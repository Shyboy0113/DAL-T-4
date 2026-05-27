# 포트폴리오 — DAL-T-4

> 작성자: 홍기태 | 2026년 5월

---

## 프로젝트 요약 카드

| 항목 | 내용 |
|---|---|
| **프로젝트명** | DAL-T-4 |
| **장르** | 타일 기반 퍼즐 액션 |
| **역할** | 1인 개발 (기획 / 프로그래밍 / 레벨 디자인 전담) |
| **엔진** | Unity 2022+ (URP) |
| **언어** | C# |
| **개발 기간** | 약 6개월 (2025년 하반기 ~ 2026년 5월) |
| **플랫폼** | PC — Windows (Steam 출시 예정) |
| **챕터** | 4챕터 × 15+ 스테이지 |
| **링크** | [ Steam 페이지 / GitHub / 플레이 영상 링크 추가 ] |

---

## 게임 소개

DAL-T-4는 세 개의 키(F4·Left ALT·TAB)만으로 조작하는 타일 기반 퍼즐 게임입니다.

플레이어는 "3번의 입력이 하나의 택틱을 이룬다"는 규칙 안에서  
이동과 회전을 조합해 25종의 타일로 구성된 퍼즐을 풀어나갑니다.

**핵심 타깃 경험:**
- 단순한 규칙에서 시작해 복잡성이 자연스럽게 깊어지는 퍼즐
- 설명 없이 "죽어서 배우는" 학습 루프
- 챕터 4의 듀얼맵 ALT+TAB 전환으로 전략적 깊이 추가

---

## 핵심 구현 내용

### 1. Toggle 시스템 — 두 변수로 무한한 퍼즐 문법

```
isToggled (초기 상태: true/false)
  × activationCount (발동 카운트: N)
  → 세 가지 Toggle 타입 (Move / Rotate / Action)
```

- **MoveToggle**: 이동 횟수 카운팅
- **RotateToggle**: 회전 횟수 카운팅
- **ActionToggle**: 이동+회전 합산 카운팅

두 변수의 조합만으로 "제한 시간 내 통과(타이밍 퍼즐)"와  
"우회 후 재접근(경로 퍼즐)"이라는 완전히 다른 퍼즐 유형을 생성합니다.

### 2. 타일 시스템 — 25종 TileType

| 카테고리 | 타일 예시 |
|---|---|
| 기본 이동 | None, Start, Destination |
| 회전 트리거 | Quarter/HalfCW/CCW Rotation |
| Toggle 계열 | MoveToggle, RotateToggle, ActiveToggle, ColorToggle |
| 특수 지형 | Ice, Stop, Breakable, Teleport |
| 조건 | ConditionalToggle, TrapToggle |
| 기타 | Star, Help, EnemySpawn |

TileType enum 기반 단일 컴포넌트(`TileBehaviour.cs`)로  
타일 종류별 동작을 중앙 관리합니다.

### 3. 듀얼맵 시스템 (챕터 4)

- **Map1 / Map2 / Static TileMap** 3레이어 구성
- **ALT+TAB**: 현재 좌표 유지하며 Map1 ↔ Map2 전환
- 각 맵의 Toggle·적(Enemy) 완전 독립 동작
- `SO_StageData` 필드(`hasSecondMap`, `canUseTAB`)로 스테이지별 활성화 제어

### 4. 스테이지 데이터 파이프라인

- 모든 스테이지 설정을 `ScriptableObject (SO_StageData)`로 관리
- 사용 가능한 키(F4/ALT/TAB), 맵 구성, 클리어 조건을 에디터에서 제어
- Addressables 기반 에셋 로딩

### 5. UI 및 클리어 시퀀스

- `BaseInfoPanel` 상속 구조로 StageNode·PauseCanvas·ClearPanel 공통 로직 공유
- `ClearSequenceCompleted` 이벤트로 클리어 연출 / 미션 달성 현황 구동
- 그래픽 품질 세팅: Very Low ~ Ultra 6단계 (URP Pipeline Asset)

---

## 레벨 디자인 철학

### "설명하지 않고, 경험하게 한다"

모든 메커닉은 세 단계로 도입됩니다:
1. **소개** — 새 타일/규칙을 처음 보여줌 (안전한 맥락)
2. **함정** — 자연스럽게 실수하게 유도
3. **우회** — 올바른 해법을 스스로 발견

텍스트 설명 없이 레벨 구조만으로 규칙을 전달하는 것을 목표로 설계했습니다.

### 메커닉 도입 순서 (챕터 1 기준)

```
F4 이동 → ALT 회전 → 사망 패턴 인지 → MoveToggle → RotateToggle → ActionToggle → 복합 응용
```

각 단계는 이전 단계를 전제로 하며, 절대로 두 가지를 동시에 처음 도입하지 않습니다.

---

## 기술 스택

| 분야 | 사용 기술 |
|---|---|
| 엔진 | Unity (URP) |
| 언어 | C# |
| 에셋 관리 | Unity Addressables |
| 데이터 관리 | ScriptableObject |
| UI | TextMeshPro, Canvas System |
| 버전 관리 | Git / GitHub |
| 개발 환경 | Windows 11, Visual Studio |

---

## 성과 및 지표

| 항목 | 수치 |
|---|---|
| 타일 타입 수 | 25종 |
| 완성 챕터 수 | 4챕터 |
| 총 스테이지 수 | [ 실제 수치 입력 ] |
| 개발 기간 | 약 6개월 |
| 코드 규모 | [ 대략적인 라인 수 또는 스크립트 수 입력 ] |

---

## 스크린샷 / 영상

> [ 게임 플레이 GIF 또는 스크린샷 첨부 ]
> [ 플레이 영상 링크 첨부 ]
> [ Toggle 시스템 시연 영상 링크 ]
> [ 듀얼맵 전환 시연 영상 링크 ]

---

## 한 줄 회고

> [ 이 프로젝트에서 가장 크게 배운 것을 한 문장으로 작성해주세요. ]
> 예: "단순한 규칙일수록 레벨 디자인의 가능성이 넓어진다는 것을 배웠습니다."

---

*포트폴리오 문의: shyboy0113@gmail.com*
