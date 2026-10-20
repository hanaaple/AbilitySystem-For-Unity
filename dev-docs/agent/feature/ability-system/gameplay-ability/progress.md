# gameplay-ability — 게임플레이 어빌리티 (GA·스킬 실행)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-10-20 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [aggregator](../aggregator/progress.md).
> **구현 방향·작업 프로세스는 [HARNESS.md](HARNESS.md)** · **요구사항 정의서(기획서)는 [requirements.md](requirements.md)** — 요구사항(SO·UE 유사·동작 동일·핵심만·판단 유동)과 진행 방식(이해 선행·구현 주체 유동·테스트)을 고정한다.
> 골격 경위: 2026-09-13 stash에서 복원했던 초안(GA 스크립트 4개 + `ASC.GiveAbility` + `GA_New.asset`)은 커밋 없이 유실됐다(2026-10-20 git 이력·stash·unreachable 객체·디스크 검색 0건). **2026-10-20 유저 지시("이전 골격만")로 아래 설계 개요 그대로 다시 만들었다**(빈 시그니처, `feat/gameplay-ability`). `GA_New.asset`은 만들지 않았다. Spec `Level`은 `int`(UE `FGameplayAbilitySpec::Level`과 같음 — 원래 초안의 타입은 기록 없음).

## 목표
GAS의 `GameplayAbility`를 이 프로젝트에 **SO 기반·UE 유사 구조·동작 동일**로 이식한다. 스킬·액션의 실행 단위 — 생명주기(`ActivateAbility`/`EndAbility`), 코스트(GE)·쿨다운(Duration GE), ASC 슬롯 등록, 입력 바인딩. 최종적으로 데미지 적용이 GA→GE로 배선돼 실제 전투 수치가 흐른다. **전부 이식하지 않고 핵심만** 가져오며, 범위는 진행하며 판단·번복한다(→ [HARNESS.md](HARNESS.md) 요구사항).

## 수용 기준 (Definition of Done)
- [ ] `GameplayAbility` 정의 + `ActivateAbility`/`EndAbility` 생명주기
- [ ] 코스트(GE)·쿨다운(Duration GE) 적용
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
  - `GameplayAbility` — `[Serializable]` abstract 일반 C# 클래스(하위 클래스도 `[Serializable]` 필요). 런타임 부분 — 부여 시 원형을 `SerializedDeepCopy.Clone`으로 복제해 액터별 상태·동작. `CurrentActorInfo`·`CurrentSpecHandle`(+ `Get…FromActorInfo` 3개), 훅 `OnGiveAbility(actorInfo, spec)`(액터 정보 저장 + Avatar 있으면 `OnAvatarSet`, 재정의 시 base 호출)·`OnRemoveAbility`·`OnAvatarSet` — UE와 같음.
  - `GameplayAbilitySpec` — 생성자 `(asset, level=1, inputID=NoInputID(-1), sourceObject=null)`가 핸들 발급(UE와 같음). Handle·Ability·Level·InputID·SourceObject·ActiveCount(`IsActive`=ActiveCount>0)·PendingRemove·`GetPrimaryInstance()`. UE FGameplayAbilitySpec 대응.
  - `GameplayAbilitySpecHandle` — struct(id만, IEquatable). `GenerateNewHandle()` static 발급(ASC 간 전역 유일). UE FGameplayAbilitySpecHandle 대응(owner 없음 = UE).
  - `GameplayAbilitySpecContainer` — UE 구조 그대로: `Items`(List, ASC가 직접 Add·RemoveAtSwap·Clear)·`Owner`·`RegisterWithOwner`(ASC Awake). 뺀 것: 네트워크 직렬화(NetDeltaSerialize·ShouldWriteFastArrayItem·MarkItemDirty/MarkArrayDirty). ASC `ClearAbility`도 UE처럼 인덱스 순회 + RemoveAtSwap(순서 보존 안 함).
  - `GameplayAbilityActorInfo` — OwnerActor·AvatarActor(GameObject)·ASC, `InitFromActor`. UE FGameplayAbilityActorInfo 축소(PlayerController·Mesh·Anim 등 없음).
  - `ScopedAbilityListLock`(using 블록)·`ConsiderPending`(flags) — UE FScopedAbilityListLock·EConsiderPending.
  - ASC 어빌리티 API(UE 구조) — `AbilityActorInfo`(Awake에서 `InitAbilityActorInfo(gameObject, gameObject)`)·`InitAbilityActorInfo`(Avatar 바뀌면 `OnAvatarSet` 통지)·`GiveAbility(spec)`(정의 null·런타임 클래스 무효면 LogError+Invalid, 락 중이면 보류)·`ClearAbility(handle)`(보류 부여 취소 / 락 중이면 PendingRemove / 아니면 잠근 채 `OnRemoveAbility`→목록 제거)·`ClearAllAbilities`·`FindAbilitySpecFromHandle(handle, ConsiderPending=PendingRemove)`·`FindAbilitySpecFromAsset`(UE FindAbilitySpecFromClass — UE 클래스 = 여기 에셋)·`GetActivatableAbilities`·`Increment/DecrementAbilityListLock`(마지막 락 해제 시 보류 처리, ClearAll 우선) + protected virtual `OnGiveAbility`·`OnRemoveAbility`·`CreateNewInstanceOfAbility`. 뺀 것: 네트워크·복제, AbilityTriggers, GE가 부여한 어빌리티, 인스턴싱 정책 선택(InstancedPerActor만), GiveAbilityAndActivateOnce(발동 구현 때). 활성 중 해제 시 EndAbility는 생명주기 때(TODO 주석).
- 선행 계층 활용: GE 적용은 ASC `ApplyGameplayEffectToSelf`/`ApplyGameplayEffectToTarget`/`MakeOutgoingSpec`(구현됨)로, 코스트/쿨다운은 각각 Instant/Duration GE로 표현.

## 세부 TODO (구현 체크리스트)
- [ ] 1. `GameplayAbility` 정의 + 생명주기
- [ ] 2. 코스트·쿨다운(GE) 배선
- [ ] 3. ASC 슬롯·입력 바인딩 — 슬롯(부여·해제)은 2026-10-20 코드 완료(에디터 미확인). 입력 바인딩은 TODO-BOARD '데모 제작'으로 옮김. 세분화는 TODO-BOARD '2. GameplayAbility' 2-1~2-8.
- [ ] 4. 데미지 GE 배선

## 결정 기록
[decisions.md](decisions.md) — D1 정의(단일 SO)·런타임(C# 클래스) 분리, D2 에셋은 타입만(⛔ D3로 번복), D3 SerializeReference 원형 + 부여 때 깊은 복사.

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 `## 다음 작업`.

## 블로커
- 없음. 선행 수치/이펙트 계층 준비됨(attribute·gameplay-effect·aggregator 임시완료). 유저가 GA 착수를 지시(2026-09-13)해 이전 "데모 씬 확정 전 폴더 신설 금지" 제약은 해제됨.

## 다음 작업
1. **TODO-BOARD 2-2 생명주기**(유저 신호 대기) — `GameplayAbility`에 virtual `CanActivateAbility`·`ActivateAbility`·`EndAbility`·`CancelAbility`, ASC `TryActivateAbility(handle)`·`CancelAbility`, Spec `ActiveCount` 관리. ASC `OnRemoveAbility`의 TODO 주석 자리에 활성 인스턴스 `EndAbility` 추가(UE `AbilitySystemComponent_Abilities.cpp:627-638`). 셀프체크에 발동·종료 묶음 추가.
2. 이어서 2-3 GA에서 GE 적용 → 2-4 코스트 → 2-5 쿨다운 → 2-6 `CommitAbility` → 2-7 태그 조건 → 2-8 검증(TODO-BOARD '2. GameplayAbility').

## 검증
- 2026-10-20 에디터 셀프체크 `Tools/Ability System/Run Gameplay Ability Self-Check`(`Assets/Scripts/Test/Editor/GameplayAbilitySelfCheck.cs`) — **58/0**. A 부여 기본 / B 원형 깊은 복사 / C 같은 정의 여러 번 / D 해제(OnRemove 시점·순서) / E 잘못된 입력(의도된 에러 로그 3줄) / F 스코프 락·보류(부여·해제·취소·ClearAll 재진입·락 중 ClearAll) / G 액터 정보(OnAvatarSet, Owner null 거부 — 의도된 에러 로그 1줄) / H 정의 에셋 인스펙터(드로어 높이)·저장(임시 `Assets/_Test/GA_SelfCheck_Temp.asset` 만들고 지움).
- 같은 날 기존 셀프체크 기준값 유지: Aggregator 83/0 · Calculation Modifier 19/0 · GE Component 89/0 · Modifier Tag 65/0(태그 경고 1줄 정상).
- 미확인: 실제 인스펙터 화면(눈으로), Play 모드.
