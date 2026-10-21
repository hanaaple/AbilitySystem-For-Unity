# gameplay-ability — 결정 기록

> 무엇을·어떻게 적나 → [feature/HARNESS.md](../../HARNESS.md) '결정 기록 기준'. `D#`는 feature 내 고유·불변 코드다.

---

## D1 — 정의(단일 SO)와 런타임(일반 C# 클래스) 분리

- 날짜: 2026-10-20
- 결정: UE `UGameplayAbility` 하나를 둘로 나눈다. `GameplayAbilityAsset`(sealed SO, 메뉴 1개, 불변 정의) + `GameplayAbility`(abstract 일반 C# 클래스, 부여될 때마다 새로 만들어 액터별 상태·동작을 가짐). 하위 클래스는 런타임 쪽만 만든다.
- 왜: (유저) "GA는 이벤트 함수, field를 직접 사용하는 게 중요" → SO 상속 + `Instantiate` 복제를 검토했으나, SO를 객체마다 복제하는 것에 대한 웹 조사(Unity 매뉴얼: SO 주 용도 = 공유 데이터 / 커뮤니티: 상태는 런타임 객체에) 뒤 "아무리 그래도 그렇지? Runtime Class 별도로 두는거로 가고, SO Asset은 단일로 가자".
- 대안: ① SO 상속 + 부여 시 `Instantiate`(클래스 1개·`this`로 정의값까지 / SO 본래 용도에서 벗어남·원본 실행 사고·`Destroy` 관리) ② SO 상속(메뉴·하위 클래스마다 에셋 작업).
- 대가: UE와 달리 "에셋 = 클래스"가 아니다. 런타임 클래스는 정의값을 에셋을 거쳐 읽는다.

## D2 — 에셋은 런타임 클래스 타입만 저장(클래스별 정의값 없음) — ⛔ D3로 번복

- 날짜: 2026-10-20
- 결정: `GameplayAbilityAsset`은 `[SubclassSelector(typeof(GameplayAbility))] string abilityClass`(AQN)만 들고, 부여 시 `Activator`로 생성한다(GE Execution과 같은 방식). 하위 클래스 고유의 직렬화 정의값(예: 반경·투사체)은 두지 않는다.
- 왜: 유저 선택(세 안 중 — 이유 미기록).
- 대안: ① 에셋이 `[SerializeReference] GameplayAbility` 템플릿을 들고 부여 시 복제(클래스별 값 가능 / 복제 구현 필요) ② 에셋이 타입 + `[SerializeReference]` 정의 데이터 객체(어빌리티당 클래스 2개).
- 대가: 어빌리티별 값은 에셋 공통 필드나 GE로만 표현한다.

## D3 — 에셋은 런타임 클래스 인스턴스를 `[SerializeReference]` 원형으로 저장, 부여 때 깊은 복사 (D2 번복)

- 날짜: 2026-10-20
- 결정: `GameplayAbilityAsset`이 `[SerializeReference] [SubclassSelector(typeof(GameplayAbility))] GameplayAbility abilityTemplate`을 든다. 하위 클래스의 직렬화 필드가 에셋 인스펙터에 그려지고 에셋마다 저장된다. 부여(`CreateNewInstanceOfAbility`) 때 `SerializedDeepCopy.Clone`으로 원형을 복제해 액터별 인스턴스를 만든다 — 직렬화 필드만 복사(값·List·배열·[Serializable] 중첩은 새로, UnityEngine.Object 참조는 유지, SerializeReference 공유 참조 유지), 나머지 필드는 새 인스턴스 초기값. (원형 실행 규칙은 D6에서 완화)
- 왜: (유저) "ability Class SubClassSelector 말고 SerializeReference로 하자 하위 field draw하고 저장할 필요가 있겠어". 복제 방식은 유저가 세 안(깊은 복사 / JsonUtility 왕복 / MemberwiseClone) 중 깊은 복사를 고름(이유 미기록 — 안 설명에 "UE `NewObject(원형)`과 같은 의미"를 적었음).
- 대안: D2(타입만, Activator) / JsonUtility 왕복(짧음 / UnityEngine.Object·중첩 SerializeReference 복원을 빌드에서 확인 필요) / MemberwiseClone(한 줄 / List·중첩 객체를 원형과 공유해 에셋 오염 위험).
- 대가: 하위 클래스마다 `[Serializable]` 필요(없으면 저장 안 됨). 클래스 이름을 바꾸면 SerializeReference 참조가 깨진다(`[MovedFrom]` 필요). 엔진 클래스(AnimationCurve·Gradient 등)는 복제하지 않고 원형과 공유한다. 원형 인스턴스를 직접 실행하지 않는다는 규칙.

## D4 — 생명주기는 UE 흐름의 뼈대만(발동 확인·발동·종료·취소·활성 횟수), 네트워크·GA 자체 락·취소 불가 플래그는 뺌

- 날짜: 2026-10-20
- 결정: UE 흐름 `TryActivateAbility`→`CanActivateAbility`→`CallActivateAbility`(`PreActivate`+`ActivateAbility`)→`EndAbility`→ASC `NotifyAbilityEnded`, 취소 `CancelAbility`→`EndAbility(취소)`, 활성 중 해제 시 `EndAbility`(취소 아님)를 그대로 따른다. 시그니처는 UE에서 네트워크 인자(ActivationInfo·bReplicate)·이벤트 인자(TriggerEventData·Source/TargetTags·실패 태그)를 뺀 `(handle, actorInfo[, wasCancelled])`. `InternalTryActivateAbility`는 `TryActivateAbility`에 합침. 뺀 것: GA 자체 스코프 락(`ScopeLockCount`·`WaitingToExecute`), `CanBeCanceled`/`SetCanBeCanceled`, `bIsAbilityEnding`, 종료·발동 델리게이트, 재발동(`bRetriggerInstancedAbility`), `CancelAbilities(태그)`. (`CancelAllAbilities`는 처음엔 뺐다가 2026-10-21 D8에서 ASC 파괴용으로 추가.)
- 왜: 근거 미기록(유저 확정 전). 에이전트 판단(유저 미확정): 빼는 것들은 각각 네트워크·AbilityTask·태그 차단/취소·이벤트 발동 — 이미 TODO에서 뺀 범위(TODO-BOARD '2. GameplayAbility')에 딸린 장치라 지금 쓰는 곳이 없다. 코스트·쿨다운·태그 조건은 2-4·2-5·2-7에서 `CanActivateAbility`에 붙인다. UE 근거: `AbilitySystemComponent_Abilities.cpp:1683`(InternalTryActivateAbility), `GameplayAbility.cpp:769`(EndAbility).
- 대가: 활성 중 다시 발동하면 항상 실패(UE 재발동 옵션 없음). 끝나지 않게 막는 구간(취소 불가)을 표현할 수 없다. 발동·종료를 밖에서 구독할 이벤트가 아직 없다(데모 표시가 필요하면 2-8에서).
- 재검토: AbilityTask·태그로 다른 어빌리티 취소(2-7 확장)·데모 HUD 이벤트가 필요해질 때.

## D5 — GA의 GE 적용은 UE 헬퍼를 따르되, 대상은 TargetData 대신 ASC 하나

- 날짜: 2026-10-20
- 결정: `MakeEffectContext`·`MakeOutgoingGameplayEffectSpec`·`ApplyGameplayEffect(Spec)ToOwner`·`ApplyGameplayEffect(Spec)ToTarget`·`GetAbilityLevel`을 UE대로 둔다. UE와 다른 점: 대상은 `AbilitySystemComponent` 하나(UE는 `FGameplayAbilityTargetDataHandle` — 타겟팅은 GA 범위에서 뺌), `Stacks` 인자 없음(GE 스택 미구현), 반환은 핸들 하나. 컨텍스트는 Instigator = ASC, EffectCauser = Avatar(UE `AddInstigator(OwnerActor, AvatarActor)`에 대응). 효과 GE 필드는 base에 두지 않고 하위 클래스가 든다(UE base의 GE 필드는 코스트·쿨다운뿐 — `GameplayAbility.h:743`·`:751`).
- 왜: 유저 "일단 UE에 맞춰서 구현하고 있어봐". 대상 축소·스택 제외는 근거 미기록(유저 확정 전). 에이전트 판단(유저 미확정): 둘 다 TODO에서 뺀 기능(타겟팅·GE 스택)에 딸린 것.
- 보류(유저 답 대기): ① UE `MakeEffectContext`의 `Context.SetAbility(this)`(어빌리티·레벨을 컨텍스트에 넣음)는 안 넣었다 — 유저가 고치는 중인 `GameplayEffectContext.cs`에 필드를 더해야 해서. ② Spec `SourceObject`(Object)와 컨텍스트 `SourceObject`(GameObject, 유저 변경)가 달라 GameObject 출처만 넘긴다. ③ `ApplyAbilityTagsToGameplayEffectSpec`(어빌리티 태그 → Spec)·SetByCaller 복사는 해당 기능이 없어 안 함.
- 대가: 여러 대상에 한 번에 거는 건 호출측이 반복한다. GE 쪽에서 "어느 어빌리티가 걸었나"를 알 수 없다(①).
- 재검토: 타겟팅·GE 스택을 넣을 때, 컨텍스트 수정이 끝났을 때(①②).

## D6 — 인스턴싱 정책은 Per Actor·Per Execution 둘만, 정책 등 정의 필드는 런타임 base 클래스에, Per Execution의 CDO 역할은 원형

- 날짜: 2026-10-21
- 결정: `GameplayAbilityInstancingPolicy` = `InstancedPerActor`·`InstancedPerExecution`(UE `NonInstanced`는 뺌). 정책(이후 코스트·쿨다운·태그도) 필드는 `GameplayAbility` base 클래스의 직렬화 필드로 둔다 — 에셋 원형에 저장되고(D3) ASC는 원형에서 읽는다. Per Execution은 대표 인스턴스 없이 발동마다 복제본을 만들고 끝나면 버리며, `OnGiveAbility`·`OnRemoveAbility`·`OnAvatarSet`·`CanActivateAbility`는 에셋 원형이 받는다(UE CDO). 원형에는 액터 정보를 쓰지 않는다(UE `IsInstantiated` 가드). 기본값은 UE와 같이 `InstancedPerExecution`(`GameplayAbility.cpp:91`).
- 왜: (유저) "GA는 Per Instance, Per Actor(ASC, GameObject)만 구현할거야" → Per Instance = 발동마다 새 인스턴스 확인("ㅇㅇ 그거 맞아"). 필드 위치·CDO 역할은 유저가 각각 "런타임 base 클래스"·"원형이 CDO 역할"을 고름(이유 미기록). 앞선 지시 "UE에 맞춰서".
- 대안: 필드를 정의 에셋에 둠(D1 '불변 정의' 취지 / UE와 자리 다름·런타임은 에셋 경유). Per Execution의 CDO 역할을 Spec마다 복제본 하나로(공유 원형 오염 없음 / UE와 다름·객체 하나 더).
- 대가: D3의 "원형은 직접 실행하지 않는다"가 "원형은 상태 없는 호출만 받는다"로 완화 — 원형은 모든 액터가 공유하므로 하위 클래스가 원형 쪽 콜백에서 상태를 쓰면 안 된다(UE CDO와 같은 규칙). 정의값이 인스턴스마다 복제된다.

## D7 — 코스트·쿨다운·커밋은 UE 그대로, 태그 조건은 Activation 3종만

- 날짜: 2026-10-21
- 결정: UE대로 코스트·쿨다운 GE 필드를 base 클래스에 둔다(D6). `CheckCost` = ASC `CanApplyAttributeModifiers`(AddBase 모디파이어만, 현재값 + 크기 ≥ 0 — UE `GameplayEffect.cpp:5177`), `CheckCooldown` = 쿨다운 GE의 부여 태그를 가졌으면 실패, `CommitAbility` = `CommitCheck`(쿨다운·코스트 재확인) → `CommitExecute`(쿨다운 → 코스트 적용). `CanActivateAbility`는 쿨다운 → 코스트 → 태그 순(UE와 같음). 남은 쿨다운 조회 2개. 태그는 `ActivationOwnedTags`(PreActivate에서 루즈 태그로 더하고 EndAbility에서 뺌)·`ActivationRequiredTags`·`ActivationBlockedTags`.
- 왜: (유저) "Cost, Cooldown, Tag 조건 구현해", 앞서 "UE에 맞춰서 구현하고 있어봐". `CommitAbility`(TODO 2-6)를 함께 넣은 것은 에이전트 판단 — UE에선 코스트·쿨다운이 커밋에서만 적용된다. 뺀 것들은 근거 미기록(유저 확정 전). 에이전트 판단(유저 미확정): 실패 사유 태그는 2-8 데모 표시 때, Block/Cancel은 조건이 아니라 다른 어빌리티 차단·취소, Source/Target 태그는 이벤트 발동 데이터 전용(범위 밖).
- 뺀 것: 실패 사유 태그(`OptionalRelevantTags`·`ActivateFail*Tag`), `AbilityTags`(AssetTags)·`BlockAbilitiesWithTag`·`CancelAbilitiesWithTag`(ASC BlockedAbilityTags), `Source/TargetRequired/BlockedTags`, 개별 커밋(`CommitAbilityCooldown/Cost`·ForceCooldown), `NotifyAbilityCommit`, 코스트·쿨다운 무시 전역 옵션.
- 대가: 발동 실패 이유를 bool로만 안다. 어빌리티끼리 막기·취소가 없다. 어트리뷰트 세트가 없는 어트리뷰트의 코스트는 검사에서 건너뛴다(UE는 checked 접근이라 크래시).
- 재검토: 데모 HUD에 실패 사유가 필요할 때, 스킬끼리 끊기·막기가 필요할 때. → 2026-10-21 유저 "어빌리티 태그·차단·취소는 넣어야지"(TODO-BOARD 2-10), 실패 사유는 2-9(데모 표시, 에이전트 판단)로 착수 예정.

## D8 — ASC 복제·파괴는 UE와 같은 결과로: 복제는 설정만, 파괴는 어빌리티 취소 → 해제

- 날짜: 2026-10-21
- 결정: 복제(`Instantiate`)는 설정(`attributeInitData`)만 따라가고 실행 중 상태(현재 값·어빌리티·GE·태그)는 새로 시작한다 — 지금 구조(직렬화 필드는 설정뿐, 나머지는 `Awake`에서 생성)가 이미 그래서 테스트로 고정만. 파괴는 `OnDestroy` → `DestroyActiveState`(Owner·ASC가 살아 있으면 `CancelAllAbilities` → `ClearAllAbilities`) → AttributeSet 목록 비움. 죽은 ASC를 의존 목록에서 빼는 건 이미 UE대로였다(`AttributeAggregator.BroadcastOnDirty`의 Unity null 확인) — 테스트로 고정.
- 왜: (유저) "UE의 결과와 동일하게 되도록 해." — 앞서 "구현 방법말고 결과, 기능으로서". UE 원문 `AbilitySystemComponent_Abilities.cpp:97`·`:1362`, 복제 기준은 `AbilitySystemComponent.h:1695`·`:1957`(`Transient`)·`:196`(설정).
- 대가: 에이전트 판단(유저 미확정) — UE의 재진입 방지 플래그(`bDestroyActiveStateInitiated`)는 뺐다. UE는 `OnUnregister`·`OnComponentDestroyed` 둘 다 부르고 EndAbility 콜백이 액터를 다시 파괴할 수 있어 필요하지만, Unity `OnDestroy`는 한 번만 불린다(PlayMode D-4로 확인). AttributeSet은 C#에 강제 무효화가 없어 목록에서만 뺀다. 해제된 GA 인스턴스를 무효로 표시하는 것은 여전히 없다(TODO-BOARD 보류 항목) — ASC를 거치는 경로로는 Spec이 없어 못 쓴다. `CancelAllAbilities`의 Ignore 인자는 쓰는 곳이 없어 뺐다.
- 재검토: 컴포넌트를 껐다 켜는(비활성화 후 재사용) 경로가 생기면 — UE `OnRegister`가 플래그를 되돌리는 경우에 해당.

## D9 — 어빌리티 이벤트·실패 사유는 UE 구조대로: 실패 사유 태그는 설정 에셋, 종료 이벤트는 둘

- 날짜: 2026-10-21
- 결정: ASC에 발동·커밋·실패·종료 이벤트 + `Notify…`(UE `AbilitySystemComponent.h:536-560`). 실패 사유는 UE처럼 `optionalRelevantTags`를 `CanActivateAbility`·`CheckCooldown`·`CheckCost`·`DoesAbilitySatisfyTagRequirements`·`CommitCheck`·`CommitAbility`에 넘겨 모은다. 사유 태그 5종(일반 거부·쿨다운·코스트·차단·필요 없음)은 **설정 에셋** `GameplayAbilitiesSettings`(Resources 고정 경로, UE `UGameplayAbilitiesDeveloperSettings`)에서 고른다. 종료는 UE처럼 **둘** — 어빌리티만(`OnAbilityEnded`, UE `AbilityEndedCallbacks`)과 데이터(`OnAbilityEndedWithData`, UE `OnAbilityEnded`).
- 왜: (유저) 설정 에셋·종료 이벤트 둘을 골랐다(2026-10-21 선택지 답). > 근거 미기록(유저 확정 전).
- 대안: 사유 태그를 Core 고정 네이티브 태그로(에셋 없음 / 이름을 프로젝트별로 못 바꿈). 종료 이벤트를 데이터 하나로(구독처 하나 / UE와 모양이 다름).
- 대가: 에이전트 판단(유저 미확정) — UE `UAbilitySystemGlobals`(설정을 옮겨 담는 전역)는 두지 않고 설정을 바로 읽는다. 네트워크 실패 태그는 뺐다. 실패 이벤트는 UE처럼 `CanActivateAbility` 거부에서만 — Per Actor 활성 중 재발동·해제 대기·Avatar 없음은 false만 돌려준다. 이벤트 이름은 규약(`On` + PascalCase) 때문에 UE와 다르다. 사유 태그 이름은 UE 설정 변수 이름을 따 `Ability.ActivateFail.CanActivateAbility/Cooldown/Cost/TagsBlocked/TagsMissing`으로 레지스트리에 등록하고 설정 에셋에 채웠다(2026-10-21 유저 "알아서 하고").
- 재검토: 2026-10-21 유저가 태그 방식이 "탐탁친 않네"(대안: 실패 정보에 종류 enum + 원인 태그) — "일단 내비둬". TODO-BOARD "뒤로 미룸".

## D10 — 어빌리티 태그·차단·취소는 UE 그대로, Source/Target 태그 조건은 뺌

- 날짜: 2026-10-21
- 결정: GA에 `assetTags`(UE `AbilityTags`/`GetAssetTags`)·`cancelAbilitiesWithTag`·`blockAbilitiesWithTag`, `IsBlockingOtherAbilities`·`SetShouldBlockOtherAbilities`. ASC에 막힌 어빌리티 태그 개수 컨테이너 + `BlockAbilitiesWithTags`/`UnBlockAbilitiesWithTags`·`AreAbilityTagsBlocked`·`ApplyAbilityBlockAndCancelTags`·`CancelAbilities(withTags, withoutTags, ignore)`. 발동(`PreActivate`)에서 막기·취소(요청한 인스턴스는 빼고, Spec 활성 횟수보다 먼저), 종료에서 풀기, 발동 조건에 "ASC가 막은 어빌리티 태그" 검사 — UE `GameplayAbility.cpp:985`·`:865-869`·`:374`·`:399`, `AbilitySystemComponent_Abilities.cpp:1305-1440`.
- 왜: (유저) "어빌리티 태그·차단·취소는 넣어야지"(TODO-BOARD 2-10). Source/Target 태그는 (유저) "빼기" → "해당 나중에 TODO로 해둬"(TODO-BOARD '뒤로 미룸').
- 대가: Source/Target Required·Blocked 태그가 없다 — UE에선 이벤트 발동 데이터가 있을 때만 쓰여 지금은 들어올 경로가 없다. 에이전트 판단(유저 미확정): 취소 불가 플래그(`CanBeCanceled`·`HandleChangeAbilityCanBeCanceled`)는 D4대로 뺀 채, `CancelAllAbilities`의 Ignore 인자도 D8대로 안 넣음(`CancelAbilitySpec`에만 ignore).
