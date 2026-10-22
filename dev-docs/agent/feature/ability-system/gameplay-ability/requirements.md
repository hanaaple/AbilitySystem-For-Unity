# GameplayAbility — 요구사항 정의서 (기획서)

- 문서 상태: 초안 — 범위는 진행하며 판단(유저). 구현 결과는 [progress](progress.md).
- 최종 갱신: 2026-10-23 (KST)
- 관련: [progress](progress.md) · [작업 방향·프로세스(HARNESS)](HARNESS.md) · [아키텍처](../../../../project/architecture/ability-system/gameplay-ability.md)

> 근거 태그: `[유저]` 유저 명시 · `[progress]` 기존 문서 · `[코드]` 작업 트리 사실. 미정은 §8. (문서화 원칙은 [HARNESS.md](HARNESS.md) 에이전트 운영 규율 4)

---

## 1. 개요 (Overview)

Unreal GAS의 **`GameplayAbility`**(스킬·액션의 실행 단위)를 이 Unity 프로젝트로 이식한다. 정의체는 **ScriptableObject 기반**, **UE와 유사한 구조 + 동일한 작동**이 목표다. 전부 복제하지 않고 **필요한 핵심만** 가져오며, 범위는 **진행하며 판단하고 번복될 수 있다.** `[유저]`

이미 선 수치/이펙트 계층(attribute · gameplay-effect · aggregator) 위에 **실행 계층**을 올려, 어빌리티가 코스트·쿨다운·효과를 GameplayEffect로 표현해 발동하게 한다. `[progress]`

---

## 2. 배경 (Background)

- 수치·효과·반응성 계층 위에서 **스킬을 실제로 발동하는 계층**이 GA다. `[progress]`
- 그 이상의 배경(어떤 게임 경험을 위해, 왜 지금)은 이번 논의에서 명시되지 않음 — 비워 둔다.

---

## 3. 목표 (Goals)

| # | 목표 | 근거 |
|---|---|---|
| G1 | GA 정의를 **SO**로, UE 유사 구조·동일 작동으로 이식 | `[유저]` |
| G2 | 어빌리티 **생명주기**(`ActivateAbility` / `EndAbility`) | `[progress]` |
| G3 | **코스트**(GE)·**쿨다운**(Duration GE) 적용 | `[progress]` |
| G4 | ASC **슬롯 등록**(`GiveAbility`) + **입력 바인딩** | `[progress]` |
| G5 | 발동이 **GE 적용으로 대상 어트리뷰트 변경**(예: Health) | `[progress]` |
| G6 | **핵심만** 이식 — 범위는 진행하며 판단·번복 | `[유저]` |

---

## 4. 비목표 (하지 않을 것)

| 항목 | 사유 | 근거 |
|---|---|---|
| UE 전체 기능 복제 | 필요 없다 판단되는 부분은 뺀다 | `[유저]` |
| 복합 데미지 계산 로직 자체 | gameplay-effect의 Execution/AttributeBased로 표현 — GA는 그 GE를 발동만 | `[progress]` |

---

## 5. 성공 기준 (Definition of Done)

에디터·Play에서 확인 가능한 조건으로 판정한다(CLI 자동 테스트 없음). `[progress]` 판정 결과는 [progress](progress.md) 수용 기준.

- `GameplayAbility` 정의 + `ActivateAbility` / `EndAbility` 생명주기 동작
- 코스트(GE)·쿨다운(Duration GE)이 발동 시 적용됨
- ASC 슬롯 등록 + 입력 바인딩으로 발동됨
- 발동이 GE 적용으로 대상 어트리뷰트(예: Health)를 실제로 변경

> 확인 방식(유저 직접 + AI 테스트)은 [HARNESS.md](HARNESS.md) 작업 프로세스 참조.

---

## 6. 현재 구현 상태

G1~G5 구현됨 — 세부는 [progress](progress.md) 설계 개요·결정 기록. `[코드]`

---

## 7. 범위 (Scope)

- **포함:** GA 실행 계층, 코스트/쿨다운, 입력 바인딩, 데미지→GE 배선. `[progress]`
- **제외:** §4.
- 선행 계층 재사용: GE 적용은 ASC의 `ApplyGameplayEffectToSelf`/`ToTarget`/`MakeOutgoingSpec`, 코스트/쿨다운은 각각 Instant/Duration GE. `[progress]`

---

## 8. 미정 (진행하며 판단)

- "핵심만"의 구체 경계 — 유저가 판단. 남은 UE 대비 항목은 TODO-BOARD '뒤로 미룸 > GA — UE 대비 미구현'.
- (확정됨) 입력 바인딩 방식 → decisions D11. GameplayTag 연동 → D7·D10.

---

## 9. 참고 (References)

- 진행 상태·다음 작업 — [progress.md](progress.md)
- 작업 방향·프로세스 — [HARNESS.md](HARNESS.md)
- 아키텍처 — [gameplay-ability.md](../../../../project/architecture/ability-system/gameplay-ability.md)
- 코드 — `Assets/Scripts/Core/AbilitySystem/Abilities/`
