# gameplay-effect — 게임플레이 이펙트 (GE·Spec·Active·Modifier)

- 상태: ✅ 임시완료
- 우선순위: P0
- 최종 갱신: 2026-10-19 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-ability](../gameplay-ability/progress.md) · [aggregator](../aggregator/progress.md).
> 핵심 배관(정의·Spec·Modifier·Execution·Capture·AttributeBased)은 구현·검증됨. 라이브 재평가(non-snapshot 추종)는 [aggregator](../aggregator/progress.md)가 얹었다.

## 목표
모든 수치 변경을 GameplayEffect 한 채널로 통과시켜, 여러 출처의 수치 조작이 순서·잔류 문제 없이 합쳐지게 한다. SO 불변 정의 + 런타임 Spec 분리, Modifier 6종 연산, 적용/해제/주기/지속, 캡처 기반 계산(AttributeBased·Execution).

## 수용 기준 (Definition of Done)
- [x] GE(SO) 적용/해제가 Attribute 값에 반영
- [x] Modifier 6종(`AddBase`/`MultiplyAdditive`/`DivideAdditive`/`MultiplyCompound`/`AddFinal`/`Override`)이 공식대로 CurrentValue 누산
- [x] GE 3종(Instant/Duration/Infinite) 실행 경로 동작 — Instant는 즉시 실행 후 소멸, Duration은 만료 제거, Infinite는 persistent, Period>0는 주기 틱
- [x] CurrentValue가 aggregator 집계(`Evaluate`)로 mod 반영해 산출 (persistent mod 등록/해제 → 재계산)
- [x] `GameplayEffectExecution` concrete 배선·플레이 검증 (`SpeedBoostExecution`, 2026-07-20)
- [x] AttributeBased magnitude 캡처·평가 (Source 생성 시·Target 적용 시 캡처 → `(v+Pre)*Coef+Post`)

> Capture·AttributeBased의 세부 검증 절차·항목: [tests.md](tests.md).

## 범위
### 포함
- 정의/런타임 분리: `GameplayEffectAsset`(SO: Type·Duration·Period·Modifiers·ExecutionTypeNames) ↔ `GameplayEffectSpec`(런타임 인스턴스, 대상별 `Clone()`)
- `GameplayModifier`(대상 어트리뷰트+연산+계산방식) / `GameplayModifierSpec`(평행 인덱스 슬롯, EvaluatedMagnitude 캐시) / `GameplayModifierOperation` 6종
- Magnitude 계산: `ScalableFloat`(고정) · `AttributeBased`(캡처값 기반)
- 캡처 계층: `GameplayEffectAttributeCaptureDefinition`/`...Spec`/`...SpecContainer` — Source(생성 시)·Target(적용 시) 2단계
- Execution: `GameplayEffectExecution`(순수 클래스, AQN 저장·1회 인스턴스화) + `...Parameters`/`...Output` — Instant/Periodic에서만 실행
- Context: `GameplayEffectContext`(+Handle) — Instigator/SourceObject 주입·조회
- 실행 경계는 `ActiveGameplayEffect`(+Handle)로 표현, 소유·수명·집계는 컨테이너(→ [aggregator](../aggregator/progress.md))
### 제외 (현 시점 — 이후 확장 가능)
- GE 스택 / Gameplay Cue / GameplayTag / ScalableFloat 커브 테이블(현재 고정 float)
- `SetByCaller` / `CustomCalculationClass` magnitude (→D20(spec API+clone 유지) SetByCaller 도입 예정)
- 실전 전투 데미지 공식(계산식 미확정 — `defense` 어트리뷰트는 추가 안 함으로 확정)

## 설계 개요
- **정의↔Spec(→D1(GE 단일 채널)·D2(정의/Spec 분리))**: 모든 수치 변경을 GE 한 경로로. SO는 불변 공유 정의, 적용 시 `GameplayEffectSpec`으로 인스턴스화하고 적용 경계에서 대상별 `Clone()`(→D19(Spec 복사/Clone)) 후 Target 캡처 → 같은 spec을 여러 대상에 적용해도 캡처값이 섞이지 않는다.
- **Modifier 슬롯(→D13(캡처 계층+snapshot)·D21(ModifierSpec UE 정렬))**: `GameplayEffectSpec.Modifiers`는 정의(`Definition.Modifiers`)와 평행한 인덱스 배열. identity(대상·연산)는 정의가 들고 슬롯은 `EvaluatedMagnitude`만 캐시 — 라이브 재평가 시 슬롯 magnitude만 제자리 갱신.
- **CurrentValue 산출**: persistent(period 0) GE의 modifier는 대상 어트리뷰트 aggregator에 mod로 등록, `AttributeAggregator.Evaluate`가 공식대로 누산해 CurrentValue를 낸다(→ [aggregator](../aggregator/progress.md)). Instant/Periodic은 BaseValue를 영구 변경.
- **쓰기 단일화(→D9(쓰기 지점 통합))**: Modifier·Execution 출력 모두 `ApplyModToAttribute` 한 지점으로 BaseValue에 적용 — 연산 지원 범위가 어긋나지 않게.
- **캡처 계산(→D12(캡처 정의 struct)~D17(Execution Defs 등록))**: AttributeBased modifier·Execution이 참조하는 어트리뷰트를 `SetupAttributeCaptureDefinitions`가 컨테이너에 등록 → Source는 생성 시, Target은 적용 시 캡처. `CalculateModifierMagnitudes`가 캡처 이후 magnitude 확정.
- **Execution(→D7(Execution=순수 클래스)·D11(입출력 타입 분리)·D17(Execution Defs 등록))**: SO 아닌 순수 클래스, AQN으로 저장·1회 resolve. 입력/출력 struct 분리, `Defs()`로 캡처 대상 선언. Instant·Period>0에서만 실행(Duration/Infinite+Period0은 실행 안 됨 — UE 동일).
- 아키텍처: [architecture/ability-system/gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) (동기화 최신 아닐 수 있음)
- 코드: `Assets/Scripts/Core/AbilitySystem/Effect/`, 적용·수명은 `ActiveGameplayEffectsContainer` / 공개 API는 `AbilitySystemComponent`

## 세부 TODO (구현 체크리스트)
- [x] 1. GE/Modifier 정의 + Spec resolve 캐싱 + Active 상태
- [x] 2. Apply/Remove + Modifier 6종 누산 + Duration/Period 틱 (현재 컨테이너 소유)
- [x] 3. GE 3종 실행 경로 (enum 주석의 "미구현"은 낡음 — 코드상 셋 다 동작)
- [x] 4. Execution 프레임워크 + concrete(`SpeedBoostExecution`) + 플레이 검증
- [x] 5. AttributeBased magnitude — 데이터 모델 + 캡처(Source/Target 2단계) + 런타임 evaluate
- [ ] 6. `SetByCaller` magnitude (→D20(spec API+clone 유지), 유저 도입 확정) — 별도 착수
- [ ] 7. 실전 전투 공식 확정 (Execution vs AttributeBased 역할 경계 →아래 결정 대기) + 호출처 배선(공격 시 target에 GE 적용)

## 결정 기록
상세는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.** 치운 결정(D3·D6·D8~D13·D16~D18·D21)은 decisions 끝에 한 줄씩.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | GE 단일 채널 | 2026-06-13 | GE 단일 채널 추상화 — 모든 수치 변경을 GE 한 경로로 |
| D2 | 정의/Spec 분리 | 2026-06-13 | SO 불변 정의 + 런타임 `Spec` 분리 (공유 GE 간섭 제거, resolve 1회 캐싱) |
| D4 | Modifier 6종·두 경로 | 2026-06-13 | Modifier 6종 + BaseValue/CurrentValue 두 경로 (Instant·Periodic=Base 영구변경 / Duration·Infinite=Current만) |
| D5 | Spec 직접 반환 | 2026-07-19 | `MakeOutgoingSpec`은 Spec(class) 직접 반환 — SpecHandle 미도입 |
| D7 | Execution=순수 클래스 | 2026-07-19 | Execution은 SO가 아닌 순수 클래스 — 공유 데이터가 없어 SO 불필요 |
| D14 | AttributeCapture 계층=UE 구조 | 2026-08-02 | 캡처 정의·Spec·Container, Spec이 컨테이너 소유, AttributeBased가 캡처 정의를 품음. Source는 생성 때·Target은 적용 때 |
| D15 | GameplayAttribute 타입 | 2026-08-03 | 어트리뷰트 참조를 직렬화 타입 `GameplayAttribute`(AQN+필드명)+전용 드로어로 추출 |
| D19 | Spec 복사/Clone | 2026-08-04 | `GameplayEffectSpec` 복사 생성자(+`Clone()`) — 적용 시점 대상별 복제 |
| D20 | spec API+clone 유지 | 2026-08-04 | spec 직접 생성 API + apply 시점 clone 유지 — SetByCaller 도입 예정이라 clone은 선배관 |
| D22 | Magnitude=UE 구조체 공유 | 2026-10-19 | `GameplayEffectModifierMagnitude`로 추출해 GE Modifier·Calculation Modifier가 같이 씀(전용 드로어 하나). 에셋 10개 직접 수정. |
| D23 | Execution=UE ExecutionDefinition, Calculation Modifier는 Defs()에서 | 2026-10-19 | `executions`(계산 클래스 AQN + Calculation Modifiers). 편집기 Backing Data = Execution `Defs()`. 런타임 적용·Transient·PassedInTags 없음. 기존 에셋은 직접 수정. |

### 결정 대기
- **Execution 직렬화 형태(구 D8, 2026-07-19 미결)** — AQN 타입 참조(지금 코드, UE `TSubclassOf`와 같음 / GE별 튜닝 값 불가) vs `[SerializeReference]` 인스턴스(GE별 튜닝 값 / 이름 변경 시 참조 깨짐, 드로어 따로). 판단 축: Execution이 GE별 튜닝 값을 갖게 될까.

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음. (구 "GE 타입 enum 주석 불일치"는 코드 확인으로 해소 — 3종 실행 경로 모두 존재. `GameplayEffectType` 주석의 "미구현"은 낡은 서술.)

## 다음 작업
1. **실전 전투 공식 확정** — Execution(클램프·크리티컬 등 분기)과 AttributeBased(단순 사칙연산) 역할 경계를 실측 근거로 decisions에 기록. `defense` 어트리뷰트를 안 쓰기로 확정했으니 데미지 계산식을 다시 정한 뒤 착수.
2. **호출처 배선** — 공격 컴포넌트가 `MakeEffectContext()`(Instigator=공격자) 기반으로 target에 GE 적용. (실행 계층은 [gameplay-ability](../gameplay-ability/progress.md)와 함께.)
3. **`SetByCaller`**(→D20(spec API+clone 유지)) — 런타임 계산값을 spec에 꽂아 적용. spec 직접생성 API+clone의 실사용 소비처. 별도 작업([TODO-BOARD](../../../TODO-BOARD.md)).
4. (확장 시) GE 스택 / Cue / Tag / ScalableFloat 커브는 데모 요구가 생기면.
   - Tag: 2026-10-17 [gameplay-tag](../gameplay-tag/progress.md) 에디터 확인 중 `GameplayEffectAssetDrawer.GetModifierElementHeight`를 고쳤다 — Modifier Op 줄을 두 번 세어(`LineGap + lineH` 뒤 다시 `spacing`) 펼친 요소마다 빈 줄 하나만큼 남던 것을 그리기 위치와 같게 맞춤(태그 접기 추가 전부터 있던 계산). 사유: gameplay-tag tests.md §3-4 "요소 높이가 맞는지", 유저 "결정 없이 고쳐야 되는 건 고치고".
   - Tag: 2026-10-16 [gameplay-tag](../gameplay-tag/progress.md) 작업으로 이 feature 코드를 고쳤다 — `GameplayModifier`에 `sourceTags`·`targetTags`(`GameplayTagRequirements`) 필드, `GameplayEffectAssetDrawer`에 표시, `Effect/GameplayTagRequirements.cs` 신설. 사유: Modifier 태그 조건이 gameplay-tag의 GE 연동 범위라서다. 판정은 아직 연결하지 않았다(gameplay-tag D13 Modifier Source/Target Tags=UE Requirements 전체·판정 미연결, D14 지속형 판정=UE 그대로).
   - Tag(판정 연결): 2026-10-16 같은 작업으로 이 feature 코드를 또 고쳤다. 사유: Modifier Source/Target 태그 조건을 UE 호출 위치대로 판정하려면 Spec·컨테이너·캡처 조회가 태그를 들고 다녀야 해서다(gameplay-tag D18 판정 태그 출처=ASC 조회 함수만·빈 값, D19 Execution 캡처 조회=UE처럼 평가 파라미터 인자, D20 Spec 캡처 태그=UE `FTagContainerAggregator` 구조, D21 지속형 태그 조건을 CurrentValue에도 적용).
     - `GameplayEffectSpec`: `CapturedSourceTags`·`CapturedTargetTags`(새 `TagContainerAggregator`), 생성 시 `RecaptureSourceActorTags`, 복제 시 깊은 복사.
     - `GameplayEffectContext(Handle)`: `GetOwnedGameplayTags`.
     - `ActiveGameplayEffectsContainer`: 적용·실행 때 Target 태그 캡처, Instant/Periodic에서 조건 못 맞춘 modifier 건너뛰기, aggregator에 조건 전달, CurrentValue는 `EvaluateCurrentValue`.
     - 캡처 조회(`GameplayEffectAttributeCaptureSpec(Container).TryGetCapturedValue`)에 새 `AggregatorEvaluateParameters` 인자. `AttributeBasedMagnitude`가 Spec 태그로 넘긴다.
     - `GameplayEffectExecutionParameters.AttemptCalculateCapturedAttributeMagnitude`에 평가 파라미터 인자. 호출부 `Character/SpeedBoostExecution`, 에디터 `AbilitySystemInspectorGUI` 수정.
     - 2026-10-17(소유 태그): `AbilitySystemComponent`의 빈 소유 태그 컨테이너를 `GameplayTagCountContainer`로 바꾸고 Loose 태그·조회·태그 이벤트를 더했다. `ActiveGameplayEffectsContainer`는 persistent GE 적용 때 modifier Target 조건 태그를 구독하고 제거 때 풀며, 태그가 바뀌면 그 GE의 Target 조건 어트리뷰트 aggregator를 dirty로 만든다. 사유: 소유 태그가 바뀌면 CurrentValue를 다시 계산하기로 해서다(gameplay-tag D22 소유 태그=UE `FGameplayTagCountContainer` 구조+Loose·이벤트, D23 태그 변화 재계산=UE식 태그별 구독).
     - 2026-10-17 수정(유저 "단순 오류나 버그는 하고"): 주기 실행마다 Target 태그를 읽은 뒤 `GameplayEffectSpec.CalculateModifierMagnitudes()`를 부른다(UE `ExecuteActiveEffectsFrom` `GameplayEffect.cpp:3081-3084`, 함수 private → internal). non-snapshot 캡처가 주기 GE에서도 지금 값을 따른다. Aggregator 셀프체크 H. `GameplayEffectType` 주석의 Instant·Duration "(미구현)"도 지웠다(둘 다 동작).
     - 2026-10-19 수정(유저 "modifier self check에 warning은 괜찮나", 메모리 "단순 버그는 묻지 말고"): `GameplayEffectSpec.Initialize`에서 `CalculateModifierMagnitudes` 호출을 없앴다 — UE `FGameplayEffectSpec::Initialize`(`GameplayEffect.cpp:1609-1660`)는 생성 때 계산하지 않고, Target 캡처 AttributeBased가 생성 때마다 `[GESpec] … Calculation Failed` 경고를 냈다. 적용 때 Target 캡처 직후 계산은 그대로. 셀프체크 4종 통과(Modifier Tag 65/0 경고 1줄, Aggregator 55/0, GE Component 27/0, PlayMode 14/0).
     - 2026-10-19(gameplay-tag 세션, 유저 요청): GE 인스펙터 Modifier Magnitude 섹션·AttributeBased 하위 묶음 접기(`GameplayEffectAssetDrawer`·`AttributeBasedMagnitudeDrawer`), `MagnitudeCalculationType`에 `CustomCalculationClass`·`SetByCaller` 추가(UE 순서, 고르면 "구현 예정" 안내, 런타임은 계산 실패 → 0 + 경고). 상세 → gameplay-tag worklog 2026-10-19.
     - 2026-10-18(gameplay-tag D28·D29): `ActiveGameplayEffectsContainer`의 CurrentValue를 UE처럼 `Evaluate(default)`로 되돌리고 modifier Target 태그 구독(D23)을 지웠다. `GameplayEffectAsset`에 `[SerializeReference] gameplayEffectComponents`(새 `GameplayEffectComponent` 베이스, 첫 Component `TargetTagRequirementsGameplayEffectComponent` — 필드만)를 더하고 `OnGameplayEffectChanged`가 각 Component에 넘긴다. 사유: 유저 "UE에 따라 … Ongoing Tag Requirements 담당으로 넘기고", "UE 처럼 GE Component 도입", "빈 거로 넣어둬". 인스펙터(`GameplayEffectAssetDrawer`)엔 아직 Component 목록이 없다.
     - 2026-10-18(gameplay-tag 현재 목표 1, 유저 "1번 진행해"): `ActiveGameplayEffectsContainer.ApplyGameplayEffectSpec`에서 Modifier·Execution이 둘 다 없는 GE를 거부하던 블록을 지웠다. UE엔 이 거부가 없고, 태그만 주는 GE(Granted Tags만 설정)를 만들려면 필요하다. 셀프체크는 gameplay-tag `ModifierTagSelfCheck` D-16.
     - (이전 기록) 남긴 차이: UE는 주기 실행마다 magnitude를 다시 계산(`CalculateModifierMagnitudes`)하지만 우리는 하지 않는다(기존 동작, `ExecuteGameplayEffect` TODO). 2026-10-16 유저 요청으로 [TODO-BOARD](../../../TODO-BOARD.md) (code) 항목에 UE 흐름·차이 예·수정안·검증안을 적었다.
