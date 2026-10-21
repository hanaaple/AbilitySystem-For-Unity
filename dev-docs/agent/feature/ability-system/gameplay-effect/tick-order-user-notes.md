# gameplay-effect — Tick 순서: 사용자(개발자)가 알아야 할 것

- 최종 갱신: 2026-10-21 (KST)

> 유저(2026-10-21): "AbilitySystem 사용자(개발자)가 알아야되는건 따로 나중에 정리할거야" → "보완/사용자 두 문서로".
> 이 문서 = 사용자 문서로 나중에 정리할 원재료. 구현·보완할 것은 [tick-order-improvements.md](tick-order-improvements.md).
> 근거 표기: (코드) = 이 저장소 코드를 읽고 확인 · (측정) = 테스트로 실행해 확인 · (UE) = UE 5.6 원문 확인 · (추론) = 코드에서 추론, 실행 안 함.

## 배경

GE의 주기 실행·만료는 `AbilitySystemComponent.Update`(컨테이너 `Tick`)에서 처리된다. 같은 프레임 안에서 ASC.Update보다 먼저 도는 코드와 나중에 도는 코드가 보는 상태가 다를 수 있다(코드).

## 항목

### 1. 만료·주기 실행은 다음 Tick까지 늦을 수 있음

- 지속시간이 프레임 중간에 끝나거나 주기가 프레임 중간에 돌아와도, 실제 처리는 다음 ASC.Update — 최대 한 프레임 늦게 만료·실행된다(코드).
- 프레임이 길면(멈춤 뒤) 밀린 주기가 한 Tick에 몰아서 여러 번 실행된다(`while (PeriodTimer >= Period)`, 코드).
- 그 사이 ASC.Update보다 먼저 도는 코드는 이미 끝났어야 할 GE의 태그·수치를 본다.

### 2. 남은 시간은 Tick 사이엔 지난 프레임 값

- `GetTimeRemaining`·`GetCooldownTimeRemaining`을 ASC.Update보다 먼저 읽으면 지난 프레임 값이다(코드). HUD 쿨다운 게이지는 `LateUpdate`에서 읽으면 같은 프레임 값이다.
- 구현 쪽에서 고칠 예정(→ improvements 항목 3) — 고치면 이 항목은 지운다.

### 3. 주기 실행 콜백 안에서 같은 ASC의 GE 목록을 바꾸지 않기

- AttributeSet `PostGameplayEffectExecute`·값 변경 이벤트(`OnValueChange`)가 주기 실행에서 불렸을 때, 그 안에서 같은 ASC에 Duration/Infinite GE를 걸거나 활성 GE를 지우면 예외가 날 수 있다(추론).
- 구현 쪽에서 고칠 예정(→ improvements 항목 2) — 고치면 이 항목은 지운다.

### 4. 여러 ASC 사이의 Update 순서는 정해져 있지 않음

- 한 ASC의 주기 실행이 다른 ASC 값을 바꾸면, 읽는 쪽 주기 실행이 이번 프레임에 이미 돌았는지에 따라 한 프레임 차이가 난다(추론).
- 라이브 캡처(비-snapshot)로 연결된 지속형 수치는 즉시 반영돼 순서와 무관하다(코드 — `AttributeAggregator.BroadcastOnDirty`).

### 5. 같은 Tick에 여러 GE가 만료될 때 순서에 기대지 않기

- 만료는 활성 목록을 돈 순서대로 제거하고 제거마다 태그 갱신·`OnEffectRemoved`가 나간다(코드). 그 순서는 보장하지 않는다(C# `Dictionary` 열거 순서).
- "A가 끝난 뒤 B" 같은 연출·로직을 같은 프레임 만료에 걸지 않는다.

### 6. 어디서 GE를 걸어도 지속시간은 맞음

- 적용 시각을 기록해 적용 뒤 흐른 시간만 센다 — `Update`·`Start`·코루틴·`LateUpdate`·물리 콜백(`OnTriggerEnter`) 어디서 걸어도 지속시간이 같다(코드·측정 — PlayMode GA P-3).
- FixedUpdate 안의 `Time.time`은 고정 스텝 시각이다(Unity 문서 기억 — 원문 미확인). 그 경우 적용 시각이 프레임 시각보다 조금 앞서고, 그만큼은 실제 흐른 시간으로 센다(추론).

### 7. ASC 파괴 순서

- 씬 언로드처럼 여러 오브젝트가 함께 파괴되면 `OnDestroy` 순서가 정해져 있지 않다. Owner가 먼저 사라졌으면 활성 어빌리티 취소를 건너뛰고 해제만 한다(코드 — `AbilitySystemComponent.DestroyActiveState`, gameplay-ability D8).
- 취소 콜백에서 다른 오브젝트를 만지는 어빌리티는 그 오브젝트가 이미 파괴됐을 수 있다.

### 8. 시간 배율·일시정지를 따름

- GE 시간은 `Time.time` 기준이라 `Time.timeScale`을 따른다 — 일시정지(0)면 GE 시간도 멈춘다(코드). 실제 시간(unscaled)으로 도는 GE는 지금 없다.

### 9. 정확히 지속시간 경계에서의 만료는 보장 안 함

- 시각이 커질수록(오래 켠 게임) `지금 - StartWorldTime`의 float 오차가 커진다. 남은 시간은 적용 시각에서 매번 다시 계산해 오차가 쌓이지 않지만, 지속시간과 똑같은 시간에서 만료되는지는 보장하지 않는다(추론 — 경과를 빼 쌓던 중간 구현에서 셀프체크가 정확히 10초를 흘린 뒤 쿨다운이 남아 실패했고, 값은 안 찍었다).
- 주기 타이머는 Tick마다 경과를 더해 오차가 조금씩 쌓인다(코드).
