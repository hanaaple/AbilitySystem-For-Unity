# gameplay-effect — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-07-05 — 문서화
- GAS 문서화 중 **⚠️ `GameplayEffectType` Instant/Duration enum 주석(="미구현") vs ASC 실행 경로(존재) 불일치** 발견 → 블로커·후속 TODO로 등록(유저 확인 대기). (D4 관련)

### 2026-06-13 — PR #3 (Ability System - GameplayEffect)
- GE 계층 구현: `GameplayEffect`(SO)·`GameplayModifier`, `GameplayEffectSpec`(resolve·Magnitude 1회 캐싱), `ActiveGameplayEffect`(+`Handle`), `GameplayEffectContext`(+`Handle`) (→D1·D2)
- `AbilitySystemComponent` 대폭 확장(+392줄): `ApplyGameplayEffectToSelf`/`RemoveActiveGameplayEffect`, Modifier 6종 CurrentValue 누산, Duration 만료·Period 틱, 읽기 캐시 (→D3·D4)
- `GameplayEffectExecution`(+Output/Parameters) — 배관·추상 SO만, concrete 0(껍데기)
- 초기 `AttributeEffect` 스텁 제거
