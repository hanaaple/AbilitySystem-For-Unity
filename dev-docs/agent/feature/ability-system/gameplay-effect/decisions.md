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
- 대가: 클래스 이름·네임스페이스를 바꾸면 AQN 참조가 끊긴다. GE별 튜닝 값을 실을 수 없다(UE Calculation Modifiers 자리 — 2026-10-20 D23으로 생김).
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
- 결정: GE의 `executionTypeNames`(AQN 목록)를 `executions`(`GameplayEffectExecutionDefinition` = `calculationClass` AQN + `calculationModifiers`)로 바꾼다. Calculation Modifier(`GameplayEffectExecutionScopedModifierInfo` = 캡처 정의·연산·magnitude·Source/Target 태그)의 대상(Backing Data)은 그 Execution 클래스의 `Defs()` 중에서만 고른다. 런타임 적용은 2026-10-20에 넣었다(유저 "실제 계산 구현해둬 Transient만 미루고") — UE처럼 실행마다 캡처 aggregator 사본에 modifier를 더해 Execution이 그 사본을 읽는다.
- 왜: 유저 — "GE Execution에 Calculation Modifiers Editor 먼저 구현해봐. 내부 구현은 이후에. Defs로 받아와서 읽어오면 어떻게 UE처럼 가능할거야", 기존 에셋은 "자동이관으로 하지 말고 어셋에 직접 넣어". UE 편집기도 계산 클래스 CDO의 캡처 정의로 콤보를 채운다(`GameplayEffectExecutionScopedModifierInfoDetails.cpp:285-301`).
- 대안: 직렬화 이관 코드(옛 필드를 읽어 새 구조로 옮김 / 에셋 그대로 둬도 됨, 코드가 남음).
- 대가: UE의 `PassedInTags`·`ConditionalGameplayEffects`, Transient 집계기, Evaluation Channel은 없다. 계산 클래스는 여전히 AQN이라 '결정 대기 — Execution 직렬화 형태'는 그대로 열려 있다.
- 재검토: Transient 집계기(2026-10-19 유저 "TODO로 두자" → TODO-BOARD)나 PassedInTags가 필요해질 때.

## D24 — Remove Other Component는 모든 질의에서 자기 GE를 뺀다 (UE와 다름)

- 날짜: 2026-10-20
- 결정: 적용될 때 같은 정의의 활성 GE 핸들을 모아 **모든** 질의의 `IgnoreHandles`에 넣는다. 에셋의 질의는 복사본으로 고쳐 원본을 건드리지 않는다.
- 왜: 유저 — "UE의 의도대로 가야될 거 같은데 더 찾아봐". UE는 핸들 목록을 첫 질의에 `MoveTemp`로 넘겨(`RemoveOtherGameplayEffectComponent.cpp:44`, `TArray` 이동 뒤 원본 `ArrayNum = 0` — `Array.h:857`) 둘째 질의부터 자기 제외가 빠진다. 주석은 "ensure we never remove ourselves"이고, UE 자신도 옛 데이터를 질의 2칸으로 옮긴다(`GameplayEffect.cpp:731-745`). UE git: `909a54274067`(2023-04) "Fixed a bug … where it would consider itself for removal" — 질의마다 자기 핸들을 넣었는데, `f1a11bc768a6`(2023-08, `OnGameplayEffectApplied` 도입 리팩터)에서 `MoveTemp`로 바뀌며 둘째 질의부터 빠짐 — 회귀로 판단.
- 대가: 질의가 둘 이상이고 자기와도 맞을 때 UE와 결과가 다르다(우리는 자기를 남긴다).

## D25 — `FGameplayEffectQuery`·ASC 질의 API는 UE 목록 그대로, 스택·시간만 우리 구조에 맞춤

- 날짜: 2026-10-20
- 결정: `GameplayEffectQuery`(조건 7종 + `IgnoreHandles`·코드용 `CustomMatchDelegate`, `MakeQuery_*` 9종)와 ASC 질의 API(찾기·개수·시간·끝나는 시각·레벨·제거)를 UE대로 둔다. `StacksToRemove`는 자리만 두고 늘 통째로 제거, 개수는 GE당 1. 남은 시간은 Tick이 갱신하는 `RemainingDuration`, 끝나는 시각은 `StartWorldTime + Duration`(2026-10-21 D27에서 `StartWorldTime` 도입 — 그 전엔 둘 다 `RemainingDuration` 기준), 끝없는 GE는 `GameplayEffectConstants.InfiniteDuration`(-1, UE `INFINITE_DURATION`).
- 왜: 유저 — 범위 "UE대로", 스택 인자 "자리만 두기". 블루프린트 델리게이트(`CustomMatchDelegate_BP`)와 서버 권한 검사는 우리에게 없는 것이라 뺐다.
- 대가: UE 컨테이너의 남은 시간은 끝없는 GE에 `-1 - 경과`를 내지만 우리는 -1(UE `FActiveGameplayEffect::GetTimeRemaining`과 같은 값). 시전자 목록은 액터 대신 ASC.
- 재검토: GE 스택을 만들 때 `StacksToRemove`·개수를 스택 수로.

## D26 — 어트리뷰트 값 변경 이벤트는 이벤트 객체를 돌려주는 getter로 (UE 참조 반환 대신)

- 날짜: 2026-10-20
- 결정: 컨테이너가 어트리뷰트별 `GameplayAttributeValueChangeEvent`(안에 `event Action<OnAttributeChangeData>`)를 들고, `GetGameplayAttributeValueChangeDelegate`가 찾거나 만들어 돌려준다. 구독자는 그 객체의 `OnValueChange`에 `+=`/`-=`. 인자는 UE대로 `{Attribute, NewValue, OldValue, GEModData}`.
- 왜: 유저 선택(질문 답, 근거 미기록). UE는 맵 안 델리게이트를 참조로 돌려준다(`GameplayEffect.cpp:5419`). C# delegate는 불변이라 delegate를 돌려주면 받은 쪽 `+=`가 맵에 닿지 않는다.
- 대안: ASC 등록·해제 메서드(태그 `RegisterGameplayTagEvent` 모양) / 타입 하나가 덜 들지만 UE 사용 모양과 달라진다.
- 대가: 어트리뷰트마다 이벤트 객체 하나(타입 하나 추가).

## D27 — GE 수명은 Tick 한 루프 그대로, 적용 시각(`StartWorldTime`)을 기록해 적용 뒤 흐른 시간만 셈 (UE 타이머 대신)

- 날짜: 2026-10-21
- 결정: `ActiveGameplayEffect.StartWorldTime`(적용 시각)·컨테이너 시계 `GetWorldTime()`(기본 `Time.time`) 도입. `Tick()`은 인자 없이 시계를 읽어, GE마다 지난 Tick(처음엔 적용 시각) 뒤 흐른 시간만 주기 타이머에 더하고, 남은 시간은 UE 공식 `Duration - (지금 - StartWorldTime)`(`GameplayEffect.h:1336`)으로 다시 계산. 끝나는 시각 = `StartWorldTime + Duration`(UE `GetEndTime`). 시계를 바꾸는 `SetWorldTimeSource`는 internal — Time.time이 흐르지 않는 에디터 셀프체크용.
- 왜: (유저) 프레임 도중 적용된 GE가 그 프레임 deltaTime까지 깎이던 버그(PlayMode 측정 0.333초)를 두 안 중 "B로 해"(적용 시각 기록). UE도 적용 때 `StartWorldTime = GetWorldTime()`(`GameplayEffect.cpp:4869`).
- 대안: ASC 실행 순서를 앞당김(`[DefaultExecutionOrder]`) / 한 줄이지만 `Start`·물리 콜백·FixedUpdate에서 건 GE는 그대로 짧음. UE처럼 GE마다 타이머 / 한 루프 구조를 바꿔야 해 범위가 큼.
- 대가: 컨테이너가 Unity 시계를 직접 읽어, 시간을 흘리는 에디터 셀프체크 4종이 시계 주입으로 바뀜. 만료·주기 실행은 여전히 Tick 때라 최대 한 프레임 늦음, Tick 도중 목록 변경 문제는 그대로 — Tick 순서 영향 전체는 [tick-order-improvements.md](tick-order-improvements.md)·[tick-order-user-notes.md](tick-order-user-notes.md).

## D28 — 지속 시간을 Spec이 갖고 `SetDuration`으로 바꿈 (에셋 Duration Magnitude 없이)

- 날짜: 2026-10-22
- 결정: `GameplayEffectSpec.Duration`(정의 값으로 시작)·`SetDuration` 추가, 활성 GE·Tick·인스펙터가 Spec 값을 읽음. UE `FGameplayEffectSpec::SetDuration`(`GameplayEffect.cpp:1834`) 대응.
- 왜: (유저) 데모 쿨다운 감소 아이템을 "캐릭터 강화 되면서 버프동안 쿨다운 비율 감소로" — 쿨다운 GE를 걸 때 길이를 바꿀 통로가 필요(지금은 에셋 고정값). Spec 쪽을 고른 이유는 에이전트 판단(유저 미확정): 코어 변경이 가장 작음.
- 대안: 에셋에 Duration Magnitude(UE `DurationMagnitude` — AttributeBased·Custom Calculation) / 데이터 모델 변경이 크고 Custom Calculation은 미구현. 먹을 때 쿨다운 초기화(Remove Other) / 코어 변경 없지만 "비율 감소"가 아님.
- 대가: UE의 `bLockDuration`·지속 시간 캡처(Outgoing/Incoming Duration)는 안 가져옴. 감소 비율은 쿨다운을 걸 때 한 번만 반영 — 이미 돌고 있는 쿨다운은 버프가 생기거나 끝나도 그대로.

## D29 — 피격 정보를 Context에 담음 (`AddHitResult`·`TryGetHitResult`, Lyra 방식)

- 날짜: 2026-10-22
- 결정: `GameplayEffectContext`(+Handle)에 `RaycastHit` 하나를 담는 `AddHitResult(hit, reset)`·`TryGetHitResult`. 이미 있으면 `reset`일 때만 덮어씀(UE `FGameplayEffectContext::AddHitResult`의 check — 우리는 에러 로그 후 무시). 데모 헤드샷: Execution이 맞은 콜라이더의 부위 태그를 읽어 배율.
- 왜: (유저) "GAS Tag 기반으로 헤드 판정" → 전달 방식 두 안 중 "컨텍스트에 피격 정보 (Lyra 방식)". Lyra `LyraDamageExecution`이 Context HitResult → 물리 재질 태그 → 무기의 태그별 배율.
- 대안: Spec 동적 Asset Tag(UE `AddDynamicAssetTag`, 코어 TODO) / 태그만 실어 단순하지만 위치·거리 같은 피격 정보는 못 씀.
- 대가: UE Context의 Origin(`AddOrigin`)·HitResult 공유 포인터는 안 가져옴 — 값 하나 복사. Context가 Spec에 묶여, 한 Spec으로 여러 대상을 맞히면 첫 피격만 담김(지금 투사체는 1발 = 1 Spec = 첫 대상).

## D30 — 디버그 표시용 읽기 전용 조회 API를 Core에 둠 (UE `DisplayDebug`가 쓰는 공개 API와 같은 이름)

- 날짜: 2026-10-23
- 결정: ASC `GetSpawnedAttributes()`·`GetActiveGameplayEffects()`(읽기 전용 열거)·`OnAnyGameplayEffectRemoved` 이벤트(만료·직접 제거 모두 `RemoveActiveGameplayEffect` 한 곳에서), `AttributeSet.GetAttributesFromSetClass(type, list)`(공개·`[SerializeField]` AttributeData 필드, 선언 순). 게임 화면 디버그 오버레이와 에디터 ASC 인스펙터가 이것만 쓴다.
- 왜: (유저 2026-10-22) "Core API도 해" — 디버그 오버레이(포트폴리오 스크린샷·영상용)용. UE `AbilitySystemComponent.h:206`(`GetSpawnedAttributes`)·`:462`(`GetActiveGameplayEffects`)·`:563`(`OnAnyGameplayEffectRemovedDelegate`), `AttributeSet.h:189`(`GetAttributesFromSetClass`).
- 대안: Game 쪽 리플렉션으로 private 필드를 읽음 / Core 무수정이지만 필드 이름에 묶여 깨지기 쉬움(실제로 에디터 인스펙터의 활성 GE 리플렉션 필드 이름이 어긋나 있었음).
- 대가: 활성 GE 열거는 컨테이너 내부 사전의 값 뷰를 그대로 돌려줌 — 열거 중 적용·제거하면 안 됨(UE도 const 참조).

## D31 — GE 스택은 스택 수 1만 (같은 GE를 합쳐 시간만 갱신)

- 날짜: 2026-10-23
- 결정: GE 에셋 `stackingType`(None/AggregateBySource/AggregateByTarget)·`stackDurationRefreshPolicy`·`stackPeriodResetPolicy`. 합칠 GE가 있으면 새로 만들지 않고 기존 GE의 지속 시간(`RestartActiveGameplayEffectDuration`)·주기 타이머만 갱신, Spec·modifier·부여 태그는 처음 것 그대로. 적용 알림·`OnApplied`는 매번.
- 왜: (유저) 쏠 때마다 거는 `GE_CombatStance`가 여러 개 쌓임 → "Core에 GE 스택 구현", "상한 1개에". UE `GameplayEffect.cpp:3507`(`FindStackableActiveGameplayEffect`)·`:4009~4100`(갱신 정책).
- 대가: UE는 합칠 때 기존 Spec을 새 Spec으로 바꾸고 스택 수를 더함(레벨·세기가 새 적용 기준) — 우리는 처음 Spec 유지. `StackLimitCount`·스택 수만큼 modifier 세기·만료 정책·overflow 없음.
- 재검토: 스택 수가 쌓여야 하는 GE(독 중첩 등)가 필요할 때.

## D32 — 한 Spec을 여러 대상에 걸 땐 대상마다 Spec·컨텍스트를 복제

- 날짜: 2026-10-23
- 결정: `GameplayEffectContext.Duplicate()`(virtual, 얕은 복사)·`GameplayEffectContextHandle.Duplicate()`·`GameplayEffectSpec.SetContext()` 추가. 데모 투사체는 맞힌 대상마다 `Clone()` + 복제 컨텍스트에 그 대상의 피격 정보를 담아 건다.
- 왜: 관통 스킬이 발사 때 만든 Spec 하나를 여러 허수아비에 건다. `Clone()`은 컨텍스트를 공유하므로 대상마다 `AddHitResult`가 같은 객체를 덮어써, 늦게 실행되는 GE(2초 뒤 터지는 낙인)가 마지막 대상의 부위로 헤드샷을 판정하게 된다. UE도 같은 이유로 대상마다 복제한다 — `FGameplayAbilityTargetData::ApplyGameplayEffectSpec`(`GameplayAbilityTargetTypes.cpp`, "otherwise the targeting info gets accumulated"), `FGameplayEffectContext::Duplicate`(`GameplayEffectTypes.h:394`).
- 대가: UE `SetContext`는 기본으로 Source 액터 태그를 새 컨텍스트에서 다시 캡처하지만 우리는 안 한다 — 복제 컨텍스트는 Instigator가 같아 결과가 같고, 생성 시점 태그를 지킨다. 파생 컨텍스트에 참조형 필드가 생기면 얕은 복사가 공유하므로 그때 override.
- 재검토: 컨텍스트에 리스트·참조형 상태(여러 피격 정보 등)를 담게 될 때.

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
