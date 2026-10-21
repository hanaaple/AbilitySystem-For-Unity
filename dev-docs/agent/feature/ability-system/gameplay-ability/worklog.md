# gameplay-ability — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-10-21 — 이름 변경: Actor → GameObject (GA 쪽)

**요청.** 유저: "Actor라는 이름을 바꾸자 안맞아. GameObject라던가 아님 아예 붙이지 말던가" → 범위 "GA 쪽만", 방식 "GameObject로 바꾸기", "열린 PR #11에 추가".

**구현.** `GameplayAbilityActorInfo` → `GameplayAbilityGameObjectInfo`(파일·meta 이름도), `AbilityActorInfo`/`CurrentActorInfo`/`InitAbilityActorInfo`/`SetCurrentActorInfo` → `…GameObjectInfo`, `OwnerActor`/`AvatarActor` → `OwnerGameObject`/`AvatarGameObject`, `Get(Owning|Avatar)ActorFromActorInfo` → `Get(Owner|Avatar)GameObjectFromInfo`, `GetAbilitySystemComponentFromActorInfo` → `GetAbilitySystemComponentFromInfo`, `InitFromActor` → `InitFromGameObject`, `InstancedPerActor` → `InstancedPerGameObject`, 매개변수 `actorInfo` → `gameObjectInfo`, 같은 파일의 주석 "액터 정보"·"Per Actor"도. 대응표에 없던 `GetAbilitySystemComponentFromInfo`·`SetCurrentGameObjectInfo`·`InitFromGameObject`는 같은 규칙으로 에이전트가 맞춤. `<remarks>UE: …</remarks>`의 UE 이름, GE의 `ActorTags` 계열(gameplay-effect), `Game/`의 일반 주석, 테스트 안 로그 문자열은 그대로. 인스턴싱 정책은 enum 정수로 저장돼 에셋은 안 바뀜. progress 설계 개요만 새 이름으로(decisions·worklog·TODO는 당시 기록이라 둠).

**확인.** 컴파일(Unity 없이, rsp의 파일 경로 갱신) 통과, cref 경고 없음. 첫 실행 도중 에디터가 종료돼(Editor.log 끝이 정상 종료 순서) 유저가 다시 켠 뒤 실행 — 셀프체크 GA 160/0·Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0, PlayMode GA 6/0·수명 13/0·Tag 9/0·Aggregator 14/0, 에러 0. PR #11에 refactor 커밋으로 추가.

### 2026-10-21 — GA 마무리 테스트: 셀프체크 Q(기능 조합·재진입·경계)

**요청.** 유저: "마지막으로 확실하게 GA 테스트하자. 다양한 경우에 따라 Test Case 해서 해".

**판단.** 기존 A~P는 기능 하나씩의 동작이라, 기능끼리 엮이는 경우·재진입·경계를 Q로 더함(케이스 선정은 에이전트 판단). 다른 어빌리티의 활성 중 태그가 차단·필요 조건이 되는 경우(스턴·버프), 자기 태그를 막는 Per Execution, 즉발형의 태그·차단 잔여, 활성·차단 중 해제, ASC 차단 + 차단 태그 동시 실패 사유, Per Execution 이벤트 수, 두 번 종료, 한 발동 두 번 커밋, 쿨다운 GE의 Spec 레벨, 코스트 확인이 Current 기준, 발동 안의 발동, 취소 연쇄(종료 구독자가 발동), 락 안 종료 구독자의 해제, 실패 구독자 안의 다른 실패, 비활성 차단 켜기, 빈 태그 취소. P의 부여 도우미는 Q와 같이 쓰려고 `GiveTaggedAbility`로 뺌(원형 설정 콜백 포함 — Per Actor는 부여 때 복제).

**발견.** 실패 이벤트로 넘기는 사유 컨테이너는 ASC 멤버 하나라, 실패 구독자 안에서 다른 발동을 시도하면 바깥 이벤트의 컨테이너가 덮어써진다(Q-15 관찰: 안쪽 사유로 바뀜). UE도 같은 구조(`InternalTryActivateAbilityFailureTags`를 시도마다 `Reset`, `AbilitySystemComponent_Abilities.cpp:1687`). 고치지 않고 TODO-BOARD 등록. 구독자는 복사해서 쓰면 안전(이벤트 설명에 이미 적혀 있음).

**확인.** 컴파일 통과. GA 셀프체크 **160/0**(Q 18개, 의도된 에러 5·경고 5). 테스트 파일만 바뀌어 다른 셀프체크·PlayMode는 다시 돌리지 않음(직전 2-10 확인 결과 그대로).

### 2026-10-21 — 어빌리티 태그·차단·취소(TODO-BOARD 2-10) + 셀프체크 P

**요청.** 유저: "어빌리티 태그·차단·취소 구현 들어가 Tag 기반으로 막기만 하면 금방이지?". Source/Target 태그는 물어 "빼기" → "해당 나중에 TODO로 해둬"(→D10).

**구현.** UE 5.6 원문대로 — GA 필드 3개(`assetTags`·`cancelAbilitiesWithTag`·`blockAbilitiesWithTag`)·`IsBlockingOtherAbilities`·`SetShouldBlockOtherAbilities`(`GameplayAbility.cpp:683-705`), `PreActivate`가 발동 알림 뒤 `ApplyAbilityBlockAndCancelTags(막기 + 취소, 자기 인스턴스 제외)`를 Spec 활성 횟수 증가 전에(`:985`), `EndAbility`가 막고 있으면 풀기(`:865-869`). `DoesAbilitySatisfyTagRequirements`에 UE `CheckForBlocked`(어빌리티 태그 vs ASC 막힌 태그 → Owner 태그 vs 차단 태그, 차단 실패 태그는 한 번만)와 `AreAbilityTagsBlocked` 재정의 대비 검사(`:320-410`). ASC는 막힌 태그를 `GameplayTagCountContainer`로 개수 관리(`AbilitySystemComponent_Abilities.cpp:1305-1440`), `CancelAbilitySpec`에 ignore 인자.

**막힌 곳.** 회귀 실행에서 Modifier Tag가 64/1(E-12)로 나옴. 이번 변경과 무관 — 진단 로그로 보니 Stunned를 뺀 뒤 1초 경계 Tick에서 주기 실행이 빠짐(Base 110), 그때 에디터 `Time.time` 10.50373(직전 Play 모드 영향). 시계를 0으로 맞추면 65/0. 딱 1초 경계에서 부동소수 오차로 빗나가는 테스트 경계 문제로 보고 고치지 않고 TODO-BOARD에 등록(GE 쪽 주기 판정 자체가 원인인지는 확인 안 함). 진단 수정은 백업으로 되돌림.

**확인.** 컴파일(Unity 없이) 통과, 손댄 파일 경고 0. 셀프체크 GA **142/0**(P 8개: 차단 중 자식 태그까지 막힘·막힌 발동 실패 사유·종료 시 풀림·같은 태그 두 인스턴스 개수·`SetShouldBlockOtherAbilities` 끄고 켜기와 재사용·발동 시 취소 태그(자기 제외)·`CancelAbilities` 포함/제외), Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 64/1(E-12, 위). PlayMode GA 6/0·수명 13/0·Tag 9/0·Aggregator 14/0.

### 2026-10-21 — 어빌리티 이벤트(발동·커밋·실패·종료) + 실패 사유(TODO-BOARD 2-9) + 셀프체크 O

**요청.** 유저: "어빌리티 이벤트(발동·커밋·실패·종료) + 실패 사유 구현해". 사유 태그 정의 위치·종료 이벤트 모양을 물어 "설정 에셋으로 지정 (UE 방식)"·"UE처럼 둘"(→D9).

**구현.** UE 5.6 원문을 보고 옮김 — 알림 위치는 `GameplayAbility.cpp:983`(PreActivate, 활성 중 태그 뒤)·`:573`(CommitAbility 끝), 실패는 `AbilitySystemComponent_Abilities.cpp:1793-1806`(CanActivate 거부, 사유가 비면 일반 거부 태그), 종료는 `:1229-1230`(ActiveCount-- 뒤, 구독 뒤 Spec 다시 찾기). 사유 태그는 `GameplayAbility.cpp:1066`(쿨다운 + 가진 쿨다운 태그)·`:1102`(코스트)·`:320-410`(차단 + 막은 Owner 태그, 필요 없음 + 빠진 필요 태그 — 둘 다 따져 모두 모음). 새 파일 `GameplayAbilitiesSettings`(설정 SO)·`AbilityEndedData`. 사유 컨테이너는 UE처럼 ASC 멤버 하나를 시도마다 비워 쓴다(구독자는 보관하려면 복사). 설정 에셋 `Assets/Resources/AbilitySystem/GameplayAbilitiesSettings.asset`을 만들었다. 처음엔 레지스트리(유저 변경 중)를 건드리지 않아 칸을 비웠다가, 유저 "이것도 알아서 하고"로 사유 태그 5개(`Ability.ActivateFail.*` — 이름은 UE 설정 변수 이름을 따름, 에이전트 판단)를 레지스트리에 더하고(태그 관리 UI와 같은 정렬 `EditorUtility.NaturalCompare` + 트리 새로 고침) 칸을 채웠다.

**판단.** `DoesAbilitySatisfyTagRequirements`를 차단에서 바로 끝내지 않고 차단·필요를 다 따지게 바꿈(결과 bool은 같음, UE와 같음). `CanActivateAbility` 등 virtual 시그니처에 선택 인자가 붙어 셀프체크 `RecordingAbility` 재정의도 고침.

**확인.** 컴파일(Unity 없이) 통과, 손댄 파일 경고 0. GA 셀프체크 **134/0**(O 11개: 이벤트 순서 발동→ActivateAbility→커밋→종료→종료(데이터)·종료 데이터·쿨다운/코스트/커밋/일반 거부/차단+필요 사유·Per Actor 재발동은 실패 이벤트 없음·취소 데이터·종료 구독자가 락 밖에서 해제). 의도된 로그 에러 5·경고 5 그대로. 테스트는 설정을 리플렉션으로 바꿔 끼움. 유저 "테스트 다 돌려" → 태그를 채운 뒤 전체 실행: 셀프체크 GA 134/0·Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0(경고 1줄 정상), PlayMode GA 6/0·수명 13/0·Tag 9/0·Aggregator 14/0(셋업 메뉴 없이 기존 에셋을 꽂아 실행). 에러 0. 에디터가 포커스를 잃으면 Play 시간이 거의 안 흘러 `Application.runInBackground = true`를 켜고 기다렸다(Play 중 값이라 프로젝트 설정은 안 바뀜).

### 2026-10-21 — 전체 테스트 실행

**요청.** 유저: "테스트 한번 전부 하자".

**확인.** 셀프체크 5종 GA 123/0·Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0, PlayMode 4종 GA 6/0·수명 13/0·Tag 9/0·Aggregator 14/0 — 에러 0. Aggregator PlayMode는 셋업 메뉴(유저가 고친 `AttrInit_PlayModeTest.asset`을 다시 만듦)를 쓰지 않고, Play 중 비활성 GO에 ASC + `AggregatorPlayModeTest`를 붙여 `SerializedObject`로 기존 `_PlayModeTest` 에셋 5개를 꽂은 뒤 켜서 돌렸다(에셋은 읽기만).

### 2026-10-21 — GA 인스펙터: 베이스·상속 필드를 선언 클래스별 상자로 나눔

**요청.** 유저: "GA 상속 field Base Field 분리해서 UI 가독성 좋게 해봐".

**구현.** `Abilities/Editor/GameplayAbilityAssetEditor`(`[CustomEditor(typeof(GameplayAbilityAsset))]`) — 맨 위 타입 선택기는 범용 `SubclassSelectorDrawer.DrawManagedReferenceSelector` 재사용, 그 아래 원형 필드를 선언한 클래스별 상자로(`GameplayAbility (공통)` → 상위 클래스 → `이 어빌리티`, 하위 클래스가 필드를 안 더했으면 "이 클래스에 선언한 필드 없음"). 상자는 editor-ui-readability-guide 3.2(반투명 회색 칠 + 1px 테두리 + 굵은 제목). 나누는 정책은 GA 전용이라 범용 드로어엔 넣지 않음(editor-drawer-guide '범용 드로어에 특정 사용처 정책 금지'). 색·여백 수치는 에이전트 판단(유저 미확정).

**확인.** 컴파일 통과, `Editor.CreateEditor(GA_New)`가 새 에디터를 돌려줌, GA 셀프체크 123/0(H 인스펙터 묶음 포함). 화면은 유저 확인 대기.

**발견.** 유저 "SubClass Select 쪽에 Script 아이콘 사라졌어" → 원인 확인: 유저가 `GA_New` 원형 타입을 `AbilitySystemLifecyclePlayModeTest/LingeringAbility`(이번 세션 PlayMode 테스트의 중첩 클래스)로 바꿔 둠 — 아이콘은 `EditorTypeUtility.FindScript`가 파일명 = 클래스명으로 찾는데 그 파일이 없어 비어 있다(`TestGameplayAbility`는 찾음). 같은 이유로 타입 선택 목록에 테스트 전용 중첩 클래스 2개(`LingeringAbility`·셀프체크 `RecordingAbility`)가 노출 — 고치지 않고 TODO-BOARD 등록.

### 2026-10-21 — GA 인스펙터: 코스트·쿨다운 GE 적합 표시

**요청.** 유저: "GA Inspector에 Cost Cooldown GE에 대해 적합한지 표시해주는 걸 만들어줘. 색상으로 초록 빨강에 Editor 용이니까 성능 신경쓰지말고 null이 아니면 자동으로 뜨는 형식으로", "Cost Cooldown은 GE가 특정하게 세팅되어있어야한다고 했잖아 그거야", 이어서 "간단하게 뭐가 안되는지도 Tooltip으로 하고 UI적으로 마우스를 가져다대면 Tooltip이 뜬다는걸 알도록".

**구현.** 런타임 `AbilityGameplayEffectSlotAttribute(AbilityGameplayEffectSlot.Cost/Cooldown)` — `GameplayAbility`의 `costGameplayEffect`·`cooldownGameplayEffect`에 붙임. 에디터 `Abilities/Editor/AbilityGameplayEffectSlotDrawer` — 필드 아래에 GE가 있을 때만 상자: 제목 줄(초록 "적합" / 빨강 "부적합" + "ⓘ에 마우스를 올리면 이유"), 판정 줄마다 ✓/✗/? + 짧은 요약, 마우스를 올리면 이유가 툴팁, 줄 끝 `_Help` 아이콘(같은 툴팁). 속성 하나 + 드로어 하나로 두 용도(editor-drawer-guide '타입만 다른 드로어 N개 금지'). ScalableFloat 수치는 `AttemptCalculateMagnitude`가 spec 없이 못 불러 GE `SerializedObject`에서 읽음.

**기준(에이전트 판단 — "우리 확인·적용 경로에서 의도대로 동작하는가").** UE는 타입을 강제하지 않는다 — 코스트 확인은 Additive만(`GameplayEffect.cpp:5190`), 쿨다운은 부여 태그로만(`GameplayAbility.cpp:1057-1062`). 코스트: Instant / 어트리뷰트 지정 / AddBase(다른 연산은 확인에서 빠져 빨강) / ScalableFloat 수치 음수(AttributeBased 등은 부호 확인 불가 → "?", 판정엔 영향 없음) / AddBase 하나 이상 / Execution 없음. 쿨다운: Duration(Instant는 막지 못함, Infinite는 영영 막음) / 지속시간 > 0 / 부여 태그 있음.

**확인.** 컴파일 통과. 임시 `Assets/_Test/GA_SlotDrawer_Temp.asset`에 기존 GE 3쌍을 넣어 높이 계산(GE 없음 = 한 줄, 있음 = 판정 줄만큼) + 인스펙터 선택 — 콘솔 에러 없음. 유저가 화면 확인 — "좋아 적절하네". 임시 에셋은 그 뒤 지움.

### 2026-10-21 — GE 수명 버그 수정(gameplay-effect D27) + Tick 순서 영향 정리

**요청.** 유저: "GE 버그 정확히 뭐야" → 설명 → "뭐 단순 버그면 고쳐" → 단순하지 않아 두 안(A 실행 순서 앞당김 / B 적용 시각 기록)을 냄 → "B로 해. 그리고 Tick의 순서가 영향을 미칠 부분을 정리해서 문서로 작성해둬. 구현이나 보완 가능한 건 할거고 AbilitySysetm 사용자(개발자)가 알아야되는건 따로 나중에 정리할거야".

**구현.** gameplay-effect 코드(사유는 그쪽 progress) — `ActiveGameplayEffect.StartWorldTime`, 컨테이너 시계 `GetWorldTime`(기본 `Time.time`)·`SetWorldTimeSource`(internal), `Tick()`은 GE마다 적용·지난 Tick 뒤 흐른 시간만, 남은 시간은 UE 공식으로 재계산, `GetEndTime` = 적용 시각 + 지속시간. ASC.Update는 `Tick()`. 셀프체크 4종의 `Tick(1f)`를 시계 주입 + `Tick()`으로(`AdvanceTime`·GA는 `AdvanceClock`/`Tick` 도우미), GE Component H-15 기대값을 컨테이너 시계 기준으로. GA M-9 추가(프레임 도중 적용 고정), PlayMode P-3에 "커밋 프레임 끝 남은 쿨다운 = 지속시간" 조건 추가.

**막힌 곳.** 처음엔 남은 시간도 경과를 빼 쌓았더니 GA N-2·N-3 실패 — 정확히 10초를 흘린 정리 Tick 뒤 쿨다운이 남음. float 경계로 판단(값은 안 찍음, 에디터 `Time.time`은 1.13초였고 시계는 테스트가 흘려 30초대). 남은 시간을 적용 시각에서 다시 계산하게 바꾸고, 경계에 걸린 정리 Tick은 11초·M-5는 6.5초로.

**확인.** 셀프체크 GA 123/0·Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0. PlayMode GA 6/0·수명 13/0·Tag 9/0(한 Play에서 셋 같이).

**문서.** [gameplay-effect/tick-order-improvements.md](../gameplay-effect/tick-order-improvements.md)(구현·보완할 것) · [tick-order-user-notes.md](../gameplay-effect/tick-order-user-notes.md)(사용자가 알아야 할 것) — 처음엔 한 문서(10항목, [보완]/[사용자] 표시)로 썼다가 유저 "문서는 따로 만들어서 해둬" → "보완/사용자 두 문서로". [보완] 둘(Tick 도중 목록 변경 예외 가능 — 미검증, 남은 시간 조회 시점 계산)은 TODO-BOARD 'GE 버그'에 등록.

### 2026-10-21 — ASC 복제·파괴를 UE 결과와 같게 + PlayMode 수명 테스트(→D8)

**요청.** 유저: "GameObject Clone, ASC 파괴, ASC Clone에 대한 테스트 및 구현 해봐". 복제 동작을 물었더니 "ue에서는 어떻게 하는데? 단순히 그대로 복사하기는 그렇고 그렇다고 아예 비워도 안되고", 파괴 범위는 "UE에서는 어떻게 하는데? 구현 방법말고 결과, 기능으로서" → UE 원문 결과 설명 → "UE의 결과와 동일하게 되도록 해."

**확인한 것.** probe(Play 중 `Instantiate`): 복제본은 어빌리티 0·태그 0·ActorInfo 자기 자신 — 직렬화 필드가 `attributeInitData`뿐이라 나머지는 `Awake`에서 새로 생김. UE: `ActivatableAbilities`·`SpawnedAttributes`는 `Transient`, `DefaultStartingData`는 설정으로 복사돼 `OnRegister`에서 새 AttributeSet 초기화(`AbilitySystemComponent.h:1695`·`:1957`·`:196`, `.cpp:197`). 파괴는 `DestroyActiveState`(재진입 플래그 → Owner·ASC 유효하면 `CancelAbilities` → `ClearAllAbilities`) + AttributeSet `MarkAsGarbage`, 활성 GE는 따로 안 지움(`AbilitySystemComponent_Abilities.cpp:97-116`·`:1362-1407`). 죽은 의존자 제거는 `GameplayEffectAggregator.cpp:638-645` — 우리 `AttributeAggregator.BroadcastOnDirty`도 이미 `if (abilitySystemComponent)`(Unity null)로 같은 처리.

**구현.** ASC `OnDestroy` → `DestroyActiveState` → `_spawnedAttributeSets.Clear()`. 새 공개 API `CancelAllAbilities()`(UE `CancelAllAbilities`, Ignore 인자 없음). 재진입 플래그는 뺌(→D8 대가). 복제·의존자 제거는 코드 변경 없음.

**확인.** PlayMode `Assets/Scripts/Test/AbilitySystemLifecyclePlayModeTest.cs` **13/0** — C(복제: 설정 초기값·실행 상태 없음·액터 정보·같은 설정 에셋·독립) 6개, D(파괴: 활성 4번 모두 취소·Spec 4개 모두 OnRemove·취소가 먼저·취소 콜백에서 Owner 재파괴해도 에러 없음) 5개, X(라이브 캡처 연결 후 대상 파괴 → Source 변경에 에러 없음) 2개. 테스트용 GO는 Awake 전에 설정을 넣으려고 비활성으로 만들어 켬. 에디터 셀프체크 5종 회귀 그대로(GA 122/0·Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0).

### 2026-10-21 — 2-8 검증: TestGameplayAbility 셀프체크 M·N + PlayMode

**요청.** 유저: "검증 — TestGameplayAbility 해". 방식은 "둘 다"(셀프체크 + PlayMode), TestGameplayAbility 동작은 "테스트는 마음껏 넣어. 더 추가하고 만들어도 돼", 앞서 TODO로 둔 시간 경과 쿨다운·커밋 미검사 3종은 "같이 넣기".

**구현.** `TestGameplayAbility.ActivateAbility` — 커밋 → `_testEffect`를 Owner에(어빌리티 레벨) → 종료, 커밋 실패면 취소로 종료(데모 즉발형 스킬과 같은 흐름). 셀프체크에 M(TestGameplayAbility 통합 8개, 시간은 컨테이너 `Tick`을 리플렉션으로 직접 불러 흘림)·N(커밋 경계 3개) 추가. L과 새 묶음이 같이 쓰는 GE 만들기(코스트 Instant·쿨다운 Duration)·코스트/쿨다운 칸 채우기를 도우미로 뽑아 L도 그걸 쓰게 바꿈. PlayMode `Test.GameplayAbilityPlayModeTest`(기존 `TagRequirementsPlayModeTest` 형식 — 메모리에 GE·GA 정의, Play 중 GameObject에 붙이면 결과 줄).

**확인.** 셀프체크 GA **122/0**(의도된 에러 5·경고 5 — N-2가 경고 3줄 더함). PlayMode는 Unity MCP로 Play 진입 → 런타임 GameObject에 붙임 → 콘솔 확인 → Play 종료(씬 수정 없음). 첫 실행(쿨다운 0.5초)은 P-3 실패 — 에디터가 포커스를 잃어 프레임이 `maximumDeltaTime`(0.333)까지 길어져 0.2초 대기 안에 쿨다운이 다 지남 → 쿨다운 3초·중간 대기 1초로 늘림. 그래도 P-3 실패: `Time.time`으로 1.00초 흘렀는데 남은 쿨다운 1.67(기대 2.00). 커밋 프레임의 deltaTime·그 프레임 끝 남은 쿨다운을 찍어 보니 0.333 / 2.667 — **적용된 그 프레임의 deltaTime 전체가 같은 프레임 ASC.Update의 Tick에서 차감**됨(적용 전에 흐른 시간까지 깎임). GA가 아니라 GE 컨테이너(`ActiveGameplayEffectsContainer.Tick`) 동작이라 고치지 않고 TODO-BOARD에 버그로 등록. P-1·P-2·P-4·P-5·P-6 통과 → **5/1**.

### 2026-10-21 — 셀프체크 L 실행: 활성 중 태그 2개 실패 → `SerializedDeepCopy` 수정 후 111/0

**요청.** 유저: "말그대로 cost 계산이구나 Test해".

**확인.** Unity MCP로 5종 실행. **Gameplay Ability 109/2**(기대 111/0) — L-1~L-7·L-9~L-11 통과(코스트 적용·남은 쿨다운·쿨다운 중 실패·코스트 부족·커밋 직전 자원 감소·딱 맞는 자원·필요/차단 태그), **L-8(활성 중 Owner에 ActivationOwnedTags)·L-12(Per Execution 2개 활성 시 태그 2개) 실패**. 의도된 에러 5·경고 2줄은 기대대로. 회귀 Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0 그대로.

**원인(확인).** 아래 '발견'의 `SerializedDeepCopy` 콜백 누락이 태그에도 걸린다. `InternedName._hash`가 `[NonSerialized]`라 복제본에서 0으로 남고, 해시를 채우는 `OnAfterDeserialize`가 불리지 않음. `GameplayTagCountContainer`는 `Dictionary<GameplayTag, int>`라 복제된 어빌리티의 `activationOwnedTags`로 더한 태그가 해시 0 자리에 들어가 원래 태그로 조회하면 못 찾는다. Unity에서 복제 전후 해시 확인: 원본 1561523603 / 복제본 0, `Equals`는 true(참조 비교라). 필요·차단 태그(L-7·L-10·L-11)는 사전 조회가 아니라 통과. 앞 항목의 "이번 구현엔 영향 없음"은 틀렸다 — 부모 캐시만 보고 해시를 안 봤음.

**수정.** 안 두 개를 냄 — A 깊은 복사가 복사본마다 `OnAfterDeserialize` 호출(부모 캐시 버그도 같이 풀림, 에이전트 추천) / B `InternedName` 해시를 처음 쓸 때 계산(이 증상만). 유저 "그래" → A. `Core.Common.SerializedDeepCopy`에 `NotifyAfterDeserialize` — `CopyReference`·인라인 복사에서 필드를 다 채운 뒤 부름(재귀라 안쪽 객체가 먼저, 구조체는 박싱된 복사본에 불러 결과가 필드에 들어감). 원본 쪽 `OnBeforeSerialize`는 부르지 않음(지금 쓰는 타입은 모두 빈 구현). `InternedName._hash`를 `[NonSerialized]`로 둔 이유 — 유저: "HashCode가 엔진 키고 끌 때마다 다르니까 그거 대비한건데"(2026-10-21). 이번 수정은 해시를 복사하지 않고 복사본에서 다시 계산하므로 그 의도와 맞음(Unity 런타임의 string 해시가 실제로 실행마다 다른지는 확인 안 함). 복제 태그 해시 원본과 일치(1561523603) 확인 → GA **111/0**, 회귀 Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0 그대로. 부모 캐시 복원은 전용 검사 없음.

### 2026-10-21 — 코스트·쿨다운·커밋·태그 조건(TODO-BOARD 2-4~2-7) + 셀프체크 L(미실행)

**요청.** 유저: "Cost, Cooldown, Tag 조건 구현해", "Test도 알아하고", 중간에 "unity는 꺼놨어 나중에 Test".

**구현.** UE 5.6 원문 — `GameplayAbility.cpp` `CommitAbility`(:559)·`CommitCheck`(:615)·`CommitExecute`(:651)·`CheckCooldown`(:1050)·`ApplyCooldown`(:1083)·`CheckCost`(:1092)·`ApplyCost`(:1115)·`GetCooldownTimeRemaining(AndDuration)`(:1128·:1162)·`GetCooldownTags`(:1197)·`DoesAbilitySatisfyTagRequirements`(:316), 필드 `GameplayAbility.h:743-786`, `FActiveGameplayEffectsContainer::CanApplyAttributeModifiers`(`GameplayEffect.cpp:5177`). 코스트·쿨다운은 UE에서 커밋 때만 적용돼 `CommitAbility`(2-6)를 함께 넣음(→D7). 컨테이너에 `CanApplyAttributeModifiers` 추가는 gameplay-effect 코드라 그쪽 progress에 사유를 남김.

**발견.** `SerializedDeepCopy`가 `ISerializationCallbackReceiver`를 부르지 않아 복제된 태그 컨테이너의 부모 캐시가 빈다 — 이번 구현엔 영향 없음(어빌리티 컨테이너는 명시 태그만 읽힘). 고치지 않고 TODO-BOARD에 등록.

**확인.** 컴파일(Unity 없이, 런타임·에디터) 통과. 셀프체크 L 13개(L-0 태그 준비 + 커밋·남은 쿨다운·쿨다운 중 실패·코스트 부족·커밋 직전 자원 감소·딱 맞는 자원·필요/차단(부모 매칭)/활성 중 태그·Per Execution 태그 누적) 작성, Unity가 꺼져 있어 **미실행**.

### 2026-10-21 — 인스턴싱 정책 Per Actor·Per Execution + 셀프체크 K

**요청.** 유저: "GA는 Per Instance, Per Actor(ASC, GameObject)만 구현할거야" → Per Instance = InstancedPerExecution 확인("ㅇㅇ 그거 맞아"). 필드 위치·Per Execution의 CDO 역할을 물어 각각 "런타임 base 클래스"·"원형이 CDO 역할"을 받음(→D6).

**구현.** UE 5.6 원문 기준 — 기본값 `InstancedPerExecution`(`GameplayAbility.cpp:91`), `SetCurrentActorInfo`의 `IsInstantiated` 가드, `FGameplayAbilitySpec::GetPrimaryInstance`(Per Actor만, `GameplayAbilityTypes.cpp:207`), ASC `OnGiveAbility`(Per Actor 인스턴스를 여기서 만듦 — 2-1 때 `GiveAbility`에 있던 생성을 UE 위치로 옮김)·`InitAbilityActorInfo`(정책별 OnAvatarSet 대상, `:169-181`)·`InternalTryActivateAbility`(AbilitySource = 인스턴스 또는 CDO, Per Execution은 발동마다 생성)·`NotifyAbilityEnded`(Per Execution 인스턴스 제거)·`CancelAbilitySpec`·`OnRemoveAbility`(모든 인스턴스 종료 후 대표 인스턴스 또는 CDO에 알림). Spec은 인스턴스 목록(`GetAbilityInstances`)을 갖게 바꿈. 새 enum `GameplayAbilityInstancingPolicy`.

**판단.** UE `IsActive()`는 Per Execution이면 "살아 있으면 활성"(CDO도 true)인데, 여기선 끝난 인스턴스를 버리므로 `_isActive` 그대로 둠(원형은 false). UE `OnRemoveAbility`의 Per Execution 재귀 예외 처리(`RemoveAfterActivation`)는 `GiveAbilityAndActivateOnce`가 없어 뺌. Per Actor인데 인스턴스가 없으면 해제 알림을 원형으로 보내지 않음(2-1 동작 유지).

**확인.** 컴파일(Unity 없이, 새 파일 rsp 추가) 통과. GA 셀프체크 98/0(K 12개; 기존 A~J는 테스트 에셋을 Per Actor로 만들도록 바꿈). 회귀 Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0.

### 2026-10-20 — GA에서 GE 적용(TODO-BOARD 2-3) + 셀프체크 J

**요청.** 유저: GA와 GE 연결을 물은 뒤 "일단 UE에 맞춰서 구현하고 있어봐 따로 이해하고 있을게", "테스트까지 하고".

**구현.** UE 5.6 `GameplayAbility.cpp`의 `MakeEffectContext`(:1888)·`MakeOutgoingGameplayEffectSpec`(:1330)·`ApplyGameplayEffectToOwner`(:2015)·`ApplyGameplayEffectSpecToOwner`(:2036)·`ApplyGameplayEffectToTarget`(:2055)·`ApplyGameplayEffectSpecToTarget`(:2100)·`GetAbilityLevel`(:1786)을 보고 옮김(→D5 GE 적용·대상은 ASC 하나). 네트워크 권한 확인·GameplayCue 묶음·스택 수·TargetData는 뺌. 기존 ASC `MakeOutgoingSpec`·`ApplyGameplayEffectSpecToSelf/ToTarget` 위에 얹음.

**막힌 곳.** UE는 컨텍스트에 어빌리티를 넣는데(`SetAbility`) 우리 `GameplayEffectContext`엔 그 자리가 없고, 그 파일은 유저가 고치는 중(SourceObject를 GameObject로)이라 손대지 않고 보류. Spec 출처(Object)와 컨텍스트 출처(GameObject) 타입이 달라 GameObject 출처만 넘김(→D5 보류).

**확인.** 컴파일(Unity 없이) 통과. 셀프체크 GA 86/0(J 12개 추가 — 에디터에선 Awake가 안 불려 ASC GE 컨테이너를 리플렉션으로 초기화, GE 셀프체크와 같은 방식). 회귀 Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0. 의도된 로그: 에러 5줄(E 3·G-5 1·J-7 1)·경고 2줄(I-11·J-10).

### 2026-10-20 — 생명주기(TODO-BOARD 2-2) + 셀프체크 I

**요청.** 유저: "생명주기 구현해".

**구현.** UE 5.6 원문(`GameplayAbility.cpp` CanActivateAbility·PreActivate·EndAbility·CancelAbility, `AbilitySystemComponent_Abilities.cpp` TryActivateAbility·InternalTryActivateAbility·NotifyAbilityEnded·CancelAbilityHandle·OnRemoveAbility)을 보고 흐름을 옮김(→D4 생명주기 뼈대만). GA: `IsActive`·`CanActivateAbility`·`PreActivate`·`ActivateAbility`·`EndAbility`·`CancelAbility`·`CallActivateAbility`·`IsEndAbilityValid`. ASC: `TryActivateAbility`(스코프 락 안에서 발동 — 발동 중 해제는 보류)·`CancelAbility(asset)`·`CancelAbilityHandle`·`NotifyAbilityEnded`, `OnRemoveAbility`의 TODO 주석 자리에 활성 인스턴스 `EndAbility(취소 아님)`. `GameplayAbility.cs` 멤버 순서를 규약(virtual → public → protected)대로 정리하며 기존 `Get…FromActorInfo`를 훅 뒤로 옮김.

**판단.** `bIsAbilityEnding`은 UE에서 base EndAbility 안의 BP 훅·델리게이트 재진입을 막는 용도인데, 여기선 그 사이에 외부 코드가 돌지 않아 넣지 않음(→D4).

**확인.** 컴파일(Unity 없이) 통과. 셀프체크 74/0(I 묶음 16개 추가, 의도된 에러 4줄·경고 1줄). 유저 "Test할거 Test해" → 유저의 이름 변경(`CurrentSpecHandle`→`CurrentAbilitySpecHandle`) 반영 뒤 다시 컴파일 + 셀프체크 5종(Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0·GA 74/0) 모두 기준값. PlayMode(Aggregator·Tag)는 안 돌림 — 셋업 메뉴가 유저가 고친 `AttrInit_PlayModeTest.asset`을 다시 만들고 씬을 배선하며, GA 변경과 닿지 않음.

### 2026-10-20 — ASC 부여·해제(TODO-BOARD 2-1) + 셀프체크

**요청.** 유저: "TODO에 GameplayAbility (GA) 구현 이거 세분화해" → 2-1~2-8(나눈 단위는 에이전트 제안). "ASC에 GA 부여·해제 구현하자" → "단순히 구현하지 말고 UE에 맞춰서"(답: ActorInfo 도입, 범위 "UE대로") → 정정 "GameplayAbilitySpecContainer 단순하게 구현하지 말고 UE에 맞춰서". "테스트 직접하고".

**구현.** UE `GiveAbility`·`ClearAbility`·`ClearAllAbilities`·스코프 락(보류 부여·해제·ClearAll)·`InitAbilityActorInfo` 구조를 따름. 새 타입 `GameplayAbilityActorInfo`·`ScopedAbilityListLock`·`ConsiderPending`. Spec 생성자가 핸들 발급, 핸들은 id만. 컨테이너는 UE처럼 `Items`·`Owner`·`RegisterWithOwner`, 해제는 인덱스 순회 + RemoveAtSwap. 뺀 것: 네트워크·AbilityTriggers·GE가 부여한 어빌리티·인스턴싱 정책 선택. 활성 중 해제 시 `EndAbility`는 2-2로(TODO 주석).

**확인.** 컴파일(Unity 없이) 통과. 셀프체크 `Tools/Ability System/Run Gameplay Ability Self-Check` 58/0(Unity MCP 메뉴 실행, 의도된 에러 로그 4줄). 회귀 Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0. 인스펙터 화면·Play 모드는 안 봄.

### 2026-10-20 — 정의/런타임 분리(→D1) + SerializeReference 원형·깊은 복사(→D3)

**요청.** 유저: "GA는 이벤트 함수, field를 직접 사용하는게 중요" → SO 상속 시도(생성 창 포함) → SO를 객체마다 Instantiate하는 방식 웹 조사 → "Runtime Class 별도로 두는거로 가고, SO Asset은 단일로 가자"(→D1, 처음엔 타입만 저장 →D2). 이후 "ability Class SubClassSelector 말고 SerializeReference로 하자 하위 field draw하고 저장할 필요가 있겠어"(→D3, D2 번복), 복제는 "직렬화 규칙 따라 깊은 복사" 선택.

**구현.** `GameplayAbilityAsset`(sealed SO) + `GameplayAbility`([Serializable] abstract C#). 에셋 `abilityTemplate`은 `[SerializeReference][SubclassSelector]`. `Core.Common.SerializedDeepCopy`(Unity 직렬화 규칙 깊은 복사 — UnityEngine.Object·엔진 클래스는 참조 공유, SerializeReference 공유 관계 유지). `SubclassSelectorDrawer`가 SerializeReference 단일 필드면 하위 필드까지 그림. SO 상속 시도분·생성 창(`NewGameplayAbilityWindow`)은 삭제. `Test.TestGameplayAbility`는 런타임 하위 클래스로.

**경위.** 예전 GA 골격(2026-09-13 stash 복원본)은 유실 확인 뒤 같은 날 "이전 골격만"으로 다시 만들고 그 위에서 진행.
