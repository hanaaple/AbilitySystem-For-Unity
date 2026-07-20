# AbilitySystem — GameplayAbility (계획)

스킬·액션 실행 단위. **현재 미구현** — 클래스 자체가 없다. 상위 개요는 [overview](overview.md).

> 이 문서는 forward-looking(계획)이다. 구현 착수 시 feature `gameplay-ability` progress에서 슬라이스로 쪼갠다.
> **반영 기준:** feature `ability-system/gameplay-ability` @ 2026-07-05 (KST) — HARNESS §3.4.

## 왜 아직 없나

GAS의 수치 계층(Attribute·GameplayEffect)이 먼저 서 있어야 GA가 코스트·쿨다운·효과를 GE로 표현할 수 있다. 현재 수치 계층(✅)까지 완성됐고, 실행 계층(GA)은 그 위에 올린다.

## ❌ 미구현 로드맵

| 항목 | 내용 | 근거 위치 | wiki todo id |
|---|---|---|---|
| **GameplayAbility(GA)** | 스킬·액션 실행 단위. `ActivateAbility/EndAbility` 생명주기, 코스트(GE)·쿨다운(Duration GE), ASC 슬롯 등록, 입력 바인딩 | 클래스 자체 없음 | `ability-ga` |
| `TakeDamage` 배선 | `CharacterBase.TakeDamage()`가 GE 적용으로 Health 감산되도록 연결 | `CharacterBase.cs:16` 스텁 | `ability-take-damage` |
| GameplayTag | 태그 기반 조건·면역·분류 | 미존재 | - |

## 의존

- [attribute](attribute.md) ✅ — 코스트/효과가 만지는 수치.
- [gameplay-effect](gameplay-effect.md) ✅(핵심) — 코스트·쿨다운·효과의 표현 수단. 단 GA의 복합 데미지 계산은 GE의 `Execution`(❌ 미구현)에 의존하므로, 실전 데미지 GA는 Execution 선행이 필요.
