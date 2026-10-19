# aggregator — 결정 기록

> 무엇을·어떻게 적나 → [feature/HARNESS.md](../../HARNESS.md) '결정 기록 기준'. `D#`는 feature 내 고유·불변 코드다.
> 경위는 [worklog](worklog.md), 2026-10-18 정리 이전의 표 본문 전체는 git `ca110d2`. UE `FAggregator` 원문 정리는 [architecture/ability-system/aggregator.md](../../../../project/architecture/ability-system/aggregator.md).

---

## D5 — 어트리뷰트별 Aggregator를 두고 mod·계산을 거기로 옮긴다 (UE `FAggregator`)

- 날짜: 2026-08-09 (mod 소유 2026-08-10)
- 결정: 어트리뷰트마다 `AttributeAggregator`를 두고, ASC 안에 있던 계산(연산별 누산·Base 연산)과 mod 저장을 옮긴다. period 0 GE는 적용 때 mod를 등록하고 제거 때 해제한다. 활성 GE와 aggregator 맵은 `ActiveGameplayEffectsContainer`가 가진다.
- 왜: 유저 — aggregator는 ASC가 하나만 드는 게 아니라 **어트리뷰트별로** 있어야 하고, 새로 만든다면 기존 계산이 그리로 옮겨져야 한다.
- 대안: 반응성(의존·dirty·재평가)만 따로 떼고 계산은 ASC에 두기(변경 적음 / 유저 의도와 다름 — 치운 D1·D2·D4).
- 대가: 적용·제거·실행 경로를 넓게 다시 짰고 컨테이너 레이어가 생겼다.

## D10 — 평가 채널 없이 mod를 한 목록으로 (UE와 다름)

- 날짜: 2026-08-11
- 결정: `AggregatorModChannel`·채널 enum·채널 맵을 없애고, aggregator가 mod를 flat `List<Mod>`로 들고 `Evaluate`가 연산별로 집계한다.
- 왜: 유저 — "채널을 1개만 쓸 거야. Channel이라는 걸 없애고 단일로 하게 해."
- 대안: 채널 맵 유지(UE `FAggregator::ModChannels`와 같음 / 쓰지 않는 구조만 남음 — 치운 D9).
- 대가: UE 채널 구조와 갈린다. 다채널이 필요하면 다시 넣어야 한다.
- 재검토: 평가 채널을 둘 이상 실제로 쓰게 되면.

## D12 — 어트리뷰트 간 라이브 재평가는 UE 방식: 의존 등록 + dirty 전파 + 깊이 상한

- 날짜: 2026-08-11
- 결정: 소스 aggregator가 non-snapshot으로 의존하는 GE 핸들(`_dependents`)을 들고, dirty가 나면 그 GE의 magnitude를 다시 계산해 대상 aggregator의 mod를 갱신한다. 반응성의 시작은 aggregator `OnDirty` → 컨테이너 `OnAttributeAggregatorDirty`(치운 D11). 순환은 `MaxBroadcastDirty` 깊이 상한으로 끊는다.
- 왜: 유저 — "ue 기준으로", "ue를 따른다".
- 대안: 위상 정렬 재평가(에이전트 제안 — DAG면 중복 재계산 없고 순환을 감지로 거부 / UE와 다름).
- 대가: 순환은 해결이 아니라 차단이다(상한에서 중간값). 다이아몬드 의존이면 중복 재계산. UE의 dirty 배칭(`FScopedAggregatorOnDirtyBatch`)이 없어 mod마다 OnDirty가 울린다.
- 재검토: 중복 재계산·순환이 실제 문제가 되면 위상 정렬.

## D13 — `GameplayModifierSpec`은 UE `FModifierSpec`처럼 `EvaluatedMagnitude` 하나만

- 날짜: 2026-08-15
- 결정: 슬롯에서 핸들·연산 캐시를 없애고, 적용·실행·인스펙터가 정의(`GameplayModifier`)를 같은 인덱스로 읽어 대상·연산을 해석한다.
- 왜: 유저 — "EvaluatedMagnitude만 남기고 Spec.def.mod를 통해 Evaluate, Def.mod와 indexing 동일하게 식별".
- 대안: 정의 쪽에 해석 결과를 1회 캐시(리플렉션 비용 없음 / 유저가 고르지 않음, 이유 `근거 미기록`).
- 대가: `GameplayAttribute`가 문자열이라 해석에 리플렉션이 들어, modifier마다 적용·실행 때 한 번씩 비용이 난다.
- 재검토: 이 해석이 핫패스 병목이 되면 정의 쪽 캐시.

## D14 — BaseValue의 진실은 `AttributeData`, aggregator의 base는 평가용 사본

- 날짜: 2026-08-16
- 결정: Base 변경은 `AttributeData.BaseValue`에 직접 쓰고, aggregator가 있을 때만 그 사본을 맞춘다. aggregator가 없으면 Current = Base로 바로 쓴다. aggregator는 캡처나 period 0 GE가 필요로 할 때만 만든다.
- 왜: 유저 — "방금 말한 방향성으로 가자"(UE 원문 모델 설명 뒤). UE `SetAttributeBaseValue`·`GetAttributeBaseValue`가 이렇게 동작한다. UE의 이중 base는 네트워크 복제·legacy float 호환 때문이라 우리에겐 해당이 없다.
- 대안: aggregator가 Base 진실(치운 D7 — "외부는 Evaluate로만"이 깔끔 / 두 곳을 늘 맞춰야 하고 UE 원문과도 어긋남).
- 대가: 외부 Base 쓰기는 여전히 aggregator 사본을 맞춰야 한다.

## 치운 결정

중요하지 않거나, 뒤집혔거나, 다른 결정에 합쳐졌거나, 코드가 이미 바뀐 결정이라 본문을 치웠다(유저 2026-10-18). 코드는 재사용하지 않는다.

- **D1** (2026-08-07) — 반응성(의존·dirty·재평가)만 별도 컴포넌트로 분리. 폐기 → D5.
- **D2** (2026-08-07) — 경량 옵저버 + 재평가 + 재진입 가드. 폐기 → D5.
- **D3** (2026-08-08) — 변경 이벤트를 `UpdateAttributeCurrentValue` 한 곳에서. 그 함수가 없어지고 `OnAttributeAggregatorDirty`·`InternalUpdateNumericalAttribute`로 바뀜.
- **D4** (2026-08-08) — registry 없이 의존 필터 + worklist 재평가. 폐기 → D5·D12.
- **D6** (2026-08-09) — `ActiveGameplayEffectsContainer` 도입 1차, struct로. 지금 코드는 class.
- **D7** (2026-08-10) — BaseValue 진실 = Aggregator. D14로 뒤집힘.
- **D8** (2026-08-10) — mod 소유 = Aggregator(적용 등록·제거 해제). D5에 합침.
- **D9** (2026-08-11) — 채널별 `AggregatorModChannel` 맵. D10으로 폐기.
- **D11** (2026-08-11) — 컨테이너 worklist를 없애고 aggregator `OnDirty` → `OnAttributeAggregatorDirty`(UE 그대로). D12에 합침.
