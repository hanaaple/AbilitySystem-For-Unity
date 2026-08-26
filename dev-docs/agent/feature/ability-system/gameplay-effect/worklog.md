# gameplay-effect — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-08-07 — GameplayEffectAsset 인스펙터: Executions에 검색형 Add 추가
- `executionTypeNames`(`List<string>` + `[SubclassSelector(typeof(GameplayEffectExecution))]`)가 Unity 기본 리스트로 그려져 요소 드로어 때문에 "+"가 안 뜨고 Size 필드로만 늘려야 했다(유저 리포트 "Add가 없네"). AttributeSet 리스트와 같은 검색형 `TypeChoiceList`로 교체 — `GameplayEffectAssetDrawer`에 `_executionList` 신설, 후보=GameplayEffectExecution 구체 서브클래스−이미 담긴 것, add 시 요소 stringValue=AQN, 요소는 `[SubclassSelector]` 전용 드로어로 그림.
- **New Script는 제외**(AttributeSet 리스트엔 있지만): `GameplayEffectExecution.Execute`가 abstract라 빈 서브클래스 템플릿(`: GameplayEffectExecution {}`)이 컴파일되지 않는다. 붙이려면 New Script 템플릿에 abstract override 스텁 생성이 선행돼야 함(현재 미구현). 유저에게 사유 보고.
- 부수: 기존 executions PropertyField의 툴팁 설명은 리스트 헤더로 바뀌며 빠짐(경미).
- 검증: 코드 정합성만(에이전트 범위). 컴파일/동작은 유저가 에디터에서.

### 2026-08-05 — GE 캡처/Execution 검증 디버그 도구 세트 + UE Execution 실행조건 원문 확정 + coefficient 버그 수정
- 성격: **5b(2026-08-04)로 "관측 변화 0→유의미" 지점에 왔으나 눈으로 볼 수단이 없던 것**을 이번 세션에 마련. 새 D 없음(유저 지시 이행 + 버그 수정 + 사실 확인).
- **캡처/Execution 인스펙터 표시:** 그리기 로직을 `AbilitySystemInspectorGUI`(static, 신설)로 **공용화**하고 `AbilitySystemComponentDrawer`는 얇은 래퍼로 축소(드로어+창 2소비처 공유 — session-protocol '작업 중' 공통화). Active Effect마다 **Modifiers/Executions/Captured Attributes**. 캡처 = Source/Target 구분 + Base/Current + snapshot 태그 + **출처 역추적**(«by Modifier[i]/ExecutionName» — `SetupAttributeCaptureDefinitions`의 두 공급원과 값-비교). Execution 줄 **더블클릭→IDE**(`MonoScript.GetClass` 일치 탐색). 컨테이너 private 리스트는 기존 리플렉션 패턴으로 읽어 프로덕션 API 안 건드림.
- **ASC Inspector 창(`AbilitySystemComponentWindow`, EditorWindow):** 선택된 GameObject의 ASC를 위 GUI로 그림(없으면 null→안내). 진입점 2개(메뉴 `Window ▸ Ability System ▸ ASC Inspector` + ASC 인스펙터 "Open in ASC Window" 버튼)가 `Open()` 공유. `selectionChanged`+`OnInspectorUpdate`로 실시간 갱신. 계기: 인스펙터가 다른 대상으로 바뀌어도 ASC 상태를 따로 띄워두고 적용 전후 캡처값을 관찰하려는 것(유저 지적 "실시간 디버깅 마땅치 않다" → 관측이 아니라 **상황 세팅** 문제로 좁혀짐 → BoxRoom).
- **coefficient 기본값 1 버그 수정:** `GameplayEffectAssetDrawer.OnAddModifier` — `arraySize++`로 modifier 추가 시 Unity가 C# 필드 이니셜라이저(`AttributeBasedMagnitude.coefficient=1f`)를 **안 돌려 0**이 되던 것(class 전환과 무관 — 직렬화 경로는 `new`가 아님). add 콜백에서 `attributeBased.coefficient=1` 명시 세팅으로 되돌림(struct 시절엔 이렇게 했던 걸 class 전환하며 "이제 필요없다"고 뺀 게 원인). 잘못된 주석 정정. ⚠ 기존 에셋(coefficient 0 저장분)은 수동 교정 필요.
- **BoxRoom 디버그 도구:** `Character/BoxRoom.cs`(+meta) + `Prefabs/BoxRoom.prefab`(+meta). 방 트리거에 들어온 액터 ASC에 **Room ASC=source**로 GE 적용(`ApplyGameplayEffectToTarget`, context=default라 Room이 instigator=Source 캡처 대상), 나가면 `RemoveActiveGameplayEffect`. **3모드**(Instant 1회성 / Buff=Duration·Infinite&&period0 / TickExecution=Duration·Infinite&&period>0) — Instant는 Invalid 핸들이라 해제 대상서 자동 제외. 로직 완성·GE 슬롯 빈 상태. 프리팹은 기존 pickup/Player 프리팹에서 실제 GUID(ASC `e98a3777…`)·구조 조회해 조립, BoxRoom 스크립트는 신규라 `.cs.meta` GUID 발급(프리팹 참조와 일치). **미검증(유저 에디터): 프리팹 GUID 조율·GE 슬롯·Room ASC 데이터·컴파일.**
- **UE Execution 실행조건 원문 확정(유저 교정 "기억으로 확인할 문제 아냐"):** ylyking UE 미러 **소스 직접 확인**(그간 HARNESS '해석 규율'에 "포럼 1건으로 단언해 틀림"으로 박혀 있던 지점). 결론: **period 0 Duration/Infinite에선 Execution 안 돎.** 근거 = `UAbilitySystemComponent::ExecuteGameplayEffect`(asc.cpp:820)의 `check(Duration==INSTANT_APPLICATION || Period!=NO_PERIOD)` assert(UE가 코드로 강제) + 주석 "Effects with no period and that aren't instant application should never be executed". Execute 경로 = Instant(asc.cpp:734) / periodic(ge.cpp:2887, `period!=NO_PERIOD`일 때만 타이머). Execution Calculation은 `ExecuteActiveEffectsFrom`(ge.cpp:1882-1894 `for Def->Executions { Execute }`)에서만. period0 지속형 계산은 UE도 MMC(=AttributeBased)로 apply 시 `CalculateModifierMagnitudes`. **architecture/decisions 반영은 유저 판단 대기(TODO-BOARD).** WebFetch 요약이 이 분기 중첩을 오독해서, 원문을 직접 받아 라인 확인해야 했음(HARNESS '해석 규율' — 2차 자료/요약도 단정 금지).
- **회고('세션 회고'):** ① UE 동작을 **처음에 기억으로 설명**(유저 교정) — HARNESS '판단은 틀릴 수 있다'·'왜의 근거는 유저 의도 → UE 원본'가 있는데 원문 확인을 먼저 안 감(2026-07-19·08-04에도 반복된 유형). ② 에셋 폴더 구조 질문에 **결론 없이 트레이드오프만 나열**해 유저가 "결국 어떻게 하라는거야"(대화 규율 'Grice 협력 원리' — 방식·결론 숨김 위반). ③ `IsValid()` 메서드 호출 실수(프로퍼티인데 괄호)—자체 발견·수정. ④ 디버그 뷰 보강을 관측 축 옵션으로 물었다가 유저 거부—진짜 문제는 "상황 세팅"이라 의도부터 좁혔어야.

### 2026-08-04 — AttributeBased 런타임 evaluate(5b) + `GameplayModifierSpec` UE 정렬 (→D21) — 첫 관측치 배선
- 유저 지시: "AttributeBasedMagnitude 마저 구현." NOW.md 재개 지점 = 5b evaluate. 진행 중 유저가 `CalculateModifierMagnitudes`의 이름-내용 불일치를 지적 → UE 원문 대조 후 `GameplayModifierSpec`을 재설계(→D21, "UE대로 가자" 확정).
- **선(先) 블로커 해소 확인:** NOW.md가 경고한 `GameplayEffectSpec.cs`의 `operator =` 스텁은 **이미 제거돼 있었음**(복사 생성자+`Clone()` 정상). 실물 확인 후 진행(HARNESS 핵심 원칙 '추측 금지' — 문서=과거 스냅샷).
- **계산 2층 신설(UE 동형):**
  - `AttributeBasedMagnitude.Evaluate(spec)` — `TryGetCapturedValue(backingAttribute, captureValueType, out v)`로 캡처값 읽어 `(v+Pre)*Coef+Post`. 조회 실패 시 0+경고(조용한 false 방지). (UE `FAttributeBasedFloat::CalculateMagnitude`)
  - `GameplayModifier.GetMagnitude(level)` → **`EvaluateMagnitude(spec, level)`** — AttributeBased면 위 Evaluate, 아니면 고정 `magnitude`(ScalableFloat 불변). (UE `FGameplayEffectModifierMagnitude::AttemptCalculateMagnitude` 분기)
- **`GameplayModifierSpec` 재설계(→D21):** 유저 지적("이 함수는 magnitude를 계산하지도 않잖아" / "attribute가 modifier의 key면 안 된다")에서 출발. UE 헤더·cpp 확인(ylyking 미러) 결과 UE `FModifierSpec`은 `EvaluatedMagnitude`만 보유, identity는 정의에 있고 `Modifiers`는 정의와 **평행 인덱스 배열(무필터)**. 이에 맞춰:
  - `GameplayModifierSpec`: readonly 번들 struct → **mutable struct**(magnitude 제자리 갱신용 `CalculateMagnitude` 메서드). identity(핸들·연산)는 슬롯에 **캐시** — 우리 `GameplayAttribute`가 문자열이라 정의에서 매번 읽으면 리플렉션(UE는 포인터라 쌈), 이게 UE 최소형과의 의도적 divergence(→D21).
  - `GameplayEffectSpec`: `Modifiers`를 `GameplayModifierSpec[]`(정의와 평행)로. `BuildModifierSpecs`(생성 시 1회, identity 해석, **무효도 안 빼고 경고만** — 빼면 인덱스 밀림) + `CalculateModifierMagnitudes`(캡처 후 magnitude만 **제자리** 갱신, 배열이라 `arr[i].CalculateMagnitude()`가 제자리로 먹음). `Initialize` 순서: 캡처 선행→평가 후행(기존 역전이 AttributeBased가 캡처값 못 읽던 버그). `CaptureAttributeDataFromTarget`가 target 캡처 직후 재평가.
- **ASC 소비처 변경 0** — 슬롯이 `Handle`·`Operation`·`EvaluatedMagnitude`를 그대로 노출해 3개 소비처(ExecuteGameplayEffect·CalculateAttributeCurrentValue·RecalculateAffectedAttributes)·에디터 드로어 전부 무수정.
- **UE 원문 대조 정확도:** 실패 처리(0+Warning)는 UE `CalculateModifierMagnitudes`와 동일. 계산식·2층 분기 구조도 일치. 유일 divergence = identity를 슬롯에 캐시(문자열 어트리뷰트라).
- **⚠ 미확정(유저 확인 대기):** Target 기반 AttributeBased는 생성 시점(target 미캡처)에 캡처 실패 경고 가능(그 결과는 소비 안 됨). 현재 검증 대상 `GE_EquipSpeedDown`은 Source 기반이라 무영향. warn-always로 단순하게 둠(억제는 UE `CanCalculateMagnitude` 게이트 필요 — TODO-BOARD 등록).
- **관측 변화: 여기서 처음 0→유의미.** evaluate가 캡처값을 실제 magnitude로 소비한다. **유저 에디터 검증 대기** — `GE_EquipSpeedDown`이 Source `CombatAttributeSet.damage` 기반 speed 감소하는지. ⚠ 선행: `Player_AttributeInitData`에 `CombatAttributeSet` 존재(없으면 캡처 무효→경고+0).

### 2026-08-04 — spec 복사(Clone) + Target 캡처 적용 경계 배선 (→D18/D19/D20)
- **ActiveGameplayEffect.Owner 추가(→D18):** target ASC를 effect가 직접 보유. `ActiveGameplayEffectsContainer`는 당장 잠정 유보(배제 아님) — UE는 컨테이너.Owner + handle 전역맵으로 owner를 아는데(원문 확인: FActiveGameplayEffect엔 Owner 멤버 없음, `CheckOngoingTagRequirements`가 컨테이너를 인자로 받음), 우리는 컨테이너 없이 effect가 직접 든다.
- **GameplayEffectSpec 복사 생성자+Clone()(→D19):** 생성자를 멤버세팅+`Initialize()`로 분리한 위에, `Initialize` 재실행 없이 상태를 옮기는 복사 경로 신설. 복사 심도 = Definition·Executions 공유 / Modifiers 리스트 복사 / `CapturedRelevantAttributes` 깊은 복사(컨테이너 `Clone()` 신설). UE `FGameplayEffectSpec` copy 생성자 대응(값 struct라 UE는 자동, 우리는 class라 명시적).
- **적용 경계 통일 배선:** `ApplyGameplayEffectSpecToSelf` 맨 위에서 `spec.Clone()` + `CaptureAttributeDataFromTarget(this)` 한 번 → `appliedSpec`을 Instant·Duration·Execute·Recalc 전부 관통. **UE 원문 검증(유저가 "Active에만 캡처 아니냐"로 교정 요구):** UE는 Duration은 `ApplyGameplayEffectSpec`(ge.cpp:2799)·Instant는 `StackSpec`(asc.cpp:677)으로 **양쪽 다 복사본에 target 캡처** — "Active만"이 아니었음. 우리 구조에선 이중 복사가 없어 경계 한 곳으로 통일.
- **설계 논의(길었음, 핵심 판단들):** ① spec을 struct로? → **아니오**(C# struct 복사는 얕은 복사라 UE 값 의미 못 얻고, 내부 참조 멤버 공유로 컨테이너 격리 실패 + mutable struct 함정). class+Clone()이 UE 값struct+copy-ctor의 정확한 대응. ② clone이 "지금 의미없잖아"(유저) → **맞음**, 캡처 소비처(evaluate)·apply-time magnitude 둘 다 없어 관측 변화 0인 선배관. ③ 그럼 asset-only로 좁혀 clone 제거? → **아니오**, SetByCaller 도입 확정이라 spec 직접생성+멀티적용이 실용도가 됨 → clone 유지(→D20).
- **회고:** UE 동작을 두 번 느슨하게 말했다 — "Instant는 copy==ref로 봐도 된다(clone 선택)"이라 했으나 UE는 Instant도 StackSpec 복사; 유저가 "UE 제대로 찾아보라"고 해서 원문 확인 후 정정. 코드만 만지지 말고 UE 원문을 **먼저** 확인했어야(HARNESS '판단은 틀릴 수 있다').
- **HARNESS 정비(코드 무관):** 그룹 HARNESS 'GAS 모방 의도'(유저 확정 문장) 신설. 일반 HARNESS 핵심 원칙(작업규약·소통방식은 개인 메모리 아닌 하네스에 기록)·대화 규율의 Gloss 규칙(`D#` 등 내부코드 인용 시 한 줄 설명)·대화 규율 '자기 점검'의 체크5·'세션 회고' 문구 수정. 메모리 `gloss-internal-doc-codes.md`는 삭제(하네스로 이동, 유저 지시).

### 2026-08-04 — 캡처 문서 정합 + 설계 감사 (코드 변경 없음)
- 유저 요청: "지금까지 한 것(GameplayAttribute·Editor·Capture) 문서화" → 대조 결과 decisions(D12~D17)·worklog는 이미 완결, **상태 포인터만 뒤처져 있어** NOW.md·progress `다음 작업`에 D16(컨테이너 UE 정렬)·D17(Execution `Defs()` 등록)·params 조회를 반영. 코드는 서술과 일치함을 파일별로 확인(GameplayAttribute·Execution.Defs·Spec Setup·SpeedBoost·params).
- **설계 감사(유저가 "코드 있냐/없냐 말고 의도·사양이 맞냐를 봐라"로 방향 교정):** 캡처 계층은 **모양은 사양에 맞음**(Source@생성=시전 / Target@적용=명중의 2단계는 값의 시간 의미로 정당화 — UE 베낌 아님, snapshot 정책·값-key 조회·읽기 일원화도). **그러나 "사양 만족"은 판정 불가** — 소비처가 0(`GetMagnitude` 고정값·Execute +10 고정)이라 지은 Source/snapshot 경로조차 검증할 수단이 없다("Capture Test 못 해봄"의 근본 원인). 미충족: ②Target 캡처(+spec 복사)·④소비. 잠재 위반: snapshot=false 소스 파괴 NRE·captureValueType 미라우팅·params 조용한 false. 코드로 못 정하는 의도 1건(spec 생성=시전인가) 표면화 → 전부 TODO-BOARD(2026-08-04)로.
- **회고:** 초반에 "호출이 없다→안 된다"로 **코드 현재 상태를 결론처럼** 말했다(유저 2회 교정). [[justify-from-ue-or-user-intent]] 취지(코드 동작은 근거가 아니다)를 "설계 왜"만이 아니라 **"사양 맞냐" 감사에도** 적용해야 했다.

### 2026-08-03 — Execution이 params로 캡처값 조회 (UE 고증 우선)
- 유저 지시: "`GameplayEffectExecutionParameters`에서 캡처값 가져오게 구현, 현재 코드에 맞추되 UE 고증 우선."
- `GameplayEffectExecutionParameters`에 **`AttemptCalculateCapturedAttributeMagnitude`(Current)·`AttemptCalculateCapturedAttributeBaseValue`(Base)** 추가 — UE `FGameplayEffectCustomExecutionParameters`의 동명 메서드 그대로. 내부는 `Spec.CapturedRelevantAttributes.TryGetCapturedValue(def, valueType, out)`에 위임(params가 `Spec`을 들고 있음).
- **UE 대비 유일한 생략:** `FAggregatorEvaluateParameters`(태그 기반 aggregator 평가) — 이 프로젝트엔 aggregator 계층이 없고 컨테이너가 이미 계산된 값을 보관하므로 인자에서 뺐다(D13 기록된 divergence와 동일 계열).
- Calculation Modifiers TODO 주석의 미래 API 스케치를 실제 메서드명으로 갱신.
- **⚠ 지금 동작 범위:** Source 캡처는 spec 생성 시 캡처돼 **조회됨**. Target 캡처는 `CaptureAttributeDataFromTarget`가 적용 경로에 아직 미배선(spec 복사 선행)이라 Target-source 정의 조회는 false — 즉 SpeedBoost의 `B`(Speed/Source)는 읽히고 `A`(Health/Target)는 아직 안 읽힘. **관측 변화 0**(SpeedBoost.Execute는 아직 캡처값을 안 씀, 고정 +10).

### 2026-08-03 — Execution 캡처 선언 API + Spec 등록 배선 (→D17)
- 베이스 `GameplayEffectExecution.Defs()`를 `abstract`→`virtual`(기본 빈 span)로. 하위(SpeedBoostExecution)는 이름 붙은 static 정의 `A`/`B`(=Execute 조회 key) + `defs = {A,B}`를 `ReadOnlySpan`으로 반환.
- `GameplayEffectSpec.SetupAttributeCaptureDefinitions` ② 배선: `Executions` 순회 → 각 `Defs()` → `AddCaptureDefinition`. modifier 등록(①)과 동형. Executions는 이미 이 호출 전에 resolve됨.
- **설계 논의 경위(길었음):** 선언식 vs 자가 등록 / static 배열 vs List / span vs IReadOnlyList를 두고 왕복. 결론은 선언식+static 배열+span(→D17 근거). **중간 실수:** 유저가 "정의를 Execute의 조회 key로 쓴다"고 이미 말했는데, 에이전트가 `A`/`B`를 익명 인라인 배열로 합쳐 **key 참조를 없앰** → 되돌림. 유저 요청으로 "복사여도 값-key라 무해, 가독성 위해 이름 필드 유지" 주석을 SpeedBoostExecution에 남김.
- **관측 변화 0** — 캡처 정의가 컨테이너에 등록되고 Source는 생성 시 캡처되나, `Execute`가 아직 캡처값을 읽지 않는다. 다음: Execute가 `params`로 캡처값을 조회(+AttributeBased evaluate 5b).

### 2026-08-03 — `GameplayAttribute(AttributeHandle)` 생성자 (핸들→GameplayAttribute)
- 배경: `CharacterAttributeSet.Speed`(정적 `AttributeHandle`)로 캡처 정의를 코드에서 만들 편의가 필요(**용도: Execution에서 캡처 등록**). 처음엔 `GameplayEffectAttributeCaptureDefinition.From(handle, source, snapshot)` 팩토리로 넣었으나, 유저가 이름을 싫어했고 **"차라리 `GameplayAttribute`에 `AttributeHandle` 1개짜리 생성자를 추가하라"**로 방향 정정 → From 제거.
- 근거(유저 의도, HARNESS '왜의 근거는 유저 의도 → UE 원본'): 핸들→`GameplayAttribute` 변환은 캡처 전용이 아니라 **어디서든 쓰는 재사용 프리미티브**(modifier 저작 등)라 `GameplayAttribute`가 소유하는 게 맞다. `From`은 캡처에 국한된 데다 `FromSource`/`FromTarget` 방향 이름과 'from'이 충돌.
- 구현: `GameplayAttribute(AttributeHandle handle) : this(handle.SetType?.AssemblyQualifiedName, handle.Name)`. 캡처 정의는 기존 생성자 `(source, new GameplayAttribute(handle), snapshot)`로 조립.
- **여전히 유저 WIP(SpeedBoostExecution.cs):** `CaptureDef` 별칭·`FromSource`·`GameplayEffectExecution.Defs()`·Spec의 Execution 캡처 등록 배선·`AddOutputModifier(AttributeHandle,...)` 오버로드는 미구현(착수 지시 대기). 그 파일의 `.From(...)` 호출은 제거됨에 따라 유저가 생성자 방식으로 바꿔야 함.

### 2026-08-03 — AttributeBased 기본값(class default)·GameplayAttribute indent
- **coefficient 기본값 = class default:** 유저 지적("coefficient 0이면 잘못될 수도"). 식 `(value+Pre)*coef+Post`에서 coef=0이면 캡처값이 통째로 0. 처음엔 에디터 콜백(onAdd·Calc Type 전환 시 세팅)으로 짰으나 유저가 **"기본값은 class 기본값 말한 것"**이라 정정 → **`AttributeBasedMagnitude`를 struct→class로 바꾸고 `coefficient = 1f` 필드 초기화.** Unity는 struct 초기화값을 무시하지만 class는 새 인스턴스에 반영하므로 이게 정공법(UE도 FAttributeBasedFloat coefficient 기본 1). 에디터 콜백 2곳·미사용 상수 제거. 기존 에셋은 직렬화값 유지, 신규/누락 필드만 1. D# 없음(자명). **⚠ 부작용:** GameplayModifier(struct)가 reference 필드를 품게 돼 struct 복사 시 attributeBased 인스턴스가 공유된다 — 현재 modifier 복사·변형 경로가 없어 무해하나 5b/spec 복사 도입 시 유의.
- **GameplayAttribute 드로어 indent:** 유저 지적("ui 직관성 별로"). `GameplayAttributeDrawer.OnGUI`에서 Set/Attribute 두 행을 `EditorGUI.indentLevel++`/`--`로 한 단 들여써 부모 필드 아래 묶음으로 보이게(높이 불변, 수평 시프트만). prev 저장 대신 ++/-- 대칭(유저 지적).

### 2026-08-03 — 캡처 컨테이너 `_definitions` 제거, UE 원형에 맞춤 (→D16)
- 대화 흐름: "컨테이너가 definitions를 spec과 따로 둔 이유"에 처음엔 코드 동작(`CaptureAttributes`가 Clear·재캡처)으로 답 → 유저: "코드 근거는 진짜 의미 없다. 가장 좋은 건 UE 원본, 그다음이 내 의도." → HARNESS HARNESS '왜의 근거는 유저 의도 → UE 원본' 신설(근거 순서) + 이 피드백 개인 메모리화.
- **UE 원문 확인**(`GameplayEffect.h` ylyking 미러 + Epic): `FGameplayEffectAttributeCaptureSpecContainer`는 `SourceAttributes`/`TargetAttributes`(spec 배열)+`bHasNonSnapshottedAttributes`뿐, **definitions 별도 리스트 없음.** `AddCaptureDefinition`이 미캡처 spec을 배열에 넣고 `CaptureAttributes`가 제자리 채움. → 우리 별도 리스트는 divergence였음이 확인됨.
- 유저 지시 "definition 없애" → **UE 원형으로 정리:** `GameplayEffectAttributeCaptureSpec`에 **선언 전용 생성자**(정의만, `IsValid=false`) 추가 → 값 스냅샷 struct로도 "미캡처 상태" 표현 가능. 컨테이너는 `_definitions` 삭제, `AddCaptureDefinition`이 미캡처 spec을 소스별 배열에 삽입(값 동등성 중복 제외), `CaptureAttributes`가 `specs[i] = new (def, asc)`로 제자리 교체(`List<struct>`라 교체가 곧 in-place). `readonly struct` 유지(class 안 감 — D13/D11 기조).
- **관측 변화 0** — 캡처 결과 동일, 순수 내부 표현 정리. 공개 API(`AddCaptureDefinition`/`CaptureAttributes`/`TryGetCapturedValue`) 시그니처 불변이라 `GameplayEffectSpec` 호출부 영향 0(grep 확인).

### 2026-08-03 — 어트리뷰트 참조를 `GameplayAttribute` 단일 타입으로 추출 (→D15)
- 대화 흐름: 유저가 "매번 TypeName+Field를 손으로 박고 드로어에 연결하는 반복, 자동화 방법 없나 — UE `FGameplayAttribute`처럼 따로 만들어봐"라고 지시. 설명 단계에서 "문자열 저장 자체는 못 없앤다(FieldInfo 직렬화 불가, UE도 이름 저장), 없앨 수 있는 건 팝업 배선 반복"을 먼저 짚고, 적용 범위(데이터 모델 변경=에셋 마이그레이션 수반)를 AskUserQuestion으로 확인 → **둘 다 적용**, 이름은 유저가 **`GameplayAttribute`**로 지정.
- **신설:** `Attribute/GameplayAttribute.cs`(직렬화 struct — AQN 문자열+필드명, `ToAttributeHandle()`·`IsSet`·equality) + `Attribute/Editor/GameplayAttributeDrawer.cs`(`[CustomPropertyDrawer]`, Set/Attribute 2행 팝업). 드로어가 타입의 모든 직렬화 필드에 자동 적용되는 것이 "자동화"의 실체.
- **compose:** `GameplayModifier`(두 문자열→`attribute`)·`GameplayEffectAttributeCaptureDefinition`(두 문자열→`attribute`, `captureSource`/`snapshot`은 유지). 각 `ToAttributeHandle()`은 `attribute.ToAttributeHandle()`에 위임.
- **드로어 위임:** `GameplayEffectAssetDrawer`·`AttributeBasedMagnitudeDrawer`가 수기 Set/Attribute 팝업을 `attribute` PropertyField로 교체 + 높이 계산 재작성. 팝업 로직을 갖던 공용 헬퍼 `AttributeReferenceGUI`는 드로어로 흡수·**삭제**(사용처 0 grep 확인 후).
- **에셋 마이그레이션:** `GE_EquipSpeedDown`(modifier 2개, 하나는 backingAttribute 채움)·`GE_MeleeDamage`(1개)를 중첩 `attribute:` 레이아웃으로 손수 이관 — 기존 YAML 값을 읽어 결정적 이동(추정 없음). 생성자 호출부·옛 private 필드 외부 참조 0(grep 확인)이라 코드 파급 없음. `AttributeSetDefinition` 계열의 동명 문자열 필드는 별개 타입이라 미변경.
- **관측 변화 0** — 순수 구조 정리라 런타임 동작·5b evaluate와 무관. **⚠ 유저 검증 필요:** Unity 컴파일(신규 파일 인덱싱 후 IDE의 `GameplayAttribute` 미해결 경고 해소) + 두 GE 에셋의 Inspector 값이 이관 후 그대로인지.

### 2026-08-02 — Spec이 캡처 컨테이너 소유 + AttributeBasedMagnitude compose (계획 파일 ~5/9, →D14)
- 유저 지시: "`GameplayEffectAttributeCaptureSpecContainer`를 GE Spec이 소유하게 하고 값을 넣는 것까지 — UE 최대한 따라가고, 못 따라가는 부분은 말하라."
- **먼저 막힌 지점을 표면화:** 컨테이너에 넣을 캡처 **정의 공급원이 0개**였다 — `AttributeBasedMagnitude`가 인라인 캡처 필드만 갖고 정의로 compose 안 됨(snapshot 필드도 없음) + `GameplayEffectExecution`에 캡처 선언 API 없음. 둘 중 무엇을 배선할지 유저에게 질문(AskUserQuestion) → **① AttributeBasedMagnitude compose** 확정(옵션 설명에 "GE_EquipSpeedDown 재배선" 명시 = 데이터 모델 변경 승인).
- **`GameplayEffectSpec` 배선(UE `FGameplayEffectSpec` 흐름):** `CapturedRelevantAttributes` 소유 → 생성자에서 `SetupAttributeCaptureDefinitions()`(AttributeBased modifier의 `BackingAttribute` 등록) → `CaptureDataFromSource()`(생성 시 Source=`Context.GetInstigator()` 캡처). `CaptureAttributeDataFromTarget(target)`는 **메서드만 제공하고 호출 안 함** — `ApplyGameplayEffectSpecToTarget`이 같은 spec을 여러 대상에 넘겨서(spec 복사 미도입) target 캡처가 서로 덮어씀, spec 복사(계획 8/9) 선행.
- **compose:** `AttributeBasedMagnitude`의 인라인 캡처 3필드를 중첩 `GameplayEffectAttributeCaptureDefinition backingAttribute`로 교체 + `snapshot` 획득(UE `FAttributeBasedFloat::BackingAttribute`와 일치). `captureValueType`(Base/Current 소비 방식)는 정의 밖에 유지(D12). `GameplayModifier`에 `MagnitudeCalculationType`·`AttributeBased` 접근자 추가. 드로어 property path 중첩화 + Snapshot 행(7→8행). `GE_EquipSpeedDown.asset` 두 modifier의 `attributeBased` 블록을 중첩 레이아웃으로 손수 이관(기존 값 그대로+`snapshot: 0`).
- **관측 변화 0(중요):** 캡처값을 magnitude로 읽는 evaluate(5b `AttemptCalculateMagnitude`)가 아직 없어 modifier magnitude는 여전히 고정값. 캡처는 컨테이너에 들어가되 읽는 곳이 없다 — 다음 단계에서 evaluate와 묶어 관측치 확보.
- **런타임 데이터 주의:** GE_EquipSpeedDown의 AttributeBased가 Source의 `CombatAttributeSet.damage`를 캡처하는데, `Player_AttributeInitData`에 `CombatAttributeSet`가 빠져 있을 수 있음(NOW.md 기록) — evaluate 배선 시 유저가 에디터에서 확인 필요.

### 2026-08-01 — 캡처 결과 계층 신설 + 정의 rename (계획 파일 2·3/9)
- 유저 지시 2건: ①정의 `AttributeCaptureDefinition` → `GameplayEffectAttributeCaptureDefinition` rename(UE 원형명 일치, `.cs`+`.meta` 함께·GUID 유지, 사용처 0이라 코드 파급 없음). ②`GameplayEffectAttributeCaptureSpec`·`GameplayEffectAttributeCaptureSpecContainer` 신설.
- **`GameplayEffectAttributeCaptureSpec`** (readonly struct) — 정의 1건의 캡처 결과. `snapshot=true`면 캡처 시점 `AttributeData`(Base·Current) 고정, `false`면 대상 ASC 참조만 보관해 `TryGetCapturedValue(valueType)` 조회 시 라이브 재조회. Base/Current는 조회 시 `AttributeCaptureValueType`로 지정(정의는 값 종류 미고정 — D12와 정합).
- **`GameplayEffectAttributeCaptureSpecContainer`** (class) — `AddCaptureDefinition`(값 동등성 중복 제외)로 정의 등록 → `CaptureAttributes(ascToCapture, captureSource)`를 **캡처 소스별로 호출**해 해당 배열 채움 → `TryGetCapturedValue(def, valueType)`가 정의의 `CaptureSource`로 배열을 골라 조회. UE `FGameplayEffectAttributeCaptureSpecContainer` 대응, 어그리게이터 계층은 없어 ASC 직접 조회로 축소.
- **UE 원문 대조(2026-08-01, 유저 요청)** — ylyking UE 미러 헤더 + Epic 문서로 3계층 확인. 결과 컨테이너 저장을 **source/target 배열 분리**(`SourceAttributes`/`TargetAttributes`)로, `CaptureAttributes`를 UE와 같은 **per-source 시그니처**로 교체(초기 단일 리스트+`(src,tgt)` 한 번 호출에서). 확인된 의도적 차이 3건은 D13에 기록(값 캡처 vs aggregator / AQN vs FGameplayAttribute / pull vs 콜백 push).
- snapshot 완전 구현은 **유저 확정**(AskUserQuestion) — 정의에 snapshot 필드가 이미 있어 두 경로를 다 처리해야 의미가 산다 (→D13). 트레이드오프: non-snapshot용 ASC 참조를 캡처 기간 보유(UE와 동일 비용).
- **아직 사용처 0(dead code).** 다음: `GameplayEffectSpec`이 컨테이너 소유 + AttributeBased evaluate(5b) 배선.

### 2026-07-28 — AttributeCapture 착수: `AttributeCaptureDefinition` 신설 (계획 파일 1/9)
- `Assets/Scripts/Core/AbilitySystem/Effect/AttributeCaptureDefinition.cs` 생성 (→D12). UE `FGameplayEffectAttributeCaptureDefinition` 대응 — "무엇을·어디서 캡처할지"만 담는 불변 정의(값은 캡처 컨테이너가 별도 보관 예정).
- 착수 전 유저에게 2개 확인: ①기존 `AttributeBasedMagnitude`와의 관계 → **독립 struct만 추가(additive)**, 기존 인라인 캡처 필드·`GE_EquipSpeedDown` 에셋은 안 건드림. ②필드 구성 → **UE 원형**(captureSource + attribute AQN + snapshot), `captureValueType`은 정의에서 제외.
- 필드: `captureSource`(기존 `AttributeCaptureSource` enum 재사용), `attributeSetTypeName`+`fieldName`(AQN — `AttributeHandle`이 직렬화 불가 struct라 프로젝트 공통 패턴), `snapshot`(bool). `ToAttributeHandle()` + `IEquatable`(중복 캡처 방지 키).
- **아직 사용처 0 (dead code)** — 다음 단계(캡처 컨테이너·AttributeBased evaluate)에서 배선. **`.meta` 미생성** — GUID를 지어내지 않기 위해 Unity 자동 생성에 맡김.

### 2026-07-19 — Execution 프레임워크 완성 (SO→클래스, Params/Output 분리, Execute 경로 UE 정합)
- **ScalableFloat 루프 검증 통과** — 유저가 에디터에서 확인. 07-19 앞 항목의 미검증 배관(`MakeEffectContext`/`MakeOutgoingSpec`/`GameplayEffectContext` 재구성)이 이 경로로 한꺼번에 검증됨. 검증 중 NRE 1건(`testEffect` 미할당 ASC) → `MakeOutgoingSpec`·`ApplyGameplayEffectSpecToSelf`에 null 가드 추가.
- **Execution을 SO에서 순수 클래스로 전환** (→D7). `GameplayEffectExecutionAsset` 폐기. 근거는 "SO의 존재 이유는 공유할 **데이터**인데 Execution엔 그게 없다(로직이다)". GE 에셋은 AQN만 저장(`executionTypeNames` + 기존 `SubclassSelector` 인프라), `GameplayEffectSpec`이 생성 시 resolve해 인스턴스화. 직렬화 형태(AQN vs `[SerializeReference]`)는 미결로 남김 (→D8). 형태가 SO였던 경위 추적: 최초 GE 커밋 `32bd6b5`(에이전트 세션)에서 근거 기록 없이 정해진 것이었음.
- **Params/Output 분리** (→D11). `ExecutionParameters`(입력) / `ExecutionOutput`(출력 컨테이너) / `GameplayModifierEvaluatedData`(결과 1건 — 기존 `GameplayEffectExecutionOutput` struct를 rename). 시그니처 `Execute(parameters, output)`. 둘 다 struct + execution 스코프 지역 변수로, UE의 루프 내부 지역 변수 구조에 맞춤.
- **Execute 경로를 UE와 정합**하게 수정:
  - 쓰기 지점을 `ApplyEvaluatedModifier` 하나로 통합 (→D9) — Execution 출력이 `AddBase`/`Override` 2종만 지원하던 결함 해소, 6종 전부 동작.
  - BaseValue 쓰기마다 CurrentValue 즉시 재계산 (→D10) — Execution이 같은 GE의 modifier 결과를 stale하게 읽던 버그 수정.
  - Execution별 출력 즉시 반영 → Execution[1]이 Execution[0] 결과를 봄 (UE 소스로 대조 확인: `ExecutionParams`/`ExecutionOutput`이 executions 루프 내부 지역 변수).
  - `ExecuteModifiers` → `ExecuteGameplayEffect` 개명 (Execution도 수행하므로 이름이 실제보다 좁았음).
  - Modifier 없이 Execution만 가진 GE가 조용히 무시되던 문제 수정.
- 에디터: GE 드로어에 `Executions` 노출(누락돼 있었음), Modifiers 리스트·각 요소 foldout + 개수 표시.
- 문서: architecture `gameplay-effect.md`에 **ASC GE 로직 전체**(적용 분기 / Execute 파이프라인 / persistent 경로 / Tick) 근거와 함께 작성. UE 대비 알려진 한계 5건 명시.
- ⏸ **캡처 도입은 유저 지시로 중단** — "구현하자"까지 갔다가 계획 단계에서 멈춤. AttributeBased 평가(5b)도 TODO로 유지.
- **D6 폐기·결번 처리** — 에이전트가 승인 없이 작성한 "캡처 미도입" 결정이었고, 유저가 **도입 방향**을 확정해 무효가 됨. 'progress 작성 규칙'의 D# 재번호 금지에 따라 번호를 당기지 않고 결번으로 남김(유저 확정).
- ⚠️ 미해결로 남긴 것: `AttributeSet` Pre/Post 훅 부재(클램프·사망 판정 자리 없음), persistent 경로 `Override` 비결정성(Dictionary 순회), `ApplyGameplayEffectSpecToTarget`의 Spec 인스턴스 공유.
- 🧹 정리 필요: `CombatAttributeSet.defense`·ASC Execution 가드는 에이전트가 무단 추가/수정한 것으로 유지 여부 미정. ASC `testEffect`는 검증용 임시 필드라 커밋 전 제거 필요.

### 2026-07-19 — ToTarget 적용 경로 + Context 팩토리·접근자 정비
- ASC에 **Target 적용 경로** 추가: `ApplyGameplayEffectToTarget(effect, target, context, level)` / `ApplyGameplayEffectSpecToTarget(spec, target)`. 후자는 `target.ApplyGameplayEffectSpecToSelf(spec)`로 위임(target null이면 Invalid). UE `ApplyGameplayEffectToTarget → Target->ApplyGameplayEffectSpecToSelf` 구조.
- **Context/Spec 팩토리** 도입: `MakeEffectContext()`(self를 Instigator로) · `MakeOutgoingSpec(effect, context, level)`. ToSelf·ToTarget 모두 이 팩토리 경유로 통일 → spec 생성 지점 단일화. context 없으면 ToTarget이 `MakeEffectContext()`로 self 주입.
- **`GameplayEffectContext` 재구성**: Instigator/EffectCauser/SourceObject를 생성자 대신 mutator로 주입 — `AddInstigator(instigator, effectCauser)`·`AddSourceObject(Object)` (UE `AddInstigator`/`AddSourceObject`). 조회는 `GetInstigator()`/`GetSourceObject()` 메서드로 통일(중복 프로퍼티 제거, 유일 소비처 `GameplayEffectExecutionParameters`도 교체). `MakeEffectContext`는 **Instigator만 기본 주입**, SourceObject는 호출처가 필요 시 별도로.
- **결정**: `MakeOutgoingSpec`은 `SpecHandle`로 감싸지 않고 `GameplayEffectSpec`(class) 직접 반환 (→D5). Spec은 이미 참조 타입이라 UE의 SpecHandle이 주는 이점이 없음. 나머지 handle(Attribute/Active/Context)은 각자 실익이 있어 유지.
- ⏳ **여전히 미배선(5b)**: `GameplayModifier`가 `spec.Context.GetInstigator()`(source)/target ASC를 읽어 AttributeBased magnitude를 evaluate하는 런타임은 미구현. 이제 context에 source가 실려 오므로 소비만 남음.

### 2026-07-18 — AttributeBased Magnitude (에디터 단계)
- `MagnitudeCalculationType.AttributeBased` 활성화. `AttributeBasedMagnitude` struct 추가: captureSource(Source/Target) · 캡처 Attribute Set+Field · captureValueType(Current/Base) · coefficient · preMultiplyAdditive · postMultiplyAdditive. 계산식 `(value + PreAdd) * Coefficient + PostAdd`. `GameplayModifier`에 `attributeBased` 필드 추가.
- 에디터 드로잉: Calc Type별 payload가 스스로 그리는 구조. `AttributeBasedMagnitude` 전용 `[CustomPropertyDrawer]`(AttributeBasedMagnitudeDrawer)가 자기 7행 레이아웃·높이 소유 → 부모 `GameplayEffectAssetDrawer`는 `PropertyField` 위임 + `GetPropertyHeight`로 높이 계산(매직넘버 제거). Set+Field 팝업 로직은 `AttributeReferenceGUI`로 추출해 modifier 대상·캡처가 공유(중복 제거). UE `FGameplayEffectModifierMagnitude` 방식 참고.
- ⏳ **런타임 미배선**: `GameplayModifier.GetMagnitude`는 아직 고정 `magnitude`만 반환. Spec/ASC 평가·호출처의 Source(Instigator) 전달 배선은 다음 단계 → 현재 AttributeBased는 **에디터 authoring만 가능, 실효과 없음**. 스냅샷 정책은 "apply 시점 1회"로 확정(캐시 불변조건 유지).

### 2026-07-05 — 문서화
- GAS 문서화 중 **⚠️ `GameplayEffectType` Instant/Duration enum 주석(="미구현") vs ASC 실행 경로(존재) 불일치** 발견 → 블로커·후속 TODO로 등록(유저 확인 대기). (D4 관련)

### 2026-06-13 — PR #3 (Ability System - GameplayEffect)
- GE 계층 구현: `GameplayEffect`(SO)·`GameplayModifier`, `GameplayEffectSpec`(resolve·Magnitude 1회 캐싱), `ActiveGameplayEffect`(+`Handle`), `GameplayEffectContext`(+`Handle`) (→D1·D2)
- `AbilitySystemComponent` 대폭 확장(+392줄): `ApplyGameplayEffectToSelf`/`RemoveActiveGameplayEffect`, Modifier 6종 CurrentValue 누산, Duration 만료·Period 틱, 읽기 캐시 (→D3·D4)
- `GameplayEffectExecution`(+Output/Parameters) — 배관·추상 SO만, concrete 0(껍데기)
- 초기 `AttributeEffect` 스텁 제거
