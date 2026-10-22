# gameplay-effect — 게임플레이 이펙트 (GE·Spec·Active·Modifier)

> 작성 규약 → [progress-harness.md](../../progress-harness.md)

- 상태: ✅ 임시완료
- 우선순위: P0
- 최종 갱신: 2026-10-23 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-ability](../gameplay-ability/progress.md) · [gameplay-tag](../gameplay-tag/progress.md) · [aggregator](../aggregator/progress.md).
> 라이브 재평가(non-snapshot 추종)는 [aggregator](../aggregator/progress.md), GE의 태그 연동(Component·태그 조건)은 [gameplay-tag](../gameplay-tag/progress.md)가 얹었다.

## 목표
모든 수치 변경을 GameplayEffect 한 채널로 통과시켜, 여러 출처의 수치 조작이 순서·잔류 문제 없이 합쳐지게 한다. SO 불변 정의 + 런타임 Spec 분리, Modifier 6종 연산, 적용/해제/주기/지속, 캡처 기반 계산(AttributeBased·Execution).

## 수용 기준 (Definition of Done)
- [x] GE(SO) 적용/해제가 Attribute 값에 반영
- [x] Modifier 6종(`AddBase`/`MultiplyAdditive`/`DivideAdditive`/`MultiplyCompound`/`AddFinal`/`Override`)이 공식대로 CurrentValue 누산
- [x] GE 3종(Instant/Duration/Infinite) 실행 경로 — Instant는 즉시 실행 후 소멸, Duration은 만료 제거, Infinite는 유지, Period>0는 주기 실행
- [x] CurrentValue가 aggregator 집계로 mod를 반영해 산출 (persistent mod 등록/해제 → 재계산)
- [x] `GameplayEffectExecution` 배선·플레이 검증
- [x] AttributeBased magnitude 캡처·평가 (Source 생성 시·Target 적용 시 캡처 → `(v+Pre)*Coef+Post`)
- [x] 어트리뷰트 값 변경 이벤트 — 구독자가 Old/New와 원인(GE 실행이면 Spec·수치·Target)을 받는다, 값이 같아도 온다

> 검증 수단(셀프체크·PlayMode): [tests.md](tests.md).

## 범위
### 포함
- 정의/런타임 분리: `GameplayEffectAsset`(SO: Type·Duration·Period·Stacking·Modifiers·Executions·Components) ↔ `GameplayEffectSpec`(런타임, 대상별 `Clone()`)
- `GameplayModifier` / `GameplayModifierSpec`(정의와 평행한 슬롯, `EvaluatedMagnitude`) / 연산 6종, magnitude 구조체 `GameplayEffectModifierMagnitude`(ScalableFloat·AttributeBased)
- 캡처 계층: `GameplayEffectAttributeCaptureDefinition`/`...Spec`/`...SpecContainer` — Source(생성 시)·Target(적용 시)
- Execution: `GameplayEffectExecutionDefinition`(계산 클래스 AQN + Calculation Modifiers) + `...Parameters`/`...Output` — Instant/Periodic에서만 실행
- Context: `GameplayEffectContext`(+Handle) — Instigator·SourceObject·피격 정보, 대상별 복제
- 활성 GE: `ActiveGameplayEffect`(+Handle), 소유·수명·집계는 `ActiveGameplayEffectsContainer`. 적용 시각 기반 수명, 스택(수 1)
- 질의: `GameplayEffectQuery` + ASC 찾기·시간·레벨·제거 API, Remove Other Component
- 이벤트: 어트리뷰트 값 변경(`GetGameplayAttributeValueChangeDelegate`), ASC 적용·주기 실행·제거 알림
- Spec 지속 시간(`SetDuration`)
### 제외 (현 시점 — 이후 확장 가능)
- GE 스택 수 2 이상 / Gameplay Cue / ScalableFloat 커브 테이블(현재 고정 float)
- `SetByCaller` / `CustomCalculationClass` magnitude — 인스펙터 자리만(→D20 SetByCaller 도입 예정)
- Execution Transient 집계기·`PassedInTags`·Conditional GE

## 설계 개요
- **정의↔Spec(→D1 GE 단일 채널·D2 정의/Spec 분리)**: SO는 불변 공유 정의, 적용 시 `GameplayEffectSpec`으로 만들고 적용 경계에서 대상별 `Clone()`(→D19) 후 Target 캡처 — 같은 Spec을 여러 대상에 걸어도 캡처값이 섞이지 않는다. 대상별로 피격 정보를 달리 담을 땐 컨텍스트도 복제(→D32).
- **Modifier 슬롯**: `Spec.Modifiers`는 정의와 평행한 배열. 대상·연산은 정의가 들고 슬롯은 `EvaluatedMagnitude`만(aggregator D13).
- **두 경로(→D4 Modifier 6종·두 경로)**: persistent(period 0) GE의 modifier는 aggregator에 mod로 등록돼 CurrentValue를 낸다. Instant/Periodic은 BaseValue를 영구 변경.
- **쓰기 단일화**: Modifier·Execution 출력 모두 `InternalExecuteMod`를 거친다 — Set `PreGameplayEffectExecute` → `ApplyModToAttribute` → `PostGameplayEffectExecute`(UE 구조).
- **값 변경 알림(→D26)**: CurrentValue를 쓰는 곳은 `InternalUpdateNumericalAttribute` 하나 — `PreAttributeChange` → 저장 → `PostAttributeChange` → 이벤트(값이 같아도). GE 실행이 원인이면 `GameplayEffectModCallbackData`를 실어 보낸다.
- **캡처 계산(→D14)**: AttributeBased·Execution이 참조하는 어트리뷰트를 Spec이 캡처 컨테이너에 등록 → Source는 생성 시, Target은 적용 시 캡처. magnitude는 Target 캡처 뒤 확정.
- **Execution(→D7·D23)**: 순수 C# 클래스, AQN으로 저장. `Defs()`로 캡처 대상을 선언하고 Calculation Modifier는 그 중에서 고른다(실행마다 캡처 aggregator 사본에 더함).
- **수명(→D27)**: 적용 시각(`StartWorldTime`)을 기록하고 Tick은 적용 뒤 흐른 시간만 센다. Tick 순서 영향 → [tick-order-improvements.md](tick-order-improvements.md)·[tick-order-user-notes.md](tick-order-user-notes.md).
- **스택(→D31)**: 같은 GE를 합쳐 지속 시간·주기만 갱신, Spec은 처음 것 유지.
- **질의(→D24·D25)**: UE `FGameplayEffectQuery`·ASC 질의 API 목록 그대로, Remove Other는 모든 질의에서 자기 제외.
- **디버그 조회(→D30)**: ASC `GetSpawnedAttributes`·`GetActiveGameplayEffects`·적용/주기/제거 이벤트 — 에디터 ASC 인스펙터와 게임 디버그 오버레이가 쓴다.
- **코스트 확인**: `CanApplyAttributeModifiers` — 임시 Spec으로 AddBase 모디파이어만 현재값 + 크기 ≥ 0인지 본다(gameplay-ability D7이 씀).
- 코드: `Assets/Scripts/Core/AbilitySystem/Effect/`·`GameplayEffectComponents/`, 공개 API는 `AbilitySystemComponent`
- 아키텍처: [architecture/ability-system/gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md)

## 세부 TODO (구현 체크리스트)
- [x] 1. GE/Modifier 정의 + Spec + Active 상태
- [x] 2. Apply/Remove + Modifier 6종 누산 + Duration/Period
- [x] 3. GE 3종 실행 경로
- [x] 4. Execution 프레임워크 + concrete + 플레이 검증
- [x] 5. AttributeBased magnitude — 데이터 모델 + 캡처(Source/Target) + 런타임 평가
- [ ] 6. `SetByCaller` magnitude (→D20, 유저 도입 확정) — TODO-BOARD 'GE 기능'
- [x] 7. 전투 데미지 배선 — 데모에서 GA가 데미지 GE를 대상에 건다(Execution `DamageExecution`, Game 쪽)
- [x] 8. 어트리뷰트 값 변경 이벤트 (→D26)
- [x] 9. 질의·제거 API + Remove Other (→D24·D25)
- [x] 10. Execution Calculation Modifier 편집기·런타임 (→D22·D23)
- [x] 11. 적용 시각 기반 수명 (→D27)
- [x] 12. Spec 지속 시간·피격 정보·컨텍스트 복제 (→D28·D29·D32)
- [x] 13. 스택 수 1 (→D31)
- [x] 14. 디버그 조회 API (→D30)

## 결정 기록
상세는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.** 치운 결정(D3·D6·D8~D13·D16~D18·D21)은 decisions 끝에 한 줄씩.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | GE 단일 채널 | 2026-06-13 | 모든 수치 변경을 GE 한 경로로 |
| D2 | 정의/Spec 분리 | 2026-06-13 | SO 불변 정의 + 런타임 `Spec` (공유 GE 간섭 제거) |
| D4 | Modifier 6종·두 경로 | 2026-06-13 | Instant·Periodic=Base 영구 변경 / Duration·Infinite=Current만 |
| D5 | Spec 직접 반환 | 2026-07-19 | `MakeOutgoingSpec`은 Spec(class) 직접 반환 — SpecHandle 없음 |
| D7 | Execution=순수 클래스 | 2026-07-19 | Execution은 SO가 아닌 순수 클래스 — 공유 데이터가 없어 SO 불필요 |
| D14 | AttributeCapture 계층=UE 구조 | 2026-08-02 | 캡처 정의·Spec·Container, Spec이 컨테이너 소유, AttributeBased가 캡처 정의를 품음 |
| D15 | GameplayAttribute 타입 | 2026-08-03 | 어트리뷰트 참조를 직렬화 타입 `GameplayAttribute`(AQN+필드명)+전용 드로어로 |
| D19 | Spec 복사/Clone | 2026-08-04 | `GameplayEffectSpec` 복사 생성자(+`Clone()`) — 적용 시점 대상별 복제 |
| D20 | spec API+clone 유지 | 2026-08-04 | Spec 직접 생성 API + 적용 시점 Clone 유지 — SetByCaller 도입 예정 |
| D22 | Magnitude=UE 구조체 공유 | 2026-10-19 | `GameplayEffectModifierMagnitude`를 GE Modifier·Calculation Modifier가 같이 씀 |
| D23 | Execution=UE ExecutionDefinition | 2026-10-19 | `executions`(계산 클래스 + Calculation Modifiers), 대상은 Execution `Defs()`에서 고름 |
| D24 | Remove Other=모든 질의에서 자기 제외 | 2026-10-20 | UE는 둘째 질의부터 자기 제외가 빠짐(회귀로 판단) — 우리는 모든 질의에 건다 |
| D25 | GE 질의 API=UE 목록 | 2026-10-20 | `GameplayEffectQuery`·ASC 찾기·시간·레벨·제거 UE대로, `StacksToRemove`는 자리만 |
| D26 | 값 변경 이벤트=이벤트 객체 getter | 2026-10-20 | C# delegate가 불변이라 어트리뷰트별 이벤트 객체를 돌려준다 |
| D27 | GE 수명=적용 시각 기록 + Tick 한 루프 | 2026-10-21 | `StartWorldTime`·컨테이너 시계, 적용 뒤 흐른 시간만 셈 |
| D28 | 지속 시간=Spec 값 + `SetDuration` | 2026-10-22 | 걸 때 길이를 바꿀 통로. 에셋 Duration Magnitude·캡처·잠금은 없음 |
| D29 | 피격 정보=Context `AddHitResult` | 2026-10-22 | `RaycastHit` 하나(Lyra 방식), Origin 없음 |
| D30 | 디버그 조회 API | 2026-10-23 | ASC `GetSpawnedAttributes`·`GetActiveGameplayEffects`·`OnAnyGameplayEffectRemoved` 등 — UE와 같은 이름 |
| D31 | GE 스택(수 1) | 2026-10-23 | 같은 GE를 합쳐 시간만 갱신, Spec 유지. 스택 수 2+ 없음 |
| D32 | 대상별 Spec·컨텍스트 복제 | 2026-10-23 | 한 Spec을 여러 대상에 걸 땐 대상마다 `Clone()` + 컨텍스트 `Duplicate()` |

### 결정 대기
- **Execution 직렬화 형태(구 D8)** — AQN 타입 참조(지금, UE `TSubclassOf`와 같음 / GE별 튜닝 값 불가) vs `[SerializeReference]` 인스턴스(GE별 튜닝 값 / 이름 변경 시 참조 깨짐). 판단 축: Execution이 GE별 튜닝 값을 갖게 될까 — 데모 `DamageExecution`은 무작위 범위·헤드 배율을 클래스 상수로 둔다.

- 다른 feature 작업으로 이 feature 코드 수정(사유는 각 feature 기록):
  - [gameplay-tag](../gameplay-tag/progress.md) — Modifier Source/Target 태그·판정, Spec 캡처 태그, GE Component·inhibit, Asset/Granted Tags, 태그만 주는 GE 허용, 주기 실행마다 magnitude 재계산(UE대로), 인스펙터 접기 (2026-10-16~19).
  - [attribute](../attribute/progress.md) — `InternalExecuteMod`·Current 쓰기 훅, Current 조회를 저장값으로 (2026-10-20).
  - [gameplay-ability](../gameplay-ability/progress.md) — `CanApplyAttributeModifiers`·`HasAnyMatchingGameplayTags`(2026-10-21), 적용 시각 기반 수명(D27의 계기, 2026-10-21).

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
1. **`SetByCaller`**(→D20) — 런타임 계산값을 Spec에 태그 키로 꽂아 적용. TODO-BOARD 'GE 기능'. 착수는 유저 지시 때.
2. 남은 GE 기능·버그(스택 수 2+, Custom Calculation, Transient, Tick 도중 목록 변경 등)는 TODO-BOARD 'GE·Tag 남은 것'.
