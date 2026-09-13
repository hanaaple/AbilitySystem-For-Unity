# gameplay-ability — 게임플레이 어빌리티 (GA·스킬 실행)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-09-13 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [aggregator](../aggregator/progress.md).
> **구현 방향·작업 프로세스는 [HARNESS.md](HARNESS.md)** · **요구사항 정의서(기획서)는 [requirements.md](requirements.md)** — 요구사항(SO·UE 유사·동작 동일·핵심만·판단 유동)과 진행 방식(이해 선행·구현 주체 유동·테스트)을 고정한다.
> ⚠ 스켈레톤 초안(GA 스크립트 4개 + `ASC.GiveAbility` + `GA_New.asset`)을 유저 `stash@{0}`에서 **작업 트리로 복원함**(2026-09-13, staged). stash 자체는 아직 유지(drop은 유저 확인 후).

## 목표
GAS의 `GameplayAbility`를 이 프로젝트에 **SO 기반·UE 유사 구조·동작 동일**로 이식한다. 스킬·액션의 실행 단위 — 생명주기(`ActivateAbility`/`EndAbility`), 코스트(GE)·쿨다운(Duration GE), ASC 슬롯 등록, 입력 바인딩. 최종적으로 데미지 적용이 GA→GE로 배선돼 실제 전투 수치가 흐른다. **전부 이식하지 않고 핵심만** 가져오며, 범위는 진행하며 판단·번복한다(→ [HARNESS.md](HARNESS.md) 요구사항).

## 수용 기준 (Definition of Done)
- [ ] `GameplayAbility` 정의 + `ActivateAbility`/`EndAbility` 생명주기
- [ ] 코스트(GE)·쿨다운(Duration GE) 적용
- [ ] ASC 슬롯 등록 + 입력 바인딩
- [ ] 어빌리티 발동이 GE 적용으로 대상 어트리뷰트(예: Health)를 변경

## 범위
### 포함
- GA 실행 계층, 코스트/쿨다운, 입력 바인딩, 데미지→GE 배선
### 제외 (현 시점 — 이후 확장 가능)
- GameplayTag(후순위)
- 복합 데미지 계산 자체는 [gameplay-effect](../gameplay-effect/progress.md)의 Execution/AttributeBased(구현됨)로 표현 — GA는 그 GE를 발동만 한다

## 설계 개요
- 아키텍처(계획): [architecture/ability-system/gameplay-ability.md](../../../../project/architecture/ability-system/gameplay-ability.md)
- 코드(현 골격, `Assets/Scripts/Core/AbilitySystem/Abilities/`):
  - `GameplayAbilityAsset` — 빈 SO(필드 없음). UE UGameplayAbility 대응 정의체가 될 자리.
  - `GameplayAbilitySpec` — 런타임 레코드(Ability/Handle/Level/Owner/IsActive)+생성자. UE FGameplayAbilitySpec 대응.
  - `GameplayAbilitySpecHandle` — 완성된 struct(id+owner, IEquatable). UE FGameplayAbilitySpecHandle 대응.
  - `GameplayAbilitySpecContainer` — `List<GameplayAbilitySpec> _specs` 필드만(로직 미구현).
  - `ASC.GiveAbility(ability, level)` — 스텁(`Invalid` 반환 + TODO).
- 선행 계층 활용: GE 적용은 ASC `ApplyGameplayEffectToSelf`/`ApplyGameplayEffectToTarget`/`MakeOutgoingSpec`(구현됨)로, 코스트/쿨다운은 각각 Instant/Duration GE로 표현.

## 세부 TODO (구현 체크리스트)
- [ ] 1. `GameplayAbility` 정의 + 생명주기
- [ ] 2. 코스트·쿨다운(GE) 배선
- [ ] 3. ASC 슬롯·입력 바인딩
- [ ] 4. 데미지 GE 배선

## 결정 기록
미착수 — 착수 시 `decisions.md` 생성.

## 작업 로그
미착수 — 착수 시 `worklog.md` 생성.

## 블로커
- 없음. 선행 수치/이펙트 계층 준비됨(attribute·gameplay-effect·aggregator 임시완료). 유저가 GA 착수를 지시(2026-09-13)해 이전 "데모 씬 확정 전 폴더 신설 금지" 제약은 해제됨.

## 다음 작업
1. **유저 이해 단계 우선** — GA 구현 전, 유저가 GAS GameplayAbility를 이해하는 것이 선행(→ [HARNESS.md](HARNESS.md) 작업 프로세스). AI 분석·소스 확인·리서치로 진행하며, 구현은 유저 신호가 있을 때.
2. (구현 착수 시) 복원된 골격 위에서 `GameplayAbilityAsset` 정의 + `ActivateAbility`/`EndAbility` 생명주기, 코스트/쿨다운 없는 최소 GA가 ASC에서 발동되는 수직 슬라이스부터. 범위는 진행하며 판단.
