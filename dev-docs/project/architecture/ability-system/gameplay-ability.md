# AbilitySystem — GameplayAbility (계획)

> **최종 갱신:** 2026-10-20 (KST)

스킬·액션의 실행 단위. **ASC 부여·해제까지 구현** — `Core/AbilitySystem/Abilities/`, 정의(SO `GameplayAbilityAsset`)와 런타임(C# `GameplayAbility`)을 나누고 부여 때 원형을 복제한다(2026-10-20). 발동·코스트·쿨다운은 아직 없다. 상위 개요는 [overview](overview.md).

> forward-looking(계획) 문서. 착수 시 feature [gameplay-ability](../../../agent/feature/ability-system/gameplay-ability/progress.md)에서 슬라이스로 쪼갠다.



## 의존

- [attribute](attribute.md) ✅ — 코스트·효과가 만지는 수치.
- [gameplay-effect](gameplay-effect.md) ✅ — 코스트·쿨다운·효과의 표현 수단.
