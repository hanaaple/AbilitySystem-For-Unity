# gameplay-ability — 게임플레이 어빌리티 (GA·스킬 실행)

- 상태: 📋 PLANNED
- 우선순위: P1
- 최종 갱신: 2026-09-09 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [aggregator](../aggregator/progress.md).
> **미착수(작업 트리엔 GA 코드 없음).** 수치 계층(attribute·gameplay-effect·aggregator)이 서야 GA가 코스트·쿨다운·효과를 GE로 표현할 수 있어 후행.
> ⚠ 스켈레톤 초안(GA 스크립트 4개 + `ASC.GiveAbility` + `GA_New.asset`)이 유저 `stash@{0}`에 있음 — 작업 트리에 미반영. 착수 시 이 stash부터 검토.

## 목표
스킬·액션의 실행 단위(GameplayAbility)를 도입한다 — 생명주기(`ActivateAbility`/`EndAbility`), 코스트(GE)·쿨다운(Duration GE), ASC 슬롯 등록, 입력 바인딩. 최종적으로 데미지 적용이 GA→GE로 배선돼 실제 전투 수치가 흐른다.

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
- 코드: (작업 트리 미존재 — 착수 시 `Assets/Scripts/Core/AbilitySystem/Ability/`. 초안은 `stash@{0}`)
- 선행 계층 활용: GE 적용은 ASC `ApplyGameplayEffectToSelf`/`ApplyGameplayEffectToTarget`/`MakeOutgoingSpec`(구현됨)로, 코스트/쿨다운은 각각 Instant/Duration GE로 표현.

## 세부 TODO (구현 체크리스트)
- [ ] 1. `GameplayAbility` 정의 + 생명주기
- [ ] 2. 코스트·쿨다운(GE) 배선
- [ ] 3. ASC 슬롯·입력 바인딩
- [ ] 4. 데미지 GE 배선

## 결정 기록
미착수 — 착수 시 `decisions.md` 생성.

## 작업 로그
미착수 — 착수 시 `worklog.md` 생성.

## 블로커
- 선행 수치/이펙트 계층은 준비됨(attribute·gameplay-effect·aggregator 임시완료). 데모 씬 확정 전 폴더 신설 금지(→ [NOW.md](../../../NOW.md)).

## 다음 작업
1. `GameplayAbility` 추상 정의 + `ActivateAbility`/`EndAbility` 생명주기부터. 코스트/쿨다운 없는 최소 GA가 ASC에서 발동되는 수직 슬라이스. (착수 전 `stash@{0}` 초안 검토.)
