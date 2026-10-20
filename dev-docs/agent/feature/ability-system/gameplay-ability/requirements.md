# GameplayAbility — 요구사항 정의서 (기획서)

- 문서 상태: **초안 (Draft)** — 이해 단계라 상당 부분이 의도적으로 미정
- 최종 갱신: 2026-09-13 (KST)
- 관련: [progress](progress.md) · [작업 방향·프로세스(HARNESS)](HARNESS.md) · [아키텍처(계획)](../../../../project/architecture/ability-system/gameplay-ability.md)

> 근거 태그: `[유저]` 유저 명시 · `[progress]` 기존 문서 · `[코드]` 작업 트리 사실. 미정은 §8. (문서화 원칙은 [HARNESS.md](HARNESS.md) 에이전트 운영 규율 4)

---

## 1. 개요 (Overview)

Unreal GAS의 **`GameplayAbility`**(스킬·액션의 실행 단위)를 이 Unity 프로젝트로 이식한다. 정의체는 **ScriptableObject 기반**, **UE와 유사한 구조 + 동일한 작동**이 목표다. 전부 복제하지 않고 **필요한 핵심만** 가져오며, 범위는 **진행하며 판단하고 번복될 수 있다.** `[유저]`

이미 선 수치/이펙트 계층(attribute · gameplay-effect · aggregator) 위에 **실행 계층**을 올려, 어빌리티가 코스트·쿨다운·효과를 GameplayEffect로 표현해 발동하게 한다. `[progress]`

---

## 2. 배경 (Background)

- 수치·효과·반응성 계층은 임시완료 상태이고, 그 위에서 **스킬을 실제로 발동하는 계층**이 아직 없다 — GA가 그 배선이다. `[progress]`
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
| GameplayTag 연동 | 후순위 | `[progress]` |
| 복합 데미지 계산 로직 자체 | gameplay-effect의 Execution/AttributeBased로 표현 — GA는 그 GE를 발동만 | `[progress]` |

---

## 5. 성공 기준 (Definition of Done)

에디터에서 눈으로 확인 가능한 조건으로 판정한다(CLI 자동 테스트 없음). `[progress]`

- [ ] `GameplayAbility` 정의 + `ActivateAbility` / `EndAbility` 생명주기 동작
- [ ] 코스트(GE)·쿨다운(Duration GE)이 발동 시 적용됨
- [ ] ASC 슬롯 등록 + 입력 바인딩으로 발동됨
- [ ] 발동이 GE 적용으로 대상 어트리뷰트(예: Health)를 실제로 변경

> 확인 방식(유저 직접 + AI 테스트)은 [HARNESS.md](HARNESS.md) 작업 프로세스 참조.

---

## 6. 현재 구현 상태 (착수 기준점)

부여·해제까지 구현됐다(2026-10-20, TODO-BOARD 2-1). 발동·코스트·쿨다운은 아직 없다. 세부는 [progress](progress.md) 설계 개요. `[코드]`

| 요소 | 상태 |
|---|---|
| `GameplayAbilityAsset` | sealed SO(정의). 필드 `abilityTemplate`(`[SerializeReference]` 런타임 클래스 원형) — decisions D1·D3 |
| `GameplayAbility` | `[Serializable]` abstract 일반 C# 클래스(런타임 부분). 액터 정보·Spec 핸들, 훅 `OnGiveAbility`·`OnRemoveAbility`·`OnAvatarSet`. 발동 함수 없음 |
| `Test.TestGameplayAbility` | 테스트용 런타임 하위 클래스(`[Serializable]`, 필드 `_testValue`·`_testEffect`, `Assets/Scripts/Test/`) |
| `GameplayAbilitySpec` | 생성자가 핸들 발급. Handle·Ability·Level·InputID·SourceObject·ActiveCount·PendingRemove·런타임 인스턴스 |
| `GameplayAbilitySpecHandle` | struct(id만, IEquatable), `GenerateNewHandle()` |
| `GameplayAbilitySpecContainer` | `Items`·`Owner`·`RegisterWithOwner`(UE 구조, 네트워크 직렬화만 뺌) |
| ASC 어빌리티 API | `GiveAbility`·`ClearAbility`·`ClearAllAbilities`·조회·스코프 락·`InitAbilityActorInfo`. 발동(`TryActivateAbility`) 없음 |

---

## 7. 범위 (Scope)

- **포함:** GA 실행 계층, 코스트/쿨다운, 입력 바인딩, 데미지→GE 배선. `[progress]`
- **제외:** §4.
- 선행 계층 재사용: GE 적용은 ASC의 `ApplyGameplayEffectToSelf`/`ToTarget`/`MakeOutgoingSpec`, 코스트/쿨다운은 각각 Instant/Duration GE. `[progress]`

---

## 8. 미정 (진행하며 판단)

> "범위는 진행하며 판단, 번복 가능"(유저)이므로 착수 전 확정하지 않는다. 아래는 **아직 정보가 없는 지점**만 적는다 — 채우지 않는다.

- 입력 바인딩의 구체 방식(무엇에 어떻게 연결) — 미정.
- GameplayTag 후순위의 실제 도입 시점 — 미정.
- "핵심만"의 구체 경계(무엇을 넣고 뺄지) — 이해·분석 진행하며 유저가 판단.

---

## 9. 참고 (References)

- 진행 상태·다음 작업 — [progress.md](progress.md)
- 작업 방향·프로세스 — [HARNESS.md](HARNESS.md)
- 아키텍처(계획) — [gameplay-ability.md](../../../../project/architecture/ability-system/gameplay-ability.md)
- 코드 골격 — `Assets/Scripts/Core/AbilitySystem/Abilities/`
