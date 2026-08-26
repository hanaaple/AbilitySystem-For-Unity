# aggregator — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-08-26 — Aggregator 검증 셀프체크 툴 + Unity MCP 스위치 ON + 캡슐화 리뷰 — 유저 주도
> 유저 지시로 "딸깍 검증" 툴 제작, non-snapshot 런타임 재평가·capture 스냅샷·에셋 다수 케이스까지 커버 요청. 이후 MCP로 실행하려 스위치 켬. 컴파일 블로커 2건은 유저가 해소.
- **검증 툴 `AggregatorSelfCheck.cs`**(`Core/AbilitySystem/Aggregator/Editor/`, 메뉴 `Tools/Ability System/Run Aggregator Self-Check`, Play 불필요·edit 모드 임시 GameObject). 5층: **A** `AttributeAggregator.Evaluate` 연산별 공식·mod 제거 / **B** ASC `GetAttributeCurrentValue`가 aggregator 경유 / **C** `GameplayEffectAttributeCaptureSpec` 스냅샷 vs 라이브(소스 흔들어 non-snapshot 추종·snapshot 고정) / **D** 실제 `GameplayEffectAsset` 적용 다수 케이스(6연산 각각·복합·2-GE 스택·Instant base) / **E** 런타임 dirty 재평가(Speed=Health*0.5 non-snapshot → Health 변경 시 Speed 라이브 재평가). `GameplayEffectAsset`·`GameplayModifier`(struct 박싱)·`AttributeBasedMagnitude`를 리플렉션으로 조립, `FindOrCreateAttributeAggregator`·`Awake`는 리플렉션 호출. 각 케이스 후 GE·GameObject `DestroyImmediate`, 직접 얹은 mod는 핸들로 제거(격리).
- **캡슐화 리뷰(보고만):** `AttributeAggregator`의 변경 메서드가 전부 `public`이라 컨테이너/캡처 파이프라인 우회로 상태 오염 가능(고아 mod·미러 desync·임의 dependent). `ActiveGameplayEffectsContainer`도 `public class`. → 전부 `internal` 권장. 단 조이면 툴 Layer B/C 깨지므로 파이프라인 기반 재작성 동반 필요. **API 변경은 검증 후 유저 승인 대기(미실행).**
- **Unity MCP 스위치 ON:** `dev-tools.md` 절차대로 `.claude/settings.local.json`의 `disabledMcpjsonServers` → `[]`. uvx 0.11.2 설치 확인(2026-07-20 기록의 "uv 없음"은 해소됨). `.mcp.json`의 `UnityMCP` 정의 정상. **세션 재시작해야 `mcp__UnityMCP__*` 도구 로드.** 테스트 후 방침대로 다시 OFF.
- **컴파일 블로커 2건 유저가 해소:** `FindCaptureSpecByDefinition`의 `= null`→`default`(CS0037), `OnAttributeAggregatorDirty` 조건 반전(`!` 제거 — 값 같을 때 skip)·미러 write `SetCurrentValueRaw` 정착.

### 2026-08-25 — 캡처 aggregator화 + D12 dependents 등록 배선 구현 + 프로젝트 전역 주석·컨벤션 스윕 — 유저 주도
> 유저 지시로 GE-Execution/aggregator를 이어 구현. capture→aggregator 전환, 라이브 재평가 dependents 등록을 UE 원문식으로 배선. 이후 별건으로 코드 주석·컨벤션 전역 정리 지시.
- **캡처를 aggregator 기반으로 전환:** `GameplayEffectAttributeCaptureSpec`이 값을 `_snapshotData`/ASC 직접 읽기 대신 **`_capturedAttributeAggregator`에서** 낸다 — snapshot=`TakeSnapshotOf`로 그 시점 aggregator를 복사해 고정, non-snapshot=컨테이너 소유 aggregator 참조(조회 시 `Evaluate`/`GetBaseValue` 라이브). UE `CaptureAttributeForGameplayEffect` 원문 확인 후 우리 구조에 맞춰 적용(값 출처가 UE는 aggregator, 우린 기존에 ASC 직접이었음 → aggregator로 통일). `AttributeAggregator.TakeSnapshotOf`(인스턴스, base+mod 통째 복사) + 빈 생성자 추가. `ASC.FindOrCreateAttributeAggregator` + 컨테이너 `FindOrCreateAttributeAggregator`(기존 private `FindOrCreateAggregator` 개명·internal화) 노출.
- **`GameplayAttributeHandle` `==`/`!=` 연산자 추가**(Equals 위임) — struct라 `==` 미정의였음.
- **`GameplayEffectSpec.Modifiers`를 배열(`GameplayModifierSpec[]`) auto-property(get; private set)로 노출** — `IReadOnlyList`는 struct 복사본을 돌려줘 컨테이너가 슬롯 magnitude를 제자리 갱신 못 함(라이브 재평가 필요). 참조 교체는 막고 원소 변경만 허용. 호출부 `.Count`→`.Length`.
- **D12 dependents 등록 배선 구현(①②④):** `AttributeAggregator._dependents` 초기화(기존 null NPE 해소) + `AddDependent`/`RemoveDependent`, `BroadcastOnDirty`의 **별칭 버그→진짜 복사**(`new List<>(_dependents)`) 수정. 캡처 spec `RegisterLinkedAggregatorCallback`/`Unregister`(non-snapshot일 때만 `AddDependent`) + 컨테이너 `RegisterLinkedAggregatorCallbacks`/`Unregister`(source+target 순회, UE `RegisterLinkedAggregatorCallbacks` 원문 확인). Apply(`period<=0`)/Remove 경로에 등록·해제 호출. **스코프 divergence:** persistent(period≤0)에만 등록 — periodic은 mod가 aggregator 비상주·매 tick 재계산이라 대상 아님. (③ `OnMagnitudeDependencyChange` 재계산은 유저가 in-place `UpdateAggregatorMod` 경로로 채움.)
- **프로젝트 전역 주석·컨벤션 스윕(유저 지시):** 코드 주석에서 `→D#`·`INV-N`·`(UE:)` 등 **외부 봐야 이해되는 포인터 전부 제거**(주석은 자기충족만), 이름 재진술 주석 삭제, 메서드 `=>`→블록(+return)·프로퍼티 `=>`는 유지, 줄넘김된 문장 한 줄로. AbilitySystem·Item·Character·Editor 전반 적용. `CODE_CONVENTION.md` 갱신: 앵커 폐지·자기충족 원칙(하우스 스타일 "근거 문서 앵커" 교체), `=>` 프로퍼티 예외, 긴 줄 줄넘김 지양 일반화.
- **미수정(유저 확인 대기):** ① `OnAttributeAggregatorDirty` `:256` 조건 반전(`!Mathf.Approximately`) — 값이 바뀌면 skip으로 거꾸로 ② `GameplayEffectAttributeCaptureSpecContainer.FindCaptureSpecByDefinition`의 `matchingSpec = null;`(struct에 null = CS0037). 둘 다 로직/컴파일 이슈라 주석 스윕 범위 밖으로 두고 보고만.

### 2026-08-24 — 저수준 raw 미러 write 계층 정리(설계) + capture 실패 UE 동작 확인 + 로그 1건 수정 — 대부분 논의, 코드 1건
> 유저 주도. `SetAttributeBaseValue`/`OnAttributeAggregatorDirty` 재작성 중 설계·개념 Q&A. 착수 지시는 로그 수정 1건뿐(나머지는 설명·방향 합의까지).
- **저수준 raw 미러 write = 핸들 소유로 확정(유저 이름: `SetBaseValueRaw`/`SetCurrentValueRaw`).** "SetBaseValue"가 세 계층(`ASC.SetAttributeBaseValue`·`AttributeSet.SetBaseValue`·핸들)에서 이름이 겹쳐 **순환 호출**(핸들→AttributeSet→ASC→컨테이너 무한 루프)이 생김을 코드로 확인. 분리: 고수준=파이프라인 진입(Pre→aggregator dirty→미러→Post), 저수준=미러 struct에 raw write(전파 없음)=핸들 `…Raw`. 핸들 `SetBaseValue` 안 `set.SetBaseValue(this,value)` 순환 + `SetCurrentValue` data 미변경 버그 식별. **코드 미반영(유저 재작성 중).**
- **`OnAttributeAggregatorDirty`의 Current 반영은 `SetCurrentValueRaw`(전파 없는 raw)로 확정(유저).** 근거: 이 메서드는 aggregator `BroadcastOnDirty` 안(OnDirty 콜백)에서 돌고, 같은 broadcast가 직후 `_dependents`를 순회 → Current write가 재-dirty하면 broadcast 재진입 → dependents 루프 중복 실행·재귀(MAX_BROADCAST_DIRTY까지). CurrentValue는 출력(sink)이라 dirty 경로 진입 자체가 category error. **발견 버그 2건(미수정, 유저 지시 없음):** ① `:256` `if(!Mathf.Approximately(new,old)) return;` 조건 반전 ② `:261` 인자 없는 `attributeSet.SetCurrentValue()` 유령 호출.
- **`OnMagnitudeDependencyChange` 의미 정리:** dependent GE **1건**의 magnitude 재계산 + mod 재동기화(remove/add)로 **대상 aggregator**를 dirty(무조건 — 변경 감지는 하류 `OnAttributeAggregatorDirty`의 Current 반영에서). BaseValue는 안 건드림 — non-snapshot 의존은 Duration/Infinite persistent mod(=CurrentValue 기여)에만 존재, base 변경은 Instant/Periodic execute 전용(라우팅 `:80`·`:96`·`:173`으로 확정).
- **`_attributeValueChangeDelegates` = named delegate `OnGameplayAttributeValueChange(handle,old,new)`로(코드에 이미 반영, `컨테이너 :35·37`).** `Action<…,float,float>`의 두 float 모호성 해소. payload struct(UE `FOnAttributeChangeData`)는 확장 필요 생길 때까지 보류(과설계 회피).
- **UE capture 실패 동작 원문 확인(ylyking `GameplayEffect.cpp` `FAttributeBasedFloat::CalculateMagnitude`):** ① 캡처 **정의 자체가 spec에 없음** → `checkf`(하드 assert/크래시, 프로그래머 설정 오류) ② 정의는 있으나 **aggregator가 값 못 냄** → `AttemptCalculate…`의 bool 반환 **무시**, `AttribValue`는 초기값 `0.f` 유지로 **조용히 0 계산**. 즉 *설정 오류는 강하게, 런타임 결측은 관용*.
- **코드 변경(유일):** `GameplayEffectSpec.CalculateModifierMagnitudes` 실패 폴백 로그의 `%s`(C# 미치환·인자 무시) → 문자열 보간 + `[GESpec]` 태그 + `{i}번 modifier`. 유저가 이후 문구 추가 수정. (UE는 이 지점 로그 없음 → 관측성 divergence.)

### 2026-08-21 — attribute 타입 리네임(`→GameplayAttributeHandle`) + 값 접근자 핸들 이관(B안) + 컨테이너 배선 정리 — 미검증·미커밋
> 유저 주도. `SetAttributeBaseValue`의 write-back이 불편 → "핸들에 값 접근자(UE `FGameplayAttribute::SetNumericValueChecked`처럼)" **B안** 채택. 리네임은 유저가 IDE로 진행, 에이전트가 잔재·주석·문서 정리.
- **타입 리네임:** `AttributeHandle` → (세션 중간 `ResolvedGameplayAttribute`) → **`GameplayAttributeHandle`**(런타임 해석·접근). 직렬화용 `GameplayAttribute`(문자열 쌍+드로어)와 짝. UE 단일 `FGameplayAttribute`를 C#(FieldInfo 직렬화 불가)으로 둘로 나눈 것 — 두 타입 주석에 "부분 대응" 명시. 변환 메서드 `ToAttributeHandle`→`ToResolvedAttribute`.
- **값 접근자 핸들로(B안):** `GameplayAttributeHandle.Get/SetBaseValue(set,…)`·`Get/SetCurrentValue` 추가 — 복사+write-back을 핸들 한 곳에 격리(UE `GetNumericValue(Set)`/`SetNumericValueChecked(v,Set)` 대응). `TryGet/TrySetData`에 `set==null` 가드. `AttributeData`는 Get/Set Base·Current 메서드형(유저).
- **`ASC.GetAttributeSet(handle)`**(UE `GetAttributeSubobject`)·**컨테이너 `FindAggregator`**(없으면 null) 추가. 컨테이너 값 읽기·쓰기를 `_owner.GetAttributeSet` + 핸들 접근자로 교체 → **없던 `_owner.TrySetAttributeData` 호출 제거(대화 초반부터의 미정의 컴파일 에러 해소)**.
- **주석 UE 대응 명시:** ASC `SetAttributeBaseValue`=`SetNumericAttributeBase`, `GetAttributeSet`=`GetAttributeSubobject`, 컨테이너 `ApplyModToAttribute`=`FActiveGameplayEffectsContainer::ApplyModToAttribute`.
- **CODE_CONVENTION:** "표현식 본문 `=>` 지양 — 메서드는 블록 본문" 규칙 추가(유저 지시). 내가 넣은 `=>` 접근자 3곳 블록 본문으로 전환.
- **⚠ 남은 불일치:** 문서(attribute D5·progress, architecture 3종)·잔재 2건이 아직 `ResolvedGameplayAttribute` — 코드(`GameplayAttributeHandle`)와 어긋남. D5 근거도 최종 이름과 모순([NOW.md](../../../NOW.md) ⚠ 참조).
- **미완:** `SetAttributeBaseValue` 본문 유저 재작성 중(dead store 잔존). `AttributeAggregator.BroadcastOnDirty`의 `_dependents` null NPE 여전.

### 2026-08-16 — 어트리뷰트 값 경로 전수 감사 + base 소유 모델 D7 반전 확정(→D14) — 설계·이해만, 코드 미착수
> 유저 주도. "SetBaseValue쪽을 UE처럼" → "aggregator 바깥 Set 경로" → "Evaluate하는 부분 싹 다 UE처럼 — 모든 경로 확인해봐"로 이어진 감사·설계 세션. 구현은 안 함(유저: "set 저거 1개로 바로 하겠다는 말은 아냐", 종료 시 "이해했다는거지").
- **UE 원문 verbatim 확인(ylyking `GameplayEffect.cpp`):** `SetAttributeBaseValue`(PreAttributeBaseChange → AttributeData 직접 씀 → aggregator **Find**, 없으면 `InternalUpdateNumericalAttribute`), `GetAttributeBaseValue`(GameplayAttributeData면 **AttributeData** 읽음), `ApplyModToAttribute`(=`GetAttributeBaseValue`→`StaticExecModOnBaseValue`→`SetAttributeBaseValue`로 합류, 별도 헬퍼 없음), `OnAttributeAggregatorDirty`(Evaluate→`InternalUpdateNumericalAttribute`), `CaptureAttributeForGameplayEffect`(`FindOrCreateAttributeAggregator` — snapshot이면 TakeSnapshotOf). → **UE에서 aggregator는 capture + non-periodic(Duration/Infinite) effect의 소비자일 때만 지연 생성.** WebFetch 요약이 Set/GetBase를 FindOrCreate 호출자로 잘못 나열 → verbatim으로 반박(HARNESS '판단은 틀릴 수 있다').
- **어트리뷰트 값 경로 전수 감사:** 읽기 R1~R5(GetCurrent/GetBase/캡처 non-snap·snap/PlayerCharacter) 이미 aggregator 경유 OK, **R6 인스펙터**만 `data.*` 직접 읽어 우회. 쓰기 **W1 `SetAttributeBaseValue`·W2 `ApplyModToAttribute`가 force-create**로 UE lazy 모델과 어긋남(W3 persistent mod 등록·W4·W5 초기화는 OK). `BroadcastOnDirty`의 `_dependents` null NPE 잠복 재확인.
- **D7 반전 확정(→D14, 유저 확정):** base 진실을 aggregator→**AttributeData**로 되돌림, aggregator base는 Evaluate용 동기화 사본. D7이 근거로 삼은 "UE=aggregator가 base 진실"(ue-reference §11 해석)이 원문과 어긋났음을 발견 — UE의 이중 base는 복제·legacy 제약 산물이고 우리에겐 그 제약이 없음. `SetAttributeBaseValue`를 UE 3단계로, Execute를 그리로 합류, `SetAggregatorBaseValue` 제거, R2/R6 정합. **미구현 — 다음 세션.**
- **HARNESS '왜의 근거는 유저 의도 → UE 원본' 원칙 추가(유저 지시):** "UE를 따른다 = UE의 본질·방향성을 따른다는 뜻이지, UE라서 무조건 세부 구현까지 옮기는 게 아니다. UE 고유 제약(복제·legacy)에서 나온 형태는 그 제약이 우리에게 없으면 안 따른다 — 원문 확인 후에도 '왜 그렇게 했는지 → 그 이유가 우리에게 성립하는지'를 먼저 판단." (그 절 2번 항목에 명시)

### 2026-08-15 — `GameplayModifierSpec` 축소(→D13) + dirty/의존 진입점 ASC 경유화(→D12 ③, D11 배선 조정) — 미검증·미커밋
> 코드 주석 정리 흐름에서 유저가 설계 변경들을 지시. UE 정합 방향(dirty 콜백을 UE처럼 ASC 진입점 경유로).
- **`GameplayModifierSpec` = EvaluatedMagnitude 단일 캐시(→D13):** UE `FModifierSpec`처럼 슬롯의 `Handle`·`Operation`·`IsValid`·생성자 제거. identity(대상·연산)는 정의 `GameplayModifier`를 **같은 인덱스**로 재해석. 고친 곳: `GameplayModifierSpec`(축소), `GameplayEffectSpec.BuildModifierSpecs`(빈 슬롯만 + 유효성 경고는 정의에서), 복사 생성자 주석, 컨테이너 `ExecuteGameplayEffect`/`AddSpecMods`/`RemoveSpecMods`(인덱스 순회), `AbilitySystemInspectorGUI`(표시). 대가: `def.ToAttributeHandle()`(`Type.GetType`) 리플렉션이 apply/execute마다 부활(GE 적용당) — 병목 시 정의 레벨 캐시로 승격.
- **`OnMagnitudeDependencyChange`(→D12 조각 ③):** UE식으로 **ASC=진입점 래퍼 / 컨테이너=로직** 분리. ASC `internal void OnMagnitudeDependencyChange(handle, changedAggregator)` → 컨테이너 위임. 컨테이너 로직: 핸들로 활성 GE 조회 → `spec.ReevaluateMagnitudes()`(라이브 캡처 재평가) → `RemoveSpecMods`+`AddSpecMods` 재동기화(D8 방식)로 새 magnitude 반영 → 대상 aggregator dirty → 재계산(+연쇄). 둘 다 `internal`(AttributeAggregator가 `internal sealed`, 반응성 배선은 어셈블리 내부). `changedAggregator`는 UE 시그니처 대응으로 받되 현재 스펙 전체 재평가(UE는 per-mod 필터 — 후속).
- **⚠ ③은 현재 inert:** 호출부(의존 등록 `Dependents`/AddDependent ① + 등록 배선 ② + dirty 전파 ④)가 없어 아무도 안 부름. 순환 폭주는 aggregator의 기존 `MAX_BROADCAST_DIRTY` 깊이 상한이 방어(단 전파 배선 전엔 이 상한도 inert).
- **`OnAttributeAggregatorDirty` ASC 진입점화(D11 배선 조정):** D11은 컨테이너가 `FindOrCreateAggregator`에서 aggregator OnDirty를 **직접** 구독했으나, UE는 `OnDirty.AddUObject(Owner, &UAbilitySystemComponent::OnAttributeAggregatorDirty, Attribute)`로 **ASC**에 바인딩 후 컨테이너로 위임한다. 이에 맞춰 ① ASC에 `internal OnAttributeAggregatorDirty(agg, attr)` 진입점 신설(컨테이너 위임), ② 컨테이너 `OnAttributeAggregatorDirty` `private`→`internal`, ③ 구독을 `aggregator.OnDirty += d => _owner.OnAttributeAggregatorDirty(d, handle)`로 변경. 결과: 두 dirty 진입점(`OnAttributeAggregatorDirty`·`OnMagnitudeDependencyChange`)이 UE처럼 ASC 대칭. **트레이드오프:** 우리 컨테이너는 UObject 제약이 없어 직접 구독도 가능했으나(한 홉 절약), UE 정합·대칭·cross-actor 시 handle→ASC 해석 일관성을 위해 경유 채택(유저 지시).
- **`ActiveGameplayEffectHandle.GetOwningAbilitySystemComponent()` 신설(→D12 ④ prep):** 핸들에 소유 ASC 참조(`_owner`) payload 추가(UE `FActiveGameplayEffectHandle::OwningAbilitySystemComponent`), 생성자 `(int id, AbilitySystemComponent owner)`로 변경, 발급 지점(컨테이너)에서 `_owner` 전달. `GetOwningAbilitySystemComponent()`는 그 참조 반환(무효 핸들=null). aggregator의 dirty 전파가 dependent 핸들만으로 ASC를 찾아 `OnMagnitudeDependencyChange`를 부르는 경로(④)에 쓰일 예정. **식별은 `_id`만** 유지 — 발급기 static=전역 유일이라 owner를 equality에서 제외(UE는 owner도 비교하나 우리는 불필요).
- **⚠ ③은 현재 inert:** 위 진입점들이 만들어졌어도 cross-attr 전파(①②④)가 없어 `OnMagnitudeDependencyChange`는 여전히 미호출(`GetOwningAbilitySystemComponent`도 아직 호출부 없음). `OnAttributeAggregatorDirty`는 base/mod 변경 때 정상 호출됨(경로만 ASC 경유로 바뀜, 동작 동일). **컴파일·Play는 유저.** 미커밋.

### 2026-08-11 (4) — cross-attr 재평가 방식 설계 확정(→D12) + 세션 종료(미완성·신뢰성 유보)
> UE 원문(GameplayEffectAggregator.cpp·GameplayEffect.cpp)으로 의존 등록·`OnDirtyRecursive` 배선 확인 후, cross-attr 라이브 재평가 복원 방식을 설계·확정. **구현은 안 함(설계까지).**
- **UE 메커니즘 확인:** `FAggregator::Dependents`=`TArray<FActiveGameplayEffectHandle>`, `AddDependent(Handle)`. 등록은 `RegisterLinkedAggregatorCallback`에서 `!bSnapshot`일 때 소스 aggregator에 의존 이펙트 핸들 추가. `BroadcastOnDirty`가 `OnDirty.Broadcast`(자기 재계산) 후 `Dependents` 순회→`ASC->OnMagnitudeDependencyChange(Handle, this)`→`AttemptRecalculateMagnitudeFromDependentAggregatorChange`로 magnitude 재계산→대상 재동기화. 순환은 `MAX_BROADCAST_DIRTY` cap.
- **설계 확정(→D12):** 위 UE식으로 복원. 에이전트가 위상정렬 대안(DAG 1회 계산·순환 감지) 제시했으나 유저가 UE 정합 선택. **재평가의 정확한 의미 정리:** `Evaluate`는 늘 Base+mod로 재계산(순수)이라, 재평가 = 의존 mod magnitude를 소스 새 current로 갱신 후 대상 Evaluate 재실행. "Base 기반이라 순환 없음"은 성립 안 함(순환은 mod magnitude에 있음) — cap으로만 끊음.
- **세션 종료(유저 지시):** dirty·aggregator 쪽 **미완성**(cross-attr 미구현)으로 두고, **작성 코드도 신뢰성 100% 아님**을 명시. 다음 세션 = Play 검증 + cross-attr 복원(4조각).
- **미커밋.** 이번 세션 코드 변경(D9→D10 채널 제거 · D11 반응성 재설계 · `BroadcastOnDirty`+`MAX_BROADCAST_DIRTY` 가드)은 전부 미검증·미커밋.

### 2026-08-11 (3) — 반응성을 aggregator OnDirty + OnAttributeAggregatorDirty로 재설계 (→D11, D8 반전) — 미검증·미커밋
> 유저 지시 2턴: (a) "우선 Aggregator별로 Action 만들어", (b) "container dirty 없애고 OnAttributeAggregatorDirty 만들어. ue 참고해서." UE 원문(GameplayEffect.cpp·GameplayEffectAggregator.cpp)으로 배선 확인 후 이식.
- **`AttributeAggregator.OnDirty`(신설):** `event Action<AttributeAggregator>`. 발화는 `BroadcastOnDirty()`(UE `FAggregator::BroadcastOnDirty` — `OnDirty?.Invoke(this)`를 감싼 유일 지점) 하나에만 두고, base/mod 변경 3지점(`SetBaseValue`·`AddAggregatorMod`·`RemoveAggregatorMod`)이 이를 호출한다(호출부는 OnDirty를 직접 부르지 않음). 자신을 인자로 넘김(UE `FAggregator*` payload 대응).
- **컨테이너 재계산 재배선:** worklist(`_dirtyQueue`/`_visited`/`_isProcessing`)·`OnAttributeChanged`(+ctor 구독)·`ReevaluateDependents` **제거**. `FindOrCreateAggregator`에서 `aggregator.OnDirty += d => OnAttributeAggregatorDirty(d, handle)` 구독(handle 캡처 = UE가 Attribute를 payload로 바인딩). `OnAttributeAggregatorDirty(agg, attr)` → `agg.Evaluate()` → `UpdateAttributeCurrentValue`(UE `OnAttributeAggregatorDirty`→`InternalUpdateNumericalAttribute`). 명시적 `RecalculateAffectedAttributes`/`RecalculateAttributeCurrentValue`와 그 호출부(Apply·Remove·SetAggregatorBaseValue) 폐기 — OnDirty가 재계산을 몬다.
- **`AttributeChanged` 이벤트는 유지**(UE `AttributeValueChangeDelegates` = 값 변경 바깥 알림, UI용). 이제 내부 구독자 없음 = 순수 알림 채널. 재계산 트리거 아님.
- **순환 재귀 가드(유저 지시 "ue 원문처럼 순환 반복 방지"):** `BroadcastOnDirty`에 UE의 `MAX_BROADCAST_DIRTY`(=10) 가드를 이식 — per-aggregator `_broadcastingDirtyCount`로 재귀 broadcast 깊이를 세고, 초과 시 이번 dirty를 건너뛰고 경고(순환 A→B→A의 무한 재귀·발산 차단). UE 원문 확인(GameplayEffectAggregator.cpp: 지역 상수 `const int32 MAX_BROADCAST_DIRTY = 10`, `BroadcastingDirtyCount++/--`, 초과 시 warning). **UE의 `OnDirtyRecursive`(의존 재귀 전파)·`FScopedAggregatorOnDirtyBatch`(배칭)는 이번 범위 밖 — 미이식.** ⚠ **현재는 cross-attr 전파가 없어 BroadcastOnDirty가 재귀하지 않으므로 이 가드는 inert(안 걸림) — cross-attr 복원 시 실효.**
- **제거된 동작(플래그):** 어트리뷰트 간 라이브 재평가(힘→방어력 라이브, D4/D8)가 빠짐. 스펙 `ReevaluateMagnitudes`/`DependsOnNonSnapshot`·캡처 `DependsOnNonSnapshot`은 **미사용 seam으로 잔존**(UE `OnDirtyRecursive`+의존 등록으로 후속 복원 예정). 재진입 가드도 제거(현재 연쇄 없음). 배칭 없음(mod 여럿→중간값 발화 여러 번, UE는 `FScopedAggregatorOnDirtyBatch`로 억제).
- **정합성:** 컨테이너 IDE 진단 = 기존 의도된 float `==` 경고(→D3) 1건뿐, 제거 심볼 잔여 참조 0(grep). **컴파일·Play는 유저.** 미커밋.

### 2026-08-11 (2) — 채널 축 제거, mod 단일 소유로 환원 (→D10, D9 반전) — 미검증·미커밋
> 유저 지시: "채널을 1개만 쓸 거야. Channel이라는 걸 없애고 단일로 하게 해." 앞 턴(D9)에 만든 채널 구조를 되돌림.
- **삭제:** `AggregatorModChannel.cs`·`GameplayModEvaluationChannel.cs`(+ Unity가 임포트해 생긴 `.meta` 2개). 채널 축(맵/enum/채널 객체) 전부 제거.
- **`AttributeAggregator` 환원:** `Dictionary<채널, AggregatorModChannel>` → flat `List<Mod>` 직접 소유(`Mod` struct 인라인 복귀). `AddAggregatorMod`=리스트 add, `Evaluate`=연산별 인라인 집계(D9 이전 공식 그대로), `RemoveAggregatorMod`=`_mods.RemoveAll(source)` 실제 구현 유지. 공개 API·컨테이너 호출 시그니처 불변.
- **정합성:** IDE 진단 0(에러/경고 없음). UE 채널 축과는 갈림 — 다채널 필요 시 D9 복원(D10 재평가 트리거). **컴파일·Play는 유저.** 미커밋.

### 2026-08-11 (1) — mod 저장을 채널별 `AggregatorModChannel` 맵으로 재구조화 (→D9) — 폐기(→D10)
> 유저 지시: "AttributeAggregator에서 FAggregatorModChannel을 channel별 map으로 갖게 하고 channelcontainer까지는 구현하지 마 — 전부 UE에 있던 거고 이런 걸 따르라." UE `FAggregator` 채널 축 구조 이식.
- **신설 `AggregatorModChannel`(UE `FAggregatorModChannel`):** 한 평가 채널의 mod 소유 + `AddMod`/`RemoveModsFrom(source)`/`EvaluateWithBase(base)`/`IsEmpty`. 옛 `AttributeAggregator.Evaluate`의 op 그룹핑·공식·Override 규칙이 그대로 이 채널의 `EvaluateWithBase`로 이관. `Mod` struct도 이리로 이동.
- **신설 `GameplayModEvaluationChannel` enum(UE `EGameplayModEvaluationChannel`, `Core.AbilitySystem.Effect`):** Channel0~15. mod에 채널 필드가 없어 실제로는 Channel0만 쓰임 — 채널 축 구조만 UE에 맞춤.
- **`AttributeAggregator` 재배선:** `List<Mod>` → `Dictionary<GameplayModEvaluationChannel, AggregatorModChannel>`(직접 소유, `FAggregatorModChannelContainer` 래퍼 생략 — 유저 지시). `AddAggregatorMod`=Channel0 채널 지연 생성 후 위임, `Evaluate`=base에서 채널 체이닝(현재 단일 채널이라 1회 평가와 동일). **부수:** 비어 있던 `RemoveAggregatorMod` 스텁을 실제 해제(모든 채널 `RemoveModsFrom`)로 구현, 호출처 없는 `UpdateAggregatorMod` 스텁 제거.
- **정합성:** 컨테이너의 `RemoveAggregatorMod(source)` 호출 시그니처 그대로 유지, 공개 API(생성자·`BaseValue`·`SetBaseValue`·`AddAggregatorMod`·`Evaluate`·`ExecModOnBaseValue`) 불변. 신규 .cs 2개의 .meta는 **유저의 Unity 임포트가 생성**(GUID 지어내지 않음). **컴파일·Play는 유저.** 미커밋.

### 2026-08-10 — per-attribute/per-channel AttributeAggregator 구현 (D5 본체 이관) — 미검증·미커밋
> 유저 지시: "AttributeAggregator를 value 계산·mod channel 쪽 구현, UE 소유 참조 관계 특히 참고, 외부는 Evaluate로만." D5가 건 선결(BaseValue 소유)을 먼저 유저에 확인해 확정(→D7·D8) 후 착수.
- **선결 결정(→D7·D8):** progress가 "BaseValue 결정 전 착수 금지"를 걸어둬, 코드 전에 두 갈림을 유저에 확인. BaseValue 소유 = **Aggregator**(AttributeData는 미러), mod 소유 = **Aggregator ModChannel**(apply 등록/remove 해제) — 둘 다 유저가 UE 근접 안 선택. UE 소유관계 근거는 [ue-reference.md](ue-reference.md) §9·§11.
- **`AttributeAggregator` 재작성(per-attribute 값/채널 본체):** BaseValue + mod 채널(`List<Mod>`, source 핸들로 식별) 소유. `Evaluate()`가 유일 계산 경로(공식은 옛 `CalculateAttributeCurrentValue` 이관), `SetBaseValue`/`AddMod`/`RemoveModsFrom`, `ExecModOnBaseValue`(static, base 연산 이관). **반응성 worklist는 이 객체에서 제거**(→컨테이너).
- **컨테이너 재배선(`ActiveGameplayEffectsContainer`):** 맵 `Dictionary<Type,…>` → **`Dictionary<AttributeHandle, AttributeAggregator>`**(per-attribute, UE `AttributeAggregatorMap` 키). Apply(period 0)=`AddSpecMods`, Remove=`RemoveSpecMods`, 재계산=`FindOrCreateAggregator(h).Evaluate()`(인라인 계산식 폐기), Execute/SetBase=`SetAggregatorBaseValue`(aggregator 진실+AttributeData 미러 동기화). 반응성 worklist(`_dirtyQueue`/`_visited`/`_isProcessing`)를 aggregator→컨테이너로 이관, `OnAttributeChanged`가 직접 구동. Get* 조회는 aggregator 우선(current는 `Evaluate`).
- **부수 개선:** `RecalculateAffectedAttributes`를 공유 스크래치(`_tempHandles`) 대신 **spec.Modifiers 직접 순회**로 바꿔, 반응성 연쇄가 재진입해 스크래치를 순회 도중 비우던 **잠재 크래시(InvalidOperationException) 제거**. dedup은 잃지만 중복 재계산은 값 불변이라 무발화(무해). `_tempHandles` 필드 삭제.
- **정합성:** 제거된 API(`CalculateAttributeCurrentValue`·private `ExecuteModOnBaseValue`)·옛 aggregator 시그니처 외부 참조 0(grep). 컨테이너 `using UnityEngine` 미사용 제거. **컴파일·Play는 유저(수용 기준 검증 대기).** 미커밋.
- **남은 것:** 유저 Play 검증 → progress 본문/architecture(`aggregator.md`) 새 방향 재작성 → 태그 seam(R5) 후속.

### 2026-08-09 (3) — GAS 책임 컨테이너 이관 (유저 턴별 지시) + 포트폴리오 방향 전환 — 미확정·미검증·미커밋
> 유저가 대화 턴마다 지시해 진행. **코드는 끝난 게 아니며 확정짓지 않음(유저: "확정짓지 말고").** Play 검증·커밋 없음.
- **컨테이너 이관(ASC → `ActiveGameplayEffectsContainer`):** 유저가 struct→**class**로 바꾸고 ASC 필드를 `activeGameplayEffects`로 리네임. UE `FActiveGameplayEffectsContainer` 경계대로 Apply/Remove/Tick/Execute(ExecuteGameplayEffect·RunExecutions·ApplyEvaluatedModifier·ExecuteModOnBaseValue)/집계(Recalculate·Calculate·UpdateAttributeCurrentValue)/ReevaluateDependents + 핸들 발급을 컨테이너로. ASC = 공개 API 위임 래퍼 + AttributeSet 저장(`_spawnedAttributeSets`·internal `TryGet/TrySetAttributeData`)만. 동작 불변 리팩터 의도.
- **AttributeChanged 전담(유저 지시):** 이벤트 선언·발화(`Invoke`)·구독 라우팅을 전부 컨테이너로. ASC의 이벤트·`NotifyAttributeChanged` 제거. UE `FActiveGameplayEffectsContainer::AttributeValueChangeDelegates` 대응(유저 확인).
- **aggregator 맵(유저 지시 "Dictionary로 AttributeSet별"):** 단일 `AttributeAggregator` → `Dictionary<Type, AttributeAggregator> _attributeAggregatorMap`. 컨테이너가 AttributeChanged 구독→`handle.SetType`으로 라우팅, 지연 생성. AttributeAggregator는 컨테이너 참조(`ReevaluateDependents` 호출).
- **AttributeSetHandle 시도→반려:** raw Type 키가 도메인 표현상 별로라는 유저 지적으로 `AttributeSetHandle` 신설(+`AttributeHandle.SetHandle`). 그러나 한 ASC 내 set이 타입당 하나라 Type보다 담을 정보가 없어 **hollow 래퍼**가 됐고 유저 반려("이럴거면 Type으로") → 파일·프로퍼티 삭제, Type 키 환원.
- **에디터 인스펙터 수정:** 유저 리네임(`_geContainer`→`activeGameplayEffects`, 프로퍼티 `ActiveEffects`→중복 오타 `ActiveActiveGameplayEffects`)로 깨진 `AbilitySystemInspectorGUI` 수정 — 필드명 상수 갱신 + class라 `as X?`(CS8651) 제거 + 중복 프로퍼티명 `ActiveGameplayEffects`로 정리(컨테이너·참조).
- **⚠ D5 미완:** 위는 컨테이너 레이어·반응성 배치까지다. **어트리뷰트별·채널별 Aggregator 본체 + base/채널 계산 이관은 아직 아님**(채널 계산은 컨테이너 `CalculateAttributeCurrentValue`가 이펙트 직접 순회). **BaseValue 소유 주체(→D5) 여전히 미확정 — 그 결정 전 본체 이관 금지.** 이 세션의 구조 변경(AttributeChanged 전담·set별 맵)은 유저 지시지만 D5 슬라이스 도중이라 **D# 확정은 유보**(확정짓지 말라는 지시).
- **정합성:** 에러 0, 경고 1(부동소수 `==`, 의도). `AttributeSetHandle`/`SetHandle` 잔여 참조 0(grep). **컴파일·Play는 유저.** 미커밋.
- **포트폴리오 방향 전환(유저 결정):** 메인 = 완성된 게임 → GAS 온전 작동 완성도 데모. 기술 선택(UI Toolkit HUD 저비용·DOTween Cue 전용·그래프 보너스·네트워크/ActorInfo 범위 밖)·목표 기능(GA/Tag/Cue/Event/Stack/Hook) = [design.md 포트폴리오 방향](../../../../project/design.md). 다음 = 데모 씬 확정 후 GAS 최소 집합 역산.

### 2026-08-09 (2) — 슬라이스 1: ActiveGameplayEffectsContainer(struct) 도입 (→D6) — 유저 검증 대기
- **방식:** UE `FActiveGameplayEffectsContainer`를 "처음부터 전부 말고 우선 컨테이너부터"(유저). 새 session-protocol '작업 중' 규칙대로 코드 전에 ①무엇인지 ②무엇을 흡수 ③어디 사는지 정리하고, 실제 갈림(struct vs class)만 유저에 확인 후 착수.
- **결정(→D6):** 활성 GE 소유를 ASC → `ActiveGameplayEffectsContainer`(**struct**, `Core.AbilitySystem.Effect`). struct 근거는 유저 제시(ASC 복사 시나리오 없음·타 사용처 없음). 안전판으로 필드 전부 참조 타입(`_owner`·`_effects`)이라 복사돼도 컬렉션 공유.
- **구현(동작 불변 리팩터):** 컨테이너 = 저장·조회·수명(`Add`/`Remove`/`TryGet`/`Count`/읽기뷰 `ActiveEffects`) + `Owner` 역참조. ASC: `_activeEffects`(dict) 필드 → `_geContainer`(struct) 교체, Awake에서 생성. 소비처 위임 전환 — Apply 등록(`Add`)·Remove(`Remove`)·Update의 Count·Tick/Reevaluate/Calculate 3곳 순회를 `_geContainer.ActiveEffects.Values`로. **핸들 발급(`_handleIdCounter`)·Tick 로직·어트리뷰트 계산은 ASC 잔류**(이번 슬라이스 밖).
- **에디터 도구 동기화:** `AbilitySystemInspectorGUI`가 `_activeEffects`를 리플렉션으로 읽어 캐스팅하던 것 → 필드명 `_geContainer`, boxed struct를 nullable 언박싱 후 `.ActiveEffects` 읽도록 수정(죽은 참조 방지, session-protocol '작업 중' 같은 턴 수정).
- **정합성:** ASC 내 `_activeEffects` 잔존 0(grep 확인). 재진입 중 컬렉션 구조 변경 없음(제거는 순회 후) — 순회 안전 불변. **컴파일·Play 실측은 유저.**
- **연관 미처리:** gameplay-effect D18(effect가 Owner 직접 보유)은 컨테이너가 생겨 풀 수 있으나 per-effect `ActiveGameplayEffect.Owner` 제거는 이번엔 안 함(후속 정리로 남김).
- **⚠ 유저 pushback(세션 종료):** 유저가 "아니 ActiveGameplayEffectsContainer를 만들으라니까"로 끊음. 지시는 "우선 struct 만들어"였는데 struct를 넘어 **ASC 위임 배선 + 에디터 수정까지** 한 것이 범위 초과였을 수 있음(session-protocol '작업 중' — 지시된 것만). **미해결로 남김** — 되돌리지 않고 다음 세션에 유저 확인 후 유지/축소 결정(NOW.md 최우선 항목). 미커밋.

### 2026-08-09 (1) — 설계 방향 반전(유저): 진짜 per-attribute/per-channel Aggregator로 재설계 + 기존 계산 이관 — 앞선 구현의 잘못 기록
> 이 항목은 앞의 2026-08-07·08 구현이 **잘못된 전제 위에 섰음**을 유저가 지적해 방향을 뒤집은 기록이다. 과거 항목은 이력으로 보존하되(HARNESS '금지 사항') 현재 방향이 아니다.

- **유저가 지적한 잘못 3건:**
  1. **미구축 레이어를 안 물어보고 신설.** "어트리뷰트 변경 → 의존 이펙트 재평가"는 UE로 치면 `FActiveGameplayEffectsContainer`의 책임이다. 유저는 그 컨테이너 레이어를 **"아직 불필요"로 일부러 안 만들어둔** 상태였는데, 에이전트가 그 로직을 ASC `ReevaluateDependents` + 신규 `AttributeAggregator`로 **임의 신설**했다. 데이터 모델·레이어 신설은 착수 지시를 받았어도 별도 승인 대상(session-protocol '작업 중')인데 안 물음.
  2. **이름·구조 오설계.** 만든 `AttributeAggregator`는 UE `FAggregator`가 아니라 **ASC가 하나만 소유하는 전역 재평가 worklist 디스패처**다. 진짜 aggregator는 **어트리뷰트별·채널별**로 존재해야 한다(유저 지적). 정작 채널 집계(AddBase/Multiply/Divide/Compound/AddFinal/Override)는 `ASC.CalculateAttributeCurrentValue`에 인라인으로 흩어져 있다 — aggregator라 이름 붙였지만 실제 aggregator 본체가 아님.
  3. **마이그레이션을 아예 생각 안 함.** "Aggregator를 새로 만든다"면 기존 Attribute 계산(채널 집계 `CalculateAttributeCurrentValue`·base 연산 `ExecuteModOnBaseValue`·평가/dirty `RecalculateAttributeCurrentValue`+`UpdateAttributeCurrentValue`)이 그 Aggregator로 **이관**돼야 하는데, worklist를 aggregator로 착각해 "만들었다"고 체크해버려 이관 범위를 통째로 빠뜨렸다.
- **원인:** worklist=aggregator 오인 → 이후 "aggregator가 무엇을 흡수하나"를 한 번도 묻지 않음. 그리고 D1("반응성 책임만 분리, full aggregator 이식은 과설계")이 유저 의도가 아니라 **에이전트가 목표 확인 전에 내린 '과설계' 단정**이었을 가능성(HARNESS '해석 규율' — 필요/불필요를 유저 목표 확인 전 세게 단정 금지).
- **방향 반전(→D5):** 진짜 **per-attribute·per-channel Aggregator**를 신설하고 기존 ASC 인라인 계산을 이관한다. 이는 mod 저장을 **이펙트 온디맨드 스캔 → aggregator 소유(Apply 등록/Remove 해제)**로 바꾸고, per-attribute aggregator 맵을 들 **컨테이너 신설**(=안 만들던 레이어)로 이어진다. Apply/Remove/Execute 경로가 광범위하게 바뀐다. D1/D2/D4의 "경량 R3-only" 전제는 폐기.
- **미해결(유저 결정 대기):** **BaseValue 소유 주체** — Aggregator가 base를 소유(=UE 근접, AttributeData는 표시용) vs AttributeData가 진실이고 Aggregator는 mod 집계만. 데이터 모델을 가르는 지점 → 이 결정 확정 후 전체 범위·순서 재제안. 구현은 승인 후.
- **산출물 상태(미승인·미커밋):** 앞서 만든 aggregator 코어 — ASC `AttributeChanged`·`_aggregator`·`ReevaluateDependents` / `AttributeAggregator`(worklist) / Spec `ReevaluateMagnitudes`·`DependsOnNonSnapshot` / 캡처 `IsLiveDependencyOn` — 는 **재설계로 대체 예정. 유지/철회 미정.**
- **하류 문서 낡음:** `architecture/ability-system/aggregator.md`는 경량안 기준이라 재작성 필요(이번엔 안 건드림, 방향 확정 후).

### 2026-08-08 — 라이브 재평가 코어 구현 (TODO 1·3·4, TODO 2 스킵) — 유저 검증 대기
> ⚠ 아래 2026-08-07·08 구현은 2026-08-09 유저 지적으로 **방향이 폐기됨** — 이력으로만 본다.
- **커밋 정리 선행:** 미커밋 다수(캡처 배관·에디터 툴·디버그 도구·이 스캐폴딩·에셋·문서)를 관심사별 7개 커밋으로 분리(유저 요청, "7개 그대로").
- **TODO 1 — 변경 이벤트(→D3):** ASC 어트리뷰트 쓰기 지점이 셋(SetBase·ApplyEvaluatedModifier[Execute Base]·UpdateAttributeCurrentValue[Current])인데 Base 쓰기가 전부 Current 재계산으로 수렴함을 코드로 확인 → **`UpdateAttributeCurrentValue` 단일 choke point**에서 `AttributeChanged(handle,old,new)` 발화(값 실제 변경 시만, 정확비교). progress 원안(SetBase+UpdateCurrent 2곳)은 Execute 경로 Base 변경(수용기준 핵심 시나리오)을 놓쳐 부적합 — 유저에 갈림 제시 후 단일 choke point 확정.
- **발견 — 재캡처 불필요:** `GameplayEffectAttributeCaptureSpec.TryGetCapturedValue`가 non-snapshot이면 이미 조회 시점 `_capturedAsc.GetAttributeCurrentValue` 라이브 재조회, snapshot은 고정. 따라서 변경 시 **재캡처 없이 `CalculateModifierMagnitudes`만 다시 돌리면** 최신 캡처값이 EvaluatedMagnitude에 반영됨 → **TODO 2(의존 등록/해지) 불필요**로 스킵(D2 registry-free 방침과 합치).
- **TODO 3·4 — 핸들러+가드(→D4, 유저 "의존 필터+worklist" 확정):** `AttributeAggregator`(ASC 소유·`AttributeChanged` 구독)가 반응성 소유. `OnAttributeChanged`가 worklist(`_dirtyQueue`)+visited로 연쇄 유한 전파, `_isProcessing` 재진입 시 큐에만 적재(재귀 없음), 같은 handle 재방문 차단(자기참조 1패스 종료). ASC `ReevaluateDependents(handle)`가 `spec.DependsOnNonSnapshot(this,handle)`인 persistent 스펙만 골라 `ReevaluateMagnitudes`+`RecalculateAffectedAttributes`. 의존 판정은 캡처 spec `IsLiveDependencyOn(asc,handle)`(`!snapshot && _capturedAsc==asc && handle 일치`)로 self-scope 한정(cross-actor 자동 제외).
- **신규/변경 심볼:** ASC `AttributeChanged` 이벤트·`_aggregator`·`ReevaluateDependents`(internal) / GameplayEffectSpec `ReevaluateMagnitudes`·`DependsOnNonSnapshot` / 컨테이너 `DependsOnNonSnapshot` / 캡처 spec `IsLiveDependencyOn` / `AttributeAggregator` 본체(스텁 대체). asmdef 없음 → 단일 어셈블리라 internal OK.
- **정합성 검증:** 참조 grep으로 호출 체인 일관 확인(에이전트 범위). **컴파일·Play 실측은 유저**(수용 기준 = TODO 6). 재진입 중 `_activeEffects`/`_tempHandles` 미변경(2차 변경은 큐 적재만)이라 순회 안전.
- **미커버(수용, →D4):** cross-actor 라이브·자기참조 진짜 fixed-point 수렴(1패스만)·다홉 연쇄는 전파하되 값-동일 시 종료. 태그 seam(TODO 5) 미착수.

### 2026-08-07 — feature 신설 + 스캐폴딩 (구현 착수 준비)
- **경위:** GE_BoxSelfEffect의 non-snapshot 모디파이어가 초기값으로만 적용되는 걸 계기로, "라이브 재평가"의 UE 메커니즘을 원문으로 확인(→ `RegisterLinkedAggregatorCallback`의 `bSnapshot==false` 시 `AddDependent`, `FAggregator::OnDirty.Broadcast`, `ASC->OnMagnitudeDependencyChange`, snapshot은 `TakeSnapshotOf`). 유저 방향 확정: **라이브 재평가를 제대로**, 태그는 미정(seam만).
- **feature 배치:** attribute(계산)·gameplay-effect(모디파이어/캡처)를 가로지르는 반응성 계층이라 둘 중 하나에 넣지 않고 **ability-system 그룹의 새 하위 feature `aggregator`**로 분리(의존: attribute, gameplay-effect). feature-list 등록(🔧 IN-PROGRESS).
- **결정(→D1/D2):** 반응성 책임만 분리(R1/R2는 ASC 유지). 구현은 경량(옵저버+재평가+재진입 억제), 범위 cross-attr·self.
- **스캐폴딩 생성:** progress·decisions·worklog(본 파일) / architecture 스켈레톤 `architecture/ability-system/aggregator.md` / 앵커 스텁 `Assets/Scripts/Core/AbilitySystem/Aggregator/AttributeAggregator.cs`(빈 셸). 구현 로직은 아직 없음.
- **다음:** progress `## 다음 작업` — ASC에 어트리뷰트 변경 이벤트부터.
