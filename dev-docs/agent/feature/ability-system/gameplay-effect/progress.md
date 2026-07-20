# gameplay-effect — 게임플레이 이펙트 (GE·Spec·Active·Modifier)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: 2026-07-19 (KST — HARNESS §3.4)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-ability](../gameplay-ability/progress.md).
> **핵심은 구현·동작(✅)**, 아래 블로커(⚠️ GE 타입 실전 상태·Execution 껍데기)만 미결이라 IN-PROGRESS.

## 목표
모든 수치 변경을 GameplayEffect 한 채널로 통과시켜, 여러 출처의 수치 조작이 순서·잔류 문제 없이 합쳐지게 한다. SO 불변 정의 + 런타임 Spec 분리, Modifier 6종 연산, 적용/해제/주기/지속.

## 수용 기준 (Definition of Done)
- [x] GE(SO) 적용/해제가 Attribute 값에 반영
- [x] Modifier 6종(`AddBase`/`MultiplyAdditive`/`DivideAdditive`/`MultiplyCompound`/`AddFinal`/`Override`)이 공식대로 CurrentValue 누산
- [x] Duration 만료 제거·Period 주기 틱 동작
- [x] 읽기 캐시로 `GetAttributeCurrentValue` O(1)
- [ ] **GE 타입별(Instant/Duration) 실전 상태 확정** (⚠️ 아래 블로커)
- [ ] `GameplayEffectExecution` concrete 1개 이상 배선·검증 (프레임워크는 2026-07-19 완료, concrete·검증 남음 — 세부 TODO 4b)

## 범위
### 포함
- `GameplayEffect`(SO)·`GameplayModifier`·`GameplayEffectSpec`·`ActiveGameplayEffect`(+Handle)·`GameplayEffectContext`(+Handle)
- ASC 실행: `ApplyGameplayEffectToSelf`/`RemoveActiveGameplayEffect`, Modifier 누산, Duration/Period 틱, 읽기 캐시
### 제외 (현 시점)
- Execution concrete / GE 스택 / Gameplay Cue / GameplayTag / ScalableFloat 커브 (→ 아키텍처 미구현 로드맵)
- Magnitude 확장 중 `SetByCaller` / `CustomCalculationClass` (AttributeBased만 진행 — 세부 TODO 5)

## 설계 개요
- 아키텍처: [architecture/ability-system/gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md)
- 코드: `Assets/Scripts/Core/AbilitySystem/Effect/`, `AbilitySystemComponent.cs`

## 세부 TODO (구현 체크리스트)
- [x] 1. GE/Modifier 정의 + Spec resolve 캐싱 + Active 상태
- [x] 2. ASC Apply/Remove + Modifier 6종 누산 + Duration/Period 틱 + 읽기 캐시
- [ ] 3. GE 타입 실전 상태 확정 (유저 확인 대기)
- [~] 4. **Execution 배선·검증** — 5b보다 먼저 진행(2026-07-19 순서 역전). 근거: 배관이 이미 있어 5b보다 훨씬 싸고, Execution 결과가 5b가 재현해야 할 **기준선 숫자**가 된다
  - [x] 4a. **프레임워크 완성** (2026-07-19) — SO→순수 클래스 전환(→D7, 직렬화 형태는 →D8 미결) · Params/Output 분리(→D11) · 쓰기 경로 단일화(→D9) · Base·Current 즉시 갱신(→D10) · Execution별 즉시 반영 · `ExecuteGameplayEffect` 개명 · GE 드로어에 `Executions` 노출
  - [ ] 4b. **concrete 구현체 + 플레이 검증** — 현재 `SpeedBoostExecution`(검증용, speed 영구 증가)뿐이고 **에셋 배선 안 됨**. 실행하려면 Instant GE의 `executionTypeNames`에 AQN을 넣어야 함. 실제 전투 공식(`Source.damage - Target.defense` 등)은 미작성 — `defense` 어트리뷰트 추가 여부가 유저 판단 대기
- [~] 5. **Magnitude 확장 — AttributeBased** (당초 "Execution보다 우선"이었으나 위 4로 순서 역전. 방향 자체(단일 어트리뷰트 값이 Execution 필요성을 줄임)는 유지 — 둘 다 짜본 뒤 역할 분담을 근거와 함께 확정한다)
  - [x] 5a. 데이터 모델(enum·`AttributeBasedMagnitude` struct·`GameplayModifier.attributeBased`) + 에디터 드로잉(payload별 자체 `[CustomPropertyDrawer]` 위임 + `AttributeReferenceGUI` 공용 팝업 추출)
  - [~] 5b. 런타임 평가: `GameplayModifier`가 source/target ASC로 evaluate → `GameplayModifierSpec`/`GameplayEffectSpec` 생성자에 target 전달 + context에 source. apply 시점 1회 스냅샷.
    - [x] (2026-07-19) source 전달 배관: ToTarget/SpecToTarget 경로 + `MakeEffectContext`(Instigator=self)/`MakeOutgoingSpec` 팩토리. context는 `AddInstigator`/`AddSourceObject` 주입·`GetInstigator`/`GetSourceObject` 조회 (→D5)
    - [ ] 미결: `GameplayModifier`가 `spec.Context.GetInstigator()`/target ASC를 읽어 magnitude를 실제 evaluate (지금은 고정 magnitude만 반환). Spec 생성자에 target ASC 전달 필요
  - [ ] 5c. 호출처 배선: `WeaponAttackComponent`가 공격 시 `MakeEffectContext()` 기반으로 target에 GE 적용(Instigator=공격자 ASC)
  - [ ] 5d. 테스트 에셋(`데미지 = Source.CombatAttributeSet.damage`)로 에디터 검증

## 결정 기록
상세(맥락·대안·근거·트레이드오프)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-06-13 | GE 단일 채널 추상화 — 모든 수치 변경을 GE 한 경로로 |
| D2 | 2026-06-13 | SO 불변 정의 + 런타임 `Spec` 분리 (공유 GE 간섭 제거, resolve 1회 캐싱) |
| D3 | 2026-06-13 | 쓰기 시점 재계산 + 읽기 캐시 → `GetCurrentValue` O(1) |
| D4 | 2026-06-13 | Modifier 6종 + BaseValue/CurrentValue 두 경로 (Instant·Periodic=Base 영구변경 / Duration·Infinite=Current만) |
| D5 | 2026-07-19 | `MakeOutgoingSpec`은 Spec(class) 직접 반환 — SpecHandle 미도입. 나머지 handle은 실익 있어 유지 |
| ~~D6~~ | 2026-07-19 | **결번(폐기)** — 에이전트가 승인 없이 작성한 "캡처 미도입" 결정. 방향은 **도입**으로 확정 |
| D7 | 2026-07-19 | Execution은 SO가 아닌 순수 클래스 — `GameplayEffectExecutionAsset` 폐기. SO의 존재 이유(공유 데이터)가 Execution엔 없음 |
| D8 | 2026-07-19 | (미결) Execution 직렬화 형태 — AQN 타입 참조 vs `[SerializeReference]` 인라인. 현재 AQN으로 구현됨 |
| D9 | 2026-07-19 | Execute 경로 쓰기 지점을 `ApplyEvaluatedModifier` 하나로 통합 — Execution 출력의 연산 지원 불일치 해소 |
| D10 | 2026-07-19 | BaseValue 쓰기마다 CurrentValue 즉시 재계산 — 일괄 갱신 시 stale 값을 읽던 버그 수정 |
| D11 | 2026-07-19 | Execution 입력/출력 타입 분리 + 둘 다 struct + execution 스코프 지역 변수 (UE 구조 정합) |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- **⚠️ GE 타입 실전 상태 미확정:** `GameplayEffectType` enum 주석(`Instant`/`Duration`="미구현")과 ASC 실행 경로(셋 다 존재)가 어긋난다. 추정 금지 — **유저 직접 확인 예정**. (TODO-BOARD에도 등록)
- **Execution 껍데기:** `GameplayEffectExecution` + ASC `RunExecutions` 배관만 있고 concrete 0·에디터 미배선·미검증.

## 다음 작업

2026-07-19 순서 재편(유저 확정). **미검증 코드 위에 새 기능을 겹쳐 쌓지 않는다** — 각 단계에서 새로 도입되는 미검증 요소를 하나로 유지해 실패 원인을 가릴 수 있게 한다.

1. **[유저 진행 중] ScalableFloat 고정 magnitude 루프 검증** — 새 계산 코드 0줄. `health AddBase -10` 류 GE를 `ApplyGameplayEffectToTarget`으로 적용.
   - 검증 대상 = **작성됐으나 한 번도 컴파일·실행 안 된 배관**: `GameplayEffectContext` 재구성(`AddInstigator`/`GetInstigator`), 이를 참조하는 `GameplayEffectExecutionParameters`·`GameplayEffectContextHandle`, ASC 신규 API 4종(`MakeEffectContext`/`MakeOutgoingSpec`/`ApplyGameplayEffectToTarget`/`ApplyGameplayEffectSpecToTarget`), 5a 드로어.
   - 참고: `coefficient` 0 주의사항은 **AttributeBased 전용**이라 이번 검증엔 무영향. `GE_EquipSpeedDown` 미작동은 item-system 파킹 건(`AbilitySystemModuleContext` 삭제)이지 GE 문제 아님.
2. **Execution concrete 1개** (세부 TODO 4) — `GameplayEffectExecution`(순수 클래스 →D7) 상속 서브클래스 하나. 배관은 전부 존재하므로 클래스만 만들면 된다. 이 결과가 3의 기준선 숫자가 된다.
   - 선행 확인 필요: 데미지 공식이 `Source.damage - Target.defense` 형태라면 **`defense` 어트리뷰트가 없다**(`CombatAttributeSet`에 `damage`뿐). 추가 여부는 유저 결정 사항.
3. **AttributeBased 런타임 evaluate** (세부 TODO 5b) — `GameplayModifier.GetMagnitude` → `EvaluateMagnitude(level, source, target)` 교체(AttributeBased는 source·target ASC의 어트리뷰트를 읽어 계산 — **AttributeCapture 계층 도입과 함께 갈지 먼저 정해야 함**, 캡처만 넣으면 Modifier 쪽 관측 변화가 0) → `GameplayModifierSpec`/`GameplayEffectSpec` 생성자에 target ASC 전달 → 2의 결과와 숫자 일치로 검증.
   - **↳ 이때 함께:** 하류 [gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) 갱신(2026-07-19 context API 변경 + evaluate 동작). context API 변경만 먼저 반영하지 않고 5b 완료 시 evaluate 서술과 **묶어서** 갱신하기로 함(유저 확정 2026-07-19).
4. **역할 분담 확정** — 2·3을 다 짜본 뒤 "Execution은 클램프·크리티컬 등 분기 로직 전용, 단순 사칙연산은 AttributeBased" 경계를 decisions에 기록(당초 3번이던 "Execution 스코프 아웃" 판단을 실측 근거로 대체).
5. (보류) 유저가 GE 타입 실전 상태 확정 → [gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) §확인 필요 + [overview.md](../../../../project/architecture/ability-system/overview.md) 요약 표 갱신.

> ⚠️ 별개 이슈(3 착수 시 처리): `ApplyGameplayEffectSpecToTarget`이 **같은 spec 인스턴스**를 target에 넘긴다(`AbilitySystemComponent.cs:170`). 지금은 magnitude가 apply 전 확정이라 무해하나, 3에서 target 기반 evaluate가 들어가면 한 spec을 여러 대상에 적용 시 서로를 덮어쓴다. UE는 `ApplyGameplayEffectSpecToSelf`에서 spec을 복사해 ActiveGE에 넣는다 — 동일 처리 필요.
