# gameplay-effect — 게임플레이 이펙트 (GE·Spec·Active·Modifier)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: 2026-07-05 20:18 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-ability](../gameplay-ability/progress.md).
> **핵심은 구현·동작(✅)**, 아래 블로커(⚠️ GE 타입 실전 상태·Execution 껍데기)만 미결이라 IN-PROGRESS.

## 목표
모든 수치 변경을 GameplayEffect 한 채널로 통과시켜, 여러 출처의 수치 조작이 순서·잔류 문제 없이 합쳐지게 한다. SO 불변 정의 + 런타임 Spec 분리, Modifier 6종 연산, 적용/해제/주기/지속.

## 수용 기준 (Definition of Done)
- [x] GE(SO) 적용/해제가 Attribute 값에 반영
- [x] Modifier 6종(`AddBase`/`MultiplyAdditive`/`DivideAdditive`/`MultiplyCompound`/`AddFinal`/`Override`)이 공식대로 CurrentValue 누산
- [x] Duration 만료 제거·Period 주기 틱 동작
- [x] 읽기 캐시로 `GetAttributeCurrentValue` O(1)
- [ ] **GE 타입별(Instant/Duration) 실전 상태 확정** (⚠️ 아래 블로커)
- [ ] `GameplayEffectExecution` concrete 1개 이상 배선·검증 (또는 명시적 스코프 아웃)

## 범위
### 포함
- `GameplayEffect`(SO)·`GameplayModifier`·`GameplayEffectSpec`·`ActiveGameplayEffect`(+Handle)·`GameplayEffectContext`(+Handle)
- ASC 실행: `ApplyGameplayEffectToSelf`/`RemoveActiveGameplayEffect`, Modifier 누산, Duration/Period 틱, 읽기 캐시
### 제외 (현 시점)
- Execution concrete / GE 스택 / Gameplay Cue / GameplayTag / ScalableFloat 커브 / Magnitude 확장 (→ 아키텍처 미구현 로드맵)

## 설계 개요
- 아키텍처: [architecture/ability-system/gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md)
- 코드: `Assets/Scripts/Core/AbilitySystem/Effect/`, `AbilitySystemComponent.cs`

## 세부 TODO (구현 체크리스트)
- [x] 1. GE/Modifier 정의 + Spec resolve 캐싱 + Active 상태
- [x] 2. ASC Apply/Remove + Modifier 6종 누산 + Duration/Period 틱 + 읽기 캐시
- [ ] 3. GE 타입 실전 상태 확정 (유저 확인 대기)
- [ ] 4. Execution concrete 배선·검증 or 스코프 아웃 결정

## 결정 기록
상세(맥락·대안·근거·트레이드오프)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-06-13 | GE 단일 채널 추상화 — 모든 수치 변경을 GE 한 경로로 |
| D2 | 2026-06-13 | SO 불변 정의 + 런타임 `Spec` 분리 (공유 GE 간섭 제거, resolve 1회 캐싱) |
| D3 | 2026-06-13 | 쓰기 시점 재계산 + 읽기 캐시 → `GetCurrentValue` O(1) |
| D4 | 2026-06-13 | Modifier 6종 + BaseValue/CurrentValue 두 경로 (Instant·Periodic=Base 영구변경 / Duration·Infinite=Current만) |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- **⚠️ GE 타입 실전 상태 미확정:** `GameplayEffectType` enum 주석(`Instant`/`Duration`="미구현")과 ASC 실행 경로(셋 다 존재)가 어긋난다. 추정 금지 — **유저 직접 확인 예정**. (TODO-BOARD에도 등록)
- **Execution 껍데기:** `GameplayEffectExecution` + ASC `RunExecutions` 배관만 있고 concrete 0·에디터 미배선·미검증.

## 다음 작업
1. **유저가 GE 타입 실전 상태 확정** → [gameplay-effect.md](../../../../project/architecture/ability-system/gameplay-effect.md) §확인 필요 + [overview.md](../../../../project/architecture/ability-system/overview.md) 요약 표를 확정으로 갱신.
2. Execution을 실제로 쓸 트리거(예: Source.Damage − Target.Armor)가 생기면 concrete 1개 배선·검증, 아니면 스코프 아웃 결정을 decisions에 기록.
