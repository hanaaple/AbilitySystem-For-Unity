# TODO-BOARD — 작업 현황 보드

> feature 단위 추적은 `feature-list.md`가 담당한다. **이 보드는 그보다 잘게 쪼개진 잡다한 작업·개인 TODO·이슈**를 모은다 — 문서 정비, 인프라(GitHub 설정 등), 실험, 리팩터, 아이디어 등 feature로 묶기 애매한 것들.
> 관리 규약은 `TODO-BOARD.harness.md`. 항목은 상태가 바뀌면 아래 칸 사이를 이동한다.

- 최종 갱신: 2026-10-21 (KST)

---

## 📋 TODO (데모 전 GAS 구현 — 2026-10-20)

> 2026-10-20 유저 "아까 말한 목표로 데모 구현 전까지 필요한 GAS 구현 TODO로 전부 작성해. 당장 세부적인게 어려우면 크게 잡아서 작성해둬". 목표(같은 날 유저): AI로 빠르게 만드는 포트폴리오 데모 — "기능을 시각적으로 보여줄 용도", "시각 효과, 짧은 영상이 필요해", "1~2일 안에".
> 데모 구성은 에이전트 제안(유저 미확정): 방 하나·플레이어 1·허수아비 적 1, 스킬 화염구(도트뎀)·스턴·가속 버프(+정화), HUD·디버그 오버레이, 스킬별 이펙트, 30~60초 영상.
> 기존 보드 항목은 본문 그대로 옮겼다(원래 칸: 'GE·Tag 남은 것'·'이전 우선순위 낮음'). 새 항목은 2026-10-20 등록. 순서는 위에서부터(에이전트 제안).
> 2026-10-20 유저 "TODO에서 데모용으로 필요한 부분은 싹 데모쪽으로 빼두고" → 게임 쪽 구현·데이터·연출·데모 결정은 아래 '📋 TODO (데모 제작)'으로 옮겼다. 이 칸엔 GAS 자체만 남는다.

### 1. Attribute — 값 변화 알림·범위

(없음 — 2026-10-20 값 변경 이벤트·`PreAttributeBaseChange` ref·AttributeSet 훅 완료(✅ Done, PR #8), 사망 처리는 데모 미결로 옮김)

### 2. GameplayAbility (최소)

(없음 — 2026-10-21 모두 완료, TODO-DONE. 남은 GA 일은 '뒤로 미룸')

### 3. GameplayCue (최소)

> **2026-10-21 유저: "아무래도 GameplayCue까지는 못하고 GameplayAbility까지 한 다음에 바로 데모 구현해야겠어"** → 데모 전 GAS 구현에서 뺌. GA(2.) 끝나면 바로 '데모 제작'으로. 아래 항목은 지우지 않고 둔다(범위 기록용).

- [ ] Gameplay Cue 구현 — **데모 전 범위에서 뺌(2026-10-21 유저)**
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

### GE 버그

> Tick 순서가 결과에 영향을 주는 곳 — 구현·보완할 것은 [tick-order-improvements.md](feature/ability-system/gameplay-effect/tick-order-improvements.md), 사용자가 알아야 할 것은 [tick-order-user-notes.md](feature/ability-system/gameplay-effect/tick-order-user-notes.md). 아래는 improvements의 할 일. ('적용 프레임 deltaTime 차감' 버그는 2026-10-21 해결 → TODO-DONE)

- [ ] **(bug)** **Tick 도중 같은 ASC에 GE를 걸거나 지우면 예외 가능(미검증)** — 주기 GE 실행 → AttributeSet 훅·값 변경 이벤트 구독자가 같은 ASC에 Duration/Infinite GE를 걸거나 활성 GE를 지우면, `Tick`의 `foreach (… _activeGameplayEffects.Values)` 도중 사전이 바뀌어 `InvalidOperationException`이 날 수 있다(코드에서 추론, 실행 안 함). UE는 스코프 락 동안의 추가를 보류 목록에(`GameplayEffect.h:1978`). 먼저 재현 테스트 → 사본 순회 또는 보류 목록. tick-order-improvements.md 항목 2. _(2026-10-21 등록)_
- [ ] **(code)** **남은 시간 조회를 조회 시점 시각으로** — `GetTimeRemaining`이 Tick 때 갱신한 값이라 같은 프레임에 ASC.Update보다 먼저 읽으면 지난 프레임 값. UE처럼 `Duration - (GetWorldTime() - StartWorldTime)`으로(`GameplayEffect.h:1336`). tick-order-improvements.md 항목 3. _(2026-10-21 등록)_

### Tag

- [ ] **(code)** **gameplay-tag D3 대소문자 무시 비교 + 표기 보존** — 결정은 됐고 구현이 없다(`InternedName`은 대소문자 구분). progress 다음 작업 5. _(2026-10-18 보드 등록)_
- [ ] **(code)** **Gameplay Event** (신규) — `HandleGameplayEvent`/`SendGameplayEventToActor`, 이벤트 기반 능력 발동(예: 피격→반격). 선행: Tag. _(2026-08-09 등록)_
- [ ] **(code)** **호출하면 예외가 나는 미구현 API** — `GameplayTag` `<`/`>`(`GameplayTag.cs:41`), `GameplayTagsManager`의 `DoneAddingNativeTags`·`CallOrRegister_OnDoneAddingNativeTagsDelegate`·`SplitGameplayTagFName`·`RequestAllGameplayTags`(`:265-287`)가 `NotImplementedException`. `AddChildrenTags`는 `onlyIncludeDictionaryTags: true`면 예외(`:415` — 노드에 명시/암시 기록이 없어서). 지금 호출하는 곳은 없다. _(2026-10-20 등록)_
- [ ] **(editor)** **ASC 인스펙터에 소유 태그 표시** — gameplay-tag 수용 기준 "인스펙터에서 확인 가능"이 아직 안 됨(progress 세부 TODO 7 남은 것). _(2026-10-20 등록)_
- [ ] **(code)** **Tag 코어 후속(미룸)** — IL2CPP 코드 제거 시 마커 필드 유실 대응(`[Preserve]` 등), 빌드 탐색 비용 측정, 노드 출처·명시/암시 기록. D5의 막는 이유 유저 확인. 출처: gameplay-tag progress 다음 작업 1. _(2026-10-20 보드 등록)_
  - **2026-10-20 확인:** 원래 있던 "레지스트리 편집 시 에디터 매니저 재구성"은 이미 됨 — 관리 창의 추가·이름 변경·삭제가 `GameplayTagRegistryEditing`에서 `EditorRefreshGameplayTagTree`를 부른다(tests.md §4-1·4-2에서 바로 반영 확인).

### 검증
- [ ] **(bug)** **Modifier Tag 셀프체크 E-12가 Play 모드 뒤에 실패** (2026-10-21 발견, 미수정) — 에디터 `Time.time`이 0이 아니면(직전 Play 모드로 10.50373) 주기 1초 GE의 경계 Tick(시계를 딱 +1초)에서 주기 실행이 빠져 64/1. 시계를 0으로 맞추면 65/0 — 부동소수 경계로 보임. 테스트 경계를 넉넉히 할지(GA 테스트는 D27 때 그렇게 함), GE 주기 판정 쪽을 볼지 유저 판단. GE 쪽이 원인인지는 확인 안 함. _(2026-10-21 등록)_

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

- [ ] **(docs)** **architecture 문서를 PR #8에 맞춤** — `architecture/ability-system/attribute.md`(`AttributeSet` "빈 추상 마커"(19행)·"Current는 aggregator가 있으면 `Evaluate()`로만"(114행) — 지금은 저장값 읽기·훅 표에 Base 훅 2개뿐(143·152행)), `aggregator.md`(CurrentValue 산출 서술), `gameplay-effect.md`(쓰기 경로 `ApplyModToAttribute`(122행) → 지금은 `InternalExecuteMod`를 거침, 값 변경 이벤트 없음). feature progress는 PR #9로 맞췄고 architecture는 유저 "feature 쪽만"으로 미룸. _(2026-10-20 등록)_
- [ ] **(code)** GE 캡처 **params 조회 함정 3종 (5b·Execute 작성 시 주의)** — 캡처를 실제로 읽는 코드를 쓸 때 걸리기 쉬운 지점. ① **false가 조용하다:** `AttemptCalculateCapturedAttribute*`(`GameplayEffectExecutionParameters.cs:47·54`)는 실패 사유(미등록/캡처 무효/Target 미캡처)를 전부 `false`+`0`으로 뭉개고 **경고 로그 없음** — 호출측이 bool을 안 보면 0이 계산식에 샌다. ② **값-동등성 key 취약:** 조회는 `BackingDefinition.Equals`(captureSource+attribute AQN+snapshot)라 **셋 중 하나만 달라도 매치 실패→false**. 조회 정의는 선언 정의와 완전히 같아야 함(SpeedBoost처럼 같은 static 필드 재사용이 안전). ③ **captureValueType 라우팅:** params 메서드가 Magnitude=Current / BaseValue=Base로 고정 분기인데 authoring의 `AttributeBasedMagnitude.captureValueType`(`:33`)가 Base/Current를 데이터로 지정 — 5b/Execute가 이 값에 맞춰 올바른 메서드를 호출해야 authoring 의도와 맞는다. _(2026-08-04 등록 — 캡처 조회 경로 분석 중 발견)_
- [ ] **(docs)** **attribute 런타임 타입 이름 정렬 + D5(직렬화/런타임 참조 분리) 재작성** — 코드는 `GameplayAttributeHandle`인데 2026-08-21 세션에 내가 적은 문서·주석 일부가 아직 `ResolvedGameplayAttribute`다(그 이름으로 확정했다가 유저가 되돌림). 정정 대상: attribute `decisions.md`(D5(직렬화/런타임 참조 분리))·`progress.md`, architecture `overview`·`attribute`·`gameplay-effect`, 잔재 2건(`AttributeAggregator.cs` 주석 `Dictionary<ResolvedGameplayAttribute,…>`·`GameplayEffectSpec.cs` 경고 문자열). **D5(직렬화/런타임 참조 분리)는 근거 자체 재작성 필요** — "Handle이 부정확"이라 `Resolved-`를 택한 논리가 최종 `GameplayAttributeHandle`(Handle 유지)과 모순. 유저에게 "왜 Handle로 되돌렸나" 받아 그 근거로 다시 쓴다(HARNESS '왜는 지어내지 않는다'). _(2026-08-21 등록)_
  - **2026-10-18:** decisions 정리로 attribute D5는 "치운 결정"으로 내려갔다(이름은 `GameplayAttributeHandle`로 되돌아갔고 분리는 D1·gameplay-effect D15). D5 재작성은 더 필요 없고, 남은 일은 문서·주석의 `ResolvedGameplayAttribute` 잔재 정리뿐.
- [ ] **(editor)** **GE Executions 리스트에 New Script 지원** — Attribute Sets 리스트엔 New Script가 있으나 Executions엔 없음(2026-08-07 Add만 붙임). 이유: `GameplayEffectExecution.Execute`가 abstract라 빈 서브클래스 템플릿(`: GameplayEffectExecution {}`)이 컴파일 안 됨. 붙이려면 New Script 템플릿(`SubclassScriptTemplate.BuildSource`)이 **abstract 멤버의 override 스텁 생성**을 해야 함(예: `Execute(...) => throw new NotImplementedException();`). 그 스텁 생성만 추가하면 GE Executions·다른 abstract-base 셀렉터에도 켤 수 있음. _(2026-08-07 등록)_
- [ ] **(code)** **타입 참조를 rename-safe 하게 (AttributeSet·GE Execution)** — 지금 둘 다 타입을 **AQN 문자열**로 저장(`attributeSetTypeName`·`executionTypeNames`)해, 클래스명·네임스페이스·asmdef 이동 중 뭐든 하면 `Type.GetType` 실패→"Missing"으로 링크가 끊긴다. **지금은 미룸** — 규모 작고(서브클래스 몇 개), 깨져도 인스펙터에 보이고 검색 드롭다운에서 다시 고르면 됨. 고치는 값이 큼(포맷·리졸버·드로어·런타임·New Script 자동배정·기존 에셋 마이그레이션 전부)이라 과설계에 가깝다. **재평가 트리거:** rename이 잦아져 여러 에셋을 반복해 다시 잇게 되면. **하게 되면:** 안정 GUID id(`[StableTypeId(guid)]` + id→Type 리졸버, AQN 폴백 가능) — 둘 다 문자열 저장이라 통일 적용됨. (`[MovedFrom]`은 커스텀 `Type.GetType` 방식엔 안 먹혀 탈락, SerializeReference는 GE만 가능.) _(2026-08-07 등록 — New Script 툴 논의 중 도출)_

## 📋 TODO (뒤로 미룸)

> 2026-10-18 목표 전환으로 미뤘다. 항목 본문은 옮기기 전 그대로다.
> 2026-10-20 유저 "TODO 쪽에 이전에 GAS 제외한 TODO인 부분들 다 치우자. 아카이빙은 해두고", "_parked로 옮겨" — 게임 쪽(아이템·캐릭터·아트) 항목은 [`_parked/agent/TODO-BOARD.md`](../_parked/agent/TODO-BOARD.md)로 보관. 그 때문에 '2026-10-18 목표에서 미룬 것'(GE·Tag는 이미 'GE·Tag 남은 것'으로 옮김)·'이전 당장 목표·진행 중이던 것' 소제목은 비어 지웠다. 남은 건 GAS·문서 인프라 항목.

> **이전 우선순위 낮음** (아래 GAS 대분류·기술은 포트폴리오 방향 전환(2026-08-09, [design.md 포트폴리오 방향](../project/design.md))의 목표 기능이다. 데모 씬 확정 후 필요분만 착수·feature 등록한다. 각 기능은 UE-complete가 아니라 "최소한 진짜 도는 버전".)

- [ ] **(code)** **GA — UE 대비 미구현(데모 뒤로 미룸)** — 2026-10-21 유저 "GameplayAbility 남은거 있어? UE 기능과 비교해서 확실하게 확인해" → "싹 정리하고 그 중에서 현재 데모를 목표로 하는데 필수로 필요한 건 현재 TODO로 넣어두고 나머지는 미루는 TODO로 해". UE 5.6 `GameplayAbility.h`·`AbilitySystemComponent.h` 선언을 우리 GA·ASC·Spec과 대조(헤더 grep). 데모 필수는 '데모 전 GAS 구현' 2-9로, 나머지가 아래. 필수/미룸 구분은 에이전트 판단(유저 미확정). D# = gameplay-ability decisions에 뺀 이유가 있는 것. _(2026-10-21 등록)_
  - **입력** — GA `InputPressed`/`InputReleased`, ASC `AbilityLocalInputPressed/Released`·`PressInputID/ReleaseInputID`·`FindAbilitySpecFromInputID`·`ClearAllAbilitiesWithInputID`·`Block/UnBlockAbilityByInputID`·`IsAbilityInputBlocked`(`AbilitySystemComponent.h:1117-1410`). 우리 Spec엔 `InputID` 값만 있다. 데모 입력은 게임 쪽에서 `TryActivateAbility(handle)`로('데모 제작 > 입력 바인딩').
  - **발동 방식** — `TryActivateAbilityByClass`(우리 쪽이면 에셋으로)·`TryActivateAbilitiesByTag`·`GetActivatableGameplayAbilitySpecsByAllMatchingTags`(`:1044-1060`). 태그 쪽은 아래 어빌리티 태그가 먼저.
  - **커밋 세부** (D7) — `CommitAbilityCooldown`·`CommitAbilityCost`(+`ForceCooldown`). 실패 태그 out 인자는 2-9에서.
  - **생명주기 세부** (D4) — `CanBeCanceled`/`SetCanBeCanceled`(+ ASC `HandleChangeAbilityCanBeCanceled` — 2-10 때도 D4대로 뺌, 2026-10-21 유저 "그것들 TODO로 해두고"), `bIsAbilityEnding`, GA 자체 스코프 락, 재발동 `bRetriggerInstancedAbility`. 기록 없이 빠진 것: `SetRemoveAbilityOnEnd`, `GiveAbilityAndActivateOnce`(progress엔 뺀 것으로 있음), `Get/SetUserAbilityActivationInhibited`, `ClearActorInfo`·`RefreshAbilityActorInfo`, `NotifyAvatarDestroyed`.
  - **이벤트 발동** — `AbilityTriggers`, `ShouldAbilityRespondToEvent`, `TriggerAbilityFromGameplayEvent`, `HandleGameplayEvent`, `SendGameplayEvent`, `IsTriggered`. 선행: 'GE·Tag 남은 것 > Tag > Gameplay Event'.
  - **GE 연동 세부** (D5) — 컨텍스트 `SetAbility`·SourceObject 타입(보류), `ApplyAbilityTagsToGameplayEffectSpec`. 기록 없이 빠진 것: `GetCurrentSourceObject`/`GetSourceObject`, GE가 부여한 어빌리티(`RemoveGrantedByEffect`·`GetGrantedByEffectContext`·ASC `FindAbilitySpecFromGEHandle` — progress엔 뺀 것으로 있음), GA의 GE 제거 헬퍼(`BP_RemoveGameplayEffectFromOwnerWith*` — ASC엔 같은 기능 있음).
  - **Source/Target 태그 조건** — `SourceRequiredTags`·`SourceBlockedTags`·`TargetRequiredTags`·`TargetBlockedTags`(`GameplayAbility.h:779-791`)와 `DoesAbilitySatisfyTagRequirements`의 sourceTags/targetTags 인자. UE에선 이벤트 발동 데이터(TriggerEventData)의 Instigator/Target 태그로만 검사돼 이벤트 발동과 같이 들어온다. 2026-10-21 2-10 착수 때 유저 "빼기" → "해당 나중에 TODO로 해둬".
  - **취소 Ignore 인자** — ASC `CancelAllAbilities(Ignore)`(`AbilitySystemComponent.h:1089`). D8 때 쓰는 곳이 없어 뺐고 2-10에서도 `CancelAbilitySpec`·`CancelAbilities`에만 넣음. 2026-10-21 유저 "그것들 TODO로 해두고".
  - **인스턴싱** (D6) — `NonInstanced`.
  - **편의** — `GetCurrentAbilitySpec`, 인자 없는 `GetCooldownTimeRemaining()`, 데이터 검증 `IsDataValid`(GE 쪽 'GE 데이터 검증'과 같은 종류).
  - **큰 기능(GA 범위 밖으로 둔 것)** — AbilityTask·GameplayTask(`ActiveTasks`·`*TaskByInstanceName`·`EndAbilityState`), 몽타주(`CurrentMontage`·`Montage*`·ASC `PlayMontage`·`GetAnimatingAbility`), 타겟팅(TargetData·`MakeTargetLocationInfo*`), GA에서 Cue 호출(`K2_*GameplayCue*` — Cue는 데모 전 범위에서 뺌).
  - **안 함(UE 고유 제약이라 해당 없음)** — 네트워크·복제·예측(Replication/NetExecution/NetSecurity 정책, `HasAuthority`·`IsPredictingClient`·`ConfirmActivateSucceed`·`MarkAbilitySpecDirty` 등), 블루프린트 래퍼(`K2_*`·`BP_*`), 스켈레탈 메시 조회.

- [ ] **(bug)** **GA 타입 선택 목록에 테스트 전용 클래스 노출** (2026-10-21 발견, 미수정) — `EditorTypeUtility.GetConcreteSubclasses(GameplayAbility)` 후보에 `Test.AbilitySystemLifecyclePlayModeTest+LingeringAbility`(이번 세션 PlayMode 테스트, 런타임 어셈블리)·`Test.Editor.GameplayAbilitySelfCheck+RecordingAbility`(기존 셀프체크)가 `TestGameplayAbility`와 함께 뜬다. 중첩 클래스라 스크립트 파일이 없어 고르면 아이콘이 비고 ping/open도 안 된다(유저가 `GA_New`에서 겪음). 고칠지·어떻게(선택 목록에서 숨김 표시 / 테스트 클래스를 따로 파일로) 유저 판단. 2026-10-21 유저 "GA 타입 선택 목록에 테스트 전용 클래스 노출 이거 뒤로 미루고" → '데모 전 GAS 구현 > 2. GameplayAbility'에서 이리로 옮김. _(2026-10-21 등록)_
- [ ] **(code)** **GA 명시적 해제(인스턴스 무효 표시)** — UE `MarkAsGarbage` 대응(무효 표시·참조 끊기·정리 훅/Dispose). 해제된 GA 인스턴스를 밖에서 들고 있으면 계속 부를 수 있다(ASC를 거치면 Spec이 없어 못 씀). 2026-10-21 유저 "GA를 당장 여기서 Dispose를 두기에는 다른 곳들도 사실 할만한 게 많아서 다음으로 미루자"(제안했던 안: A 무효 표시 / B A+정리 훅 / C 약한 핸들), 같은 날 "GA 명시적 해제는 따로 빼둬" → '데모 전 GAS 구현 > 2. GameplayAbility'에서 이리로 옮김. 원래 'ASC 파괴 시 어빌리티 정리 없음' 항목에 묶여 있었고, 파괴 쪽은 2026-10-21 해결(→ TODO-DONE). _(2026-10-21 등록)_
- [ ] **(code)** **GA 실패 사유를 태그 + 설정 에셋 대신 실패 정보(종류 enum + 원인 태그)로 바꿀지** — 2026-10-21 유저 "굳이굳이 Tag가 필요해? 그냥 실패했다고 정보에 각각 뭐가 실패했는지 보내면 되는거 아냐?" → 비교(enum: 설정 에셋·레지스트리 불필요·누락 없음 / 하위 어빌리티 자기 사유 추가가 어렵고 UE와 시그니처 다름) 뒤 "일단 내비둬 탐탁친 않네". 지금은 D9(태그 + `GameplayAbilitiesSettings`) 유지. _(2026-10-21 등록)_

- [ ] **(infra)** **Ability System 플러그인화(asmdef/UPM 패키지)** — 유저 의도: "애초에 Ability System은 별개의 플러그인처럼". 2026-10-12 논의 후 **유저가 미룸**. 에이전트 제안(미확정): `Packages/com.<scope>.abilitysystem` embedded package(개발·설치 경로 동일). 정할 것: 패키지 이름(scope), feature-local `Editor/` 폴더 처리(asmdef 아래선 Editor 폴더 규칙이 안 먹힘 → 표준 Runtime/Editor 구조 vs `.asmref`), `Core.Common` 처리(패키지 안으로 vs 별도 common 패키지), 브랜치. 확인한 사실: 에셋에 `Core.AbilitySystem…, Assembly-CSharp` AQN 저장 0건, `Common`→AbilitySystem 코드 의존 없음. 착수 시 gameplay-tag D5(생성 차단=Request 입구+internal 생성 함수)의 `GameplayTag.Create`가 실제로 막히는지 확인. _(2026-10-12 등록)_

- [ ] **(editor, bonus)** **에셋 그래프(노드) 에디터** — 시각적 payoff 크나 GraphView 직렬화 rabbit hole. **맨 마지막 선택적 보너스.** 1~2일 스파이크로 리스크 확인 후, 가장 좁은 조각(GE 하나 저작→SO)으로만. 데모 성공을 여기 걸지 않음. _(2026-08-09 등록)_

- [ ] **(docs)** **UE Execution 실행조건 원문 확정분 반영 여부** — 2026-08-05 ylyking UE 미러 원문으로 "period 0 Duration/Infinite에선 Execution 안 돎"을 확정(근거: `ExecuteGameplayEffect` asc.cpp:820 `check(...)` assert + Execute 경로 라인). 이걸 `architecture/ability-system/gameplay-effect.md`나 gameplay-effect `decisions.md`에 근거(파일·라인)와 함께 남길지 **유저 판단 대기**. 위 19번(GameplayEffectType 실전 상태 확정)과 인접하나 별개(enum 확정 ≠ Execution 실행조건). _(2026-08-05 등록)_
- [ ] **(wiki)** 설계 문서 상호 연결 — 각 섹션 요약에서 `dev-docs/project/architecture/*` 심화 문서로 링크. 로컬/Pages에선 `.md`가 raw로 뜨므로 **GitHub blob URL** 사용. 저장소 public 전제. `SECTION_META.sources`도 같은 방식으로 클릭 가능하게. _(2026-07-05 등록)_
- [ ] **(docs/refactor)** **에이전트 생성 "왜"의 유저 소유화 — 이번 세션(2026-08-04) 산출물 정리** — 유저 지적: 코드 주석·decisions(특히 D21(ModifierSpec UE 정렬))에 에이전트가 만든 근거가 많고 본인 생각이 반영 안 됨(HARNESS '왜는 지어내지 않는다'/'왜의 근거는 유저 의도 → UE 원본'). 방침: **코드 주석은 사실(무엇/어떻게+함정)만 lean하게, "왜"는 decisions에만·유저가 소유한 것만.** 접근 미정 — ① `GameplayModifierSpec` 시범 후 세션 파일(`AttributeBasedMagnitude`·`GameplayModifier`·`GameplayModifierSpec`·`GameplayEffectSpec`) 톤 일괄 적용(추천) ② D21(ModifierSpec UE 정렬)부터 같이 훑으며 근거 재작성/삭제. 하네스 규칙화(주석 lean·왜는 유저 소유)도 유저 승인 시 docs.harness '각색은 쓰기 전에 묻는다'/HARNESS '왜는 지어내지 않는다'에 추가. _(2026-08-04 등록)_
- [ ] **(docs)** architecture 하류 문서 **반영 기준 마커 정비** — `camera.md`·`item-equipment.md`·`controller-character.md`·`architecture/overview.md`는 마커 자체가 없어 architecture/HARNESS '최종 갱신' 마커의 낡음 판별(하류 날짜 < 상류 날짜)이 불가능하다. 실제 반영 날짜를 알 수 없어 **지어내지 않고 그대로 뒀다**(그 절 "모르는 날짜는 지어내지 않는다"). 채우려면 **유저가 날짜를 알려주거나, 다음에 그 문서를 실제로 갱신할 때 그날 날짜로 stamp**한다. 더불어 `ability-system/overview.md`는 상류를 feature-id가 아닌 **그룹명**(`ability-system`)으로 적어, 어느 progress와 비교할지 모호하다(그룹 대표 규칙을 정하든 하위 3개 중 하나로 바꾸든 결정 필요). _(2026-07-19 등록 — 하네스 점검 중 발견)_
- [ ] **(docs)** architecture 문서 **UE 구조 미러링 분류 검토** — UE 모듈/타입 계층(`GameplayAbilities` → `U*` → `F*`)을 참고한 폴더·네이밍 분류. 억지로 서두르지 않음. _(상위/하위 계층 정리·Capture 전용 문서(`capture.md`) 분리는 2026-09-10 완료.)_ 상세 `architecture/HARNESS.md` '문서 구조'. _(2026-09-10 등록 — 유저 발의)_
- [ ] **(docs)** **§번호 제거 잔여분** — 이번(2026-08-22) 스윕에서 **의도적으로 남긴 것**: ① ✅ Done 로그의 과거 § 기록(동결 이력 — 당시 섹션번호를 서술한 기록이라 §9로 보존) ② `ue-reference.md` 자체 섹션 번호(§2·§9·§11 — 자기완결 UE 참고 자료, decisions/worklog가 "참고문서 §N"으로 참조) ③ `wiki/HARNESS.md` 자체 번호(§2·§2.1). ①은 이력이라 그대로 두는 게 맞고, ②③은 해당 문서 자체를 이름 기반으로 개편할지 유저 판단 필요. _(2026-08-22 등록)_

## 🔧 진행 중

(없음)

## ✅ Done

완료 항목은 [TODO-DONE.md](TODO-DONE.md)로 옮긴다(2026-10-20부터 별도 파일).
