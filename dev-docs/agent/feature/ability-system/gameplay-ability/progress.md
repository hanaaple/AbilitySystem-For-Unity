# gameplay-ability — 게임플레이 어빌리티 (GA·스킬 실행)

- 상태: 📋 PLANNED
- 우선순위: P1
- 최종 갱신: 2026-07-05 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) ✅ · [gameplay-effect](../gameplay-effect/progress.md) 🔧.
> **미착수.** 수치 계층(attribute·gameplay-effect)이 서야 GA가 코스트·쿨다운·효과를 GE로 표현할 수 있어 후행.

## 목표
스킬·액션의 실행 단위(GameplayAbility)를 도입한다 — 생명주기(`ActivateAbility`/`EndAbility`), 코스트(GE)·쿨다운(Duration GE), ASC 슬롯 등록, 입력 바인딩. 최종적으로 `TakeDamage`가 GE로 배선돼 실제 전투 수치가 흐른다.

## 수용 기준 (Definition of Done)
- [ ] `GameplayAbility` 정의 + `ActivateAbility`/`EndAbility` 생명주기
- [ ] 코스트(GE)·쿨다운(Duration GE) 적용
- [ ] ASC 슬롯 등록 + 입력 바인딩
- [ ] `CharacterBase.TakeDamage()`가 GE 적용으로 Health 감산

## 범위
### 포함
- GA 실행 계층, 코스트/쿨다운, 입력 바인딩, `TakeDamage` GE 배선
### 제외 (명시적으로 하지 않을 것)
- GameplayTag(후순위). 복합 데미지 계산은 [gameplay-effect](../gameplay-effect/progress.md)의 `Execution`(미구현) 선행 필요

## 설계 개요
- 아키텍처(계획): [architecture/ability-system/gameplay-ability.md](../../../../project/architecture/ability-system/gameplay-ability.md)
- 코드: (미존재 — 착수 시 `Assets/Scripts/Core/AbilitySystem/Ability/`)

## 세부 TODO (구현 체크리스트)
- [ ] 1. `GameplayAbility` 정의 + 생명주기
- [ ] 2. 코스트·쿨다운(GE) 배선
- [ ] 3. ASC 슬롯·입력 바인딩
- [ ] 4. `TakeDamage` GE 배선

## 결정 기록
미착수 — 착수 시 `decisions.md` 생성.

## 작업 로그
미착수 — 착수 시 `worklog.md` 생성.

## 블로커
- 선행: [gameplay-effect](../gameplay-effect/progress.md) 핵심(✅ 완료). 단 복합 데미지 GA는 `Execution`(미구현) 필요.

## 다음 작업
1. `GameplayAbility` 추상 정의 + `ActivateAbility`/`EndAbility` 생명주기부터. 우선 코스트/쿨다운 없는 최소 GA가 ASC에서 발동되는 수직 슬라이스.
