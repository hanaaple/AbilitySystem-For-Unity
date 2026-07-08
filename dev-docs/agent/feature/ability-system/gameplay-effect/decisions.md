# gameplay-effect — 결정 기록

> 트레이드오프가 있었던 설계 결정의 상세. 품질 기준은 일반 [HARNESS.md](../../../HARNESS.md) §8.
> **ID(`D#`)는 feature 내 고유·불변**이며 progress 인덱스·worklog가 이 코드로 이 표를 가리킨다.
> 이 시스템은 **UE GAS를 참고해 필요한 축만 직접 구현**한 것 — 각 행의 `(UE 대응)` 표기와 전체 채택/생략 범위는 [architecture overview](../../../../project/architecture/ability-system/overview.md) 참조.

| ID | 날짜 | 결정 | 대안 | 채택 사유 |
|---|---|---|---|---|
| D1 | 2026-06-13 | **모든 수치 변경을 GameplayEffect 단일 채널로** 통과시킨다 (버프·장비·데미지 등이 Attribute를 직접 쓰지 않음) | 각 출처가 Attribute를 직접 가감 | 수십 개 출처가 수치를 직접 조작하면 적용 순서 의존성·해제 시 잔류 값·이중 적용 버그가 생긴다. GE 한 경로로 모으면 적용/해제/집계가 한곳에서 관리돼 그 문제류가 구조적으로 사라진다. 트레이드오프: 단순한 1회 수치 변경도 GE 정의를 요구하는 오버헤드가 있으나, 수치 출처가 늘수록 이득이 커짐. **(UE 대응)** UE GAS의 핵심 원칙(모든 수치 변경은 GE 경유)을 그대로 채택 |
| D2 | 2026-06-13 | **SO(불변 정의) + 런타임 `GameplayEffectSpec`(가변 인스턴스) 분리.** Spec 생성 시 AttributeHandle resolve·Magnitude 계산을 1회 완료해 캐싱 | GE SO에 런타임 상태를 직접 저장 | 여러 캐릭터가 같은 GE 에셋을 공유하므로 SO에 상태를 두면 서로 간섭한다(= item INV-1 무상태 SO와 같은 원리). 정의/인스턴스 분리로 공유 안전. 추가로 resolve를 Spec 생성 1회로 캐싱해 런타임 재해석 비용 제거(실패 modifier는 경고 후 skip). **(UE 대응)** UE `UGameplayEffect`(정의) ↔ `FGameplayEffectSpec`(런타임 인스턴스) 분리를 채택 |
| D3 | 2026-06-13 | **CurrentValue는 Effect 변경(적용/해제/SetBase) 시점에만 재계산**해 캐시, 읽기는 캐시 반환 | 읽을 때마다 활성 GE를 순회해 계산 | CurrentValue 읽기는 매 프레임 다수 발생(UI·전투 판정) 하지만 변경은 드물다. 쓰기 시점 재계산이면 읽기 O(1)이라 읽기 빈도와 무관하게 비용 0. 트레이드오프: 재계산 트리거를 빠뜨리면 stale 값 위험 → 변경 API를 좁게 유지(SetBase/Apply/Remove) |
| D4 | 2026-06-13 | **Modifier 연산 6종 + BaseValue/CurrentValue 두 경로.** Instant·Periodic(period>0)=`ExecuteModifiers`로 Base 영구 변경(배열 순서 영향), Duration·Infinite persistent(period==0)=aggregator가 Current만 계산(Base 불변, 해제 시 회수) | 단일 경로(전부 Base 변경 or 전부 Current) | 영구 소모(데미지·힐=Base)와 일시 버프(장비=Current, 해제 시 원복)는 성격이 근본적으로 다르다. 두 경로로 나눠야 "장비 벗으면 정확히 원복"과 "데미지는 영구 반영"이 동시에 성립. 연산 6종(Add/Multiply additive·compound/Divide/Override)은 가산·배율 스택 규칙을 표현하기 위한 최소 집합. **(UE 대응)** UE의 GE Duration 타입(Instant/Duration/Infinite)과 Modifier Op를 6종으로 축소 채택 |
