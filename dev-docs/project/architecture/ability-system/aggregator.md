# AbilitySystem — Aggregator (어트리뷰트 반응성 / 라이브 재평가)

> **최종 갱신:** 2026-09-10 (KST)

어트리뷰트별 CurrentValue 집계와, 캡처 대상이 바뀌면 의존 이펙트를 **즉시 재평가**하는 반응성 계층. UE `FAggregator`의 집계 + dirty/dependents 개념을 재구현한다. 상위 개요는 [overview](overview.md).

> UE 대비는 [overview §UE GAS 대비](overview.md#ue-gas-대비--채택생략). **개념·방향** 중심. feature: [ability-system/aggregator](../../../agent/feature/ability-system/aggregator/progress.md).

## 무엇을 푸는가

두 가지를 한 객체가 맡는다.

1. **CurrentValue 집계** — 지속형 GE의 mod를 어트리뷰트별로 모아 공식으로 누산해 CurrentValue를 낸다. 값은 `Evaluate()` 한 경로로만 얻는다.
2. **라이브 재평가** — "방어력 = 힘의 10%"처럼 다른 어트리뷰트를 캡처한(non-snapshot) magnitude가, **소스 어트리뷰트가 바뀌면 그 즉시** 재계산돼 따라간다. 이게 없으면 magnitude가 apply 시점에 고정돼 non-snapshot이 무력해진다.

## 소유 구도

- **어트리뷰트별 `AttributeAggregator`** — base 사본 + mod 리스트를 들고 `Evaluate()`로 CurrentValue를 산출한다.
- **`ActiveGameplayEffectsContainer`가 소유** — 어트리뷰트별로 하나씩 **지연 생성**(캡처나 지속 mod가 실제로 필요할 때만). 지속형 GE의 mod를 등록/해제한다.
- **base 진실은 `AttributeData`** — aggregator의 base는 동기화 사본이다. base 쓰기는 항상 `AttributeData`가 진실이고, aggregator가 있을 때만 함께 갱신한다. 모든 어트리뷰트에 aggregator를 강제로 만들지 않는 UE의 lazy 모델을 따른 것.

## 반응성 흐름

```
어트리뷰트 변경 (base/mod)
  → AttributeAggregator dirty 발화
    → 그 어트리뷰트 CurrentValue 재계산 (Evaluate)
    → non-snapshot으로 이 값에 의존하는 이펙트들에 전파
        → 의존 modifier magnitude 재평가 → 대상 aggregator 갱신 → (연쇄)
```

- **의존 등록:** non-snapshot 캡처만 소스 aggregator에 "이 이펙트가 당신에게 의존한다"고 등록한다(snapshot은 값만 복사하고 등록하지 않아 고정된다).
- **연쇄와 순환:** 재평가가 다른 어트리뷰트를 dirty시키면 연쇄된다. 자기참조(A→B→A) 순환은 전파 깊이 상한으로 끊는다(수렴 보장이 아니라 폭주 차단).

## 범위 (UE 대비 생략)

- **대상:** 지속형(persistent) AttributeBased 모디파이어의 cross-attribute·self-effect 라이브 재평가. → 검증 완료.
- **생략(후속):** cross-actor 라이브(다른 액터 값 변화를 실시간 추종하는 cross-ASC 구독), 자기참조 fixed-point 수렴, Execution 라이브 재평가(Execution은 이벤트성이라 대상 아님), 태그 자격 판정(seam만).
