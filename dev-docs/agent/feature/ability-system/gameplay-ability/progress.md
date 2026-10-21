# gameplay-ability — 게임플레이 어빌리티 (GA·스킬 실행)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-10-21 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [aggregator](../aggregator/progress.md).
> **구현 방향·작업 프로세스는 [HARNESS.md](HARNESS.md)** · **요구사항 정의서(기획서)는 [requirements.md](requirements.md)** — 요구사항(SO·UE 유사·동작 동일·핵심만·판단 유동)과 진행 방식(이해 선행·구현 주체 유동·테스트)을 고정한다.
> 골격 경위: 2026-09-13 stash에서 복원했던 초안(GA 스크립트 4개 + `ASC.GiveAbility` + `GA_New.asset`)은 커밋 없이 유실됐다(2026-10-20 git 이력·stash·unreachable 객체·디스크 검색 0건). **2026-10-20 유저 지시("이전 골격만")로 아래 설계 개요 그대로 다시 만들었다**(빈 시그니처, `feat/gameplay-ability`). `GA_New.asset`은 만들지 않았다. Spec `Level`은 `int`(UE `FGameplayAbilitySpec::Level`과 같음 — 원래 초안의 타입은 기록 없음).

## 목표
GAS의 `GameplayAbility`를 이 프로젝트에 **SO 기반·UE 유사 구조·동작 동일**로 이식한다. 스킬·액션의 실행 단위 — 생명주기(`ActivateAbility`/`EndAbility`), 코스트(GE)·쿨다운(Duration GE), ASC 슬롯 등록, 입력 바인딩. 최종적으로 데미지 적용이 GA→GE로 배선돼 실제 전투 수치가 흐른다. **전부 이식하지 않고 핵심만** 가져오며, 범위는 진행하며 판단·번복한다(→ [HARNESS.md](HARNESS.md) 요구사항).

## 수용 기준 (Definition of Done)
- [x] `GameplayAbility` 정의 + `ActivateAbility`/`EndAbility` 생명주기 (2026-10-20, 셀프체크 기준 — Play 모드 미확인)
- [x] 코스트(GE)·쿨다운(Duration GE) 적용 (2026-10-21, 셀프체크 기준 — Play 모드 미확인)
- [ ] ASC 슬롯 등록 + 입력 바인딩
- [ ] 어빌리티 발동이 GE 적용으로 대상 어트리뷰트(예: Health)를 변경

## 범위
### 포함
- GA 실행 계층, 코스트/쿨다운, 입력 바인딩, 데미지→GE 배선
### 제외 (현 시점 — 이후 확장 가능)
- GameplayTag(후순위)
- 복합 데미지 계산 자체는 [gameplay-effect](../gameplay-effect/progress.md)의 Execution/AttributeBased(구현됨)로 표현 — GA는 그 GE를 발동만 한다

## 설계 개요
- 아키텍처(계획): [architecture/ability-system/gameplay-ability.md](../../../../project/architecture/ability-system/gameplay-ability.md)
- 코드(현 골격, `Assets/Scripts/Core/AbilitySystem/Abilities/`):
  - `GameplayAbilityAsset` — sealed SO(메뉴 `Ability System/Gameplay Ability`). 불변 정의. 필드: `abilityTemplate`(`[SerializeReference] [SubclassSelector(typeof(GameplayAbility))]` — 하위 클래스 필드까지 인스펙터에 그려지고 저장되는 원형) — D1·D3.
  - `GameplayAbility` — `[Serializable]` abstract 일반 C# 클래스(하위 클래스도 `[Serializable]` 필요). 런타임 부분 — 부여 시 원형을 `SerializedDeepCopy.Clone`으로 복제해 액터별 상태·동작. `CurrentActorInfo`·`CurrentSpecHandle`(+ `Get…FromActorInfo` 3개), 훅 `OnGiveAbility(actorInfo, spec)`(액터 정보 저장 + Avatar 있으면 `OnAvatarSet`, 재정의 시 base 호출)·`OnRemoveAbility`·`OnAvatarSet` — UE와 같음. 생명주기(2-2, D4) — `IsActive`, virtual `CanActivateAbility(handle, actorInfo)`(Avatar·ASC·Spec 확인)·`PreActivate`(활성 표시·액터 정보·Spec `ActiveCount++`)·`ActivateAbility`(빈 본체)·`EndAbility(handle, actorInfo, wasCancelled)`(`IsEndAbilityValid` 통과 시 비활성 + ASC `NotifyAbilityEnded`)·`CancelAbility`(= 취소로 EndAbility), `CallActivateAbility`·`IsEndAbilityValid`. GE 적용(2-3, D5) — virtual `MakeEffectContext(handle, actorInfo)`(Instigator = ASC, EffectCauser = Avatar, GameObject 출처만 SourceObject)·`MakeOutgoingGameplayEffectSpec(handle, actorInfo, effect, level)`(+ 현재 Spec 판), `ApplyGameplayEffect(Spec)ToOwner`·`ApplyGameplayEffect(Spec)ToTarget(…, target ASC, …)`·`GetAbilityLevel()`. 인스턴싱(D6) — 직렬화 필드 `instancingPolicy`(기본 Per Execution, UE와 같음)·`InstancingPolicy`·`IsInstantiated`(복제본만 true, 원형엔 `SetCurrentActorInfo`가 액터 정보를 안 씀). 코스트·쿨다운·커밋·태그(D7) — 직렬화 필드 `costGameplayEffect`·`cooldownGameplayEffect`·`activationOwned/Required/BlockedTags`, `CanActivateAbility`가 `CheckCooldown`→`CheckCost`→`DoesAbilitySatisfyTagRequirements`, `CommitAbility`(=`CommitCheck`→`CommitExecute`: `ApplyCooldown`→`ApplyCost`), `GetCooldownTags`·`GetCooldownTimeRemaining(AndDuration)`, `PreActivate`/`EndAbility`가 ActivationOwnedTags를 루즈 태그로 더하고 뺌. ASC `CanApplyAttributeModifiers`·`HasAnyMatchingGameplayTags` 추가.
  - `GameplayAbilitySpec` — 생성자 `(asset, level=1, inputID=NoInputID(-1), sourceObject=null)`가 핸들 발급(UE와 같음). Handle·Ability·Level·InputID·SourceObject·ActiveCount(`IsActive`=ActiveCount>0)·PendingRemove·`GetPrimaryInstance()`(Per Actor일 때만)·`GetAbilityInstances()`(UE NonReplicatedInstances — Per Actor 1개 / Per Execution 활성 발동 수만큼). UE FGameplayAbilitySpec 대응.
  - `GameplayAbilitySpecHandle` — struct(id만, IEquatable). `GenerateNewHandle()` static 발급(ASC 간 전역 유일). UE FGameplayAbilitySpecHandle 대응(owner 없음 = UE).
  - `GameplayAbilitySpecContainer` — UE 구조 그대로: `Items`(List, ASC가 직접 Add·RemoveAtSwap·Clear)·`Owner`·`RegisterWithOwner`(ASC Awake). 뺀 것: 네트워크 직렬화(NetDeltaSerialize·ShouldWriteFastArrayItem·MarkItemDirty/MarkArrayDirty). ASC `ClearAbility`도 UE처럼 인덱스 순회 + RemoveAtSwap(순서 보존 안 함).
  - `GameplayAbilityActorInfo` — OwnerActor·AvatarActor(GameObject)·ASC, `InitFromActor`. UE FGameplayAbilityActorInfo 축소(PlayerController·Mesh·Anim 등 없음).
  - `ScopedAbilityListLock`(using 블록)·`ConsiderPending`(flags) — UE FScopedAbilityListLock·EConsiderPending.
  - ASC 어빌리티 API(UE 구조) — `AbilityActorInfo`(Awake에서 `InitAbilityActorInfo(gameObject, gameObject)`)·`InitAbilityActorInfo`(Avatar 바뀌면 `OnAvatarSet` 통지)·`GiveAbility(spec)`(정의 null·런타임 클래스 무효면 LogError+Invalid, 락 중이면 보류)·`ClearAbility(handle)`(보류 부여 취소 / 락 중이면 PendingRemove / 아니면 잠근 채 `OnRemoveAbility`→목록 제거)·`ClearAllAbilities`·`FindAbilitySpecFromHandle(handle, ConsiderPending=PendingRemove)`·`FindAbilitySpecFromAsset`(UE FindAbilitySpecFromClass — UE 클래스 = 여기 에셋)·`GetActivatableAbilities`·`Increment/DecrementAbilityListLock`(마지막 락 해제 시 보류 처리, ClearAll 우선) + protected virtual `OnGiveAbility`·`OnRemoveAbility`·`CreateNewInstanceOfAbility`. 뺀 것: 네트워크·복제, AbilityTriggers, GE가 부여한 어빌리티, 인스턴싱 정책 NonInstanced(Per Actor·Per Execution만 — D6), GiveAbilityAndActivateOnce. 생명주기(2-2) — `TryActivateAbility(handle)`(없는 핸들 경고·PendingRemove·Owner/Avatar 없음·CanActivate 거부·이미 활성이면 false, 스코프 락 안에서 `CallActivateAbility`)·`CancelAbility(asset)`·`CancelAbilityHandle(handle)`·`NotifyAbilityEnded`(ActiveCount--), `OnRemoveAbility`가 활성 인스턴스를 `EndAbility`(취소 아님) 후 알림(UE `AbilitySystemComponent_Abilities.cpp:627-638`).
- 선행 계층 활용: GE 적용은 ASC `ApplyGameplayEffectToSelf`/`ApplyGameplayEffectToTarget`/`MakeOutgoingSpec`(구현됨)로, 코스트/쿨다운은 각각 Instant/Duration GE로 표현.

## 세부 TODO (구현 체크리스트)
- [x] 1. `GameplayAbility` 정의 + 생명주기 — 2026-10-20 TODO-BOARD 2-2 완료(셀프체크).
- [x] 2. 코스트·쿨다운(GE) 배선 — 2026-10-21 TODO-BOARD 2-4~2-6 완료(셀프체크).
- [ ] 3. ASC 슬롯·입력 바인딩 — 슬롯(부여·해제)은 2026-10-20 코드 완료(에디터 미확인). 입력 바인딩은 TODO-BOARD '데모 제작'으로 옮김. 세분화는 TODO-BOARD '2. GameplayAbility' 2-1~2-8.
- [ ] 4. 데미지 GE 배선

## 결정 기록
[decisions.md](decisions.md) — D1 정의(단일 SO)·런타임(C# 클래스) 분리, D2 에셋은 타입만(⛔ D3로 번복), D3 SerializeReference 원형 + 부여 때 깊은 복사, D4 생명주기는 UE 흐름 뼈대만(네트워크·GA 자체 락·취소 불가 플래그 뺌 — 근거 미기록), D5 GA의 GE 적용은 UE 헬퍼대로·대상은 ASC 하나(컨텍스트 SetAbility·SourceObject 타입 보류), D6 인스턴싱 Per Actor·Per Execution만·정의 필드는 런타임 base·Per Execution의 CDO 역할은 원형, D7 코스트·쿨다운·커밋은 UE 그대로·태그 조건은 Activation 3종만, D8 ASC 복제는 설정만·파괴는 어빌리티 취소 → 해제(UE 결과와 같게), D9 어빌리티 이벤트·실패 사유는 UE 구조대로(사유 태그는 설정 에셋, 종료 이벤트 둘 — 근거 미기록), D10 어빌리티 태그·차단·취소는 UE 그대로·Source/Target 태그 조건은 뺌.

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 `## 다음 작업`.

## 블로커
- 없음. 선행 수치/이펙트 계층 준비됨(attribute·gameplay-effect·aggregator 임시완료). 유저가 GA 착수를 지시(2026-09-13)해 이전 "데모 씬 확정 전 폴더 신설 금지" 제약은 해제됨.

## 다음 작업
1. TODO-BOARD '2. GameplayAbility' 2-1~2-10 완료(2026-10-21 — GA 셀프체크 160/0). **데모 전 GA 필수 끝 — 다음은 데모 제작.** UE 대비 나머지는 TODO-BOARD '뒤로 미룸 > GA — UE 대비 미구현'. 그다음 데모 제작. 남은 GA 보류: 해제된 GA 인스턴스 무효 표시(TODO-BOARD "뒤로 미룸" 칸으로 뺌).

## 검증
- 2026-10-20 에디터 셀프체크 `Tools/Ability System/Run Gameplay Ability Self-Check`(`Assets/Scripts/Test/Editor/GameplayAbilitySelfCheck.cs`) — **98/0**(2026-10-21 인스턴싱 뒤; 2-3 뒤 86/0, 2-2 뒤 74/0, 2-1 뒤 58/0). A 부여 기본 / B 원형 깊은 복사 / C 같은 정의 여러 번 / D 해제(OnRemove 시점·순서) / E 잘못된 입력(의도된 에러 로그 3줄) / F 스코프 락·보류(부여·해제·취소·ClearAll 재진입·락 중 ClearAll) / G 액터 정보(OnAvatarSet, Owner null 거부 — 의도된 에러 로그 1줄) / H 정의 에셋 인스펙터(드로어 높이)·저장(임시 `Assets/_Test/GA_SelfCheck_Temp.asset` 만들고 지움) / I 생명주기(발동·재발동 거부·종료·중복 종료 무시·취소·즉발형·CanActivate 거부·Avatar 없음·없는 핸들(의도된 경고 1줄)·정의로 취소·활성 중 해제/ClearAll은 End→OnRemove·발동 중 자기 해제·해제 대기 발동 거부) / J GA에서 GE 적용(컨텍스트 Instigator·Causer·출처, Spec 레벨, Owner·대상 적용, null GE·대상(의도된 에러 1줄), 레벨·없는 핸들(의도된 경고 1줄), GameObject 아닌 출처, ActivateAbility 안에서 대상에 적용) / K Per Execution(기본값, 부여 때 인스턴스 없음·원형이 OnGive/OnAvatarSet/CanActivate/OnRemove, 원형에 액터 정보 안 씀, 발동마다 새 인스턴스·동시 활성 2개, 종료·취소·즉발형 뒤 인스턴스 정리, 활성 여럿일 때 해제, Per Actor와 혼재).
- 같은 날 기존 셀프체크 기준값 유지: Aggregator 83/0 · Calculation Modifier 19/0 · GE Component 89/0 · Modifier Tag 65/0(태그 경고 1줄 정상). 2-2·2-3·인스턴싱 뒤(유저의 `CurrentAbilitySpecHandle` 이름 변경 반영 상태)에 5종 다시 돌려 모두 같음.
- 2026-10-21 코스트·쿨다운·커밋·태그(L 묶음 12개 + L-0) 작성 → 같은 날 실행 **109/2** — 코스트·쿨다운·커밋·필요/차단 태그 통과, **L-8·L-12(활성 중 ActivationOwnedTags) 실패**(복제된 태그 해시 0 — worklog). `SerializedDeepCopy` 수정 뒤 **111/0**(의도된 에러 5·경고 2), 회귀 4종 기준값 그대로.
- 2026-10-21 2-8 — 셀프체크 M(`TestGameplayAbility` 통합: 부여·발동→커밋→효과→종료·쿨다운 중·Tick 4초/10초로 만료·코스트 부족·차단 태그 8개) + N(커밋 경계: 커밋 전 쿨다운·무효 핸들/Spec/액터 정보·Per Execution 커밋 3개) → **122/0**(의도된 에러 5·경고 5). PlayMode `Assets/Scripts/Test/GameplayAbilityPlayModeTest.cs`(Play 중 빈 GameObject에 붙임, 쿨다운 3초) **5/1** — P-3 실패(TODO-BOARD 'GE Duration이 적용 프레임 deltaTime까지 차감').
- 2026-10-21 GE 수명 수정(gameplay-effect D27) 뒤 — GA 셀프체크 **123/0**(M-9 추가: 시간이 흐른 뒤 같은 프레임에 걸린 쿨다운은 그 프레임 Tick에서 안 줄어듦, 경계 Tick은 지속시간보다 넉넉히), PlayMode GA **6/0**(P-3 통과 — 커밋 프레임 끝 남은 쿨다운 3.000).
- 2026-10-21 ASC 복제·파괴(D8) — PlayMode `Assets/Scripts/Test/AbilitySystemLifecyclePlayModeTest.cs` **13/0**(C 복제 6 · D 파괴 5 · X ASC 간 라이브 캡처 2). 셀프체크 5종 회귀 그대로.
- 2026-10-21 GA 인스펙터 — 코스트·쿨다운 GE 적합 표시는 유저가 화면 확인("좋아 적절하네"). 선언 클래스별 상자(`GameplayAbilityAssetEditor`)는 컴파일·적용만 확인, 화면은 유저 확인 대기.
- 2026-10-21 전체 실행(유저 "테스트 한번 전부 하자") — 셀프체크 5종 GA 123/0·Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0, PlayMode 4종 GA 6/0·수명 13/0·Tag 9/0·Aggregator 14/0(셋업 메뉴 없이 기존 에셋을 런타임에 꽂아 실행). 에러 0.
- 2026-10-21 어빌리티 이벤트·실패 사유(D9) — 셀프체크 O 11개 → GA **134/0**(의도된 에러 5·경고 5). 사유 태그를 채운 뒤 전체 실행 — 셀프체크 5종 GA 134/0·Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0, PlayMode 4종 GA 6/0·수명 13/0·Tag 9/0·Aggregator 14/0. 에러 0.
- 2026-10-21 어빌리티 태그·차단·취소(D10) — 셀프체크 P 8개 → GA **142/0**. 회귀 Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag **64/1**(E-12 — Play 모드 뒤 에디터 `Time.time`이 0이 아니면 1초 경계 Tick이 빗나감, 이번 변경과 무관, TODO-BOARD), PlayMode 4종 GA 6/0·수명 13/0·Tag 9/0·Aggregator 14/0.
- 2026-10-21 GA 마무리 테스트 — 셀프체크 Q(기능 조합·재진입·경계) 18개 → GA **160/0**.
- 미확인: 선언 클래스별 상자 화면(밝은·어두운 스킨, 좁은 창). Play 모드는 위 PlayMode 테스트 2개 범위만.
