# NOW — 단일 재개 지점

> 휘발성 재개 메모. 진실은 코드 + progress/decisions. 세션 종료 시 통째 재작성.
> 최종 갱신: 2026-08-26 (KST)

## ✅ 방금 완료 — Aggregator 검증 통과 (Edit 셀프체크 + PlayMode 실제 Asset)

**feature: `ability-system/aggregator`** (branch `Feature/GE-Execution`). 두 층으로 검증:
1. **Edit 셀프체크** — `Tools ▸ Ability System ▸ Run Aggregator Self-Check` → **`passed 44 / failed 0`**. 5층(A 단위 / B ASC 라우팅 / C capture 스냅샷 / D 에셋 다수 / E dirty 재평가 = D12).
2. **PlayMode 실제 Asset** — `Tools ▸ Ability System ▸ Setup PlayMode Test`가 GE `.asset` 5개 + AttributeDefinitionAsset 생성·씬 배선 → Play → **`[PlayMode Self-Check] passed 14 / failed 0`**, 예외 0. 실제 ASC.Awake 초기화 → Apply 파이프라인으로 라이브 재평가(Health→Speed 추종)·snapshot 고정·Instant Base 영구 확인. **수용 기준 4개 중 3개 체크(자기참조 루프만 미검증).**

산출물: `Assets/Scripts/Character/Testing/AggregatorPlayModeTest.cs`(+Editor/Setup) + `Assets/_PlayModeTest/` 에셋 5개 + Test Scene 배선. **전부 미커밋.** 상세 → [aggregator/progress.md `## 다음 작업`](feature/ability-system/aggregator/progress.md).

## ▶ 다음 할 일 — Phase 3 캡슐화 조이기 (유저 승인 대기)

동작 검증 끝났으니 이제 API 가시성 조이기 가능(순서 섞으면 FAIL 원인 구분 불가라 뒤로 미뤄뒀던 것):
- `AttributeAggregator` 변경 메서드(`AddAggregatorMod`/`Remove`/`SetBaseValue`/`AddDependent`/`RemoveDependent`/`UpdateAggregatorMod`/`OnDirty`) → `internal`, `ActiveGameplayEffectsContainer` → `internal class`.
- 이러면 셀프체크 툴의 Layer B/C(aggregator 직접 찌르기)가 깨짐 → **파이프라인/리플렉션으로 재작성**(D/E는 무영향).
- **유저 승인 후 착수.** 승인 나면 Unity MCP 다시 ON 필요(현재 OFF).

그 밖 잔여: D14(base 진실 = AttributeData, aggregator 지연 생성) — progress `## 다음 작업` 참조. 미러 검증 층(선택).

## ⚙ 선행조건 / 환경
- **Unity MCP:** 현재 OFF(테스트 종료 후 정상). dev-tools.md 방침. 재검증·Phase 3 재작성 시 `.claude/settings.local.json` `disabledMcpjsonServers: []`로 ON. uvx 0.11.2.
- 검증 중 콘솔의 `get_tool_states Unknown command` 에러는 MCP 클라 폴링 노이즈(이 Unity 패키지 빌드 미지원) — 결과 무관.

## 📌 검증 후 다음 (동작 확정된 뒤에만)
- **Phase 3 — 캡슐화 조이기:** `AttributeAggregator` 변경 메서드(`AddAggregatorMod`/`Remove`/`SetBaseValue`/`AddDependent`/`RemoveDependent`/`UpdateAggregatorMod`/`OnDirty`) → `internal`, `ActiveGameplayEffectsContainer` → `internal class`. 그러면 툴 Layer B/C(aggregator 직접 찌르기)가 깨짐 → 파이프라인/리플렉션으로 재작성(D/E는 무영향). **동작 검증(위) 끝난 뒤에** — 순서 섞으면 FAIL 원인 구분 불가.
- **미러 검증 층(선택):** 툴은 `Evaluate()` 직접 읽어 AttributeData 미러 갱신은 안 본다. 미러까지 보려면 층 추가(현재 `OnAttributeAggregatorDirty` 조건은 유저가 이미 바로잡음 — 미러도 맞을 것).
- D14(base 진실 = AttributeData, aggregator 지연 생성) 잔여는 progress `## 다음 작업` 참조.

## 산출물 (이 세션)
- `Core/AbilitySystem/Aggregator/Editor/AggregatorSelfCheck.cs` — 5층 셀프체크(A 단위 / B ASC 라우팅 / C capture 스냅샷 / D 에셋 다수 케이스 / E 런타임 dirty 재평가). 리플렉션으로 `GameplayEffectAsset`·`GameplayModifier`·`AttributeBasedMagnitude` 조립. IDE 진단 clean.
- 캡슐화 리뷰 완료(위 Phase 3) — **API 가시성 변경은 미실행, 검증 후 유저 승인 대기.**
