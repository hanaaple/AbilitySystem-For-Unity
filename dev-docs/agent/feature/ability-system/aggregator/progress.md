# aggregator — 어트리뷰트 반응성 / Aggregator (라이브 재평가)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-08-07  (KST — HARNESS §3.4)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md).
> UE `FAggregator`의 **반응성(dirty/dependents) 책임만** 떼어와 라이브 재평가를 제공한다. UE 대비 채택/생략 범위는 아래 `## 범위`.

## 목표
non-snapshot 캡처를 쓰는 AttributeBased 모디파이어가, **캡처 대상 어트리뷰트가 바뀌면 그 즉시** magnitude를 재평가해 반영되게 한다(예: "방어력 = 힘의 10%"에서 힘이 바뀌면 방어력이 즉시 갱신). 현재는 magnitude가 apply 시점에 고정돼 non-snapshot이 무력한 상태를 해소한다.

## 수용 기준 (Definition of Done)
유저가 에디터에서 확인 가능한 조건으로:
- [ ] non-snapshot AttributeBased 모디파이어: 캡처 대상 어트리뷰트를 다른 경로로 바꾸면, 그 모디파이어가 적용된 어트리뷰트가 **즉시** 재계산된다 (cross-attribute).
- [ ] self-effect(같은 ASC) 케이스에서 동일하게 동작한다.
- [ ] snapshot=true 모디파이어는 기존처럼 고정(재평가 안 됨) — 회귀 없음.
- [ ] 자기참조/연쇄 config에서 **무한 루프가 발생하지 않는다**(재진입 억제).

## 범위
### 포함
- **R3 반응성**: 어트리뷰트 변경 통지 → 의존 모디파이어 magnitude 재평가(라이브 캡처 재조회) → 영향 어트리뷰트 재계산 → 유한 연쇄 + 재진입 가드.
- 대상: **AttributeBased 모디파이어**(persistent/CurrentValue 경로). cross-attribute·self-effect 라이브.
- 태그(R5)는 미도입이되 **얹을 seam만** 남긴다(자격 판정 함수 경계 + 일반화된 dirty 트리거).
### 제외 (명시적으로 하지 않을 것)
- **cross-actor 라이브**(Source가 다른 액터인 버프가 그 액터 값 변화를 실시간 추종 — cross-ASC 구독) → 필요해질 때 후속.
- **자기참조 fixed-point 수렴**(1회-패스로 정의; 진짜 수렴은 후속).
- **Execution의 라이브 재평가**(Execution은 instant/periodic 이벤트성이라 대상 아님).
- **UE per-attribute FAggregator 전면 이식**(R1 mod 채널 소유 / R2 current 계산 이관) — R1·R2는 ASC 유지.
- **태그 자격 판정(R5) 실제 구현** — seam만.

## 설계 개요
- **UE 확인 메커니즘(원문 근거):** `FGameplayEffectAttributeCaptureSpec::RegisterLinkedAggregatorCallback`이 `bSnapshot==false`일 때만 `Agg->AddDependent(Handle)`로 소스 aggregator의 `Dependents`에 등록 → 소스 변경 시 `FAggregator`가 `OnDirty.Broadcast` + `ASC->OnMagnitudeDependencyChange`로 dependent GE를 재평가. snapshot=true는 `TakeSnapshotOf`로 값 복사(등록 안 함). 상세는 [architecture/ability-system/aggregator.md](../../../../project/architecture/ability-system/aggregator.md).
- **우리 구현 방향(경량):** UE의 per-attribute aggregator 객체 전면 대신, **반응성 책임만** 분리한다.
  1. 어트리뷰트 변경 이벤트(옵저버) — 기존 쓰기 지점(`SetBaseAttributeValue`/`UpdateAttributeCurrentValue`)에서 fire. (현재 그런 이벤트 없음 → 신설)
  2. 핸들러가 **magnitude 재평가**(`GameplayEffectSpec.CalculateModifierMagnitudes` 라이브 캡처 재조회) 후 영향 어트리뷰트 재계산. *(지금은 frozen `EvaluatedMagnitude`만 써서 값이 안 변함 — 이 재평가 훅이 핵심)*
  3. **재진입 억제**(재계산 패스 중 이벤트 잠금)로 연쇄를 1패스로, 자기참조 무한루프 차단.
- **코드 위치:** `Assets/Scripts/Core/AbilitySystem/Aggregator/`(신설). 앵커 스텁 `AttributeAggregator.cs`. 관련 훅: `AbilitySystemComponent`(이벤트 발화·구독), `GameplayEffectSpec.CalculateModifierMagnitudes`(대상 슬롯 재평가), `GameplayEffectAttributeCaptureSpec`(라이브 재조회 — 이미 있음).

## 세부 TODO (구현 체크리스트)
- [ ] 1. 어트리뷰트 변경 이벤트를 ASC 쓰기 지점에 신설(옵저버 채널).
- [ ] 2. 의존 등록/해지: 스펙 apply 시 non-snapshot 캡처를 대상 어트리뷰트에 등록, remove 시 해지.
- [ ] 3. 변경 핸들러: 의존 모디파이어 magnitude 재평가 → 영향 어트리뷰트 재계산.
- [ ] 4. 재진입 억제(가드) — 연쇄 1패스·자기참조 무한루프 차단.
- [ ] 5. 태그 seam: 자격 판정 함수 경계 + dirty 트리거 일반화(구현은 안 함, 자리만).
- [ ] 6. 유저 에디터 검증(수용 기준).

## 결정 기록
→ [decisions.md](decisions.md) (상세). progress엔 요지 인덱스만.

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-08-07 | 라이브 재평가 위해 **반응성(R3) 책임만 별도 분리** — UE full aggregator(R1/R2) 이식은 안 함 |
| D2 | 2026-08-07 | 구현은 **경량(옵저버+재평가+재진입 억제)**으로 시작, 범위=cross-attr·self / cross-actor·자기순환·태그는 후속·seam |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
1. `AbilitySystemComponent`에 어트리뷰트 변경 이벤트 신설 — `SetBaseAttributeValue`/`UpdateAttributeCurrentValue`가 값 실제 변경 시 `(handle, old, new)`로 발화. (세부 TODO 1)
2. 이어서 non-snapshot 캡처 의존 등록/해지(세부 TODO 2) → 변경 핸들러 재평가(세부 TODO 3).
