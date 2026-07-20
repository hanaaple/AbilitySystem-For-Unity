# NOW — 지금 이어서 할 일 (단일 재개 지점)

> **"하던거 하자" = 이 파일 하나만 읽고 곧장 `## ▶ 지금 할 일`부터 실행한다.**
> feature-list·progress·worklog·코드를 **미리 훑지 않는다** — 필요할 때만 링크로 연다.
> 세션 종료 시 이 파일을 현재 상태로 갱신한다(§3.3). 그래야 다음 세션이 이것만 읽고 재개한다.
>
> 최종 갱신: 2026-07-20 (KST)

---

## ▶ 지금 할 일

**⏸ 유저 지시로 일시 중단(2026-07-19).** 재개 시 무엇부터 할지는 **유저가 정한다** — 에이전트가 임의로 다음 단계를 고르지 않는다.
진행: [feature/ability-system/gameplay-effect/progress.md](feature/ability-system/gameplay-effect/progress.md) §다음 작업 · [TODO-BOARD](TODO-BOARD.md).

**직전 세션 (2026-07-19) — 하네스 문서 정비만. 게임 코드·에셋 변경 없음.**
HARNESS 전수 점검 후 8건 수정(§2.1 NOW.md 성격 정의 신설 / §3.1↔§7.2 충돌 해소 / §4.2 전이도 / §9에 NOW 미갱신 금지 / §3.3 종료 절차에 TODO-BOARD 갱신 / 별도 progress 템플릿 파일 삭제 / §7.1 유저 직접 작성 존중 / §3.4 날짜 규정을 Claude Code 기준으로) + 회고 도출 2건(모르는 날짜 지어내기 금지 / 문서 삭제·이동 시 참조 같은 턴에 수정). **타임스탬프는 시:분 없이 `YYYY-MM-DD` 날짜만**으로 바뀌었다(유저 결정, 전 문서 적용). 상세·미처리분 → [TODO-BOARD](TODO-BOARD.md). **아래 GE 관련 상태는 그대로 유효하다.**

**GE 세션 완료분 (2026-07-19, 이전)**
1. **ScalableFloat 루프 검증 통과** — `MakeEffectContext`/`MakeOutgoingSpec`/`GameplayEffectContext` 재구성 등 미검증 배관이 이 경로로 한 번에 검증됨.
2. **Execution을 SO → 순수 클래스로 전환** (→decisions **D7**). `GameplayEffectExecutionAsset` 폐기. GE는 AQN(타입 이름)만 저장(`executionTypeNames`, `SubclassSelector`), `GameplayEffectSpec`이 생성 시 resolve. 직렬화 형태(AQN vs `[SerializeReference]`)는 **D8에 미결로 기록**.
3. **Params / Output 분리** — `GameplayEffectExecutionParameters`(입력만) + `GameplayEffectExecutionOutput`(출력 컨테이너, `AddOutputModifier`) + `GameplayModifierEvaluatedData`(결과 1건 — 기존 `GameplayEffectExecutionOutput` struct를 rename). 시그니처 `Execute(parameters, output)`. UE 대응 구조.
4. 에디터 — GE 드로어에 `Executions` 노출, Modifiers 리스트·각 요소 foldout + 개수 표시.
5. **문서·규약 정비** — architecture `gameplay-effect.md`에 ASC GE 로직 전체(적용 분기 / Execute 파이프라인 / persistent 경로 / Tick) 근거와 함께 작성. HARNESS에 §3.5 대화 규율·§3.3.1~2 세션 종료 회고 신설, `D#`은 유저 확정 결정에만 부여·타임스탬프는 로컬 셸 확인 규정 추가. CLAUDE.md의 HARNESS 읽기 트리거 확장. **D6 폐기·결번 처리**(에이전트가 승인 없이 쓴 캡처 미도입 결정 — 방향은 도입으로 확정).

**멈춘 지점 — AttributeCapture 도입** (유저가 "구현하자" 직후 중단 지시)
**방향은 도입으로 확정.** 반대 결정이던 D6은 에이전트가 승인 없이 쓴 것이라 **폐기·결번 처리**됨(2026-07-19). 계획 초안 = 파일 9개 단위(캡처 정의 / 컨테이너 / `AttributeBasedMagnitude`에 snapshot / Spec이 컨테이너 소유·2단계 pass / `AttemptCalculateMagnitude` / ModifierSpec 후채움 / ASC target 캡처 + **spec 복사** / `GetCapturedValue` / 문서). **미확정:** AttributeBased 평가(5b)를 같이 갈지 — 캡처만 넣으면 Modifier 쪽 관측 변화가 0이라 동반을 추천했으나 유저 미확정.

**⚠️ 캡처/5b 착수 시 함께 처리:** `ApplyGameplayEffectSpecToTarget`이 같은 spec 인스턴스를 넘긴다(`AbilitySystemComponent.cs`). target 기반 evaluate가 들어가면 한 spec을 여러 대상에 적용 시 서로를 덮어씀 → UE처럼 apply 시 spec 복사 필요.

**처리 여부 미정 (유저 판단 대기)**
- `CombatAttributeSet.defense` 필드 — 에이전트가 **무단 추가**. 유지/철회 미정
- ASC의 "Modifier 없이 Execution만 있는 GE도 통과" 가드 — 에이전트가 **무단 수정**. 유지/철회 미정 (현재 `spec.Executions.Count == 0` 형태)
- `Assets/Data/GE_TestSpeedUp.asset`(Duration 5초 speed +5, **modifier 방식**) · `SpeedBoostExecution`(Instant 전용, speed **영구** +10) — **에셋 배선 안 됨.** Execution을 실제로 돌리려면 Instant GE의 `executionTypeNames`에 AQN을 넣어야 함
- ASC `testEffect` 필드 — 검증용 임시 배선. 커밋 전 제거 필요

**주의:** `coefficient` 기본값 1은 ReorderableList `onAddCallback`에서 세팅(Unity struct 직렬화가 초기화값을 안 먹음). 이 변경 **이전에 만든 기존 modifier**는 coefficient 0으로 남아 있으니 필요 시 수동으로 1로 (AttributeBased 전용).

---

## 브랜치·커밋 상태 (2026-07-20)

**브랜치명 변경: `Feature/item-system` → `Feature/GE-Execution`.** 내용물이 item이 아니라 GAS라 실체에 맞췄다.
업스트림은 아직 `origin/feature/item-system`을 가리킨다 — 원격 정리는 PR 단계에서 한다(유저 결정: 지금은 로컬만).

**커밋 3건 완료** (main 대비 총 11커밋):
1. `feat(gas)` — GE Execution 순수 클래스 전환, Execute 경로 통합, Context/Spec 팩토리, AttributeBasedMagnitude
2. `chore(assets)` — GE_Speed 추가, GE_EquipSpeedDown AttributeBased modifier, Player_AttributeInitData
3. `docs` — 하네스·아키텍처 문서 정비, item-system 문서 아카이브

**커밋 전 정리된 것**
- ASC `testEffect` 필드·Awake 임시 호출 — **제거 완료**
- `CombatAttributeSet.defense` — **제거 완료** (유저 결정: 에이전트 무단 추가분이라 철회)
- ASC "Modifier 없이 Execution만 있는 GE도 통과" 가드 — **유지 확정** (유저 결정)
- `.vsconfig`, `Assets/Lit.mat` — 미추적으로 남겨둠(이번 커밋 제외)

**⚠️ 미확인 항목**
- `GE_EquipSpeedDown`의 새 AttributeBased modifier가 `coefficient: 0`이다 — 이 값이면 AttributeBased 항이 결과에 기여하지 않는다. 의도인지 확인 필요(아래 §주의 참고).
- `Player_AttributeInitData`에서 `CombatAttributeSet` 항목이 빠졌다. 의도인지 확인 필요.

**⚠️ 미기록 항목**
- `feature/ability-system/HARNESS.md` §1 설계 목적(동등성 기준) — **유저가 말한 목적만 적혀 있고 채택 근거는 비어 있다.** 유저에게 근거를 확인해 채워야 한다(HARNESS §8.4).

---

### 🅿 파킹: item-system 무기 재설계 (유저 결정 대기)
item 문서 전면 아카이브·재시작 상태(2026-07-18). 새 시작점 = [item-system/README.md](feature/item-system/README.md). 무기 구조는 **유저 주도 결정 대기 — 에이전트 선설계 금지.** 기존 공격 경로(`WeaponAttackComponent` 등) 워킹 트리에 잔존. `AbilitySystemModuleContext` 삭제로 StatModifier(이속 −5) 미작동.
