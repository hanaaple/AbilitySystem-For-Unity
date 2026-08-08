# gameplay-effect — 게임플레이 이펙트 (GE·Spec·Active·Modifier)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: 2026-08-05 (KST — HARNESS §3.4)

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
- [x] `GameplayEffectExecution` concrete 1개 이상 배선·검증 (프레임워크 2026-07-19, concrete `SpeedBoostExecution` 배선·플레이 검증 2026-07-20)

> Capture·AttributeBased의 **세부 검증 절차·항목**: [tests.md](tests.md) (에디터 관측 기반 체크리스트).

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
  - [x] 4b. **concrete 구현체 + 플레이 검증** (2026-07-20) — `SpeedBoostExecution`(검증용, speed 영구 증가)을 `GE_Speed`(Duration + **period 1**)의 `executionTypeNames`에 배선하고 `GameplayEffectPickup` 트리거로 적용해 **유저가 실행 확인**. Execution은 **Instant 또는 Period > 0** 에서만 실행된다(Duration/Infinite + Period 0은 실행 안 됨 — UE 동일, 유저 확인).
    - 남은 것: **실제 전투 공식은 미작성.** `defense` 어트리뷰트는 추가하지 않기로 확정(2026-07-20)이라 `Source.damage - Target.defense` 형태를 못 쓴다 — 계산식을 다시 정해야 한다
- [~] 5. **Magnitude 확장 — AttributeBased** (당초 "Execution보다 우선"이었으나 위 4로 순서 역전. 방향 자체(단일 어트리뷰트 값이 Execution 필요성을 줄임)는 유지 — 둘 다 짜본 뒤 역할 분담을 근거와 함께 확정한다)
  - [x] 5a. 데이터 모델(enum·`AttributeBasedMagnitude` struct·`GameplayModifier.attributeBased`) + 에디터 드로잉(payload별 자체 `[CustomPropertyDrawer]` 위임 + 공용 Set/Attribute 팝업). _(2026-08-03 →D15: 그 팝업 헬퍼 `AttributeReferenceGUI`를 `GameplayAttribute` 전용 드로어로 흡수·삭제)_
  - [x] 5b. 런타임 평가 (2026-08-04) — 캡처값을 실제 magnitude로 소비. apply 시점 1회 스냅샷.
    - [x] (2026-07-19) source 전달 배관: ToTarget/SpecToTarget 경로 + `MakeEffectContext`(Instigator=self)/`MakeOutgoingSpec` 팩토리. context는 `AddInstigator`/`AddSourceObject` 주입·`GetInstigator`/`GetSourceObject` 조회 (→D5)
    - [x] (2026-08-04) `GameplayModifier.GetMagnitude` → `EvaluateMagnitude(spec, level)`: AttributeBased면 `AttributeBasedMagnitude.Evaluate(spec)`가 `TryGetCapturedValue`로 캡처값 읽어 `(v+Pre)*Coef+Post`. `GameplayEffectSpec.CalculateModifierMagnitudes`가 캡처 후(생성 시 Source·적용 시 Target 재평가)에 modifier magnitude 확정. **유저 에디터 검증 대기**(관측 변화 0→유의미 첫 지점).
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
| D12 | 2026-07-28 | `GameplayEffectAttributeCaptureDefinition`(신설 당시 `AttributeCaptureDefinition`) 독립 struct 신설(additive) — UE 원형 필드만, AttributeBasedMagnitude 불변 |
| D13 | 2026-08-01 | 캡처 결과 계층 신설 — Spec(캡처 1건)+Container(묶음), snapshot 완전 구현(고정/라이브 재조회). 정의를 UE 원형명으로 rename |
| D14 | 2026-08-02 | `GameplayEffectSpec`이 캡처 컨테이너 소유 + `AttributeBasedMagnitude`를 정의로 compose(중첩 `backingAttribute`+`snapshot`). Setup이 AttributeBased modifier 정의 등록·생성 시 Source 캡처. **관측 변화 0**(evaluate 미배선). D12 additive 대체 |
| D15 | 2026-08-03 | 어트리뷰트 참조를 직렬화 타입 `GameplayAttribute`(AQN+필드명)로 추출 + 전용 드로어 → `GameplayModifier`·`GameplayEffectAttributeCaptureDefinition` 둘 다 compose. 팝업 배선 반복 제거(UE `FGameplayAttribute` 대응). `AttributeReferenceGUI` 흡수·삭제, GE 에셋 2개 마이그레이션 |
| D16 | 2026-08-03 | 캡처 컨테이너의 별도 `_definitions` 리스트 제거 — UE처럼 각 spec이 정의를 품고 `AddCaptureDefinition`이 미캡처 spec 삽입·`CaptureAttributes`가 제자리 채움. Spec에 선언 전용 생성자 추가. UE 원문 확인 근거, **관측 변화 0**. D13 저장 구조 일부 대체 |
| D17 | 2026-08-03 | Execution이 캡처 대상을 `Defs()`(선언, static 배열의 이름 정의 = Execute의 조회 key)로 노출 → `GameplayEffectSpec`이 순회 등록(자가 등록 아님, modifier 경로와 동형). 베이스 `Defs()`는 virtual 기본 빈 목록. **관측 변화 0**(등록만, Execute 미소비) |
| D18 | 2026-08-04 | `ActiveGameplayEffect`가 Owner(target ASC)를 직접 필드로 보유, `ActiveGameplayEffectsContainer`는 **당장(잠정) 유보** — 배제 아님, 도입 가능성 열림(유저 확정). UE(컨테이너.Owner + handle 전역맵)와 갈리는 잠정 divergence — 단순성 우선. 재평가 트리거: 컨테이너 도입 시 Owner 이관 |
| D19 | 2026-08-04 | `GameplayEffectSpec` 복사 생성자(+`Clone()`) 신설 — 적용 시점 대상별 복제용. 복사 심도: Definition·Executions 공유, Modifiers 리스트 복사, CapturedRelevantAttributes 깊은 복사(컨테이너 `Clone()` 신설). `Initialize()` 재실행 안 함. **관측 변화 0**(아직 미배선). UE `FGameplayEffectSpec` copy 생성자 대응 |
| D21 | 2026-08-04 | AttributeBased evaluate(5b) 배선 + `GameplayModifierSpec`을 UE `FModifierSpec`에 정렬 — 정의와 평행 인덱스 슬롯 배열(무필터, 위치가 key), magnitude는 캡처 후 제자리 재계산. 계산 2층(`GameplayModifier.EvaluateMagnitude`→`AttributeBasedMagnitude.Evaluate`). identity(핸들·연산)는 문자열 어트리뷰트라 슬롯에 캐시(UE 최소형과의 의도적 divergence). ASC 소비처 변경 0. **관측 변화 0→유의미 첫 지점**(유저 검증 대기) |
| D20 | 2026-08-04 | spec 직접 생성 API + apply 시점 clone(→D19) **유지**, asset-only로 안 좁힘. 근거: **SetByCaller 도입 예정**(유저 확정) — 런타임 값을 spec에 꽂아 멀티적용하는 실용도가 생겨 clone이 필수 안전장치. clone은 죽은 코드가 아니라 소비처(SetByCaller·apply-time evaluate) 대기 중인 선배관. 재평가 트리거: SetByCaller/AoE 철회 시 asset-only로 축소 |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- **⚠️ GE 타입 실전 상태 미확정:** `GameplayEffectType` enum 주석(`Instant`/`Duration`="미구현")과 ASC 실행 경로(셋 다 존재)가 어긋난다. 추정 금지 — **유저 직접 확인 예정**. (TODO-BOARD에도 등록)
- ~~**Execution 껍데기**~~ **해소(2026-07-20)** — concrete(`SpeedBoostExecution`) 배선·플레이 검증 완료. 실제 전투 공식 작성은 남았으나 블로커는 아니다.

## 다음 작업

2026-07-19 순서 재편(유저 확정). **미검증 코드 위에 새 기능을 겹쳐 쌓지 않는다** — 각 단계에서 새로 도입되는 미검증 요소를 하나로 유지해 실패 원인을 가릴 수 있게 한다.

1. **[✅ 완료 2026-07-19] ScalableFloat 고정 magnitude 루프 검증** — 새 계산 코드 0줄. `health AddBase -10` 류 GE를 `ApplyGameplayEffectToTarget`으로 적용.
   - 검증 대상 = **작성됐으나 한 번도 컴파일·실행 안 된 배관**: `GameplayEffectContext` 재구성(`AddInstigator`/`GetInstigator`), 이를 참조하는 `GameplayEffectExecutionParameters`·`GameplayEffectContextHandle`, ASC 신규 API 4종(`MakeEffectContext`/`MakeOutgoingSpec`/`ApplyGameplayEffectToTarget`/`ApplyGameplayEffectSpecToTarget`), 5a 드로어.
   - 참고: `coefficient` 0 주의사항은 **AttributeBased 전용**이라 이번 검증엔 무영향. `GE_EquipSpeedDown` 미작동은 item-system 파킹 건(`AbilitySystemModuleContext` 삭제)이지 GE 문제 아님.
2. **[✅ 완료 2026-07-20] Execution concrete 1개** (세부 TODO 4) — `SpeedBoostExecution` 작성 후 **유저가 에디터에서 실행 확인**. Execution이 실제로 돌아가는 것을 처음 관측했다.
   - 검증 구성: `GE_Speed`(**Duration + period 1**) + `GameplayEffectPickup` 트리거 적용. 1초마다 speed +10이 BaseValue에 누적된다. 관측은 이동 속도로 — `PlayerCharacter.MoveDelta`가 `CharacterAttributeSet.Speed`를 매 프레임 읽는다.
   - **확정된 동작(유저 확인):** Execution은 **Instant** 또는 **Period > 0** 에서만 실행된다. **Duration/Infinite + Period 0이면 실행되지 않는다**(UE 동일). 그 경우 계산이 필요하면 Execution이 아니라 **AttributeBased magnitude**(UE의 MMC 대응 = 아래 3번)를 써야 한다. → 반복해서 헷갈린 지점이라 [gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) 기록 여부는 **유저 판단 대기**.
   - `defense` 어트리뷰트는 **추가하지 않기로 확정**(2026-07-20, 유저) — 에이전트가 무단 추가했던 필드를 철회했다. 데미지 공식을 `Source.damage - Target.defense`로 잡을 수 없으므로, 실전 Execution 설계 시 계산식을 다시 정해야 한다.
3. **AttributeCapture 계층 (핵심 배관 완료 — 정의→컨테이너→소유→2단계 캡처→evaluate)** — 2026-07-28 정의 `GameplayEffectAttributeCaptureDefinition`(→D12), 2026-08-01 `GameplayEffectAttributeCaptureSpec`+`...Container`(→D13), 2026-08-02 `GameplayEffectSpec` 소유+compose+Source 캡처(→D14), 2026-08-04 Target 캡처+spec 복사(→D18/D19/D20) + **evaluate(5b)+`GameplayModifierSpec` UE 정렬(→D21)**. **남은 것: 유저 에디터 검증(evaluate가 만든 첫 관측치) → SetByCaller.** evaluate 전까지는 캡처값 읽는 곳이 없어 **관측 변화 0**이었고, 5b가 그 게이트다.
   - **[✅ 2026-08-02] Spec 소유·정의 등록·2단계 캡처 배관(→D14):** `GameplayEffectSpec.CapturedRelevantAttributes` 소유. `SetupAttributeCaptureDefinitions`가 AttributeBased modifier의 `BackingAttribute` 등록 → `CaptureDataFromSource`가 생성 시 Source(=Context.Instigator) 캡처. `CaptureAttributeDataFromTarget(target)`는 **메서드만 제공, ASC 적용 경로 미배선**(아래 ⚠ spec 복사 선행). `AttributeBasedMagnitude`를 정의(`GameplayEffectAttributeCaptureDefinition backingAttribute`+`snapshot`)로 compose하고 드로어(7→8행)·`GE_EquipSpeedDown` 에셋 마이그레이션.
   - **[✅ 2026-08-03] 어트리뷰트 참조 타입 추출(→D15):** `attributeSetTypeName`+`fieldName` 두 문자열을 직렬화 타입 `GameplayAttribute`로 뽑고 전용 `[CustomPropertyDrawer]`를 붙여, `GameplayModifier`·`GameplayEffectAttributeCaptureDefinition`이 이 타입을 compose. 드로어 2개는 Set/Attribute 2행을 `attribute` PropertyField로 위임(팝업 배선 자동화). `AttributeReferenceGUI` 흡수·삭제, `GE_EquipSpeedDown`·`GE_MeleeDamage` 중첩 `attribute:` 레이아웃으로 마이그레이션. **관측 변화 0**(순수 구조 정리·evaluate와 무관).
   - **[✅ 2026-08-03] 캡처 컨테이너 UE 원형 정렬(→D16):** 별도 `_definitions` 리스트 제거 — 각 spec이 정의를 품고 `AddCaptureDefinition`이 미캡처 spec 삽입·`CaptureAttributes`가 제자리 채움(선언 전용 생성자, `IsValid=false`). 공개 API 시그니처 불변(호출부 영향 0). **관측 변화 0**.
   - **[✅ 2026-08-03] Execution 캡처 선언 등록(→D17) + params 조회:** Execution이 `Defs()`(선언, static 배열의 이름 정의 = Execute의 값-동등성 조회 key)로 캡처 대상을 노출 → `SetupAttributeCaptureDefinitions`가 modifier와 동형으로 순회 등록. `GameplayEffectExecutionParameters`에 `AttemptCalculateCapturedAttributeMagnitude`(Current)·`AttemptCalculateCapturedAttributeBaseValue`(Base) 추가(UE `FGameplayEffectCustomExecutionParameters` 대응, `TryGetCapturedValue`에 위임). **관측 변화 0**(Execute가 아직 캡처값 미소비, SpeedBoost는 고정 +10).
   - **[✅ 2026-08-04] Target 캡처 + spec 복사 배선(계획 8/9, →D18/D19/D20):** `ActiveGameplayEffect.Owner`(target ASC) 추가(→D18). `GameplayEffectSpec` 복사 생성자+`Clone()`(→D19, 컨테이너 `Clone()` 깊은 복사). `ApplyGameplayEffectSpecToSelf`가 적용 경계에서 `spec.Clone()`+`CaptureAttributeDataFromTarget(this)` → Instant·Duration 공통 관통(UE 원문: 양 경로 모두 복사본에 target 캡처). spec 직접생성 API+clone **유지**(asset-only 반려, SetByCaller 예정 →D20). **관측 변화 0**(evaluate 없어 캡처값 미소비).
   - **[✅ 2026-08-04] AttributeBased 런타임 evaluate = 키스톤 (세부 TODO 5b):** `GameplayModifier.GetMagnitude` → `EvaluateMagnitude(spec, level)`. AttributeBased면 `AttributeBasedMagnitude.Evaluate(spec)`가 `TryGetCapturedValue`로 캡처값 읽어 `(v+Pre)*Coef+Post`(실패 시 0+경고). `GameplayEffectSpec.CalculateModifierMagnitudes` 신설 — `Initialize` 순서를 캡처 선행→modifier 평가 후행으로 재배치(기존 순서 역전이 AttributeBased가 캡처값 못 읽던 원인), `CaptureAttributeDataFromTarget`가 target 캡처 직후 재평가. **함께 `GameplayModifierSpec`을 UE `FModifierSpec`에 정렬(→D21)** — 정의와 평행 인덱스 배열(무필터), magnitude 제자리 갱신(`BuildModifierSpecs` 1회 + `CalculateModifierMagnitudes` 재계산), identity는 슬롯 캐시. ASC 소비처 변경 0. **⚠ 여기부터 관측 변화 0→유의미** — 캡처가 실제 값을 담는지 유저가 에디터에서 처음 확인(§블로커 아래 검증 대상).
   - **[✅ 2026-08-05] 검증 디버그 도구 세트** (코드/에셋 변경 아님 — 관측 인프라) — 5b가 만든 "관측 변화 0→유의미"를 **눈으로 확인할 수단**. ① ASC 인스펙터/전용 창(`AbilitySystemInspectorGUI`+`AbilitySystemComponentDrawer`+`AbilitySystemComponentWindow`)에 Active Effect의 Modifiers/Executions/**Captured Attributes**(Source/Target·Base/Current·snapshot·출처·Execution 더블클릭→IDE) 표시. ② `BoxRoom`(prefab+스크립트) — Room ASC=source로 들어온 액터에 3모드(Instant/Buff/TickExecution) GE 적용·나가면 해제. ③ coefficient 기본값 1 버그 수정(`OnAddModifier`). **미커밋·유저 에디터 검증 대기.** 상세 = worklog 2026-08-05.
   - **[남음] 유저 에디터 검증** — 위 도구로 `GE_EquipSpeedDown`(Source `CombatAttributeSet.damage` 기반 speed 감소)이 실제 캡처·계산되는지 확인. 절차 = [tests.md](tests.md). ⚠ 선행: Room/Player ASC에 캡처 대상 AttributeSet 존재.
   - **[남음] SetByCaller** — 유저 도입 확정(→D20). 런타임 계산값(어트리뷰트 아닌 값)을 spec에 꽂아 적용. spec 직접생성 API+clone의 실사용 소비처. 별도 작업(TODO-BOARD 등록).
   - **↳ 이때 함께:** 하류 [gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) 갱신(2026-07-19 context API 변경 + evaluate 동작). context API 변경만 먼저 반영하지 않고 5b 완료 시 evaluate 서술과 **묶어서** 갱신하기로 함(유저 확정 2026-07-19).
4. **역할 분담 확정** — 2·3을 다 짜본 뒤 "Execution은 클램프·크리티컬 등 분기 로직 전용, 단순 사칙연산은 AttributeBased" 경계를 decisions에 기록(당초 3번이던 "Execution 스코프 아웃" 판단을 실측 근거로 대체).
5. (보류) 유저가 GE 타입 실전 상태 확정 → [gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) §확인 필요 + [overview.md](../../../../project/architecture/ability-system/overview.md) 요약 표 갱신.

> ✅ 해소(2026-08-04): 위 "같은 spec 인스턴스 공유" 이슈는 `ApplyGameplayEffectSpecToSelf`가 적용 경계에서 `spec.Clone()` 후 target 캡처하도록 배선해 해결(→D18/D19/D20). 한 spec을 여러 대상에 적용해도 각자 복사본에 캡처해 안 덮어씀.
