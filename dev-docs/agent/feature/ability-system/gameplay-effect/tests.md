# gameplay-effect — 검증(테스트) 목록

> 이 프로젝트는 자동 테스트가 없다 — **전부 Unity Play 모드 + Inspector 관측**으로, 각 항목은 유저가 에디터에서 수행한다.
> 대상: **AttributeCapture 계층 + AttributeBased magnitude evaluate**(progress 세부 TODO 3·5b, →D14~D21).
> 진행 문서: [progress.md](progress.md). 최종 갱신: 2026-08-04 (KST — HARNESS §3.4)

---

## 0. 공통 전제 · 관측 수단

**선행 조건**
- `Player_AttributeInitData`(또는 대상 ASC의 init data)에 **캡처 대상 AttributeSet이 존재**해야 한다(예: `CombatAttributeSet`). 없으면 모든 캡처가 무효 → 경고 + magnitude 0.
- ⚠ 아래에서 쓰는 `GE_EquipSpeedDown` 배선(Source `CombatAttributeSet.damage` → `Speed`)은 **문서 기준**이다 — 실제 modifier 필드값은 Inspector로 먼저 확인할 것.

**관측 수단**
- **(a) 이동 속도** — `CharacterAttributeSet.Speed`가 바뀌면 이동 속도로 바로 보인다(`PlayerCharacter.MoveDelta`가 매 프레임 읽음).
- **(b) `AbilitySystemComponentDrawer`** — active effect의 `Handle / Operation / EvaluatedMagnitude`를 Inspector에 출력한다. **계산된 magnitude 숫자를 직접 확인하는 주 수단.**
- **(c) Console** — 캡처 실패 `[AttributeBased] 캡처값 조회 실패`, 무효 핸들 `[GESpec] … modifier는 적용 시 반영되지 않는다`.

**GE 타입 주의**
- persistent(Duration/Infinite, period 0) = **CurrentValue** modifier → 활성 중에만 반영, 해제 시 원복.
- Instant / Periodic(period>0) = **BaseValue** 영구 변경.
- 각 테스트는 의도한 타입을 골라서 한다(원복 확인이 필요하면 Duration).

**적용 방법(공통)** — 둘 중 하나
- `GameplayEffectPickup` 프리팹에 GE를 배선하고 트리거로 진입, 또는
- 코드에서 `asc.ApplyGameplayEffectToTarget(effect, target)` / `ApplyGameplayEffectToSelf(effect)` 호출.

---

## A. AttributeBased evaluate (5b / D21)

- [ ] **A1. 계산 공식 `(v + Pre) * Coef + Post`**
  - 세팅: AttributeBased modifier의 backing = Source의 알려진 어트리뷰트(예: `damage = 10`), `coefficient = -0.5`, `preMultiplyAdditive = 0`, `postMultiplyAdditive = 0`.
  - 절차: 적용 후 드로어(b)에서 해당 modifier의 `EvaluatedMagnitude` 확인.
  - 기대: `(10 + 0) * -0.5 + 0 = -5`. Pre/Post도 바꿔가며 식이 맞는지 몇 조합 확인.

- [ ] **A2. Source 기반 실동작 (첫 관측치)**
  - 세팅: `GE_EquipSpeedDown`(Source `damage` → `Speed`), Duration/Infinite로.
  - 절차: 적용 → Source의 `damage`를 다른 값으로 바꿔 재적용.
  - 기대: `damage`에 연동해 `Speed`(이동 속도, 관측 a)와 드로어(b)의 magnitude가 변한다. → **캡처→evaluate가 실제 값을 만든다는 첫 증거.**

- [ ] **A3. 한 어트리뷰트에 여러 modifier (평행 배열 / 위치-key)**
  - 세팅: 같은 `Speed`를 건드리는 modifier 2개를 한 GE에 — 예: ① AttributeBased(`AddBase`), ② ScalableFloat(`AddBase`, 고정값).
  - 기대: **둘 다 누산**되어 반영(하나가 다른 하나를 덮지 않음). 드로어에 슬롯 2개가 각각 보임.

- [ ] **A4. 무효 핸들 modifier**
  - 세팅: 존재하지 않는 필드를 가리키는 modifier를 정상 modifier들 사이에 끼워 넣음.
  - 기대: Console에 `[GESpec] … N번 modifier는 적용 시 반영되지 않는다` 경고 + **그 modifier만 미반영**, 앞뒤 modifier는 정상(인덱스 안 밀림).

- [ ] **A5. ScalableFloat 회귀 (리팩터가 안 깼는지)**
  - 세팅: 기존 고정 magnitude GE(예: `GE_Speed`)를 그대로 적용.
  - 기대: D21 리팩터 이전과 **동일 동작**. 드로어 magnitude = 설정한 고정값.

- [ ] **A6. 캡처 실패 fallback**
  - 세팅: backing을 **Source에 없는** 어트리뷰트로 지정.
  - 기대: `[AttributeBased] 캡처값 조회 실패` 경고 + 그 항이 캡처값 0으로 계산(= `(0+Pre)*Coef+Post`).

---

## B. Capture 계층

- [ ] **B1. Source 캡처 (생성 시점)** — A2가 겸한다(Source `damage`가 반영되면 생성 시 Source 캡처 OK).

- [ ] **B2. Target 캡처 + 대상별 격리 (`Clone()` 검증)**
  - 세팅: backing을 **Target** 소스로 바꾼 GE. 대상 A·B의 캡처 대상 어트리뷰트 값을 **서로 다르게**.
  - 절차: **같은 GE**를 A와 B에 각각 적용, 두 active effect의 드로어 magnitude 비교.
  - 기대: A는 A의 값으로, B는 B의 값으로 계산 → **안 섞임**. (섞이면 clone/컨테이너 격리 결함)
  - 참고: Target 기반은 spec **생성 시점**에 target 미캡처라 A6류 경고가 한 번 뜰 수 있으나, 적용 시 재평가본이 정답 — 무해(TODO: 생성 시점 경고 억제).

- [ ] **B3. Base vs Current 캡처값 (`captureValueType`)**
  - 세팅: 캡처 대상 어트리뷰트에 persistent modifier를 걸어 **Base ≠ Current** 상태를 만든 뒤, backing의 `captureValueType`를 Base/Current로 토글.
  - 기대: 두 설정이 **서로 다른 값**을 캡처(하나는 Base, 하나는 Current).

### ⚠ snapshot(true/false) — 현재 구현상 modifier 경로에선 테스트 불가

- modifier magnitude는 **적용 시점 1회만** 계산된다(`CalculateModifierMagnitudes`가 생성·적용에서만 호출, 이후 재트리거 없음). 따라서 활성 중 source가 변해도 non-snapshot이라도 magnitude가 안 바뀔 것으로 **예상**된다(코드 구조 기반 추론 — 확인 필요).
- snapshot=false의 "라이브 재조회"가 관측되려면 **재평가가 반복되는 소비처**(주기 Execution이 캡처를 읽는 경로)가 필요하다. 현재 `SpeedBoostExecution`은 캡처값을 안 쓰므로(고정 +10) 이 테스트는 **소비처 배선 후**로 미룬다.
- 지금 유효한 snapshot 확인 범위 = "apply 시점 값으로 고정되는가"(true·false 공통)까지. "활성 중 라이브 반영"은 검증 대상 아님.

---

## C. 우선순위 (첫 관측치용)

**A2 → A5 → A4 → A3 → B2** 순.
- A2: 캡처→evaluate가 값을 만든다는 첫 관측.
- A5: 리팩터 회귀(기존 동작 보존).
- A4·A3: 평행 배열 불변식(무효 스킵·위치-key).
- B2: clone 격리.
- A1·A6·B3: 정밀·경계. snapshot(라이브)은 소비처 배선 후.
