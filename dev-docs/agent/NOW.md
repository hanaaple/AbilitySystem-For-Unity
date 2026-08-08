# NOW — 지금 이어서 할 일 (단일 재개 지점)

> **"하던거 하자" = 이 파일 하나만 읽고 곧장 `## ▶ 지금 할 일`부터 실행한다.**
> feature-list·progress·worklog·코드를 **미리 훑지 않는다** — 필요할 때만 링크로 연다.
> 세션 종료 시 이 파일을 현재 상태로 갱신한다(§3.3). 그래야 다음 세션이 이것만 읽고 재개한다.
>
> 최종 갱신: 2026-08-07 (KST)

---

## ▶ 지금 할 일

### ▶▶ 다음 세션 시작점 — **aggregator (라이브 재평가) 구현 착수**

**스캐폴딩 완료(2026-08-07):** feature 등록(`aggregator`, ability-system 그룹) · progress/decisions/worklog · architecture 스켈레톤 · 앵커 스텁 `Assets/Scripts/Core/AbilitySystem/Aggregator/AttributeAggregator.cs`(빈 셸). **구현 로직은 아직 없음.**

**다음 액션 = progress `## 다음 작업` 1번:** `AbilitySystemComponent`에 **어트리뷰트 변경 이벤트** 신설 — `SetBaseAttributeValue`/`UpdateAttributeCurrentValue`가 값이 실제로 바뀔 때 `(handle, old, new)`로 발화. 이어서 ② non-snapshot 캡처 의존 등록/해지 → ③ 변경 핸들러 재평가(`CalculateModifierMagnitudes` 라이브) → ④ 재진입 가드.

- **방향(결정 D1/D2):** UE `FAggregator`의 **반응성 책임만** 분리(R1/R2는 ASC 유지). 경량 = 옵저버 + 재평가 + 재진입 억제. 범위 = cross-attribute·self-effect 라이브. **후속(제외):** cross-actor 라이브(cross-ASC 구독)·자기참조 fixed-point·태그 자격(R5, seam만).
- 문서: [feature/ability-system/aggregator/progress.md](feature/ability-system/aggregator/progress.md) · [decisions](feature/ability-system/aggregator/decisions.md) · [architecture](../project/architecture/ability-system/aggregator.md).
- 계기: GE_BoxSelfEffect의 non-snapshot 모디파이어가 초기값 고정으로만 적용 → 원인=magnitude가 apply 시점 고정(재평가 트리거 없음). UE 원문으로 dirty/dependents 메커니즘 확인.

### (병행 대기) GE 캡처/Execution 유저 에디터 검증 — 아직 미완

5b evaluate(2026-08-04)까지 캡처 배관 섰고, 관측용 디버그 도구(ASC Inspector 창 · BoxRoom, 2026-08-05)도 만들어 둠. **유저가 Play 모드에서 캡처/snapshot 실측**하는 단계가 남음. 검증 항목 = [tests.md](feature/ability-system/gameplay-effect/tests.md).
⚠ BoxRoom 미검증(프리팹 GUID·GE 슬롯 3개·Room ASC 데이터) — 상세는 [gameplay-effect worklog](feature/ability-system/gameplay-effect/worklog.md) 2026-08-05.

> (그 밖의 대기 항목 — UE Execution 실행조건 문서 반영·에셋 폴더 관례·미커밋 커밋 정리 등 — 은 [TODO-BOARD](TODO-BOARD.md).)

---

## 브랜치·커밋 상태

**브랜치: `Feature/GE-Execution`** (업스트림은 아직 `origin/feature/item-system` — 원격 정리는 PR 단계, 유저 결정).
**미커밋 산출물 다수** — 2026-08-04 5b + 2026-08-05 디버그 도구 + 2026-08-07 에디터 New Script 툴·GE Executions Add·aggregator 스캐폴딩 전부 미커밋. 커밋 정리는 TODO-BOARD.

---

### 🅿 파킹: item-system 무기 재설계 (유저 결정 대기)
item 문서 전면 아카이브·재시작 상태(2026-07-18). 새 시작점 = [item-system/README.md](feature/item-system/README.md). 무기 구조는 **유저 주도 결정 대기 — 에이전트 선설계 금지.**
