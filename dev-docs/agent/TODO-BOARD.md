# TODO-BOARD — 작업 현황 보드

> feature 단위 추적은 `feature-list.md`가 담당한다. **이 보드는 그보다 잘게 쪼개진 잡다한 작업·개인 TODO·이슈**를 모은다 — 문서 정비, 인프라(GitHub 설정 등), 실험, 리팩터, 아이디어 등 feature로 묶기 애매한 것들.
> 관리 규약은 `TODO-BOARD.harness.md`. 항목은 상태가 바뀌면 아래 칸 사이를 이동한다.

- 최종 갱신: 2026-10-20 (KST)

---

## 📋 TODO (데모 전 GAS 구현 — 2026-10-20)

> 2026-10-20 유저 "아까 말한 목표로 데모 구현 전까지 필요한 GAS 구현 TODO로 전부 작성해. 당장 세부적인게 어려우면 크게 잡아서 작성해둬". 목표(같은 날 유저): AI로 빠르게 만드는 포트폴리오 데모 — "기능을 시각적으로 보여줄 용도", "시각 효과, 짧은 영상이 필요해", "1~2일 안에".
> 데모 구성은 에이전트 제안(유저 미확정): 방 하나·플레이어 1·허수아비 적 1, 스킬 화염구(도트뎀)·스턴·가속 버프(+정화), HUD·디버그 오버레이, 스킬별 이펙트, 30~60초 영상.
> 기존 보드 항목은 본문 그대로 옮겼다(원래 칸: 'GE·Tag 남은 것'·'이전 우선순위 낮음'). 새 항목은 2026-10-20 등록. 순서는 위에서부터(에이전트 제안).
> 2026-10-20 유저 "TODO에서 데모용으로 필요한 부분은 싹 데모쪽으로 빼두고" → 게임 쪽 구현·데이터·연출·데모 결정은 아래 '📋 TODO (데모 제작)'으로 옮겼다. 이 칸엔 GAS 자체만 남는다.

### 0. 착수 전 결정

- [ ] **(decision)** **GA 작업 방식** — gameplay-ability `HARNESS.md`는 "유저 이해가 구현에 선행"·착수는 유저 신호. 데모 일정상 AI가 최소 GA를 바로 구현할지(이번만 그 원칙을 접음), GA 없이 입력→GE 지름길로 갈지. 에이전트 추천: 최소 GA. _(2026-10-20 등록)_

### 1. Attribute — 값 변화 알림·범위

(없음 — 2026-10-20 값 변경 이벤트·`PreAttributeBaseChange` ref·AttributeSet 훅 완료(✅ Done, PR #8), 사망 처리는 데모 미결로 옮김)

### 2. GameplayAbility (최소)

- [ ] GameplayAbility (GA) 구현
  - **2026-10-20 범위(에이전트 제안, 유저 미확정):** ① `GameplayAbilityAsset`(SO: 코스트 GE·쿨다운 GE·태그·효과 GE) ② 데모용 즉발형 `ActivateAbility`→`CommitAbility`→`EndAbility`(UE `GameplayAbility.cpp:424`·`:559`·`:769`) ③ `GameplayAbilitySpec`·`Handle` ④ ASC `GiveAbility`·`TryActivateAbility`·`CancelAbility` ⑤ 쿨다운 판정 = 쿨다운 GE의 Granted Tags를 갖고 있으면 실패(UE `:1050`·`:1197`) + HUD용 남은 쿨다운 조회 ⑥ 코스트 판정 = ASC `CanApplyAttributeModifiers`(새로 필요, UE `:1092`) ⑦ 태그 조건 최소 `ActivationBlockedTags`(스턴 중 막기)·`ActivationOwnedTags`(UE 전체 목록 `GameplayAbility.h:756-791`). ⑧ 입력 바인딩은 '데모 제작'으로 옮김(2026-10-20). 빼는 것: 인스턴싱 정책·네트워크·AbilityTask·몽타주·타겟팅·이벤트 발동.
  - 문서 정리 필요: gameplay-ability requirements·progress가 "골격 복원함(2026-09-13)"이라 적었지만 작업 트리·stash에 GA 코드가 없다(2026-10-20 확인).

### 3. GameplayCue (최소)

- [ ] Gameplay Cue 구현
  - **2026-10-20 범위(에이전트 제안, 유저 미확정):** ① `GameplayCueEvent`(`OnActive`·`WhileActive`·`Executed`·`Removed`, UE `GameplayEffectTypes.h:958`) ② GE 에셋 Cue 태그 목록(UE `GameplayEffect.h:2299`, 레벨 범위·MagnitudeAttribute 뺌) ③ 발생 지점: Executed=`ExecuteActiveEffectsFrom` 끝(UE `GameplayEffect.cpp:3059`), OnActive·WhileActive=`AddActiveGameplayEffectGrantedTagsAndModifiers`(`:4434-4435`), Removed=`RemoveActiveGameplayEffectGrantedTagsAndModifiers`(`:4734`) ④ Cue 매니저 + 태그→프리팹 표(SO) ⑤ 프리팹 쪽 기본 컴포넌트(DOTween 연출 자리) ⑥ GE 인스펙터 Cue 칸. 빼는 것: 노티파이 클래스 계층·태그 계층 상속 조회·풀링·네트워크. 시간이 모자라면 태그 이벤트로 연출을 직접 붙이는 쪽으로 내려간다.

### 데모에 안 쓰는 것 (참고)

- SetByCaller·Custom Calculation·Transient·GE 스택·Conditional GE·Gameplay Event(적 반격은 데모에서 뺌, 넣으면 필요)·GE 태그 부가 기능 — 'GE·Tag 남은 것'에 그대로 둔다.

## 📋 TODO (데모 제작 — 2026-10-20)

> 2026-10-20 유저 "TODO에서 데모용으로 필요한 부분은 싹 데모쪽으로 빼두고" — 데모(게임) 쪽 결정·구현·데이터·연출을 한곳에 모았다. 항목 본문은 옮기기 전 그대로(원래 칸을 각 항목에 적음). GAS 자체 작업은 위 '데모 전 GAS 구현'.

### 결정 (데모 착수 시)

- [ ] **(decision)** **데모 구성·범위 확정** — 위 제안 구성으로 갈지, 스킬 구성, 아트(기본 도형+조명·블룸 / 에셋), 이펙트 출처(직접 파티클 / Asset Store 팩), 녹화(Unity Recorder 설치 / OBS·게임 바). 2026-10-20 대화에서 물음, 답 대기. _(2026-10-20 등록)_
  - **2026-10-20 유저 확정분:** 캐릭터 = 무료 에셋 + 무료 애니메이션(걷기·스킬 모션만으로 충분) · 스킬 = 스킬 공격, 방어 마법 "등" · 이펙트 = "진짜 싸구려도 괜찮" · 녹화 = 유저가 직접. **남은 것(에이전트 정리, 유저 답 대기):** 스킬 목록 확정, 방어 마법 효과 방식, 방어를 보여 줄 적의 공격, 시점·조작, HUD 수준, 영상 시나리오. 근거 미기록(유저 확정 전).
  - **2026-10-20 유저 — 데모의 핵심:** "GAS가 적용되어 캐릭터의 Tag 변화와 각 투사체, 스킬, 디버프, 버프 구간 등을 확실하게 보여주는거". → 보유 태그·걸린 GE·남은 시간을 화면에 띄우는 표시(디버그 오버레이 등)가 부가 기능이 아니라 데모의 중심이 된다(에이전트 해석, 유저 확인 전).
  - **2026-10-20 유저 — 적 공격:** "진짜 단순하게 멈춰두고 범위 안에 들어오면 GA 발동 및 애니메이션 딱 이정도만". → 적은 제자리, 범위 진입 시 GA 발동 + 애니메이션. 이동·AI 없음.
  - **2026-10-20 유저 — 시점·조작:** 3인칭.
  - **2026-10-20 유저:** "GA의 발동, GE의 과정이 눈으로 확실하게 보이게 해야해". → 상태(지금 무엇이 걸렸나)뿐 아니라 **과정(GA 발동·실패 사유·커밋·종료, GE 적용·주기 실행·만료/제거·태그 부여/회수)** 을 화면에 보여야 한다. 이 흐름을 내보낼 런타임 이벤트가 필요 — 2026-10-20 `Core/AbilitySystem`에서 `event`·`Action<` 검색 결과 공개 이벤트는 `ActiveGameplayEffectEvents.OnEffectRemoved`(활성 GE별)·`AttributeAggregator.OnDirty`뿐(GA는 미구현).
  - **2026-10-20 유저:** "단순 이벤트 로그 뿐 아니라 게임 화면 상에도 정확하게 이벤트가 표시되어야해. 이펙트 뿐 아니라 text를 띄어서라도". → 이벤트마다 해당 캐릭터 위치에 월드 텍스트(+이펙트)로 표시. 로그는 보조.
  - **2026-10-20 유저 — 방어 마법·플레이어 체력:** "방어 마법은 차단보다는 감소" · "조종하는 캐릭 체력은 무한" · "방어 마법의 효과로 줄어든게 발동할때마다 팅 팅 소리가 나고 이펙트가 나는게 필요". → 피해 감소형, 감소가 실제로 적용된 피격마다 소리+이펙트(사운드 에셋 필요). 감소량을 어디서 계산·통지할지는 미정(데미지 공식 미확정과 연결).
  - **2026-10-20 유저:** "되었고 더 이상 데모에 대한 얘기는 그때가서" — 나머지는 데모 착수 시 정한다. 미결: 체력 무한 방식(깎이되 안 죽음 / 안 깎임) · 감소 값 위치(신규 감소 어트리뷰트 / 태그만) · 화면 표시 연결(Cue / 이벤트 구독 / 둘 다) · 적 공격(근접 / 투사체) · 스킬 구성.
  - **사망 처리 필요 여부** (2026-10-20 유저 "데모 미결로 옮겨" — '1. Attribute'에서 옮김, GAS가 아닌 게임 규칙이라): 원래 항목 "체력 0이 되면 상태 태그(예: `State.Dead`) 부여 + 이후 GE·스킬 차단, 데모에선 허수아비 리셋. 위 Hook(`PostGameplayEffectExecute`)과 묶어서." 플레이어는 체력 무한이라 해당 없음 — 허수아비가 죽는지·리셋하는지부터 정한다. 만들면 `Core` 밖(게임 쪽). _(원 등록 2026-10-20)_
- [ ] **(decision)** **데모용 지름길 기록 방식** — "데모 한정"으로 묶어 짧게 기록해도 되는지. _(2026-10-20 등록)_
- [ ] **(design)** ⭐ **데모 씬 한 장면 확정** — 포트폴리오 방향 전환(2026-08-09, [design.md 포트폴리오 방향](../project/design.md))의 **다음 액션.** 씬을 정하면 필요한 GAS 최소 집합이 역산된다. 초안: 플레이어 도트뎀+스턴 / 자버프 / 적 피격→반격, 스탯·상태·쿨다운 UI Toolkit HUD. **확정 전 GA/Tag/… feature 폴더 신설 금지**(등록 먼저 — feature/HARNESS '폴더 구조·네이밍'). 원래 칸: '뒤로 미룸' 이전 당장 목표. 위 '데모 구성·범위 확정'과 같은 결정 — 함께 닫는다. _(2026-08-09 등록)_
  - **2026-10-20 유저:** AI로 빠르게 만드는 데모 — "포트폴리오용으로 빠르고 기능을 시각적으로 보여줄 용도로". 에이전트가 씬 구성안·순서를 제안함(대화), 씬·GA 수준·HUD·아트·기록 방식은 유저 결정 대기. 이어서 유저 "시각 효과, 짧은 영상이 필요해", "1~2일 안에 해야해".

### 게임 쪽 구현

- [ ] **(design)** **실전 전투 공식 확정** — Execution(클램프·크리티컬 등 분기)과 AttributeBased(단순 사칙연산) 역할 경계. `defense` 어트리뷰트를 안 쓰기로 했으니 데미지 계산식부터 다시 정한다. 출처: gameplay-effect progress 다음 작업 1. 원래 칸: '데모 전 GAS 구현' 2. 데미지 경로. _(2026-10-20 보드 등록)_
- [ ] **(code)** **GE 호출처 배선** — 공격 컴포넌트가 `MakeEffectContext()`(Instigator=공격자)로 target에 GE 적용. 실행 계층은 gameplay-ability와 함께. 출처: gameplay-effect progress 다음 작업 2. 원래 칸: '데모 전 GAS 구현' 2. 데미지 경로. _(2026-10-20 보드 등록)_
- [ ] **(code)** **입력 바인딩** — `PlayerController` 키 1~4로 GA 발동. 원래 'GA 구현' 범위 ⑧(에이전트 제안)에서 2026-10-20 분리. _(2026-10-20 등록)_
- [ ] **(code)** **스턴 등 상태 태그가 행동을 막게** — 이동은 `Status.Stun`이 있으면 멈추고(캐릭터 이동 코드에서 태그 확인), 스킬은 GA `ActivationBlockedTags`로. 적(허수아비)도 같은 규칙. 원래 칸: '데모 전 GAS 구현' 5. 태그 → 게임 동작. _(2026-10-20 등록)_
- [ ] **(code)** **디버그 오버레이용 조회** — 걸린 GE 목록·남은 시간·보유 태그·어트리뷰트 Base→Current를 게임 화면에서 읽을 API. 질의·시간 API(2026-10-20)와 ASC Inspector 창 로직이 있어 크게는 모아 쓰는 일. 원래 칸: '데모 전 GAS 구현' 5. 태그 → 게임 동작. _(2026-10-20 등록)_

### 데이터·연출

- [ ] **(chore)** **데모 스킬용 GE·태그 데이터** — 화염구(Instant 데미지 + 주기 도트), 스턴(Duration + `Status.Stun` 부여), 가속(Duration, 속도 배율), 정화(Remove Other 또는 태그로 제거), 코스트·쿨다운 GE. 태그는 `Status.*`·`Cooldown.*`·`GameplayCue.*`. `GE_MeleeDamage` 계수 0은 '결정 대기' 항목. 원래 칸: '데모 전 GAS 구현' 2. 데미지 경로. _(2026-10-20 등록)_
- [ ] **(ui)** **UI Toolkit HUD** — 데모 HUD(어트리뷰트 바·쿨다운·상태/Tag 아이콘)를 UI Toolkit으로. AttributeChanged(컨테이너 이벤트) 바인딩. 저비용 원칙(AI 스캐폴딩, 씨름 시 uGUI 후퇴) — 스킬 투자 아님. 원래 칸: '뒤로 미룸' 이전 우선순위 낮음. _(2026-08-09 등록)_
- [ ] **(fx)** **DOTween — Cue juice** — Cue 레이어 전용(피격 `DOPunch`·스턴 `DOShake`·버프 pulse·데미지 숫자·카메라 shake). GAS 코어엔 넣지 않고 Cue 이음새 뒤에. `SetLink`로 오브젝트 파괴 시 자동 kill. 원래 칸: '뒤로 미룸' 이전 우선순위 낮음. _(2026-08-09 등록)_

## 📋 TODO (GE·Tag 남은 것 — 2026-10-20 정리)

> 2026-10-20 유저 "그것들 TODO 문서로 해두고 기존 문서에 있던건 해당으로 옮기면서 하고" — GE·Tag 전체 회귀·코드 점검 뒤 남은 것을 한곳에 모았다. 이미 보드에 있던 항목은 본문 그대로 옮겼고(원래 칸: '뒤로 미룸'·'이전 당장 목표'·'이전 우선순위 낮음'), 새 항목은 2026-10-20 등록. 근거: gameplay-tag worklog 2026-10-20 '전체 회귀 + 코드상 빈 곳 점검', gameplay-effect worklog 2026-10-20.

### GE 기능 (미구현)

- [ ] **(code)** GE **SetByCaller** 구현 — 유저 도입 확정(→gameplay-effect D20(spec API+clone 유지)). 런타임 계산값(차징·콤보·RNG 등 **어트리뷰트가 아닌 값**)을 spec에 태그 키로 꽂아 적용. spec 직접생성 API(`MakeOutgoingSpec`)+`Clone()`의 실사용 소비처 — 이 기능 때문에 asset-only 축소를 반려하고 spec-API·clone을 유지했다(D20(spec API+clone 유지)). AttributeBased(어트리뷰트 유도)로 표현 안 되는 magnitude를 다룬다. 2026-10-19 `MagnitudeCalculationType.SetByCaller` 자리만 생김(인스펙터 "구현 예정", 런타임 계산 실패 → 0 + 경고). _(2026-08-04 등록 — class/struct·clone 설계 논의 중 확정)_
- [ ] **(code)** GE **Custom Calculation Class** magnitude 구현 — UE `UGameplayModMagnitudeCalculation`(`EGameplayEffectMagnitudeCalculation::CustomCalculationClass`). 2026-10-19 enum 자리만 생김(인스펙터 "구현 예정", 런타임 계산 실패 → 0 + 경고). _(2026-10-19 등록)_
- [ ] **(code)** GE Execution **Transient(임시 변수) 집계기** — 캡처 어트리뷰트 쪽 런타임 적용(①)은 2026-10-20 완료(gameplay-effect worklog), 남은 건 ②. 아래는 등록 당시 본문. 편집기·데이터는 2026-10-19(→gameplay-effect D23). ① Execution 파라미터를 만들 때 Calculation Modifier로 집계기 두 묶음: 캡처 어트리뷰트 → 그 aggregator 사본 + mod(실제 값 불변), Transient 태그 → 기본값 0 새 aggregator + mod(UE `GameplayEffectExecutionCalculation.cpp:26-57`). ② Transient: Execution이 `Defs()`처럼 virtual 함수로 태그 선언 → 편집기 Backing Data 선택지에 추가, Calculation Modifier에 `aggregatorType`·`transientAggregatorIdentifier` 필드, Execute는 `AttemptCalculateTransientAggregatorMagnitude`로 읽음(mod 없으면 false → 기본값은 Execute가). 쓰임: 같은 Execution을 여러 GE가 쓰면서 GE별 보정값(+20·×1.5)을 줄 때. 코드 TODO 주석: `GameplayEffectExecution`·`GameplayEffectExecutionScopedModifierInfo`·`GameplayEffectExecutionParameters`. 구현하면 `CalculationModifierSelfCheck` E-2(지금은 "런타임이 아직 안 씀"을 고정) 기대값을 바꾼다. 착수 전 확인: `AttributeAggregator` 단독 생성 가능 여부, UE가 mod 없는 캡처도 사본에 넣는지(`:63` 생성자). _(2026-10-19 등록 — 유저 "TODO로 두자")_
- [ ] **(code)** GE Execution **파라미터 조회 API 나머지** — UE `FGameplayEffectCustomExecutionParameters` 중 없는 것. 빈 시그니처(`NotImplementedException`)+TODO: `AttemptCalculateCapturedAttributeMagnitudeWithBase`(aggregator `EvaluateWithBase(base, 평가 파라미터)` 선행)·`AttemptCalculateCapturedAttributeBonusMagnitude`(UE `EvaluateBonus` 선행)·`AttemptGetCapturedAttributeAggregatorSnapshot`. TODO 주석만: `AttemptGatherAttributeMods`·`ForEachQualifiedAttributeMod` — aggregator `Mod`가 private이라 시그니처 불가, 공개 방식 결정 필요. 모두 Calculation Modifier 사본을 먼저 읽어야 한다. 위치: `GameplayEffectExecutionParameters.cs`. _(2026-10-20 등록 — 유저 "빠진것들 TODO로 빈 시그니처로만 만들어둬")_
- [ ] GE 스택 구현
- [ ] **(code)** **ScalableFloat 레벨 커브** — magnitude가 지금은 고정 float. Level 기반 커브 테이블로(UE `FScalableFloat`). 코드 TODO `GameplayEffectModifierMagnitude.cs:14`. _(2026-10-20 등록)_
- [ ] **(code)** **Conditional GE** (신규) — GE 적용 시 Source 태그 조건이 맞으면 다른 GE를 따라 적용한다. UE `FConditionalGameplayEffect::RequiredSourceTags`(5.6 `GameplayEffect.h:495`). 우리 쪽엔 기능 자체가 없다. 선행: gameplay-tag의 GE 태그 연동. _(2026-10-16 등록 — 태그 후보 조사)_
- [ ] **(code)** **GE 태그 부가 기능** — UE GE Component 중 Immunity·Chance to Apply·Additional Effects·Custom Can Apply, Spec 동적 태그(`DynamicGrantedTags`·`DynamicAssetTags`, 세부 TODO 11), AttributeBased 태그 필터(세부 TODO 9), Execution `PassedInTags`·scoped modifier 태그(세부 TODO 12). Block Ability Tags·Abilities Component는 gameplay-ability 때. _(2026-10-18 등록 — 목표 범위 정리 중 제외)_
- [ ] **(code)** **GE 데이터 검증(UE `IsDataValid`)** — 에디터에서 잘못된 GE 조합을 오류로 알리는 체계가 없다. 알려진 경우: Instant GE에 Granted Tags(UE `TargetTagsGameplayEffectComponent`), Instant GE에 Ongoing 태그 조건(UE `TargetTagRequirementsGameplayEffectComponent`). 할지 유저 결정 대기(gameplay-tag progress 세부 TODO 6). _(2026-10-20 등록)_

### Tag

- [ ] **(code)** **gameplay-tag D3 대소문자 무시 비교 + 표기 보존** — 결정은 됐고 구현이 없다(`InternedName`은 대소문자 구분). progress 다음 작업 5. _(2026-10-18 보드 등록)_
- [ ] **(code)** **Gameplay Event** (신규) — `HandleGameplayEvent`/`SendGameplayEventToActor`, 이벤트 기반 능력 발동(예: 피격→반격). 선행: Tag. _(2026-08-09 등록)_
- [ ] **(code)** **호출하면 예외가 나는 미구현 API** — `GameplayTag` `<`/`>`(`GameplayTag.cs:41`), `GameplayTagsManager`의 `DoneAddingNativeTags`·`CallOrRegister_OnDoneAddingNativeTagsDelegate`·`SplitGameplayTagFName`·`RequestAllGameplayTags`(`:265-287`)가 `NotImplementedException`. `AddChildrenTags`는 `onlyIncludeDictionaryTags: true`면 예외(`:415` — 노드에 명시/암시 기록이 없어서). 지금 호출하는 곳은 없다. _(2026-10-20 등록)_
- [ ] **(editor)** **ASC 인스펙터에 소유 태그 표시** — gameplay-tag 수용 기준 "인스펙터에서 확인 가능"이 아직 안 됨(progress 세부 TODO 7 남은 것). _(2026-10-20 등록)_
- [ ] **(code)** **Tag 코어 후속(미룸)** — IL2CPP 코드 제거 시 마커 필드 유실 대응(`[Preserve]` 등), 빌드 탐색 비용 측정, 노드 출처·명시/암시 기록. D5의 막는 이유 유저 확인. 출처: gameplay-tag progress 다음 작업 1. _(2026-10-20 보드 등록)_
  - **2026-10-20 확인:** 원래 있던 "레지스트리 편집 시 에디터 매니저 재구성"은 이미 됨 — 관리 창의 추가·이름 변경·삭제가 `GameplayTagRegistryEditing`에서 `EditorRefreshGameplayTagTree`를 부른다(tests.md §4-1·4-2에서 바로 반영 확인).

### 검증

- [ ] **(verify)** GE 캡처 **end-to-end 유저 검증** — 5b evaluate로 소비처가 생겼다(`GameplayModifier.EvaluateMagnitude` → `AttributeBasedMagnitude.Evaluate`가 캡처값 조회). 이제 **유저가 에디터에서 Source·snapshot·evaluate 경로가 실제로 맞게 캡처·계산하는지 처음 확인**한다 — `GE_EquipSpeedDown`이 Source `CombatAttributeSet.damage` 기반으로 speed를 깎는지. ⚠ 선행: `Player_AttributeInitData`에 `CombatAttributeSet` 존재(없으면 캡처 무효→경고+0). (`SpeedBoostExecution.Execute`는 여전히 +10 고정 — Execute 경로의 캡처 소비는 별도.) **검증 절차·항목 = [gameplay-effect/tests.md](feature/ability-system/gameplay-effect/tests.md).** _(2026-08-04 갱신 — 5b로 소비처 배선됨, 검증만 남음)_
- [ ] **(verify/setup)** **BoxRoom 검증 세팅 완성** — 디버그 도구는 만들어짐(2026-08-05, `BoxRoom.cs`+`.prefab`, ASC Inspector 창). 남은 세팅: ① 프리팹의 BoxRoom이 **Missing Script면** `m_Script` guid를 실제 값으로 교체(에이전트가 `.cs.meta` GUID `3a9f2b7c…` 발급). ② 3모드 GE 슬롯 꽂기 — 특히 **Buff에 `GE_EquipSpeedDown`**(Source damage 기반 speed 감소) → 5b evaluate end-to-end 검증과 직결. ③ **Room ASC `attributeInitData`**(Source 캡처 테스트 시) + **Player ASC에 `CombatAttributeSet`**(없으면 캡처 무효→경고+0). ④ 씬 배치·시각화(선택). 검증 항목 = [gameplay-effect/tests.md](feature/ability-system/gameplay-effect/tests.md). _(2026-08-05 등록)_
  - **2026-10-20 확인:** ① 해소 — 프리팹의 `m_Script` guid가 `BoxRoom.cs.meta`(`3a9f2b7c…`)와 같다. ②③은 프리팹 구조가 바뀌어(`otherEffect`·`selfEffect`·`sourceAttributes`) 판단 못 함.

### 결정 대기

- [ ] **(decision)** **Execution 파라미터 생성자 순서** — 지금 `(target, spec, scopedMods)`, UE는 `(Spec, ScopedMods, Target, PassedInTags, PredictionKey)`. UE 순서로 바꿀지(호출처 1곳). _(2026-10-20 등록)_
- [ ] **(decision)** **gameplay-tag 결정 대기 3개** — ① 경량 경계(레지스트리 유무·형태, 등록 시점 — D3 대소문자와 묶임) ② GE 연동 범위 남은 ②④(GE 범위 / 요구 조건 TagQuery 포함 여부) ③ `RequestGameplayTag` 입력 ergonomics. 상세: gameplay-tag progress '결정 대기'. _(2026-10-20 보드 등록)_
- [ ] **(decision)** **Query 내부 표현** — UE대로 토큰 스트림(D26). 식 트리로 바꿀지 유저 미확정(바꾸면 저장된 질의를 옮겨야 함). 출처: gameplay-tag progress 다음 작업 7. _(2026-10-20 보드 등록)_
- [ ] **(design)** GE 캡처 **사양 확정 2건 (소비처 배선 전에 정해야 함)** — 2026-08-04 설계 감사에서 도출. ① **"spec 생성 시점 = 시전 시점"이 이 프로젝트 의도인가** — Source를 `GameplayEffectSpec` ctor에서 캡처(`CaptureDataFromSource`)하는 전제다. `MakeOutgoingSpec`을 시전보다 훨씬 먼저 만들어 들고 있다가 적용하는 사용을 허용할 거면 ctor 캡처가 어긋난다(snapshot이 시전이 아닌 생성 시점에 얼어붙음). 코드·UE가 아니라 **유저 사양**으로 확정 필요. ② 확정되면 그 근거를 gameplay-effect `decisions.md`에 D로 남긴다(HARNESS '왜는 지어내지 않는다' — 지어내지 말고 유저 답을 적음). _(2026-08-04 등록)_
- [ ] **(chore)** **기존 GE 에셋 coefficient 0 점검** — `OnAddModifier` 버그(add 시 coefficient 0)로 이 수정 **이전에** 만든 AttributeBased modifier는 0으로 저장돼 있을 수 있음(식 전체가 0이 됨). 새 수정은 신규 추가분만 1로. 기존 에셋(`GE_EquipSpeedDown` 등)은 인스펙터에서 수동 확인·1로. _(2026-08-05 등록)_
  - **2026-10-20 확인:** `Assets/Data/GE_MeleeDamage.asset`이 AttributeBased(`magnitudeCalculationType: 1`, Source `stamina`)인데 `coefficient: 0` → magnitude가 늘 0. 의도인지 유저 확인 필요(고치지 않음). `GE_EquipSpeedDown`의 `coefficient: 0`(`:78`)은 ScalableFloat modifier라 쓰이지 않는다.

### 정리 작업

- [ ] **(code)** GE 캡처 **params 조회 함정 3종 (5b·Execute 작성 시 주의)** — 캡처를 실제로 읽는 코드를 쓸 때 걸리기 쉬운 지점. ① **false가 조용하다:** `AttemptCalculateCapturedAttribute*`(`GameplayEffectExecutionParameters.cs:47·54`)는 실패 사유(미등록/캡처 무효/Target 미캡처)를 전부 `false`+`0`으로 뭉개고 **경고 로그 없음** — 호출측이 bool을 안 보면 0이 계산식에 샌다. ② **값-동등성 key 취약:** 조회는 `BackingDefinition.Equals`(captureSource+attribute AQN+snapshot)라 **셋 중 하나만 달라도 매치 실패→false**. 조회 정의는 선언 정의와 완전히 같아야 함(SpeedBoost처럼 같은 static 필드 재사용이 안전). ③ **captureValueType 라우팅:** params 메서드가 Magnitude=Current / BaseValue=Base로 고정 분기인데 authoring의 `AttributeBasedMagnitude.captureValueType`(`:33`)가 Base/Current를 데이터로 지정 — 5b/Execute가 이 값에 맞춰 올바른 메서드를 호출해야 authoring 의도와 맞는다. _(2026-08-04 등록 — 캡처 조회 경로 분석 중 발견)_
- [ ] **(docs)** **attribute 런타임 타입 이름 정렬 + D5(직렬화/런타임 참조 분리) 재작성** — 코드는 `GameplayAttributeHandle`인데 2026-08-21 세션에 내가 적은 문서·주석 일부가 아직 `ResolvedGameplayAttribute`다(그 이름으로 확정했다가 유저가 되돌림). 정정 대상: attribute `decisions.md`(D5(직렬화/런타임 참조 분리))·`progress.md`, architecture `overview`·`attribute`·`gameplay-effect`, 잔재 2건(`AttributeAggregator.cs` 주석 `Dictionary<ResolvedGameplayAttribute,…>`·`GameplayEffectSpec.cs` 경고 문자열). **D5(직렬화/런타임 참조 분리)는 근거 자체 재작성 필요** — "Handle이 부정확"이라 `Resolved-`를 택한 논리가 최종 `GameplayAttributeHandle`(Handle 유지)과 모순. 유저에게 "왜 Handle로 되돌렸나" 받아 그 근거로 다시 쓴다(HARNESS '왜는 지어내지 않는다'). _(2026-08-21 등록)_
  - **2026-10-18:** decisions 정리로 attribute D5는 "치운 결정"으로 내려갔다(이름은 `GameplayAttributeHandle`로 되돌아갔고 분리는 D1·gameplay-effect D15). D5 재작성은 더 필요 없고, 남은 일은 문서·주석의 `ResolvedGameplayAttribute` 잔재 정리뿐.
- [ ] **(editor)** **GE Executions 리스트에 New Script 지원** — Attribute Sets 리스트엔 New Script가 있으나 Executions엔 없음(2026-08-07 Add만 붙임). 이유: `GameplayEffectExecution.Execute`가 abstract라 빈 서브클래스 템플릿(`: GameplayEffectExecution {}`)이 컴파일 안 됨. 붙이려면 New Script 템플릿(`SubclassScriptTemplate.BuildSource`)이 **abstract 멤버의 override 스텁 생성**을 해야 함(예: `Execute(...) => throw new NotImplementedException();`). 그 스텁 생성만 추가하면 GE Executions·다른 abstract-base 셀렉터에도 켤 수 있음. _(2026-08-07 등록)_
- [ ] **(code)** **타입 참조를 rename-safe 하게 (AttributeSet·GE Execution)** — 지금 둘 다 타입을 **AQN 문자열**로 저장(`attributeSetTypeName`·`executionTypeNames`)해, 클래스명·네임스페이스·asmdef 이동 중 뭐든 하면 `Type.GetType` 실패→"Missing"으로 링크가 끊긴다. **지금은 미룸** — 규모 작고(서브클래스 몇 개), 깨져도 인스펙터에 보이고 검색 드롭다운에서 다시 고르면 됨. 고치는 값이 큼(포맷·리졸버·드로어·런타임·New Script 자동배정·기존 에셋 마이그레이션 전부)이라 과설계에 가깝다. **재평가 트리거:** rename이 잦아져 여러 에셋을 반복해 다시 잇게 되면. **하게 되면:** 안정 GUID id(`[StableTypeId(guid)]` + id→Type 리졸버, AQN 폴백 가능) — 둘 다 문자열 저장이라 통일 적용됨. (`[MovedFrom]`은 커스텀 `Type.GetType` 방식엔 안 먹혀 탈락, SerializeReference는 GE만 가능.) _(2026-08-07 등록 — New Script 툴 논의 중 도출)_

## 📋 TODO (뒤로 미룸)

> 2026-10-18 목표 전환으로 미뤘다. 항목 본문은 옮기기 전 그대로다.

> **2026-10-18 목표에서 미룬 것** (유저 2026-10-18: "GE 관련으로는 나중으로 넘길거고 대소문자는 나중에 넘기자"). GE·Tag 항목은 2026-10-20 위 'GE·Tag 남은 것'으로 옮겼다.

- [ ] **(idea)** **게임 쪽 태그 적용** — ① `InputModule`의 입력 enum(`GameplayInputAction`·`ItemInputAction`)을 InputTag로 바꿀지(Lyra 패턴, 에이전트 기억이며 원문 미확인) ② 아이템 종류·진영·데미지 타입 분류 태그. 적용 여부는 유저 미정. _(2026-10-16 등록 — 태그 후보 조사)_

> **이전 당장 목표·진행 중이던 것**

- [ ] Item Instance - Item Behaviour, Socket에 Item prefab 장착까지
- [ ] **(design)** ⭐ **기획안 확정 (선행 — 아이템 설계보다 먼저)** — 아이템 구조를 못 정하는 이유가 결국 **기획이 안 정해져서**임이 드러났다(2026-07-13). 전투가 콤보·탄약·재장전·패링을 실제로 요구하는지에 따라 구조 결론이 뒤집힌다(design-draft의 미결 D-1). `dev-docs/project/design.md`를 전투 중심으로 확실히 세운 뒤 아이템 설계로 돌아온다. _(2026-07-13 등록)_
- [ ] **(design)** 아이템 시스템 신규 설계 — **기획안 확정 대기로 보류.** 유저와 설계 논의 중, 결론 전 구현 금지. 아래 유저 의도가 목적:
  - 어떤 캐릭터든 아이템 장착에 따라 **알아서** 작동 (캐릭터 특화 없이)
  - 아이템에 종속되는 로직이 아이템 외부에 작성되지 않을 것
  - Gun·Melee 등 종류별 로직이 들어갈 자리가 있되, 아이템별 클래스 분리·상속을 강제하지 않을 것 (전략 패턴 방향은 유저 언급)
  - 아이템을 구현할 때 알아야 하는 것(컨텍스트, 외부 접근 방법, 시스템 전체 이해)이 복잡하지 않을 것 — 복잡도 대비 이득 없는 과설계 배제
  - 표현부(ItemBehaviour·GameplayCue 활용 포함)의 역할과 Combat 로직과의 연동 방식은 재검토 대상
  - **진행(2026-07-12):** 규약 백지화 완료(이전 INV·D·로드맵 → `feature/item-system/archive/`). 새 판단 기준 4축 채택(feature/HARNESS '폴더 구조·네이밍', Nystrom Type Object vs Subclass Sandbox).
  - **진행(2026-07-13):** 설계도 백지에서 재작성 → [`feature/item-system/design-draft.md`](feature/item-system/design-draft.md)(확정 아님). `ItemModule`·`ItemInstance`·"모듈 조합"은 **권위 없음**으로 강등하고, 그렇게 서술하던 project 문서(architecture/item-equipment.md·overview.md, design.md)를 **전부 미정 처리**. 후보 3안(A: 무기=Runtime 서브클래스 / B: 현행 시스템이 알고리즘 보유 / C: 어빌리티) — GAS엔 GameplayAbility가 없음(코드 확인). **결론은 기획안(위 ⭐)이 서야 남** — 콤보/탄약/패링 채택 여부가 A vs B를 가름. _(2026-07-10 등록, 07-13 갱신)_

> **이전 우선순위 낮음** (아래 GAS 대분류·기술은 포트폴리오 방향 전환(2026-08-09, [design.md 포트폴리오 방향](../project/design.md))의 목표 기능이다. 데모 씬 확정 후 필요분만 착수·feature 등록한다. 각 기능은 UE-complete가 아니라 "최소한 진짜 도는 버전".)

- [ ] **(infra)** **Ability System 플러그인화(asmdef/UPM 패키지)** — 유저 의도: "애초에 Ability System은 별개의 플러그인처럼". 2026-10-12 논의 후 **유저가 미룸**. 에이전트 제안(미확정): `Packages/com.<scope>.abilitysystem` embedded package(개발·설치 경로 동일). 정할 것: 패키지 이름(scope), feature-local `Editor/` 폴더 처리(asmdef 아래선 Editor 폴더 규칙이 안 먹힘 → 표준 Runtime/Editor 구조 vs `.asmref`), `Core.Common` 처리(패키지 안으로 vs 별도 common 패키지), 브랜치. 확인한 사실: 에셋에 `Core.AbilitySystem…, Assembly-CSharp` AQN 저장 0건, `Common`→AbilitySystem 코드 의존 없음. 착수 시 gameplay-tag D5(생성 차단=Request 입구+internal 생성 함수)의 `GameplayTag.Create`가 실제로 막히는지 확인. _(2026-10-12 등록)_

- [ ] **(editor, bonus)** **에셋 그래프(노드) 에디터** — 시각적 payoff 크나 GraphView 직렬화 rabbit hole. **맨 마지막 선택적 보너스.** 1~2일 스파이크로 리스크 확인 후, 가장 좁은 조각(GE 하나 저작→SO)으로만. 데모 성공을 여기 걸지 않음. _(2026-08-09 등록)_

- [ ] **(chore)** **아트 에셋 폴더 관례 적용** — 관례 확정(2026-08-05, 유저): **오브젝트 단위 co-locate** `Assets/Art/<모델>/`에 mesh·texture·material 다 같이(모델 통째 임포트에 강함), **여러 곳 공유하는 것만** `Assets/Art/_Shared/`. 미실행 — 필요 시 폴더 생성 + 루트에 뜬 `Lit.mat`을 공용이면 `_Shared/`로 이관(Room 전용이면 `Art/Room/`에 새로). _(2026-08-05 등록)_
- [ ] **(docs)** **UE Execution 실행조건 원문 확정분 반영 여부** — 2026-08-05 ylyking UE 미러 원문으로 "period 0 Duration/Infinite에선 Execution 안 돎"을 확정(근거: `ExecuteGameplayEffect` asc.cpp:820 `check(...)` assert + Execute 경로 라인). 이걸 `architecture/ability-system/gameplay-effect.md`나 gameplay-effect `decisions.md`에 근거(파일·라인)와 함께 남길지 **유저 판단 대기**. 위 19번(GameplayEffectType 실전 상태 확정)과 인접하나 별개(enum 확정 ≠ Execution 실행조건). _(2026-08-05 등록)_
- [ ] Inventory/Equipment System 구현
- [ ] **(wiki)** 설계 문서 상호 연결 — 각 섹션 요약에서 `dev-docs/project/architecture/*` 심화 문서로 링크. 로컬/Pages에선 `.md`가 raw로 뜨므로 **GitHub blob URL** 사용. 저장소 public 전제. `SECTION_META.sources`도 같은 방식으로 클릭 가능하게. _(2026-07-05 등록)_
- [ ] **(docs/refactor)** **에이전트 생성 "왜"의 유저 소유화 — 이번 세션(2026-08-04) 산출물 정리** — 유저 지적: 코드 주석·decisions(특히 D21(ModifierSpec UE 정렬))에 에이전트가 만든 근거가 많고 본인 생각이 반영 안 됨(HARNESS '왜는 지어내지 않는다'/'왜의 근거는 유저 의도 → UE 원본'). 방침: **코드 주석은 사실(무엇/어떻게+함정)만 lean하게, "왜"는 decisions에만·유저가 소유한 것만.** 접근 미정 — ① `GameplayModifierSpec` 시범 후 세션 파일(`AttributeBasedMagnitude`·`GameplayModifier`·`GameplayModifierSpec`·`GameplayEffectSpec`) 톤 일괄 적용(추천) ② D21(ModifierSpec UE 정렬)부터 같이 훑으며 근거 재작성/삭제. 하네스 규칙화(주석 lean·왜는 유저 소유)도 유저 승인 시 docs.harness '각색은 쓰기 전에 묻는다'/HARNESS '왜는 지어내지 않는다'에 추가. _(2026-08-04 등록)_
- [ ] **(code)** **Player 이동 구동 방식 교체** — 현재 `PlayerCharacter.MoveDelta`가 `transform.position`에 직접 대입해 **충돌을 전부 무시한다**(벽 통과, 중력 없음). `CharacterController.Move()`로 교체하면 충돌·계단(`stepOffset`)·경사(`slopeLimit`)가 해결된다. 중력은 별도로 y속도 누적 + `isGrounded` 필요(`Move`는 중력을 넣지 않음). 교체 후 `transform.position` 직접 대입과 혼용 금지(컨트롤러 내부 상태와 어긋남).
  - **부수 정리:** Player 루트에 `CapsuleCollider`·`CharacterController`·`NavMeshAgent`가 붙어 있었으나 **코드 참조 0건**(전부 미사용, 첫 커밋 `c49afa1` 프로토타입부터 존재).
    - ✅ **`CapsuleCollider` 제거 완료(2026-07-20, 유저)** — `CharacterController`가 `Collider` 상속이라 형상까지 중복(둘 다 r0.5/h2/center0)이었고 **트리거 콜백 2회의 원인**이었다. 해소됨.
    - 남음: `NavMeshAgent`는 `MonsterCharacter`만 사용하므로 Player에선 불필요. `CharacterController`는 아직 미사용(위 교체 시 사용하게 됨).
  - 필드명 주의: `CharacterBase._controller`는 possess 하는 `ControllerBase`라 이름이 겹친다.
  - 관련: 이동 모드가 상태로 갈리는 장르(구르기·공격 중 이동·피격 경직)라 GA 착수 시 "어빌리티가 이동을 어떻게 점유하는가" 경계와 함께 봐야 한다. _(2026-07-20 등록 — GE 픽업 트리거 2회 원인 추적 중 발견)_
- [ ] **(code)** `ItemPickup.OnTriggerEnter` **중복 적용 버그** — `GameplayEffectPickup`과 동일 원인(콜라이더 단위 콜백 + `Destroy` 프레임 지연)으로 인벤토리에 아이템이 2번 들어간다. `GameplayEffectPickup`에는 프레임+대상 가드를 넣었으나 `ItemPickup`은 파킹 중이라 손대지 않았다. 무기 재설계 때 함께 처리. _(2026-07-20 등록)_
- [ ] **(docs)** architecture 하류 문서 **반영 기준 마커 정비** — `camera.md`·`item-equipment.md`·`controller-character.md`·`architecture/overview.md`는 마커 자체가 없어 architecture/HARNESS '최종 갱신' 마커의 낡음 판별(하류 날짜 < 상류 날짜)이 불가능하다. 실제 반영 날짜를 알 수 없어 **지어내지 않고 그대로 뒀다**(그 절 "모르는 날짜는 지어내지 않는다"). 채우려면 **유저가 날짜를 알려주거나, 다음에 그 문서를 실제로 갱신할 때 그날 날짜로 stamp**한다. 더불어 `ability-system/overview.md`는 상류를 feature-id가 아닌 **그룹명**(`ability-system`)으로 적어, 어느 progress와 비교할지 모호하다(그룹 대표 규칙을 정하든 하위 3개 중 하나로 바꾸든 결정 필요). _(2026-07-19 등록 — 하네스 점검 중 발견)_
- [ ] **(docs)** architecture 문서 **UE 구조 미러링 분류 검토** — UE 모듈/타입 계층(`GameplayAbilities` → `U*` → `F*`)을 참고한 폴더·네이밍 분류. 억지로 서두르지 않음. _(상위/하위 계층 정리·Capture 전용 문서(`capture.md`) 분리는 2026-09-10 완료.)_ 상세 `architecture/HARNESS.md` '문서 구조'. _(2026-09-10 등록 — 유저 발의)_
- [ ] **(docs)** **§번호 제거 잔여분** — 이번(2026-08-22) 스윕에서 **의도적으로 남긴 것**: ① ✅ Done 로그의 과거 § 기록(동결 이력 — 당시 섹션번호를 서술한 기록이라 §9로 보존) ② `ue-reference.md` 자체 섹션 번호(§2·§9·§11 — 자기완결 UE 참고 자료, decisions/worklog가 "참고문서 §N"으로 참조) ③ `wiki/HARNESS.md` 자체 번호(§2·§2.1). ①은 이력이라 그대로 두는 게 맞고, ②③은 해당 문서 자체를 이름 기반으로 개편할지 유저 판단 필요. _(2026-08-22 등록)_

## 🔧 진행 중

(없음)

## ✅ Done

- [x] **(code)** **AttributeSet Hook** (신규) — `PreAttributeChange`(클램핑)·`PostGameplayEffectExecute`(파생·후처리)·meta attribute(Damage 등). 원래 칸: '데모 전 GAS 구현' 1. Attribute. → 훅 4개(`Pre/PostGameplayEffectExecute`·`Pre/PostAttributeChange`) + 컨테이너 `InternalExecuteMod`, Current 조회를 저장값으로(UE `GetNumericAttribute`), `TestAtrributeSet`으로 셀프체크 K. 경위: attribute worklog 같은 날. _(2026-08-09 등록, 2026-10-20 완료)_

- [x] **(bug)** **`AttributeSet.PreAttributeBaseChange`로 값을 고칠 수 없음** — `newValue`를 값으로 받는다(`AttributeSet.cs:13`). UE는 `float& NewValue`(`AttributeSet.h:229`)라 이 훅에서 클램프할 수 있다. 원래 칸: '데모 전 GAS 구현' 1. Attribute. → 선언·호출을 `ref`로(override 없음). 경위: attribute worklog 같은 날. _(2026-10-20 등록, 2026-10-20 완료)_

- [x] **(code)** **어트리뷰트 변경 이벤트** — HUD 바·데미지 숫자용. 변경 전·후 값과 원인(어떤 GE·Instigator)을 넘긴다(UE `GetGameplayAttributeValueChangeDelegate`·`FOnAttributeChangeData`). 원래 칸: '데모 전 GAS 구현' 1. Attribute. 발화·저장 맵은 있었으나 등록 API가 없어 늘 비어 있던 것 → 구독 API(`GetGameplayAttributeValueChangeDelegate`가 이벤트 객체 반환) + 원인 `GEModData`(`GameplayEffectModCallbackData`, Spec→`Context.GetInstigator()`) + 값이 같아도 발화(UE대로). Aggregator 셀프체크 I 69/0. 경위: gameplay-effect worklog 같은 날, 결정 D26. _(2026-10-20 등록, 2026-10-20 완료)_

- [x] **(docs)** **PR 병합 규칙** — 유저 "PR 병합 규칙 적절히 만들어봐"(세션 회고에서 "PR 확인해서 해"를 묻지 않고 병합한 것을 짚은 뒤). `git-convention.md` PR 절에 '병합': 명시할 때만·모호하면 되묻기, 병합 전 확인(충돌·체크·리뷰·base), 병합 뒤 원격 브랜치 삭제(PR #1~#6 관례, 에이전트는 확인받고), 병합 뒤 로컬 갱신·미커밋 변경 보호. 이어서 유저 "웹에 검색해서 일반적인 규칙도 해줘" → '일반 원칙'(작은 PR, base 최신화 — 본문·브랜치 정리는 겹쳐서 위 규칙에 합침, 병합 방식 항목은 유저가 지움) — 저장소 현황(main 보호 없음·자동 삭제 꺼짐) `gh api`로 확인. _(2026-10-20)_
- [x] **(docs)** **데모 전 GAS 구현 TODO 작성** — 유저 "아까 말한 목표로 데모 구현 전까지 필요한 GAS 구현 TODO로 전부 작성해. 당장 세부적인게 어려우면 크게 잡아서". 맨 위 '데모 전 GAS 구현' 칸(0 결정 → 1 Attribute → 2 데미지 경로 → 3 GA → 4 Cue → 5 태그→게임 동작). 기존 6개 항목은 본문 그대로 옮김. _(2026-10-20)_
- [x] **(infra)** **feat/gameplay-tag push + PR #7 병합** — 유저 "일단 push pr하고 main으로 가", "PR 확인해서 해". 미커밋 에이전트 작업을 6개 커밋으로 나눔(섞인 파일은 hunk 단위), gh 설치·로그인(유저) 뒤 PR #7 생성 → merge commit `3d9c5bc`. 유저 미커밋 변경은 main 위에 원래 그대로 복원, 안전망 stash 2개 남김. _(2026-10-20)_
- [x] **(docs)** **TODO-BOARD 재정리** — 유저 "그것들 TODO 문서로 해두고 기존 문서에 있던건 해당으로 옮기면서 하고", "현재 목표 … 쓸모 없으니 없애고". 'GE·Tag 남은 것' 칸 신설(기존 20줄은 본문 그대로 옮김 — 스크립트로 중복·누락 0 확인, 새 항목 14개), '현재 목표' 칸 삭제(목표 문구는 gameplay-tag worklog 2026-10-18에 남음, 가리키던 progress 2곳·NOW 수정). _(2026-10-20)_
- [x] **(code)** **GE Execution Calculation Modifier 런타임 적용(캡처 어트리뷰트)** — 유저 "실제 계산 구현해둬 Transient만 미루고". UE `FGameplayEffectCustomExecutionParameters`대로 실행마다 캡처 aggregator 사본 + modifier. 셀프체크 19/0. 빠진 조회 API는 빈 시그니처+TODO(유저 "빠진것들 TODO로 빈 시그니처로만"). Transient는 'GE·Tag 남은 것'. 경위: gameplay-effect worklog 2026-10-20, D23. _(2026-10-20)_
- [x] **(verify)** **GE·Tag 전체 회귀 + 코드상 빈 곳 점검** — 유저 "구현되어있는거 전반 다 잘 작동하나? … 코드상에 빈 것도 확인하고". 셀프체크 6종 전부 기준값 그대로. 빈 곳·찾은 문제는 'GE·Tag 남은 것'으로. 경위: gameplay-tag worklog 2026-10-20. _(2026-10-20)_
> 2026-10-20 유저 "TODO에서 이미 한거 확인해서 넘겨" — 아래 7개는 코드·git·문서로 이미 끝난 것을 확인해 옮겼다(본문은 근거만 짧게).

- [x] **(code)** ⭐ **① AttributeCapture 계층 → ② Execution Calculation Modifiers** — ① 코드 완료(gameplay-effect D12~D20, `GameplayEffectAttributeCaptureSpec`), ② 편집기·데이터 완료(gameplay-effect D23). 남은 것은 별도 항목: Calculation Modifier 런타임 적용('뒤로 미룸'), 캡처 end-to-end 유저 검증. _(2026-07-20 등록, 2026-10-20 확인)_
- [x] **(code)** GE **AttributeCapture 계층** 도입 — 코드 9/9 완료(위와 같음). 유저 에디터 검증은 '캡처 end-to-end 유저 검증' 항목으로 남김. _(2026-07-19 등록, 2026-10-20 확인)_
- [x] **(code)** GE **Calculation Modifiers** 구현 — 편집기·데이터는 gameplay-effect D23(2026-10-19). 런타임 적용은 '뒤로 미룸'의 "Calculation Modifier 런타임 적용 + Transient 집계기"가 이어받음. _(2026-07-19 등록, 2026-10-20 확인)_
- [x] GE Execution 구현 — `GameplayEffectExecution`(abstract `Execute`)·`SpeedBoostExecution`·Execution 정의 목록(gameplay-effect D23). _(2026-10-20 확인)_
- [x] **(code)** `GameplayEffectType` Instant/Duration 실전 상태 확정 — enum 주석이 더는 '미구현'이 아니고(`GameplayEffectType.cs`), 경로는 gameplay-effect D4(Instant·Periodic=Base / Duration·Infinite=aggregator)와 architecture `overview.md:44-59`에 있다. Duration 경로는 Tag PlayMode 테스트(9/0)가 시간 흐름으로 돌린다. 유저가 직접 확인하기로 했던 부분은 따로 기록이 없다. _(2026-07-05 등록, 2026-10-20 확인)_
- [x] **(chore)** **미커밋 산출물 커밋 정리 (2026-08 `Feature/GE-Execution`)** — 커밋됨: `GameplayModifierSpec` 등 → `459aac3`(#3), BoxRoom·ASC 창 → `a47ecac`. 브랜치는 남아 있지 않다. _(2026-08-05 등록, 2026-10-20 확인)_
- [x] **(bug)** GE 캡처 **snapshot=false 소스 ASC 파괴 → NRE 잠복** — 구조가 바뀌어 사라짐: 캡처 Spec이 ASC가 아니라 `AttributeAggregator`(일반 C# 객체)를 들고(`GameplayEffectAttributeCaptureSpec.cs:48`, `TryGetCapturedValue` `:67-79`), `_capturedAsc`는 코드에 없다. 소스가 파괴돼도 예외는 안 나고 마지막 aggregator 값을 읽는다. _(2026-08-04 등록, 2026-10-20 확인)_
- [x] **(verify)** **중첩 struct `ISerializationCallbackReceiver` 콜백 실측** — 유저 "해봐". `GameplayTagTest.asset`의 `_tag`·`_tagList` 2개·`_tagContainer` 2개 모두, ① 강제 import ② 메모리에서 내린 뒤 디스크에서 다시 읽기 ③ 도메인 리로드 뒤에 이름 문자열이 intern된 인스턴스와 같고 `Equals(RequestGameplayTag)` true → fallback 불필요. 빌드·플레이어 로드는 안 봄. 임시 도우미(`Assets/_McpCheck/`)는 지움. 경위: gameplay-tag worklog 2026-10-20. _(2026-10-18 보드 등록, 2026-10-20 완료)_
- [x] **(decision)** **피커 입력란 버그 2건 수정 → 유지** — 유저 "이미 고쳤는데 뭐가 문젠데". 규칙("보고만") 확정 전에 고친 `GameplayTagPicker.DrawAddNewTagWidget` 수정(추가 뒤 입력란 옛 글자·Enter 뒤 입력 불가)을 그대로 둠. _(2026-10-20 등록, 같은 날 완료)_
- [x] **(decision)** **태그 추가 입력란 — `부모.` 접두어가 전체 선택됨 → 그대로 둠** — 유저 "그대로 둬 문제 없구만". Add Sub Tag·`+`(행 선택 시)로 채운 `Test.A.`가 포커스 때 전체 선택돼 바로 치면 지워짐(End 키로 피함). UE는 전체 선택 안 함 — 차이로 남김. 경위: gameplay-tag worklog 2026-10-20. _(2026-10-20 등록, 같은 날 완료)_
- [x] **(decision)** **"발견한 버그는 보고만" vs "단순 버그는 묻지 말고 고침" 충돌** — 유저 "발견 시 보고만 하는거야. 다만, 단순 버그는 거기서 얘기를 해줘." `work-scope.md` '범위'에 반영, 어긋나던 에이전트 메모리(`fix-bugs-without-asking`)는 지움. _(2026-10-20 등록, 같은 날 완료)_
- [x] **(verify)** **gameplay-tag §5-4 레지스트리 자동 생성** — 유저 "마음대로 해". 레지스트리 에셋·meta를 백업하고 지운 뒤(MCP `AssetDatabase.DeleteAsset`은 "유저 상호작용 필요"로 거부돼 파일을 직접 지움) Refresh → 그때는 안 생김 → `RequestScriptReload` 도메인 리로드 직후 `GameplayTagRegistryInitializer`가 같은 경로에 `_tags: []`로 만듦(Editor.log 순서로 확인). 백업으로 되돌림, md5 같음(`c253a6b1…`·meta `e56d98ca…`). 에디터 재시작이 아니라 리로드로 본 것. _(2026-10-17 등록, 2026-10-20 완료)_
- [x] **(verify)** **gameplay-tag 태그 관리 창·대화창 남은 것** — 유저 "TODO에 gameplay-tag 태그 관리 창·대화창 남은 것 이거 go". tests.md §4-1(관리 창 추가)·4-2(Rename·Delete, native 행 Rename·Delete 회색)·4-6(미등록 태그 대화창 먼저 → OK 뒤 피커 맨 앞)·5-1(레지스트리 `Test.ModifierTag.Native.Calm`이 native 노드 아래로 합쳐짐 — 관리 창·선택 피커 둘 다) 통과. Unity가 앞에 있어 실제 마우스 클릭으로 했고, 대화창은 MCP 밖에서 열어 실제로 떴다. 찾아서 고친 버그 2건: 추가 뒤 입력란에 옛 글자가 남아 힌트와 겹치던 것·Enter로 추가하면 다음 태그를 칠 수 없던 것(`GameplayTagPicker.DrawAddNewTagWidget`). 레지스트리·`GameplayTagTest.asset`은 백업과 md5 같음. 접두어 전체 선택은 위 (decision). _(2026-10-17 등록, 2026-10-20 완료)_
- [x] **(docs)** **하네스 재편 — session-protocol을 세션 시작·종료 절차만으로** — 유저 "session protocol이라는게 좀 광범위하고 추상적으로 사용되고 있는데 문제가 될 거같아"·"그래 해봐". 대화 규율 → 새 `conversation.md`, 착수·범위 → 새 `work-scope.md`, 즉시 기록·블로커·중간 요약 → `feature/HARNESS.md` '작업 중 기록', 각색 전 묻기·참조 갱신 → `docs.harness.md` 원칙 5·6, 커밋 스테이징 → `project/git-convention.md`. `agent/HARNESS.md`·`CLAUDE.md` 인덱스에 열 시점과 함께 등록, 옛 절 참조 5곳(깨진 '최종 갱신 타임스탬프' 포함) 수정. 같은 날 앞서: 대화 규율을 규칙만 남김(`a006064`), 코드 설계 규율 → `code-guide/HARNESS.md` '설계 원칙'(`a006064`), `code-guide/HARNESS.md` 인덱스에 `CODE_CONVENTION.md` 연결. _(2026-10-20)_

- [x] **(code)** **6. 태그로 활성 GE 찾기·제거** — UE `FGameplayEffectQuery`(`GameplayEffectQuery`, 조건 7종 + IgnoreHandles·CustomMatchDelegate, `MakeQuery_*` 9종), ASC 질의 API UE 목록 그대로(찾기·개수·시간·끝나는 시각·레벨·제거 `RemoveActiveEffects`·`RemoveActiveEffectsWith*`), `RemoveOtherGameplayEffectComponent`. 유저 "UE대로"·스택 인자 "자리만 두기"·Remove Other 자기 제외는 "UE의 의도대로"(→gameplay-effect D24(모든 질의에서 자기 제외)·D25(질의 API=UE 목록)). GE Component 셀프체크 H·I 추가, 89/0. 인스펙터 화면은 안 봄. _(2026-10-16 등록, 2026-10-20 완료)_

- [x] **(code)** **5. Target Tag Requirements 판정** — 지금 필드만 있는 빈 껍데기(D29). GE 쪽 Component 호출·inhibit 경로는 2026-10-19 완료(`SetActiveGameplayEffectInhibit`). 남은 것: Application·Removal로 적용 거부, Ongoing·Removal 태그 구독 → GE inhibit·제거(UE `TargetTagRequirementsGameplayEffectComponent.cpp:19-150`, `SetActiveGameplayEffectInhibit`). D28 뒤 소유 태그로 지속형 GE를 켜고 끄는 유일한 길. **2026-10-19 완료**: `CanGameplayEffectApply`(Application·Removal로 적용 거부), `OnActiveGameplayEffectAdded`(Ongoing·Removal 조건 태그 구독, Ongoing으로 켜진 채 시작할지), `OnTagChanged`(Removal이면 제거·아니면 Ongoing으로 켜고 끔), GE 제거 때 구독 해제. 새 `ActiveGameplayEffectEvents.OnEffectRemoved`(UE `FActiveGameplayEffectEvents`), 컨테이너 `Owner`, ASC `GetActiveGameplayEffect`·`GetActiveEffectEventSet`. GE Component 셀프체크 G(9). 남은 것: UE `IsDataValid`의 "Instant인데 Ongoing 조건" 오류(Granted Tags와 같은 결정 대기). _(2026-10-18 등록, 2026-10-19 완료)_

- [x] **(code)** **3. Granted Tags Component** — UE `UTargetTagsGameplayEffectComponent`. GE가 활성인 동안 대상에 태그 부여. 부여·회수 배선(`Add/RemoveActiveGameplayEffectGrantedTagsAndModifiers`)은 이미 있고 설정 Component만 없다. 2026-10-19 완료: `GameplayEffectComponents/TargetTagsGameplayEffectComponent`(Asset Tags와 같은 `InheritedTagContainer`·부모 없음 — gameplay-tag D30), GE `CachedGrantedTags`, `Spec.GetAllGrantedTags`. 부여·회수 경로는 이미 있었다. GE Component 셀프체크 F(9). 남은 것: UE `IsDataValid`의 "Instant GE에 Granted Tags" 오류 — 데이터 검증 체계가 없어 유저 결정 대기. _(2026-10-18 등록, 2026-10-19 완료)_

- [x] **(editor)** **4. GE 인스펙터 Component 목록** — `GameplayEffectAssetDrawer`가 필드를 직접 나열해 `gameplayEffectComponents`가 안 보인다. 이게 없으면 2·3·5를 에셋에서 설정할 수 없다. 2026-10-19 완료: Components 목록(검색형 Add + 칸마다 "Component Class" 드롭다운으로 종류 바꾸기, 같은 종류 중복 막음), 본문은 `GameplayEffectComponentDrawer`(필드 이름·[Tooltip]으로 그림, 전용 드로어는 그리기가 다를 때만), 인스펙터 순서 Components → Modifiers → Executions. 편집 → OnValidate → GE Asset Tags 캐시 갱신 확인. _(2026-10-18 등록, 2026-10-19 완료)_

- [x] **(code)** **2. Asset Tags Component** — UE `UAssetTagsGameplayEffectComponent`. GE 자체 태그(`GetAssetTags()`, 지금 늘 빈 값)를 채우고, Spec 생성 때 Spec 태그에 넣는다(UE `GameplayEffect.cpp:1627`). UE의 `FInheritedTagContainer`(부모 GE 상속)를 어떻게 할지, Component가 GE 캐시에 쓰는 길(UE `GetOwner()`가 없음)을 착수 때 정한다. 2026-10-19 완료(→gameplay-tag D30 부모 없음): `InheritedTagContainer`·`GameplayEffectComponents/AssetTagsGameplayEffectComponent`·Component `GetOwner()`·Spec 태그·`Spec.GetAllAssetTags`, GE Component 셀프체크 E. 인스펙터에서 넣는 건 4번. _(2026-10-18 등록, 2026-10-19 완료)_

- [x] **(code, low)** GE 캡처 **Target 기반 AttributeBased의 생성 시점 경고 억제** — `GameplayEffectSpec.Initialize`가 캡처 후 `CalculateModifierMagnitudes`를 1회 도는데, Target 기반 AttributeBased는 이 시점 target 미캡처라 `AttributeBasedMagnitude.Evaluate`가 캡처 실패 경고를 낸다(그 결과는 소비 안 됨 — 적용 시 재평가본만 소비). Source 기반(현 검증 대상 `GE_EquipSpeedDown`)은 무영향. 지금은 warn-always로 단순하게 뒀음. 억제하려면 UE `CanCalculateMagnitude`류(캡처 준비됐을 때만 경고)가 필요 — 필요해지면 처리. _(2026-08-04 등록 — 5b evaluate 구현 중 도출)_
  - **2026-10-17 확인:** 지금 나오는 경고는 `[GESpec] '<GE 이름>'의 N번 modifier magnitude Calculation Failed - falling back to 0.`(`GameplayEffectSpec.cs` `CalculateModifierMagnitudes`, `Initialize`에서 호출). gameplay-tag Modifier Tag 셀프체크 C-12·C-13에서 2줄 나온다(값은 맞음). UE 5.6은 `FGameplayEffectSpec::Initialize`(`GameplayEffect.cpp:1609-1660`)·`CaptureDataFromSource`(`:1754-1774`) 안에서 `CalculateModifierMagnitudes`를 부르지 않는다(이 파일의 호출 위치 grep 결과: `:2953`·`:3084`·`:3690`·`:3703`·`:3718`·`:4159`·`:4857`·`:5183`). 그래서 UE에선 생성 시점에 이 경고가 나지 않는다. 경고 문구 자체는 UE에도 있다(`:1958`).
  - **2026-10-19 완료** — 유저 "modifier self check에 warning은 괜찮나". `GameplayEffectSpec.Initialize`의 `CalculateModifierMagnitudes` 호출을 UE처럼 없앰(적용 때 Target 캡처 직후 계산은 그대로). 경고 4줄 사라짐, Modifier Tag 65/0·Aggregator 55/0·GE Component 27/0·PlayMode 14/0. feature: gameplay-effect(gameplay-tag 작업 중 수정).

- [x] **(verify)** **셀프체크 다시 돌리기** — 2026-10-18 코드(D27 자격 판정 분리·D28 CurrentValue=빈 문맥·D29 GE Component 빈 껍데기) 확인. MCP 실행 결과 Modifier Tag 65/0(경고 5줄 정상), Aggregator 55/0, PlayMode 14/0. _(2026-10-18 등록, 2026-10-19 완료)_

- [x] **(docs)** **decisions 전체 정리 + 결정 기록 기준 개정** — 유저 "진짜로 중요한 UE의 기능을 가져오면서 선택해야되는 순간이나 새로운 게 나오거나 설계를 하거나 등에 대한 것을 적는거지. 그리고 그것은 간단 명료해야해." `feature/HARNESS.md` '결정 기록 품질 기준' → '결정 기록 기준'(무엇을·어떻게 적나, 항목 템플릿). 4개 feature decisions를 새 형식(결정·왜·대안·대가·재검토)으로 다시 쓰고, 사소하거나 낡은 결정은 "치운 결정" 한 줄로: gameplay-tag 12개 유지, gameplay-effect 9개 유지(D8 미결은 progress 결정 대기로), aggregator 5개 유지, attribute 4개 유지. progress 인덱스도 맞춤. 이전 본문은 git `ca110d2`·`c98c21a`. _(2026-10-18)_

- [x] **(code)** **현재 목표 1. 태그만 주는 GE 허용** — `ActiveGameplayEffectsContainer.ApplyGameplayEffectSpec`의 "Modifier·Execution 둘 다 없으면 거부"를 지웠다(UE엔 없는 거부). Modifier Tag 셀프체크 D-16 추가(기준 46/0, 미실행). 컴파일 통과. _(2026-10-18 등록, 같은 날 완료)_

- [x] **(decision)** **gameplay-tag ①② CurrentValue 태그 문맥 질문 해소** — CurrentValue를 UE처럼 빈 문맥으로 되돌려(→D28, 유저 "UE에 따라 … Ongoing Tag Requirements 담당으로 넘기고 가보자. 기존에 선택 치우고") 두 질문이 없어졌다. D21·D23 코드를 걷어냈고, GE Component를 빈 껍데기로 넣었다(→D29, 유저 "UE 처럼 GE Component 도입할거야"·"셋 다"·"빈 거로 넣어둬"). GE·ASC 태그 연동 질문 ① GE 구조도 이것으로 답함. Modifier Tag 셀프체크 기준 53 → 45(미실행, 같은 날 D-16으로 46). _(2026-10-16 등록, 2026-10-18 완료)_

- [x] **(code)** **mod 자격 판정 UE처럼 갱신·확인 분리** — `Mod.UpdateQualifies`·`Qualifies()`·`EvaluateQualificationForAllMods`(→D27). 결과는 전과 같음, 컴파일만 확인. _(2026-10-18)_

- [x] **(bug)** **Query 편집기를 열어 둔 채 재컴파일하면 NullReferenceException 반복 + 창이 두 개 뜨던 것** — feature: gameplay-tag. 유저 "null이 뜨거나 하면 그냥 빈 editor로 되게 해줘. 오류 안뜨게"·"빈 Window의 Editor로". 리로드 뒤나 대상이 지워지면 대상 연결을 끊고 빈 편집기로 둠(`ClearTarget`), 열 때 열린 창을 모두 찾아 닫음(정적 `_openWindow` 제거). MCP로 확인(tests.md §2-13). _(2026-10-17)_

- [x] **(decision)** **gameplay-tag 결정 4건 확정** — ⑤ Query 편집기 접힘 상태 키 = 배열 경로 그대로(유저 "접힌 상태는 뭐 지금대로 두자. Undo나 Redo 둘 것도 아니고", →D24). None 하위 식 `undefined` 저장 = UE처럼 막지 않음, `GE_EquipSpeedDown.asset`(유저 미커밋 변경의 깨진 Query)도 손대지 않음(유저 "undefined는 굳이 건드리지 말고", →D25). ⑥ `TokenStreamVersion` = 넣음(유저 "TokenStreamVersion 넣자", →D26) — `GameplayTagQuery._tokenStreamVersion` + `GameplayTagQueryStreamVersion` enum 구현, 스트림 첫 토큰엔 안 넣음(에이전트 판단, D26). Aggregator 55/0·Modifier Tag 53/0. _(2026-10-16 등록, 2026-10-17 완료)_

- [x] **(bug)** **`AttributeAggregator.UpdateAggregatorMod`가 다른 GE의 mod를 덮어쓰거나 범위를 벗어나던 것** — feature: aggregator. 원인: Spec의 modifier 번호를 `_mods`(여러 GE의 mod가 섞인 목록) 칸 번호로 써서, 라이브 재평가 때 먼저 건 GE의 mod를 덮거나(130 → 140) 범위 밖 예외. 유저 "단순 오류나 버그는 하고"로 결정 대기(③ 수정 시점)를 거치지 않고 수정 — UE `FAggregator::UpdateAggregatorMod`(`GameplayEffectAggregator.cpp:503-521`)처럼 그 핸들의 mod를 빼고 이 어트리뷰트를 겨냥한 modifier를 조건·GE Source 액터 태그와 함께 다시 넣은 뒤 dirty 1회. Aggregator 셀프체크 G 6개(고정+라이브 섞임, modifier 번호 > mod 수). 같이 `Evaluate` 요약 주석 "마지막 Override 우선" → "조건을 만족하는 Override 중 먼저 만난 것"(코드·UE와 맞춤). _(2026-10-16 등록, 2026-10-17 완료)_

- [x] **(bug)** **주기 실행 때 magnitude를 다시 계산하지 않던 것** — feature: gameplay-effect. non-snapshot 캡처가 주기 GE에선 첫 적용 값으로 굳었다(DOT가 공격자 버프를 안 따름). 유저 "단순 오류나 버그는 하고"로 결정 대기(④)를 거치지 않고 수정 — UE `ExecuteActiveEffectsFrom`(`GameplayEffect.cpp:3081-3084`)처럼 실행마다 Target 태그를 읽은 뒤 `GameplayEffectSpec.CalculateModifierMagnitudes()`(private → internal). Instant는 적용 때와 실행 때 두 번 계산(UE도 같음). Aggregator 셀프체크 H 2개. 같이 `GameplayEffectType` 주석의 Instant·Duration "(미구현)" 제거(둘 다 구현돼 동작). Aggregator 55/0·Modifier Tag 53/0. _(2026-10-16 등록, 2026-10-17 완료)_

- [x] **(bug)** **에디터 확인에서 찾은 버그 4건 수정 (유저 "결정 없이 고쳐야 되는 건 고치고")** — ① aggregator가 없는 어트리뷰트는 Base가 바뀌어도 Current가 그대로(주기·Instant GE, 기존 버그) → `SetAttributeBaseValue`가 aggregator 없으면 Current=Base(UE `InternalUpdateNumericalAttribute`), Aggregator 셀프체크 F 추가 47/0. ② 태그 드로어 다중 선택 시 칩·▾ 글자가 `—` → 드로어가 그리는 동안 `showMixedValue` 끔, 재확인 통과. ③ GE 인스펙터 Modifier 요소 아래 빈 줄(높이 계산이 Op 줄 두 번 셈) → 그리기와 같게. ④ Query 편집기 루트 단계 Tags 상자 3px 어긋남 → 위 줄 팝업 x를 받게. Modifier Tag 셀프체크 53/0 유지. 기록: gameplay-tag tests.md·worklog, aggregator·gameplay-effect progress. _(2026-10-17)_

- [x] **(verify)** **gameplay-tag 에디터 확인 — Unity 앞으로 띄운 뒤(유저 "맨 앞에 띄워서 해봐")** — tests.md §2-11 창 안 피커, §2-12 요약 툴팁, §4-3 필드별 피커(형제 없으면 부모·칩 ×·우클릭 Copy/Paste/Clear All), §4-4 단일·List 저장, §4-5 다중 선택(수정 뒤), §4-6 피커 안 메뉴 3종에서 피커 유지. 남은 것은 '당장 목표' (verify) 2개. _(2026-10-17)_

- [x] **(verify)** **gameplay-tag 에디터 확인(에이전트, 유저 "에디터 확인은 직접 해")** — Unity 창을 `PrintWindow`로 캡처해 보고 `EditorWindow.SendEvent`로 클릭. 통과: §1-3-2 PlayMode 셀프체크 14/0, §1-3-3 실제 GE의 Base 값이 계산과 같음, §1-3-4 ASC 인스펙터 캡처 줄 에러 0, §1-3-5 `SpeedBoostExecution` 로그, §2-1~2-10·2-12(Query 편집기), §3-1~3-5(Modifier 태그 인스펙터), §5-2·§5-3. 발견: CurrentValue 미갱신·다중 선택 `—`·빈 줄·3px(→ 같은 날 수정, 위 항목), `GE_EquipSpeedDown` Query('당장 목표' (data))·None 칸('우선순위 낮음' (idea)). 임시로 만든 `Assets/_McpCheck/`(캡처 도구·테스트 에셋)는 휴지통으로 옮김. Editor 테마는 Light 확인 뒤 Dark로 되돌림. _(2026-10-17)_

- [x] **(code)** **gameplay-tag 소유 태그 입구 + 태그 변경 시 CurrentValue 재계산** — `GameplayTagCountContainer`(UE 구조) + ASC Loose 태그·조회·이벤트, persistent GE가 modifier Target 조건 태그를 구독해 바뀌면 해당 어트리뷰트만 다시 계산(gameplay-tag D22·D23). Modifier Tag 셀프체크 53/0, Aggregator 44/0(MCP). Play 모드 회귀는 같은 날 위 에이전트 에디터 확인에서 통과. _(2026-10-16 등록, 2026-10-17 완료)_

- [x] **(verify)** **gameplay-tag MCP 자동 확인분** — Unity MCP로 tests.md §0-1 컴파일 에러 0, §1-1 Modifier Tag 셀프체크 `passed 38 / failed 0`, §1-3-1 Aggregator 셀프체크 `passed 44 / failed 0`, §5-5 미등록 태그 이름 에러가 이름당 1회. 나머지 확인은 위 '당장 목표'의 (verify) 항목들. _(2026-10-17)_

- [x] **(docs)** **하네스 간결화 세션** — 인덱싱·중복 제거 방향으로 대거 정리: `금지 사항` 순수 인덱스화(+feature 스코프 금지·폴더 구조·네이밍 규약을 `feature/HARNESS.md`로 이관), `session-protocol '작업 중'` 압축, `HARNESS 관련 하위 문서 및 폴더 (Indexing)` 자기설명 재구성, `NOW.md — 무엇인가` 절 제거(인덱스 자기설명화), **핵심 원칙 1~8 평면 번호화**(판단·해석·읽기트리거·왜는지어내지 승격). **cases 시스템 전면 제거** — 3개 `*.cases.md` 삭제 + `→ 사례 [C-NN]` 포인터 전부 제거(2026-08-23 분리 결정 반전). feature-list attribute·aggregator·gameplay-effect `✅ 임시완료` 표기(progress 미동기화·유저 판단 대기). **전부 미커밋.** _(2026-09-08)_

- [x] **(docs/refactor)** **전역 코드 주석·컨벤션 스윕** — 코드 주석에서 `→D#`·`INV-N`·`(UE:)` 외부 포인터 전부 제거(주석은 자기충족만), 이름 재진술 주석 삭제, 메서드 `=>`→블록(프로퍼티 예외)·줄넘김된 문장 한 줄로. AbilitySystem·Item·Character·Editor 전반. `CODE_CONVENTION.md` 갱신 — 하우스 스타일 "근거 문서 앵커(INV/D#)"를 **"주석은 그 자체로 완결·외부 포인터 금지"**로 교체 + `=>` 사소한 프로퍼티 예외 + "긴 줄 줄넘김 지양" 일반화(유저 지시). 로직/컴파일 이슈 2건은 스코프 밖으로 두고 보고만(→ NOW.md). _(2026-08-25)_

- [x] **(docs)** **하네스 다이어트 — 규칙 본문 ↔ 사례 아카이브 파일 분리** — 각 규칙에 인라인으로 붙어 있던 `실제 발생`/`*사례` 25건을 **규칙 문서마다 형제 `*.cases.md`** 로 분리: `HARNESS.cases.md`(C-01~09)·`session-protocol.cases.md`(C-01~15)·`feature/HARNESS.cases.md`(C-01). ID는 **파일별 로컬 번호**. 본문(`HARNESS.md`·`session-protocol.md`·`feature/HARNESS.md`)엔 규칙만 남기고 `→ 사례 [C-NN]` 포인터로 연결. 각 문서 상단에 "규칙 ↔ 사례 분리" 안내, `HARNESS.md` 디렉토리 트리·문서 인덱스에 세 파일 등록, `CLAUDE.md` 메모 완료 처리. _(2026-08-23)_

- [x] **(docs)** **하네스 문서 대재편(2026-08-22)** — ① 세션 프로토콜→`session-protocol.md`, feature 문서 작성 규약(feature-list·progress·worklog·decisions + 생성/완료/폐기 + decisions 품질)→`feature/HARNESS.md`, 작업 보드 규약→`todo-board-convention.md`로 **분리**. ② `HARNESS.md`를 프로세스 허브로 축소 — **관련 하위 문서 인덱스** 신설(CLAUDE.md식), 디렉토리 트리를 **agent/ 하위만**으로. ③ **§번호 상호참조 제거** — 하네스 4개 + 활성 feature/arch 문서 + TODO-BOARD + project 3문서의 헤딩 번호·문서간 §참조를 **이름 기반**으로(타임스탬프 마커 `(KST — HARNESS §3.4)`→`(KST)`). ④ `feature-list.md`를 `feature/`로, `wiki/`를 `dev-docs/project/`로 이동(참조 갱신). 남은 §는 위 "§번호 제거 잔여분" TODO 참조. _(2026-08-22)_

- [x] **(docs)** 하네스 §1.1-2에 **"엔진·외부 API 동작 질문 = 답 전에 원문 확인" 실행 트리거** 추가 — UE 동작을 기억으로 답부터 하는 실수가 3회 반복(2026-07-19·08-04·08-05)돼, §1.1/§8.5가 있어도 "질문 받는 순간"이 트리거로 안 걸리던 것을 명시적 멈춤 규칙으로. WebFetch/검색 요약도 2차 자료(오독 가능)라 결론 지점은 원문 라인 확인. 유저 승인. _(2026-08-05)_
- [x] **(docs)** 하네스에 **습관·증분 기록 규약** 추가(§1-2) — 유저가 "문서 작성을 지시 안 하고 세션을 닫는 일이 많다"고 밝혀, 기록을 세션 종료 절차가 아니라 **진전마다 즉시 남기는 습관**으로 재정의. 시키지 않아도 남기고, NOW.md 우선. 텍스트 규약은 강제 종료 시 못 켜지므로 Stop 훅이 더 확실하다는 한계도 명시(요청 시 훅). _(2026-08-03)_
- [x] **(docs)** 하네스 정합성 점검·수정 (유저 요청으로 "이상하거나 잘못된 것" 전수 대조) — 발견 11건 중 유저가 지목한 8건 수정: ①§3.1 vs §7.2 **TODO-BOARD 충돌** 해소(미리 훑지 않는 목록에서 제외, §7.2 예외 명시) ②§4.2 전이도에 `DONE ⇄ IN-PROGRESS` 반영(아래 문장과 모순이었음) ③**§2.1 신설 — NOW.md 성격 정의**(위치 명시 + "지금 뭐 하던 중이었더라"용 임시 메모: 매 세션 덮어씀·이력 없음·진실의 출처 아님·짧게, 단 선행조건/미승인 산출물은 필수) ④§9에 "NOW.md 갱신 없이 종료" 금지 추가 ⑤§3.3 종료 절차에 **TODO-BOARD 갱신** 단계 신설(5번) ⑥`feature-progress-TEMPLATE.md` **삭제** — §5 템플릿과 두 벌이라 이미 드리프트, 파일 쪽 안내 문구를 §5로 흡수하고 §6은 "§5 복사"로 ⑦§7.1에 "3칸은 에이전트 기본 틀 — **보드는 유저도 직접 씀**, 형식 벗어난 항목 존중·임의 재편 금지" ⑧§3.4 시각 확인 명령을 **Claude Code 실행 환경 기준**으로(Bash 툴 `TZ=...`, PowerShell 대체식 병기). 이어서 회고에서 도출된 2건 추가 — ⑨**모르는 날짜는 지어내지 않고 그대로 둔다**(하류 `반영 기준`에 상류 progress 값을 가져다 쓴 것도 추정이었음. §3.4 + §9) ⑩**문서를 지우거나 옮기면 그것을 가리키는 곳을 같은 턴에 고친다**(§3.2 + §9 — TEMPLATE 삭제는 지켰고, 07-18 item 아카이브 이관은 안 지켜 feature-list 링크 4개가 죽어 있음). **타임스탬프 형식은 시:분을 빼고 `YYYY-MM-DD` 날짜만으로 변경**(유저 결정) — HARNESS §3.4·§5, wiki/HARNESS §5, 실제 문서 전부 일괄 적용. 미처리분은 위 TODO(반영 기준 마커) 참조. _(2026-07-19)_
- [x] **(docs)** 하네스에 **대화 규율(§3.5)** 신설 — Grice 협력 원리 4격률(양·질·관계·방식), Gordon 커뮤니케이션 로드블록 중 해당분(요청 없는 조언·훈계·과도한 되묻기·화제 돌리기·값싼 안심·판단), 질문에 질문으로 답하지 않기, shift↔support response(주제 뺏기 금지), 전송 전 자기 점검 4문항. §3.2엔 "대화 스코프 이탈 금지"(요청 없는 추천·주제 밖 시스템·끝난 결정 재론·요청 축 넘는 비교·과한 길이) + 2026-07-19 사례 4건. §9 금지 사항 2줄. _(2026-07-19)_
- [x] **(docs)** 하네스에 **세션 종료 회고(§3.3.1·§3.3.2)** 신설 — 종료 시 ①문제가 있었던 지점을 스스로 보고(규약 위반·범위 이탈·승인 없이 만든 산출물 목록화) ②하네스 자체 점검(트리거 미발동·규칙 충돌·실제와 안 맞는 절차·반복된 실수는 빠진 규약·형식 규칙 준수 대조). 첫 실행에서 개정 2건 도출 → 아래 항목. _(2026-07-19)_
- [x] **(docs)** 하네스 개정 2건 — ①**`D#`은 유저가 확정한 결정에만 부여**(에이전트 판단을 D로 박지 않음. 제안 단계는 worklog·progress에 "제안"으로) ②**타임스탬프는 로컬 셸(`TZ=Asia/Seoul date`)로 확인**해 사용(세션 컨텍스트 날짜 불신). 둘 다 이번 세션에서 실제로 터진 문제(D6(캡처 미도입(폐기)) 무단 작성·날짜 오기 7파일)에서 도출. _(2026-07-19)_
- [x] **(docs)** `CLAUDE.md` — HARNESS 읽기 트리거를 "구현 작업 착수 전" → **"게임 feature에 해당하는 내용을 다루기 시작하면(구현이 아니어도 — 질문 답변·설명·설계 논의·문서 작성 포함)"** 으로 확장. 이유: 대화가 *설명 → 논의 → 편집*으로 미끄러지면 "착수" 시점이 없어 규약이 끝까지 안 켜진다(2026-07-19 실제 발생). _(2026-07-19)_

- [x] **(editor)** `SubclassSelector` 중복 제외 **다른 스코프** 지원 — 코드 대조로 확인 완료: 범용 `SubclassSelectorDrawer`는 `excluded`를 받아 그리기만(`DrawSelector`), 수집은 호출측 전용 드로어가 트리거(`AttributeSetDefinitionDrawer`→`CollectSiblingValues`, 수집 코어 `CollectArrayValues` 공유). 다른 스코프는 전용 드로어가 자체 수집 로직으로 `excluded`를 만들어 넘기면 됨 — 범용 드로어 변경 불필요, 추가 수집기는 필요 전까지 미구현. 확장 가이드는 코드 주석·`CODE_CONVENTION.md`에 이미 기록돼 있어 보드에서 내림. _(2026-07-09 확인)_
- [x] **(docs)** SO→Asset 네이밍 문서 반영 — architecture 문서의 클래스명 언급을 문맥별 갱신: `ability-system/gameplay-effect.md`(SO 정의·MCP type_name·미구현 표 파일 참조), `ability-system/overview.md`(구조도·현황/UE 대비 표 + "UE 용어 따르되 SO는 `Asset` 접미어"로 원칙 서술 조정), `item-equipment.md`(계층도·규칙·코드 위치 `ItemDataAsset`·`EquipItemAsset.cs`·`ConsumeItemAsset.cs`), `controller-character.md`(`possessEffect` 타입). UE 개념(`FGameplayEffect` 등)·개념적 GE 언급·ASC 메서드명은 유지, `attribute.md`는 이미 갱신돼 있었음. _(2026-07-09)_
- [x] **(refactor)** Attribute·SO 네이밍 정리 — Attribute 초기화 3층 헷갈림 해소(`AttributeInitData`→`AttributeDefinitionAsset` / `AttributeSetInitData`→`AttributeSetDefinition` / `AttributeFieldInitData`→`AttributeFieldDefinition`, 런타임 `AttributeSet`/`AttributeData`와 구분) + 모든 SO에 `Asset` 접미어 통일(`GameplayEffect(Execution)`·`ItemData`·`EquipItem`·`ConsumeItem`→`...Asset`). 대응 드로어·참조 갱신, `.meta`/GUID 보존. 문서 문맥별 갱신은 위 (docs) TODO로 분리. _(2026-07-08)_
- [x] **(docs)** 코드 설계 규칙 문서화 — `CODE_CONVENTION.md`에 ① **설계 원칙**(필요할 때만 범용화·과설계 지양, 좁은 스코프/설계 갈림은 착수 전 확인, 범용 코드는 정책 모름) ② **에디터 확장(드로어) 가이드**(범용/전용 분리, `SubclassSelector` 예시, 런타임/에디터 경계) 섹션 신설. _(2026-07-08)_
- [x] **(editor)** `SubclassSelector` 필드를 오브젝트 참조 필드처럼 개선 — 선택된 타입의 소스 스크립트를 `ObjectField` 스타일로 표시(단일클릭 ping / 더블클릭 open), 타입 선택은 우측 버튼 → **검색 가능한 네이티브 팝업**(`AdvancedDropdown`, Add Component 창과 동일). `EditorTypeUtility.FindScript`(타입→MonoScript, 캐시) + `SubclassAdvancedDropdown` 추가. 범용이라 ItemData `runtimeClass` 등 모든 `[SubclassSelector]`에 적용. (New Class 생성은 안 함 — 유저 결정) 이후 `uniqueInList` 옵션 추가(리스트 내 이미 쓰인 타입 제외 — 형제 배열 훑는 로직 범용화)하고, `AttributeSetInitData.attributeSetTypeName`을 커스텀 `EditorGUI.Popup` → `[SubclassSelector(typeof(AttributeSet), uniqueInList:true)]`로 전환(드로어 죽은코드 `DrawAttributeSetPopup`·`GetUsedTypeNamesByOthers` 제거). _(2026-07-08)_
- [x] **(refactor)** 에디터 스크립트 배치를 `Editor/<feature>` 미러 구조 → **feature-local `Core/<feature>/.../Editor`** 로 통일. 입도는 **서브시스템 레벨**(기존 `ItemSystem/Inventory/Editor` 선례와 일치): 드로어를 대상 타입 옆에 둠(Attribute·Effect·ItemSystem 각각 Editor). feature 무관 공통 에디터 유틸(ConditionalShow·SubclassSelectorDrawer·EditorDrawUtility·EditorTypeUtility·TypeChoiceList)은 `Core/Common/Editor`. `.meta`(폴더 meta 포함) 그대로 이동해 GUID 보존, asmdef 없어 참조 영향 없음. _(2026-07-08)_
- [x] **(docs)** 기존 구현분 정리 — **Player Character 계층 설계**(Controller · Character 분리, Possession 패턴) 문서화. 기존 `controller-character.md`가 얇은 스텁+코드 불일치라 재작성: 설계 의도(왜 분리), Possession 생명주기(사망 시 자동 언포제스·재빙의), 시스템 연결점(possessEffect·Speed Attribute·EquipmentComponent 위임), MonsterCharacter까지 반영. _(2026-07-07)_
- [x] **(infra/docs)** "하던거 하자" 재개가 매번 어긋나는 문제 수정 — 재개 지점이 모호(IN-PROGRESS 3개 + TODO-BOARD 진행중 공백)·낡음(item 다음작업 stale)·선행조건 미기록(Unity MCP)이 원인. **단일 재개 파일 `dev-docs/agent/NOW.md` 도입**: 이어하기 시 이것 하나만 읽고 즉시 실행(전체 확인 안 함). SessionStart 훅을 NOW.md 기반으로 교체, `CLAUDE.md`·`HARNESS.md §3.1/3.3` 재배선(시작=NOW만 읽기 / 종료=NOW 최우선 갱신 + 선행조건 명시). _(2026-07-06)_
- [x] **(docs)** GAS를 feature로 등록 — `ability-system` 그룹 신설(attribute ✅ / gameplay-effect 🔧 / gameplay-ability 📋), PR#2·#3 기준 progress·decisions·worklog 작성, feature-list 동기화. _(2026-07-05)_
- [x] **(docs)** architecture `ability-system.md` → `ability-system/` 폴더 4분할(overview·attribute·gameplay-effect·gameplay-ability), `overview.md` 링크 갱신. _(2026-07-05)_
- [x] **(docs)** GAS 문서화 — `architecture/ability-system.md`를 구현됨/미구현(계획)으로 개편, Modifier 연산·CurrentValue 공식·Execution 프레임워크·SO 초기화 반영, 미구현 로드맵(GA·스택·Cue·Execution concrete·TakeDamage 배선) 통합. ⚠️ GE 타입 enum 불일치 1건은 후속 TODO로 분리. _(2026-07-05)_
- [x] **(docs)** wiki 편집 하네스 규약 작성 — `wiki/HARNESS.md` 신설, `CLAUDE.md` 링크, feature `✅ DONE` 시 wiki 갱신 리마인드(`dev-docs/agent/HARNESS.md` §6) 추가. _(2026-07-05)_
