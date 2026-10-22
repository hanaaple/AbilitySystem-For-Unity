# aggregator — 어트리뷰트 반응성 / Aggregator (라이브 재평가)

> 작성 규약 → [progress-harness.md](../../progress-harness.md)

- 상태: ✅ 임시완료
- 우선순위: P1
- 최종 갱신: 2026-10-23 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-tag](../gameplay-tag/progress.md) · [gameplay-ability](../gameplay-ability/progress.md).

## 목표
non-snapshot 캡처를 쓰는 AttributeBased 모디파이어가, **캡처 대상 어트리뷰트가 바뀌면 그 즉시** magnitude를 재평가해 반영되게 한다(예: "방어력 = 힘의 10%"에서 힘이 바뀌면 방어력이 즉시 갱신).

## 수용 기준 (Definition of Done)
- [x] non-snapshot AttributeBased 모디파이어: 캡처 대상 어트리뷰트를 다른 경로로 바꾸면 그 모디파이어가 적용된 어트리뷰트가 즉시 재계산된다 (cross-attribute) — PlayMode: Health 100→200 → Speed 50→100
- [x] self-effect(같은 ASC)에서 같은 동작 — PlayMode. 다른 ASC를 캡처한 경우도 같다(PlayMode `AbilitySystemLifecyclePlayModeTest` X-1)
- [x] snapshot 모디파이어는 고정(재평가 안 됨) — PlayMode
- [~] 자기참조/연쇄 config에서 무한 루프가 없다 — `MaxBroadcastDirty` 깊이 상한으로 폭주는 끊는다. 전용 자기참조 케이스 검증은 아직(폭주 방지지 수렴 보장 아님).

## 범위
### 포함
- **어트리뷰트별 `AttributeAggregator`**: base 사본 + mod 목록(채널 없음 →D10), `Evaluate`가 CurrentValue 계산(6종 연산). Instant/Periodic의 영구 base 변경은 `ExecModOnBaseValue`.
- **소유·수명**: 컨테이너(`ActiveGameplayEffectsContainer`)가 어트리뷰트별로 지연 생성(`FindOrCreateAttributeAggregator`). persistent(period 0) GE의 mod를 적용 때 등록, 제거 때 해제.
- **base 동기화(→D14)**: base 진실은 `AttributeData`. aggregator가 있을 때만 사본을 맞추고, 없으면 Current = Base.
- **반응성**: base/mod 변경 → `OnDirty` → 컨테이너 `OnAttributeAggregatorDirty`가 그 어트리뷰트를 다시 계산 → `InternalUpdateNumericalAttribute`(Pre/PostAttributeChange·값 변경 이벤트, 값이 같아도).
- **cross-attribute 라이브(→D12)**: non-snapshot 캡처가 소스 aggregator에 의존 GE 등록 → 소스 dirty 때 `BroadcastOnDirty`가 의존 GE의 magnitude를 다시 계산하고 대상 aggregator의 mod를 갱신(`UpdateAggregatorMod`) → 대상 dirty로 연쇄. `MaxBroadcastDirty`로 순환 차단.
- **태그 자격**: mod가 Source/Target 태그 조건을 들고, 평가 문맥으로 `UpdateQualifies` → `Qualifies()`(UE 두 단계). CurrentValue는 빈 문맥(gameplay-tag D28).
### 제외 (현 시점 — 이후 확장 가능)
- 자기참조 fixed-point 수렴(지금은 깊이 상한까지)
- Execution의 라이브 재평가(Execution은 Instant/Periodic 이벤트성)
- 평가 채널·dirty 배칭(`FScopedAggregatorOnDirtyBatch`)

## 설계 개요
> UE 참고: `FAggregator` 개념·구조·평가 흐름 정리는 [ue-reference.md](ue-reference.md)(유저 제공 분석), 포팅 Q&A는 [porting-notes.md](porting-notes.md)(유저 임시 노트).

- **소유 구도**: ASC는 얇은 위임 래퍼, 활성 GE 소유·수명·집계·반응성은 `ActiveGameplayEffectsContainer`. dirty 진입점(`OnAttributeAggregatorDirty`·`OnMagnitudeDependencyChange`)은 UE처럼 ASC를 거쳐 컨테이너로.
- **값 경로**: `GetAttributeCurrentValue`는 저장값(`AttributeData` Current) — aggregator 계산값은 dirty마다 동기로 저장되고, 저장 직전 `PreAttributeChange`가 고친 값이 조회에도 보인다. `GetAttributeBaseValue`는 `AttributeData` Base.
- **지연 생성(→D14)**: aggregator는 캡처·persistent mod가 필요할 때만 만든다(UE lazy 모델).
- **dirty 전파(→D12)**: 위상 정렬 대신 UE처럼 의존 핸들 목록 + `OnDirty` 연쇄 + 깊이 상한.
- **캡처**: snapshot은 그 시점 aggregator 사본(`TakeSnapshotOf`), non-snapshot은 컨테이너 소유 aggregator 참조.
- 코드: `Assets/Scripts/Core/AbilitySystem/Effect/AttributeAggregator.cs`(값·mod·전파) · `ActiveGameplayEffectsContainer.cs`(소유·재계산·dirty 진입) · `AbilitySystemComponent.cs`(진입 래퍼)
- 아키텍처: [architecture/ability-system/aggregator.md](../../../../project/architecture/ability-system/aggregator.md)

## 세부 TODO (구현 체크리스트)
- [x] 1. 어트리뷰트별 `AttributeAggregator`(base+mod, `Evaluate`, `ExecModOnBaseValue`)
- [x] 2. 컨테이너 소유·지연 생성 + persistent mod 등록/해제
- [x] 3. base 동기화 — `AttributeData` 진실 + aggregator 사본 (→D14)
- [x] 4. 반응성 — `OnDirty` → 컨테이너 재계산
- [x] 5. cross-attribute 라이브 재평가 — 의존 등록/전파 + 재계산 + 순환 상한 (→D12)
- [x] 6. 태그 자격 판정 — gameplay-tag에서 구현(D14·D27·D28)
- [ ] 7. 자기참조 전용 케이스 검증

## 결정 기록
상세는 → [decisions.md](decisions.md). 치운 결정(D1~D4·D6~D9·D11)은 decisions 끝에 한 줄씩. **해당 영역을 건들 때 그 결정을 연다.**

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D5 | 어트리뷰트별 Aggregator | 2026-08-09 | 어트리뷰트마다 aggregator — 계산·mod 저장을 ASC에서 옮김, 컨테이너가 맵 소유 |
| D10 | 채널 축 제거 | 2026-08-11 | 채널 1개라 flat `List<Mod>`로 직접 소유 |
| D12 | cross-attr 라이브 재평가 | 2026-08-11 | UE 방식 — 의존 등록 + `BroadcastOnDirty` 연쇄 + `MaxBroadcastDirty` 상한 |
| D13 | ModifierSpec 단일 캐시 | 2026-08-15 | `GameplayModifierSpec`은 `EvaluatedMagnitude`만, 대상·연산은 정의를 같은 인덱스로 |
| D14 | base 진실=AttributeData | 2026-08-16 | base 진실 = `AttributeData`(UE 원문), aggregator base = 동기화 사본, 지연 생성 |

- 다른 feature 작업으로 이 feature 코드 수정(사유는 각 feature 기록):
  - [gameplay-tag](../gameplay-tag/progress.md) — mod 태그 조건·평가 파라미터(2026-10-16), aggregator 없는 어트리뷰트의 Current 미갱신 수정(D14 누락분)·`UpdateAggregatorMod`를 핸들로 빼고 다시 넣게 수정(2026-10-17), 자격 판정 두 단계(2026-10-18, D27), CurrentValue 빈 문맥(D28).
  - [gameplay-effect](../gameplay-effect/progress.md)·[attribute](../attribute/progress.md) — 값이 같아도 이벤트, `InternalUpdateNumericalAttribute` 훅, Current 조회를 저장값으로(2026-10-20).

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
1. **자기참조 전용 케이스 검증** — A→B→A 류 GE 구성으로 `MaxBroadcastDirty` 상한이 폭주를 끊는지 셀프체크 또는 PlayMode로 확인(수용 기준 4). 착수는 유저 지시 때.
2. (확장 시) dirty 배칭·자기참조 수렴은 요구가 생기면.
