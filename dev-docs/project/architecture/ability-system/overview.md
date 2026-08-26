# AbilitySystem (GAS-like) — Overview

`Assets/Scripts/Core/AbilitySystem/` — 캐릭터 수치 관리를 위한 경량 Ability System. **UE5의 GAS를 참고해, 이 게임에 필요한 축만 골라 Unity에 직접 구현**한 것이다 — 라이브러리 이식이 아니라 개념의 선택적 재구현. 구현 진행 중이며, 채택/생략 범위는 아래 매핑 표에 정리한다.

> 이 문서군은 **구현된 것 / 미구현(계획)**을 명확히 구분한다. 범례: ✅ 구현·동작 · ⚠️ 코드 존재하나 상태 확인 필요 · ❌ 미구현(계획).
> **심화 문서:** [Attribute](attribute.md) · [GameplayEffect](gameplay-effect.md) · [GameplayAbility](gameplay-ability.md) · [Aggregator (라이브 재평가·설계 준비)](aggregator.md)
> **반영 기준:** feature `ability-system` @ 2026-08-21 (KST) — 상류 progress의 `최종 갱신`보다 오래되면 갱신 대상(dev-docs HARNESS의 '최종 갱신 타임스탬프' 규약).

## 구현 현황 요약

| 영역 | 상태 | 비고 |
|---|---|---|
| Attribute (Set·Handle·Data·SO 초기화) | ✅ | Character/Combat AttributeSet 구현 → [attribute](attribute.md) |
| GameplayEffect 정의·Spec·Active | ✅ | SO 정의 + 런타임 Spec 분리 → [gameplay-effect](gameplay-effect.md) |
| Modifier 연산(6종) + CurrentValue 누산 | ✅ | Add/Multiply/Divide/Override |
| GE 적용/해제·주기(Period)·지속(Duration) 틱 | ✅ | ASC에 실행 경로 존재 |
| GE 타입별(Instant/Duration) 실전 상태 | ⚠️ | enum 주석은 '미구현' 표기 — [gameplay-effect §확인 필요](gameplay-effect.md) |
| `GameplayEffectExecutionAsset` (프레임워크·concrete 전체) | ❌ | 껍데기 클래스 — 에디터 미배선·미검증, concrete 0 |
| GameplayAbility(GA) — 스킬 실행 단위 | ❌ | 클래스 자체 없음 → [gameplay-ability](gameplay-ability.md) |
| GE 스택 / Gameplay Cue / GameplayTag | ❌ | 코드 내 TODO 마커 |
| Magnitude: AttributeBased / SetByCaller | ❌ | enum 주석 처리 상태 |
| `CharacterBase.TakeDamage()` GE 배선 | ❌ | 빈 스텁 |

## 핵심 구조

```
AbilitySystemComponent (MonoBehaviour)
├── Dictionary<Type, AttributeSet>              ← Type당 하나만 등록 가능
└── Dictionary<AGEHandle, ActiveGameplayEffect> ← 활성 이펙트 목록

AttributeSet (abstract, 빈 마커)                 ← 수치 컨테이너
├── CharacterAttributeSet  — health, maxHealth, stamina, maxStamina, speed
└── CombatAttributeSet     — damage

ResolvedGameplayAttribute (readonly struct)     ← 경량 식별자 (SetType + fieldName + FieldInfo 캐싱, IEquatable)
AttributeData  (struct)                         ← BaseValue / CurrentValue 쌍

GameplayEffectAsset (ScriptableObject)          ← 불변 정의 (type·duration·period·modifiers·executions)
GameplayEffectSpec                              ← 런타임 인스턴스 (Level·Context 스냅샷, Modifier resolve 캐싱)
ActiveGameplayEffect                            ← 활성 상태 (Handle, RemainingDuration, PeriodTimer)
```

## 설계 방향 — UE GAS의 선택적 이식

이 시스템의 첫 결정은 **"UE GAS를 통째로 옮기지도, 완전 자작하지도 않는다"**였다.

- **완전 이식(full port) 거부** — GameplayTag·Cue·Execution·Prediction 등 UE GAS 무게의 대부분은 이 싱글플레이 소울라이크에 당장 불필요하다(북극성: 트리거 전 복잡화 금지).
- **완전 자작(from scratch) 거부** — "수치 변경을 단일 채널(GE)로 통과", "정의(SO)/인스턴스(Spec) 분리", "쓰기 시 재계산·읽기 캐시" 같은 UE GAS의 **검증된 아키텍처 판단**은 그대로 이득이라 버릴 이유가 없다.
- **채택: 필요한 축만 이식** — 수치(Attribute)·이펙트(GameplayEffect) 두 축을 UE 개념·용어 그대로 가져와 Unity에 재구현하고, 실행 계층(GA)·태그·연출은 필요해질 때 얹는다.

> **포트폴리오 관점:** 이 문서군은 "UE GAS를 안다"가 아니라 **"UE GAS의 어느 부분을 왜 취하고 왜 뺐는지 판단할 수 있다"**를 보이는 게 목적이다. 코드 클래스명(`AttributeSet`·`GameplayEffectAsset`·`Spec`·`ActiveGameplayEffect`·`Modifier`·`Execution`)은 대응을 명확히 하려고 UE 용어를 의도적으로 따르되, **SO(에셋 정의)에는 `Asset` 접미어**를 붙여 런타임 타입과 구분한다.

## UE GAS 대비 — 채택/생략 범위

| UE GAS | 이 프로젝트 | 상태 | 판단 |
|---|---|---|---|
| `UAbilitySystemComponent` | `AbilitySystemComponent` | ✅ | 수치·이펙트 허브. 핵심이라 채택 |
| `UAttributeSet` / `FGameplayAttribute` | `AttributeSet` / `GameplayAttribute`·`ResolvedGameplayAttribute` | ✅ | 어트리뷰트 개념 채택. UE 단일 `FGameplayAttribute`(UProperty 기반, 직렬화+런타임 겸용)를 C# 제약(FieldInfo 직렬화 불가)상 **직렬화용 `GameplayAttribute`(문자열 쌍) + 런타임 해석용 `ResolvedGameplayAttribute`(FieldInfo 캐싱 struct)** 둘로 분리 → [attribute](attribute.md) |
| `FGameplayEffectSpec` / `FActiveGameplayEffect` | `GameplayEffectSpec` / `ActiveGameplayEffect` | ✅ | 정의(SO)/런타임 인스턴스 분리 그대로 채택 → [gameplay-effect](gameplay-effect.md) |
| `FGameplayModifierInfo` + Aggregator mod channels | `GameplayModifier` 6종 + CurrentValue 공식 | ✅(단순화) | UE의 다채널 aggregator를 **6종 연산 단일 공식**으로 축소 |
| `FGameplayEffectContext` | `GameplayEffectContext`(+Handle) | ✅ | 출처·타깃 스냅샷 채택 |
| `FGameplayEffectExecutionCalculation` | `GameplayEffectExecutionAsset` | ⚠️ 껍데기 | 복합 계산 계층 — 배관만, 실제 필요 트리거 전 보류 |
| `UGameplayAbility` | (계획) | ❌ | 실행 계층 — 수치 계층 뒤로 → [gameplay-ability](gameplay-ability.md) |
| `SetByCaller` / `ScalableFloat`(CurveTable) | (미채택) | ❌ | Magnitude 확장 — 현재 고정 float로 충분 |
| `GameplayTag` / `GameplayCue` / GE Stack | (미채택) | ❌ | 태그·연출·스택 — 현재 스코프 밖 |
| Ability/Attribute Prediction (네트워크) | (미채택) | ❌ | 싱글플레이라 불필요 |

## 설계 근거 (핵심 결정)

- **GE 단일 채널 추상화** — 수십 개 출처가 수치를 직접 조작할 때 생기는 순서 의존성·잔류 값 문제를, 모든 수치 변경을 GameplayEffect 한 경로로 통과시켜 제거.
- **쓰기 시점 재계산 + 읽기 캐시** — CurrentValue를 Effect 변경 시점에만 재계산. 매 프레임 다수의 읽기가 발생해도 재계산 비용 0.
- **SO 불변 정의 + 런타임 Spec 분리** — 여러 캐릭터가 같은 GE SO를 공유해도 적용 상태가 간섭하지 않음.
- **Handle의 FieldInfo 캐싱** — 생성 시 Reflection 1회로 격리, 이후 런타임엔 string 필드 탐색 없음.

---

- 프로젝트 아키텍처 인덱스: [../overview.md](../overview.md)
- feature 추적: [feature/ability-system](../../../agent/feature/feature-list.md)
