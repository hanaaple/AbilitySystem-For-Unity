# gameplay-effect — Tick 순서: 구현·보완할 것

- 최종 갱신: 2026-10-21 (KST)

> 유저(2026-10-21): "Tick의 순서가 영향을 미칠 부분을 정리해서 문서로 작성해둬. 구현이나 보완 가능한 건 할거고 AbilitySystem 사용자(개발자)가 알아야되는건 따로 나중에 정리할거야" → "보완/사용자 두 문서로".
> 이 문서 = 구현·보완할 것. 사용자(개발자)가 알아야 할 것은 [tick-order-user-notes.md](tick-order-user-notes.md).
> 근거 표기: (코드) = 이 저장소 코드를 읽고 확인 · (측정) = 테스트로 실행해 확인 · (UE) = UE 5.6 원문 확인 · (추론) = 코드에서 추론, 실행 안 함.

## Tick에서 일어나는 일

`AbilitySystemComponent.Update` → `ActiveGameplayEffectsContainer.Tick()` 한 곳에서 프레임마다 주기 GE 실행(한 Tick에 여러 번일 수 있음)·Duration GE 만료(이번 Tick 끝에 한꺼번에 제거)·남은 시간/주기 타이머 갱신을 한다(코드). 시간은 컨테이너 시계(`GetWorldTime()`, 기본 `Time.time`)로 재고, GE마다 적용 시각(`StartWorldTime`)을 기록해 적용 뒤 흐른 시간만 센다(2026-10-21, D27).

UE는 GE마다 타이머를 걸어(`TimerManager.SetTimer(…DurationHandle…)` `GameplayEffect.cpp:4230`, 만료 `CheckDuration` `:5053`, 주기 `ASC::ExecutePeriodicEffect` `AbilitySystemComponent.cpp:995`) 한 루프로 전체를 돌지 않는다(UE). 아래 항목 중 여럿은 "한 Tick에서 전체를 돈다"는 우리 구조에서 나온다.

## 항목

### 1. 적용된 프레임의 시간이 지속시간에서 깎임 — ✅ 해결(2026-10-21)

- 예전: 프레임 도중 ASC.Update보다 먼저 적용된 Duration GE를 그 프레임 ASC.Update가 `deltaTime` 전체만큼 줄여 지속시간이 최대 한 프레임 짧았다(측정 — 커밋 프레임 deltaTime 0.333 → 그 프레임 끝 남은 쿨다운 2.667).
- 지금: 적용 시각 기록 + 적용 뒤 흐른 시간만 셈. 남은 시간은 UE 공식 `Duration - (지금 - StartWorldTime)`(`GameplayEffect.h:1336`). 셀프체크 GA M-9·PlayMode GA P-3로 고정(D27).

### 2. Tick 도중 같은 ASC에 GE를 걸거나 지우면 예외 가능 — 할 일

- 상황: 주기 GE 실행 → AttributeSet `PostGameplayEffectExecute` 훅이나 어트리뷰트 값 변경 이벤트(`OnValueChange`) 구독자 → 그 안에서 **같은 ASC에** Duration/Infinite GE 적용 또는 다른 활성 GE 제거. 예: 도트 데미지로 체력이 낮아지면 이벤트에서 디버프를 건다.
- 지금: `Tick`이 `foreach (… in _activeGameplayEffects.Values)`로 도는 중에 사전이 바뀌어 `InvalidOperationException`이 날 수 있다(추론 — 실행 안 함). 만료 제거는 따로 모은 목록을 돌아 안전(코드).
- 방향: UE는 컨테이너 스코프 락 동안의 추가를 보류 목록(`PendingGameplayEffectHead` `GameplayEffect.h:1978`)에 넣는다(UE). 먼저 재현 테스트 → 사본 순회 또는 보류 목록.
- 추적: TODO-BOARD 'GE·Tag 남은 것 > GE 버그'.

### 3. 남은 시간 조회가 Tick 사이엔 지난 값 — 할 일

- 지금: `GetTimeRemaining`·`GetCooldownTimeRemaining`이 읽는 `RemainingDuration`은 Tick에서만 갱신돼, 같은 프레임에 ASC.Update보다 먼저 읽으면 지난 프레임 값이다(코드). 같은 프레임에 막 적용한 GE는 Tick 전이라도 전체 지속시간이 나오므로 맞다.
- 방향: UE처럼 조회 시점에 `Duration - (GetWorldTime() - StartWorldTime)`으로 계산(`GameplayEffect.h:1336` `GetTimeRemaining(WorldTime)`). `StartWorldTime`이 이미 있어 작은 변경.
- 추적: TODO-BOARD 'GE·Tag 남은 것 > GE 버그'.

### 4. 여러 ASC의 Tick 순서 고정 — 후보(필요가 보일 때만)

- 배경: ASC끼리 Update 순서는 Unity가 정한다. 한 ASC의 주기 실행이 다른 ASC 값을 바꾸면 읽는 쪽 주기 실행에 한 프레임 차이가 날 수 있다(추론). 지속형 라이브 캡처는 dirty 전파가 즉시라 무관(코드).
- 후보: 전역 매니저가 모든 ASC를 정해진 순서로 Tick. 구조 변경이라 실제 문제가 보일 때만.

### 5. 시간 배율 무시(unscaled) GE — 후보(필요가 생길 때)

- 배경: 기본 시계가 `Time.time`이라 `Time.timeScale`을 따른다(코드).
- 후보: 컨테이너 시계를 바꾸는 `SetWorldTimeSource`가 internal로 있다 — 공개하거나 GE별 시계 선택을 둘지는 필요가 생길 때.
