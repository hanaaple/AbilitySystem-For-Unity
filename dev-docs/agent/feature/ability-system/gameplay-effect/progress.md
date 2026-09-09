# gameplay-effect — 게임플레이 이펙트 (GE·Spec·Active·Modifier)

- 상태: ✅ 임시완료
- 우선순위: P0
- 최종 갱신: 2026-09-09 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-ability](../gameplay-ability/progress.md) · [aggregator](../aggregator/progress.md).
> 핵심 배관(정의·Spec·Modifier·Execution·Capture·AttributeBased)은 구현·검증됨. 라이브 재평가(non-snapshot 추종)는 [aggregator](../aggregator/progress.md)가 얹었다.

## 목표
모든 수치 변경을 GameplayEffect 한 채널로 통과시켜, 여러 출처의 수치 조작이 순서·잔류 문제 없이 합쳐지게 한다. SO 불변 정의 + 런타임 Spec 분리, Modifier 6종 연산, 적용/해제/주기/지속, 캡처 기반 계산(AttributeBased·Execution).

## 수용 기준 (Definition of Done)
- [x] GE(SO) 적용/해제가 Attribute 값에 반영
- [x] Modifier 6종(`AddBase`/`MultiplyAdditive`/`DivideAdditive`/`MultiplyCompound`/`AddFinal`/`Override`)이 공식대로 CurrentValue 누산
- [x] GE 3종(Instant/Duration/Infinite) 실행 경로 동작 — Instant는 즉시 실행 후 소멸, Duration은 만료 제거, Infinite는 persistent, Period>0는 주기 틱
- [x] CurrentValue가 aggregator 집계(`Evaluate`)로 mod 반영해 산출 (persistent mod 등록/해제 → 재계산)
- [x] `GameplayEffectExecution` concrete 배선·플레이 검증 (`SpeedBoostExecution`, 2026-07-20)
- [x] AttributeBased magnitude 캡처·평가 (Source 생성 시·Target 적용 시 캡처 → `(v+Pre)*Coef+Post`)

> Capture·AttributeBased의 세부 검증 절차·항목: [tests.md](tests.md).

## 범위
### 포함
- 정의/런타임 분리: `GameplayEffectAsset`(SO: Type·Duration·Period·Modifiers·ExecutionTypeNames) ↔ `GameplayEffectSpec`(런타임 인스턴스, 대상별 `Clone()`)
- `GameplayModifier`(대상 어트리뷰트+연산+계산방식) / `GameplayModifierSpec`(평행 인덱스 슬롯, EvaluatedMagnitude 캐시) / `GameplayModifierOperation` 6종
- Magnitude 계산: `ScalableFloat`(고정) · `AttributeBased`(캡처값 기반)
- 캡처 계층: `GameplayEffectAttributeCaptureDefinition`/`...Spec`/`...SpecContainer` — Source(생성 시)·Target(적용 시) 2단계
- Execution: `GameplayEffectExecution`(순수 클래스, AQN 저장·1회 인스턴스화) + `...Parameters`/`...Output` — Instant/Periodic에서만 실행
- Context: `GameplayEffectContext`(+Handle) — Instigator/SourceObject 주입·조회
- 실행 경계는 `ActiveGameplayEffect`(+Handle)로 표현, 소유·수명·집계는 컨테이너(→ [aggregator](../aggregator/progress.md))
### 제외 (현 시점 — 이후 확장 가능)
- GE 스택 / Gameplay Cue / GameplayTag / ScalableFloat 커브 테이블(현재 고정 float)
- `SetByCaller` / `CustomCalculationClass` magnitude (→D20 SetByCaller 도입 예정)
- 실전 전투 데미지 공식(계산식 미확정 — `defense` 어트리뷰트는 추가 안 함으로 확정)

## 설계 개요
- **정의↔Spec(→D1·D2)**: 모든 수치 변경을 GE 한 경로로. SO는 불변 공유 정의, 적용 시 `GameplayEffectSpec`으로 인스턴스화하고 적용 경계에서 대상별 `Clone()`(→D19) 후 Target 캡처 → 같은 spec을 여러 대상에 적용해도 캡처값이 섞이지 않는다.
- **Modifier 슬롯(→D13·D21)**: `GameplayEffectSpec.Modifiers`는 정의(`Definition.Modifiers`)와 평행한 인덱스 배열. identity(대상·연산)는 정의가 들고 슬롯은 `EvaluatedMagnitude`만 캐시 — 라이브 재평가 시 슬롯 magnitude만 제자리 갱신.
- **CurrentValue 산출**: persistent(period 0) GE의 modifier는 대상 어트리뷰트 aggregator에 mod로 등록, `AttributeAggregator.Evaluate`가 공식대로 누산해 CurrentValue를 낸다(→ [aggregator](../aggregator/progress.md)). Instant/Periodic은 BaseValue를 영구 변경.
- **쓰기 단일화(→D9)**: Modifier·Execution 출력 모두 `ApplyModToAttribute` 한 지점으로 BaseValue에 적용 — 연산 지원 범위가 어긋나지 않게.
- **캡처 계산(→D12~D17)**: AttributeBased modifier·Execution이 참조하는 어트리뷰트를 `SetupAttributeCaptureDefinitions`가 컨테이너에 등록 → Source는 생성 시, Target은 적용 시 캡처. `CalculateModifierMagnitudes`가 캡처 이후 magnitude 확정.
- **Execution(→D7·D11·D17)**: SO 아닌 순수 클래스, AQN으로 저장·1회 resolve. 입력/출력 struct 분리, `Defs()`로 캡처 대상 선언. Instant·Period>0에서만 실행(Duration/Infinite+Period0은 실행 안 됨 — UE 동일).
- 아키텍처: [architecture/ability-system/gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) (동기화 최신 아닐 수 있음)
- 코드: `Assets/Scripts/Core/AbilitySystem/Effect/`, 적용·수명은 `ActiveGameplayEffectsContainer` / 공개 API는 `AbilitySystemComponent`

## 세부 TODO (구현 체크리스트)
- [x] 1. GE/Modifier 정의 + Spec resolve 캐싱 + Active 상태
- [x] 2. Apply/Remove + Modifier 6종 누산 + Duration/Period 틱 (현재 컨테이너 소유)
- [x] 3. GE 3종 실행 경로 (enum 주석의 "미구현"은 낡음 — 코드상 셋 다 동작)
- [x] 4. Execution 프레임워크 + concrete(`SpeedBoostExecution`) + 플레이 검증
- [x] 5. AttributeBased magnitude — 데이터 모델 + 캡처(Source/Target 2단계) + 런타임 evaluate
- [ ] 6. `SetByCaller` magnitude (→D20, 유저 도입 확정) — 별도 착수
- [ ] 7. 실전 전투 공식 확정 (Execution vs AttributeBased 역할 경계 →아래 결정 대기) + 호출처 배선(공격 시 target에 GE 적용)

## 결정 기록
상세(맥락·대안·근거·트레이드오프)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-06-13 | GE 단일 채널 추상화 — 모든 수치 변경을 GE 한 경로로 |
| D2 | 2026-06-13 | SO 불변 정의 + 런타임 `Spec` 분리 (공유 GE 간섭 제거, resolve 1회 캐싱) |
| D3 | 2026-06-13 | ~~쓰기 시점 재계산 + 읽기 캐시~~ — aggregator 재설계로 대체(현재 `Evaluate` 읽기 시 누산, → [aggregator](../aggregator/progress.md) D10) |
| D4 | 2026-06-13 | Modifier 6종 + BaseValue/CurrentValue 두 경로 (Instant·Periodic=Base 영구변경 / Duration·Infinite=Current만) |
| D5 | 2026-07-19 | `MakeOutgoingSpec`은 Spec(class) 직접 반환 — SpecHandle 미도입 |
| ~~D6~~ | 2026-07-19 | **결번(폐기)** — 승인 없이 작성한 "캡처 미도입" 결정. 방향은 도입으로 확정 |
| D7 | 2026-07-19 | Execution은 SO가 아닌 순수 클래스 — 공유 데이터가 없어 SO 불필요 |
| D8 | 2026-07-19 | (미결) Execution 직렬화 형태 — 현재 AQN 타입 참조로 구현됨 |
| D9 | 2026-07-19 | Execute 쓰기 지점을 `ApplyModToAttribute` 하나로 통합 |
| D10 | 2026-07-19 | BaseValue 쓰기마다 CurrentValue 즉시 재계산 (stale 읽기 버그 수정) |
| D11 | 2026-07-19 | Execution 입력/출력 타입 분리 + 둘 다 struct + execution 스코프 지역 변수 |
| D12 | 2026-07-28 | `GameplayEffectAttributeCaptureDefinition` 독립 struct 신설 (UE 원형 필드) |
| D13 | 2026-08-01 | 캡처 결과 계층 — Spec(1건)+Container(묶음), snapshot 고정/라이브 재조회 |
| D14 | 2026-08-02 | `GameplayEffectSpec`이 캡처 컨테이너 소유 + AttributeBased를 정의로 compose, 생성 시 Source 캡처 |
| D15 | 2026-08-03 | 어트리뷰트 참조를 직렬화 타입 `GameplayAttribute`(AQN+필드명)+전용 드로어로 추출 |
| D16 | 2026-08-03 | 캡처 컨테이너가 각 spec에 정의를 품게 정렬(별도 `_definitions` 리스트 제거) |
| D17 | 2026-08-03 | Execution이 `Defs()`로 캡처 대상 선언 → spec이 순회 등록 (modifier와 동형) |
| D18 | 2026-08-04 | `ActiveGameplayEffect`가 Owner 직접 보유. ~~컨테이너 유보~~ **→ 컨테이너 도입됨**(→ [aggregator](../aggregator/progress.md) D6) |
| D19 | 2026-08-04 | `GameplayEffectSpec` 복사 생성자(+`Clone()`) — 적용 시점 대상별 복제 |
| D20 | 2026-08-04 | spec 직접 생성 API + apply 시점 clone 유지 — SetByCaller 도입 예정이라 clone은 선배관 |
| D21 | 2026-08-04 | AttributeBased evaluate 배선 + `GameplayModifierSpec`을 UE `FModifierSpec`에 정렬 |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음. (구 "GE 타입 enum 주석 불일치"는 코드 확인으로 해소 — 3종 실행 경로 모두 존재. `GameplayEffectType` 주석의 "미구현"은 낡은 서술.)

## 다음 작업
1. **실전 전투 공식 확정** — Execution(클램프·크리티컬 등 분기)과 AttributeBased(단순 사칙연산) 역할 경계를 실측 근거로 decisions에 기록. `defense` 어트리뷰트를 안 쓰기로 확정했으니 데미지 계산식을 다시 정한 뒤 착수.
2. **호출처 배선** — 공격 컴포넌트가 `MakeEffectContext()`(Instigator=공격자) 기반으로 target에 GE 적용. (실행 계층은 [gameplay-ability](../gameplay-ability/progress.md)와 함께.)
3. **`SetByCaller`**(→D20) — 런타임 계산값을 spec에 꽂아 적용. spec 직접생성 API+clone의 실사용 소비처. 별도 작업([TODO-BOARD](../../../TODO-BOARD.md)).
4. (확장 시) GE 스택 / Cue / Tag / ScalableFloat 커브는 데모 요구가 생기면.
