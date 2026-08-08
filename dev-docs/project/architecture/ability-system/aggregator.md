# AbilitySystem — Aggregator (어트리뷰트 반응성 / 라이브 재평가)

> **상태: 설계 준비 — 미구현.** 코드가 생기면 이 문서를 실제 구조로 채운다. 지금은 UE 확인 결과 + 채택 방향의 스켈레톤.
> feature: [ability-system/aggregator](../../../agent/feature/ability-system/aggregator/progress.md). 상위 개요는 [overview](overview.md).
> **반영 기준:** feature `ability-system/aggregator` @ 2026-08-07 (KST) — HARNESS §3.4. (미구현이라 구현 시 갱신)

캡처(non-snapshot) 값이 바뀌면 그에 의존하는 모디파이어 magnitude를 **즉시 재평가**하는 반응성 계층. UE `FAggregator`의 dirty/dependents 책임만 축소 재현한다.

## 왜 필요한가
현재 모디파이어 magnitude는 apply 시점에 `GameplayEffectSpec.CalculateModifierMagnitudes`로 계산돼 `GameplayModifierSpec.EvaluatedMagnitude`에 **고정**된다(생성·apply 2회만 호출, 이후 재계산 없음). 그래서 non-snapshot 캡처의 라이브 재조회가 실행될 트리거가 없어, snapshot=false가 사실상 무력하다. → 재평가를 트리거하는 "반응성 소유자"가 필요.

## UE 원문 확인 (참고 모델)
> 출처: ylyking UE 미러 — [GameplayEffect.cpp](https://raw.githubusercontent.com/ylyking/UnrealEngineNiv/master/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Private/GameplayEffect.cpp) · [GameplayEffectAggregator.cpp](https://raw.githubusercontent.com/ylyking/UnrealEngineNiv/master/Engine/Plugins/Runtime/GameplayAbilities/Source/GameplayAbilities/Private/GameplayEffectAggregator.cpp)

- **캡처 시 분기:** `bSnapshot`이면 `AttributeAggregator.TakeSnapshotOf(...)`(값 복사·고정), 아니면 라이브 aggregator 참조를 유지.
- **non-snapshot만 의존 등록:** `FGameplayEffectAttributeCaptureSpec::RegisterLinkedAggregatorCallback` — `if (bSnapshot == false) Agg->AddDependent(Handle);` → 소스 aggregator의 `Dependents`에 이 GE 핸들 등록.
- **소스 변경 → dirty 전파:** `FAggregator::BroadcastOnDirty`가 `OnDirty.Broadcast(this)` + `Dependents`를 돌며 `ASC->OnMagnitudeDependencyChange(Handle, this)` → dependent GE가 magnitude 재평가.
- **자격/태그:** `FAggregatorEvaluateParameters` + `UpdateQualifies`(태그 기반 mod 적용 여부) — 본 feature에선 seam만.

## 채택 방향 (경량 — 우리 구현)
UE의 per-attribute aggregator 객체 전면 대신 **반응성 책임만** 분리한다(→ feature decisions D1/D2):
1. **어트리뷰트 변경 이벤트(옵저버):** ASC 쓰기 지점(`SetBaseAttributeValue`/`UpdateAttributeCurrentValue`)에서 `(handle, old, new)` 발화. *(현재 없음 — 신설)*
2. **의존 등록/해지:** 스펙 apply 시 non-snapshot 캡처를 대상 어트리뷰트에 등록, remove 시 해지(UE `AddDependent`/`RemoveDependent` 대응).
3. **재평가 핸들러:** 변경 통지 → 의존 모디파이어 magnitude 재평가(`CalculateModifierMagnitudes` 라이브 캡처) → 영향 어트리뷰트 재계산.
4. **재진입 억제:** 재계산 패스 중 이벤트 잠금 → 연쇄 1패스, 자기참조 무한루프 차단(UE `OnDirtyRecursive` 가드 대응).

## 범위 (UE 대비 생략)
- 대상: **AttributeBased 모디파이어**(persistent/CurrentValue). cross-attribute·self-effect 라이브.
- **생략(후속):** cross-actor 라이브(cross-ASC 구독), 자기참조 fixed-point 수렴, Execution 라이브 재평가, per-attribute aggregator의 mod 채널 소유(R1)·CurrentValue 계산 이관(R2), 태그 자격(R5, seam만).

## 코드 위치 (예정)
- `Assets/Scripts/Core/AbilitySystem/Aggregator/` — 반응성 컴포넌트(앵커 `AttributeAggregator.cs`).
- 훅: `AbilitySystemComponent`(이벤트·구독), `GameplayEffectSpec`(대상 슬롯 재평가), `GameplayEffectAttributeCaptureSpec`(라이브 재조회 — 이미 있음).
