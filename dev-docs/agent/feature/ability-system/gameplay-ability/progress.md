# gameplay-ability — 게임플레이 어빌리티 (GA·스킬 실행)

> 작성 규약 → [progress-harness.md](../../progress-harness.md)

- 상태: ✅ 임시완료
- 우선순위: P1
- 최종 갱신: 2026-10-23 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-tag](../gameplay-tag/progress.md) · [aggregator](../aggregator/progress.md).
> 작업 방향·프로세스는 [HARNESS.md](HARNESS.md), 요구사항 정의서는 [requirements.md](requirements.md).

## 목표
GAS의 `GameplayAbility`를 **SO 기반·UE 유사 구조·동작 동일**로 이식한다. 스킬·액션의 실행 단위 — 생명주기, 코스트(GE)·쿨다운(Duration GE), ASC 부여, 입력. 최종적으로 데미지 적용이 GA→GE로 배선돼 실제 전투 수치가 흐른다. **전부 이식하지 않고 핵심만** 가져오며, 범위는 진행하며 판단·번복한다.

## 수용 기준 (Definition of Done)
- [x] `GameplayAbility` 정의 + `ActivateAbility`/`EndAbility` 생명주기 — 셀프체크, 데모 Play
- [x] 코스트(GE)·쿨다운(Duration GE) 적용 — 셀프체크, PlayMode, 데모 Play(파동 스킬 마나·쿨다운)
- [x] ASC 슬롯 등록 + 입력 바인딩 — 데모 `PlayerCharacter`가 InputID로 부여하고 입력을 `AbilityLocalInputPressed/Released`로 넘김(→D11), 데모 Play
- [x] 어빌리티 발동이 GE 적용으로 대상 어트리뷰트(Health)를 변경 — 데모 공격·파동 스킬이 허수아비 Health를 깎음(Play)

## 범위
### 포함
- GA 실행 계층(정의·런타임 분리, 인스턴싱), 생명주기, GE 적용 헬퍼, 코스트/쿨다운/커밋, 태그 조건·차단·취소, 이벤트·실패 사유, 입력, ASC 복제·파괴
### 제외 (현 시점 — 이후 확장 가능)
- 네트워크·예측·복제, AbilityTask, 타겟팅(TargetData), AbilityTriggers·GameplayEvent 발동, GE가 부여하는 어빌리티 — 남은 UE 대비 목록은 TODO-BOARD '뒤로 미룸 > GA — UE 대비 미구현'
- 복합 데미지 계산 자체 — [gameplay-effect](../gameplay-effect/progress.md)의 Execution/AttributeBased로 표현, GA는 그 GE를 건다

## 설계 개요
- 아키텍처: [architecture/ability-system/gameplay-ability.md](../../../../project/architecture/ability-system/gameplay-ability.md)
- 코드: `Assets/Scripts/Core/AbilitySystem/Abilities/`
- **정의·런타임(→D1·D3)**: `GameplayAbilityAsset`(sealed SO, 불변 정의)이 런타임 클래스 인스턴스를 `[SerializeReference]` 원형(`abilityTemplate`)으로 들고, 부여·발동 때 `SerializedDeepCopy.Clone`으로 복제한다. 하위 클래스는 런타임 쪽(`GameplayAbility` 상속, `[Serializable]`)만 만든다.
- **인스턴싱(→D6)**: Per GameObject(부여 때 하나)·Per Execution(발동마다, 끝나면 버림). 정책·코스트·쿨다운·태그 등 정의 필드는 런타임 base 클래스 직렬화 필드. Per Execution의 CDO 역할(부여·해제·발동 확인 콜백)은 원형.
- **Spec·컨테이너**: `GameplayAbilitySpec`(핸들·정의·레벨·InputID·출처·활성 횟수·입력 눌림·인스턴스 목록), `GameplayAbilitySpecHandle`(id만, 전역 유일), `GameplayAbilitySpecContainer`(UE 구조, 네트워크 직렬화만 뺌), `GameplayAbilityGameObjectInfo`(Owner·Avatar·ASC), `ScopedAbilityListLock`(락 중 부여·해제는 보류).
- **생명주기(→D4)**: `TryActivateAbility` → `CanActivateAbility`(쿨다운 → 코스트 → 태그) → `CallActivateAbility`(`PreActivate` + `ActivateAbility`) → `EndAbility` → ASC `NotifyAbilityEnded`. 취소 = 취소로 `EndAbility`. 활성 중 해제 시 `EndAbility`(취소 아님).
- **GE 적용(→D5)**: `MakeEffectContext`(Instigator = ASC, EffectCauser = Avatar)·`MakeOutgoingGameplayEffectSpec`·`ApplyGameplayEffect(Spec)ToOwner/ToTarget`. 대상은 ASC 하나.
- **코스트·쿨다운·커밋(→D7)**: `CommitAbility` = `CommitCheck` → `CommitExecute`(쿨다운 → 코스트). 코스트 확인은 GE 컨테이너 `CanApplyAttributeModifiers`, 쿨다운 확인은 쿨다운 GE 부여 태그. 인스펙터가 코스트·쿨다운 GE 적합 여부를 표시.
- **태그·차단·취소(→D10)**: Activation Owned/Required/Blocked 태그, Asset Tags·Block/Cancel Abilities With Tag, ASC 막힌 어빌리티 태그 개수.
- **이벤트·실패 사유(→D9)**: ASC 발동·커밋·실패·종료(어빌리티만 / 데이터) 이벤트, 실패 사유 태그는 설정 에셋 `GameplayAbilitiesSettings`(`Assets/Resources/AbilitySystem/`).
- **입력(→D11)**: Spec이 눌림 상태, ASC `AbilityLocalInputPressed/Released(inputID)`가 비활성이면 발동 시도·활성이면 인스턴스에 전달, GA는 `InputReleased`에서 스스로 끝낼 수 있다.
- **ASC 복제·파괴(→D8)**: 복제는 직렬화 설정만 따라가고, 파괴는 활성 어빌리티 취소 → 해제.
- 게임 쪽 사용(Game 레이어, 이 feature 밖): `CharacterGameplayAbility` 기반 데모 어빌리티(점프·대시·조준·발사체·실드·존).

## 세부 TODO (구현 체크리스트)
- [x] 1. 정의·런타임 분리 + ASC 부여·해제
- [x] 2. 생명주기 (→D4)
- [x] 3. GA에서 GE 적용 (→D5)
- [x] 4. 인스턴싱 정책 (→D6)
- [x] 5. 코스트·쿨다운·커밋·Activation 태그 (→D7)
- [x] 6. ASC 복제·파괴 (→D8)
- [x] 7. 이벤트·실패 사유 (→D9)
- [x] 8. 어빌리티 태그·차단·취소 (→D10)
- [x] 9. 입력 (→D11)
- [x] 10. 데미지 GE 배선 — 데모에서 확인

## 결정 기록
→ [decisions.md](decisions.md). progress엔 인덱스만.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | 정의·런타임 분리 | 2026-10-20 | 단일 SO(정의) + 일반 C# 클래스(런타임) |
| D2 | 에셋은 타입만 | 2026-10-20 | ⛔ D3로 번복 |
| D3 | SerializeReference 원형 + 깊은 복사 | 2026-10-20 | 에셋이 런타임 클래스 인스턴스를 원형으로, 부여 때 직렬화 규칙 깊은 복사 |
| D4 | 생명주기 뼈대만 | 2026-10-20 | UE 흐름 그대로, 네트워크·GA 자체 락·취소 불가 플래그는 뺌 (근거 미기록) |
| D5 | GE 적용=UE 헬퍼, 대상은 ASC 하나 | 2026-10-20 | 컨텍스트 `SetAbility`·SourceObject 타입은 보류 |
| D6 | 인스턴싱 Per GameObject·Per Execution | 2026-10-21 | 정의 필드는 런타임 base, Per Execution의 CDO 역할은 원형 |
| D7 | 코스트·쿨다운·커밋=UE, 태그는 Activation 3종 | 2026-10-21 | `CommitAbility` 포함 |
| D8 | ASC 복제·파괴=UE 결과 | 2026-10-21 | 복제는 설정만, 파괴는 취소 → 해제 |
| D9 | 이벤트·실패 사유=UE 구조 | 2026-10-21 | 사유 태그는 설정 에셋, 종료 이벤트 둘 (근거 미기록) |
| D10 | 어빌리티 태그·차단·취소=UE | 2026-10-21 | Source/Target 태그 조건은 뺌 |
| D11 | 입력=UE 구조 | 2026-10-22 | Spec 눌림 상태·ASC InputID 발동·전달·GA `InputReleased` |

- 다른 feature 코드 수정: gameplay-effect `CanApplyAttributeModifiers`·`HasAnyMatchingGameplayTags`(2026-10-21, D7), 적용 시각 기반 GE 수명(2026-10-21, gameplay-effect D27 — PlayMode에서 쿨다운이 적용 프레임 시간까지 깎이던 버그).

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
1. 2026-10-23 유저 지시로 `✅ 임시완료`.
2. UE 대비 남은 것(AbilityTask·이벤트 발동·Source/Target 태그·해제된 인스턴스 무효 표시 등)은 TODO-BOARD '뒤로 미룸'. 착수는 유저 지시 때.

## 검증
- 에디터 셀프체크 `Tools ▸ Ability System ▸ Run Gameplay Ability Self-Check`(`Assets/Scripts/Test/Editor/GameplayAbilitySelfCheck.cs`) — A~Q 묶음(부여·복제·해제·락·생명주기·GE 적용·인스턴싱·코스트/쿨다운/태그·통합·커밋 경계·이벤트·차단/취소·조합/재진입). 의도된 에러·경고 로그가 몇 줄 나온다.
- PlayMode `GameplayAbilityPlayModeTest`(실제 프레임의 쿨다운), `AbilitySystemLifecyclePlayModeTest`(ASC 복제·파괴).
- 데모 Play — 입력·발동·코스트·쿨다운·데미지.
- 안 본 것: GA 인스펙터 선언 클래스별 상자의 밝은 스킨·좁은 창.
