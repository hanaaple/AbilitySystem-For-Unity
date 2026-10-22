# gameplay-effect — 검증 수단

> 진행 문서: [progress.md](progress.md). 최종 갱신: 2026-10-23 (KST)
> 실행 방법(메뉴 실행·결과 줄 읽기·PlayMode 붙이기)은 → [test-harness.md](../../../test-harness.md).

GE 동작은 아래 자동 검사로 확인한다. 메뉴는 모두 `Tools ▸ Ability System`, 결과 줄은 `passed N / failed N`.

## 에디터 셀프체크

| 셀프체크 | 파일 (`Assets/Scripts/Test/Editor/`) | GE 쪽에서 보는 것 |
|---|---|---|
| Aggregator | `AggregatorSelfCheck.cs` | mod 집계·연산 6종, 라이브 재평가, aggregator 없는 어트리뷰트, 값 변경 이벤트·원인, AttributeSet 훅 |
| GE Component | `GameplayEffectComponentSelfCheck.cs` | Component 호출 순서·inhibit, 부여 태그 개수, Target Tag Requirements, 질의·제거 API, Remove Other, 스택 |
| Calculation Modifier | `CalculationModifierSelfCheck.cs` | Execution Calculation Modifier 편집기 후보·기본값·저장, 런타임 적용 (원본 에셋 `Assets/_Test/GE_Speed.asset`) |
| Modifier Tag | `ModifierTagSelfCheck.cs` | Modifier Source/Target 태그 판정, 주기 실행 |

## PlayMode 테스트

`Assets/Scripts/Test/`의 컴포넌트를 Play 중 GameObject에 붙인다.

| 테스트 | 보는 것 |
|---|---|
| `AggregatorPlayModeTest` | 초기 스탯 GE, Infinite 버프, snapshot/라이브 캡처, Instant 회복 (에셋은 `Assets/_Test`, 셋업 메뉴 `Setup PlayMode Test`) |
| `TagRequirementsPlayModeTest` | Ongoing·Removal 태그 조건으로 시간이 걸린 켜고 끄기·제거 |
| `AbilitySystemLifecyclePlayModeTest` | ASC 복제·파괴, 라이브 캡처 상대 ASC 파괴 |

## 자동 검사로 안 보는 것

- GE 인스펙터 화면(밝은 스킨·좁은 창) — 캡처로만 확인해 왔다.
- Execution 출력 경로의 값 변경 원인·훅 호출 — Modifier 경로와 같은 `InternalExecuteMod`를 타지만 전용 검사는 없다.
