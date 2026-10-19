# gameplay-effect — 결정 기록

> 무엇을·어떻게 적나 → [feature/HARNESS.md](../../HARNESS.md) '결정 기록 기준'. `D#`는 feature 내 고유·불변 코드다.
> 경위는 [worklog](worklog.md), 2026-10-18 정리 이전의 표 본문 전체는 git `ca110d2`. UE 대비 채택·생략 범위는 [architecture overview](../../../../project/architecture/ability-system/overview.md).

---

## D1 — 모든 수치 변경은 GE 한 경로로

- 날짜: 2026-06-13
- 결정: 버프·장비·데미지 등 모든 수치 변경이 GameplayEffect를 거친다. 어트리뷰트를 직접 쓰지 않는다.
- 왜: 출처가 많아지면 직접 쓰기는 적용 순서 의존·해제 시 잔류 값·이중 적용 버그를 만든다. UE GAS의 핵심 원칙.
- 대안: 각 출처가 어트리뷰트를 직접 가감(단순 / 위 버그류).
- 대가: 1회성 수치 변경에도 GE 정의가 필요하다.

## D2 — 정의(SO)와 런타임 Spec 분리

- 날짜: 2026-06-13
- 결정: `GameplayEffectAsset`(불변 정의)과 `GameplayEffectSpec`(적용마다 만드는 런타임 인스턴스)을 나눈다. UE `UGameplayEffect` ↔ `FGameplayEffectSpec`.
- 왜: 여러 캐릭터가 같은 GE 에셋을 공유하므로 SO에 상태를 두면 서로 간섭한다.
- 대안: GE SO에 런타임 상태 저장(간단 / 공유 간섭).

## D4 — Modifier 연산 6종 + Base/Current 두 경로

- 날짜: 2026-06-13
- 결정: Instant·Periodic(period > 0)은 Base를 영구로 바꾸고, period 0인 Duration·Infinite는 aggregator가 Current만 계산한다(해제 시 회수). 연산은 AddBase·MultiplyAdditive·DivideAdditive·MultiplyCompound·AddFinal·Override 6종.
- 왜: 영구 소모(데미지·힐)와 일시 버프(장비)는 성격이 달라 "벗으면 정확히 원복"과 "데미지는 영구"가 동시에 성립해야 한다. UE의 Duration 타입·ModOp를 축소 채택.
- 대가: UE의 평가 채널은 없다(aggregator D10).

## D5 — `MakeOutgoingSpec`은 Spec(class)을 직접 반환, SpecHandle 없음

- 날짜: 2026-07-19
- 결정: UE `FGameplayEffectSpecHandle` 같은 래퍼를 두지 않는다. Context·ActiveEffect·Attribute 핸들은 그대로 둔다.
- 왜: UE가 SpecHandle을 쓰는 건 Spec이 C++ 값 struct라 공유·nullable·유효성 표현에 `TSharedPtr`가 필요해서다. 우리 Spec은 class라 언어가 이미 제공한다.
- 대안: UE처럼 SpecHandle 도입(UE와 같음 / `.Data` 역참조만 늘고 얻는 것 없음).
- 대가: 이 부분 API 모양이 UE와 다르다.

## D7 — Execution은 ScriptableObject가 아니라 순수 C# 클래스

- 날짜: 2026-07-19
- 결정: `GameplayEffectExecutionAsset`(SO)을 없애고 Execution을 클래스로 둔다. GE는 타입 이름(AQN)을 저장한다.
- 왜: SO는 "여러 참조가 공유할 데이터를 한 벌만 두기"인데 Execution은 데이터가 아니라 로직이다. UE도 `TSubclassOf`(클래스 참조).
- 대안: SO 유지(GUID 참조라 이름 변경에 강함 / 계산식마다 에셋 파일, GE별 튜닝 값은 에셋 복제).
- 대가: 클래스 이름·네임스페이스를 바꾸면 AQN 참조가 끊긴다. GE별 튜닝 값을 실을 수 없다(UE Calculation Modifiers 자리 — 미구현).
- 재검토: Execution에 GE별 튜닝 값이 필요해지면 `[SerializeReference]` 인스턴스(progress 결정 대기 — 구 D8).

## D14 — AttributeCapture 계층은 UE 구조로: Spec이 캡처 컨테이너를 소유하고 AttributeBased가 캡처 정의를 품는다

- 날짜: 2026-08-02 (계층 신설 2026-08-01)
- 결정: `GameplayEffectAttributeCaptureDefinition`(무엇을)·`CaptureSpec`(캡처 1건, snapshot 고정 / non-snapshot 라이브)·`CaptureSpecContainer`(Source·Target 배열)를 두고, `GameplayEffectSpec`이 컨테이너를 소유한다. Source는 Spec 생성 때, Target은 적용 때 캡처한다. `AttributeBasedMagnitude`는 캡처 정의를 중첩으로 품는다(UE `FAttributeBasedFloat::BackingAttribute`).
- 왜: 유저가 compose를 골랐다(AskUserQuestion 2026-08-02). 캡처 위치·방식은 UE `FGameplayEffectSpec` 흐름 그대로.
- 대안: 인라인 캡처 필드 유지 + snapshot만 추가(드로어·에셋 변경 최소 / UE와 어긋나고 정의 중복이 남음).
- 대가: 직렬화 레이아웃이 바뀌어 기존 GE 에셋을 손으로 옮겼다.

## D15 — 어트리뷰트 참조는 직렬화 타입 `GameplayAttribute` 하나 + 전용 드로어

- 날짜: 2026-08-03
- 결정: 어트리뷰트를 가리키는 두 문자열(AttributeSet AQN + 필드명)을 `GameplayAttribute` 타입으로 묶고 `[CustomPropertyDrawer]`를 붙인다. modifier와 캡처 정의가 모두 이 타입을 쓴다. 런타임 접근은 해석된 `GameplayAttributeHandle`이 맡는다(attribute 치운 D5).
- 왜: 소비처마다 두 필드 + 팝업 배선이 반복됐다. 전용 드로어는 그 타입의 모든 필드에 자동 적용된다. 범위는 유저 확정(AskUserQuestion 2026-08-03 — 둘 다).
- 대가: UE는 `FGameplayAttribute` 하나가 직렬화·해석을 겸하지만, C#은 `FieldInfo`를 직렬화하지 못해 직렬화 타입과 런타임 핸들이 둘로 나뉜다.
- 재검토: 다른 곳에서 어트리뷰트 참조가 필요하면 이 필드 하나만 추가한다.

## D19 — 적용 시점에 Spec을 대상별로 복제(`Clone`)

- 날짜: 2026-08-04
- 결정: `GameplayEffectSpec`에 복사 생성자(+`Clone()`)를 둔다. `Definition`·`Executions`는 공유, `Modifiers`는 배열 복사, 캡처 컨테이너·캡처 태그는 깊은 복사. 복사 경로는 `Initialize`를 다시 돌지 않는다(Source 재캡처 없음).
- 왜: 같은 Spec을 여러 대상에 적용하면 Target 캡처가 서로 덮어쓴다. UE는 값 struct라 복사가 기본이지만 우리 Spec은 class라 명시적 복사가 필요하다.
- 대안: `Initialize` 재실행으로 재구성(단순 / Source를 다시 캡처해 UE 복사 의미와 어긋남).
- 대가: 생성 경로가 둘(빌드·복사) — 필드를 더하면 두 곳을 다 고쳐야 한다(UE도 같은 주석).

## D20 — Spec 직접 생성 API와 적용 시점 Clone을 유지 (asset-only로 좁히지 않음)

- 날짜: 2026-08-04
- 결정: `MakeOutgoingSpec` + `ApplyGameplayEffectSpec*`를 공개로 둔다.
- 왜: 유저 — **SetByCaller를 도입할 것**. 런타임 값을 Spec에 꽂아 여러 대상에 적용하려면 Spec 직접 생성과 대상별 Clone이 필요하다.
- 대안: asset-only(공개 적용은 에셋만, 적용마다 새 Spec / 단순하지만 SetByCaller 불가).
- 재검토: SetByCaller·AoE 계획이 철회되면 asset-only로 좁힌다.

## D22 — Modifier magnitude를 UE `FGameplayEffectModifierMagnitude`처럼 구조체로 뽑아 함께 쓴다

- 날짜: 2026-10-19
- 결정: `GameplayModifier`에 풀려 있던 Calc Type·값·AttributeBased를 `GameplayEffectModifierMagnitude`(`magnitudeCalculationType`·`scalableFloatMagnitude`·`attributeBasedMagnitude` + 계산 함수)로 옮기고, GE Modifier(`modifierMagnitude`)와 Execution Calculation Modifier가 같이 쓴다. 인스펙터도 전용 PropertyDrawer 하나.
- 왜: 유저 — 에이전트가 낸 선택지 중 "구조체로 뽑아 같이 씀"을 고름. 고른 이유는 `근거 미기록`.
- 대안: Calculation Modifier에만 같은 필드 3개 복사(GE Modifier 직렬화 그대로 / 코드 중복, UE 구조와 다름).
- 대가: 모든 GE 에셋의 Modifier 직렬화 경로가 바뀌었다 — 이관 코드 없이 에셋 10개를 직접 고침(→D23과 같은 방식).

## D23 — Execution을 UE `FGameplayEffectExecutionDefinition` 목록으로, Calculation Modifier 편집기는 Execution의 `Defs()`에서 고른다

- 날짜: 2026-10-19
- 결정: GE의 `executionTypeNames`(AQN 목록)를 `executions`(`GameplayEffectExecutionDefinition` = `calculationClass` AQN + `calculationModifiers`)로 바꾼다. Calculation Modifier(`GameplayEffectExecutionScopedModifierInfo` = 캡처 정의·연산·magnitude·Source/Target 태그)의 대상(Backing Data)은 그 Execution 클래스의 `Defs()` 중에서만 고른다. 런타임 적용은 아직 없다.
- 왜: 유저 — "GE Execution에 Calculation Modifiers Editor 먼저 구현해봐. 내부 구현은 이후에. Defs로 받아와서 읽어오면 어떻게 UE처럼 가능할거야", 기존 에셋은 "자동이관으로 하지 말고 어셋에 직접 넣어". UE 편집기도 계산 클래스 CDO의 캡처 정의로 콤보를 채운다(`GameplayEffectExecutionScopedModifierInfoDetails.cpp:285-301`).
- 대안: 직렬화 이관 코드(옛 필드를 읽어 새 구조로 옮김 / 에셋 그대로 둬도 됨, 코드가 남음).
- 대가: UE의 `PassedInTags`·`ConditionalGameplayEffects`, Transient 집계기, Evaluation Channel은 없다. 계산 클래스는 여전히 AQN이라 '결정 대기 — Execution 직렬화 형태'는 그대로 열려 있다.
- 재검토: Transient 집계기(2026-10-19 유저 "TODO로 두자" → TODO-BOARD)나 PassedInTags가 필요해질 때.

## 치운 결정

중요하지 않거나, UE를 그대로 옮겨 고를 것이 없었거나, 단계 기록이거나, 코드가 이미 바뀐 결정이라 본문을 치웠다(유저 2026-10-18). 코드는 재사용하지 않는다.

- **D3** (2026-06-13) — CurrentValue를 쓰기 시점에 재계산해 캐시. aggregator `OnDirty` 재계산으로 바뀜(aggregator D5·D11).
- **D6** — (결번) 유저 승인 없이 쓴 "캡처 미도입". 방향은 도입(D14).
- **D8** (2026-07-19) — Execution 직렬화 형태 AQN vs `[SerializeReference]` — 미결이라 progress '결정 대기'로 옮김. 코드는 AQN.
- **D9** (2026-07-19) — Execute 경로 쓰기 지점을 하나로(UE `InternalExecuteMod`처럼). 버그 수정 성격.
- **D10** (2026-07-19) — Base를 쓸 때마다 Current 즉시 재계산. aggregator 재설계로 대체.
- **D11** (2026-07-19) — Execution 입력·출력 타입 분리(UE 그대로), 둘 다 struct·루프 지역 변수.
- **D12** (2026-07-28) — 캡처 정의 struct를 additive로 신설. D14의 단계.
- **D13** (2026-08-01) — 캡처 결과 계층(Spec·Container)·snapshot. D14로 합침.
- **D16** (2026-08-03) — 캡처 컨테이너에서 `_definitions` 리스트 제거(UE 정렬).
- **D17** (2026-08-03) — Execution이 `Defs()`로 캡처 대상을 선언, Spec이 등록(UE 그대로).
- **D18** (2026-08-04) — `ActiveGameplayEffect`가 Owner를 직접 보유하고 컨테이너는 유보. 컨테이너는 이후 도입됨(aggregator D5·D6), effect의 `Owner` 필드는 코드에 남아 있다.
- **D21** (2026-08-04) — `GameplayModifierSpec`이 핸들·연산을 캐시. 이후 `EvaluatedMagnitude` 하나로 줄임(aggregator D13).
