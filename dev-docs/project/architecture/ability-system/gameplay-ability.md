# AbilitySystem — GameplayAbility (계획)

스킬·액션의 실행 단위. **현재 미구현** — 작업 트리에 클래스가 없다(초안은 stash). 상위 개요는 [overview](overview.md).

> forward-looking(계획) 문서. 착수 시 feature [gameplay-ability](../../../agent/feature/ability-system/gameplay-ability/progress.md)에서 슬라이스로 쪼갠다.

## 왜 아직 없나

GAS의 수치 계층(Attribute·GameplayEffect·Aggregator)이 먼저 서 있어야 GA가 코스트·쿨다운·효과를 GE로 표현할 수 있다. 수치 계층은 완성됐고(✅), 실행 계층은 그 위에 올린다.

## 방향

- **`GameplayAbility`** — `ActivateAbility`/`EndAbility` 생명주기, ASC 슬롯 등록, 입력 바인딩.
- **코스트·쿨다운** — 각각 Instant/Duration GE로 표현(별도 자원 시스템 없이 GE 재사용).
- **데미지 배선** — 어빌리티 발동이 GE 적용으로 대상 수치를 바꾸도록 연결.

복합 데미지 계산 자체는 GE의 Execution/AttributeBased(구현됨)로 표현하고, GA는 그 GE를 발동만 한다.

## 의존

- [attribute](attribute.md) ✅ — 코스트·효과가 만지는 수치.
- [gameplay-effect](gameplay-effect.md) ✅ — 코스트·쿨다운·효과의 표현 수단.
