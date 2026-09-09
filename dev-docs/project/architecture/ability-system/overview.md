# AbilitySystem (GAS-like) — Overview

`Assets/Scripts/Core/AbilitySystem/` — 캐릭터 수치 관리를 위한 경량 Ability System. **UE5 GAS를 참고해 이 게임에 필요한 축만 골라 Unity에 직접 재구현**한 것이다 — 라이브러리 이식이 아니라 개념의 선택적 재구현.

> **심화 문서:** [Attribute](attribute.md) · [GameplayEffect](gameplay-effect.md) · [Aggregator (라이브 재평가)](aggregator.md) · [GameplayAbility (계획)](gameplay-ability.md)
> 이 문서군은 **개념·방향·판단** 중심이다. 필드·API 세부는 코드와 feature progress를 본다.

## 설계 방향 — UE GAS의 선택적 이식

첫 결정은 **"통째로 옮기지도, 완전 자작하지도 않는다"**였다.

- **완전 이식 거부** — GameplayTag·Cue·Prediction 등 UE GAS 무게의 대부분은 이 싱글플레이 소울라이크에 당장 불필요하다(트리거 전 복잡화 금지).
- **완전 자작 거부** — "수치 변경을 단일 채널(GE)로 통과", "정의(SO)/인스턴스(Spec) 분리" 같은 UE의 검증된 아키텍처 판단은 그대로 이득이라 버릴 이유가 없다.
- **채택: 필요한 축만** — 수치(Attribute)·이펙트(GameplayEffect)·집계 반응성(Aggregator)을 UE 개념·용어 그대로 가져와 재구현하고, 실행 계층(GA)·태그·연출은 필요해질 때 얹는다.

> **포트폴리오 관점:** "UE GAS를 안다"가 아니라 **"어느 부분을 왜 취하고 왜 뺐는지 판단할 수 있다"**를 보이는 게 목적이다. 클래스명은 대응을 명확히 하려 UE 용어를 따르되, **SO(에셋 정의)에는 `Asset` 접미어**로 런타임 타입과 구분한다.

## 핵심 구조

```
AbilitySystemComponent (MonoBehaviour)          ← 얇은 위임 래퍼(진입점)
 ├ AttributeSet 들 (타입당 하나)                 ← 수치 저장 · AttributeData(Base/Current)
 └ ActiveGameplayEffectsContainer                ← 소유·수명·집계
    ├ ActiveGameplayEffect 들                    ← 지속형 활성 GE (Spec + 타이머)
    └ AttributeAggregator (어트리뷰트별, 지연 생성) ← CurrentValue 산출 + 라이브 재평가

GameplayEffectAsset (SO, 불변 정의)
  → GameplayEffectSpec (런타임, 대상별 Clone)
  → ActiveGameplayEffect (지속형 활성 상태)

어트리뷰트 참조:  GameplayAttribute (직렬화)  ↔  GameplayAttributeHandle (런타임)
```

- **ASC:** 수치·이펙트의 진입점. 실제 소유·수명·집계는 컨테이너에 위임하는 얇은 래퍼.
- **수치·이펙트·집계:** 각각 [attribute](attribute.md) · [gameplay-effect](gameplay-effect.md) · [aggregator](aggregator.md)에서 상세.

## UE GAS 대비 — 채택/생략

> 범례: ✅ 구현·동작 · ❌ 미구현(계획)

| UE GAS | 이 프로젝트 | 상태 | 판단 |
|---|---|---|---|
| `UAbilitySystemComponent` | `AbilitySystemComponent` (+ 컨테이너) | ✅ | 수치·이펙트 허브. 핵심이라 채택 |
| `UAttributeSet` / `FGameplayAttribute` | `AttributeSet` / `GameplayAttribute`·`GameplayAttributeHandle` | ✅ | UE 단일 `FGameplayAttribute`를 C# 제약(FieldInfo 직렬화 불가)상 직렬화용+런타임용 둘로 분리 → [attribute](attribute.md) |
| `FGameplayEffectSpec` / `FActiveGameplayEffect` | `GameplayEffectSpec` / `ActiveGameplayEffect` | ✅ | 정의(SO)/런타임 인스턴스 분리 채택 → [gameplay-effect](gameplay-effect.md) |
| `FGameplayModifierInfo` | `GameplayModifier` 6종 + CurrentValue 공식 | ✅ | 연산 6종 공식으로 축소 |
| `FAggregator` (dirty/dependents) | `AttributeAggregator` | ✅ | 어트리뷰트별 집계 + 라이브 재평가 → [aggregator](aggregator.md) |
| `FGameplayEffectContext` | `GameplayEffectContext`(+Handle) | ✅ | 출처·타깃 채택 |
| `FGameplayEffectExecutionCalculation` | `GameplayEffectExecution` | ✅ | 복합 계산 훅. 프레임워크·검증 완료, 실전 전투 공식은 미확정 → [gameplay-effect](gameplay-effect.md) |
| AttributeBased Magnitude | `AttributeBasedMagnitude` | ✅ | 캡처 기반 magnitude(snapshot/live) |
| `UGameplayAbility` | (계획) | ❌ | 실행 계층 — 수치 계층 뒤로 → [gameplay-ability](gameplay-ability.md) |
| `SetByCaller` / `ScalableFloat`(Curve) | (미채택) | ❌ | Magnitude 확장 — SetByCaller 도입 예정 |
| `GameplayTag` / `GameplayCue` / GE Stack | (미채택) | ❌ | 태그·연출·스택 — 현재 스코프 밖 |
| Prediction (네트워크) | (미채택) | ❌ | 싱글플레이라 불필요 |

## 설계 근거 (핵심 결정)

- **GE 단일 채널** — 여러 출처가 수치를 직접 조작할 때 생기는 순서 의존성·잔류 값 문제를, 모든 수치 변경을 GameplayEffect 한 경로로 통과시켜 제거.
- **SO 불변 정의 + 런타임 Spec 분리** — 여러 캐릭터가 같은 GE를 공유해도 적용 상태가 간섭하지 않음.
- **어트리뷰트별 Aggregator 집계** — CurrentValue를 mod 집계로 산출하고, 캡처 대상이 바뀌면 의존 이펙트를 라이브 재평가(→ [aggregator](aggregator.md)).
- **핸들의 FieldInfo 캐싱** — 생성 시 Reflection 1회로 격리, 런타임 읽기/쓰기에 string 탐색 없음.

---

- 프로젝트 아키텍처 인덱스: [../overview.md](../overview.md)
- feature 추적: [feature/ability-system](../../../agent/feature/feature-list.md)
