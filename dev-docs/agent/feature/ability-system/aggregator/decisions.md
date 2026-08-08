# aggregator — 결정 기록

> 트레이드오프가 있었던 설계 결정의 상세. 품질 기준은 일반 [HARNESS.md](../../../HARNESS.md) §8.
> **ID(`D#`)는 feature 내 고유·불변**이며 progress 인덱스·worklog가 이 코드로 이 표를 가리킨다.
> UE `FAggregator` 원문 확인 근거는 [architecture/ability-system/aggregator.md](../../../../project/architecture/ability-system/aggregator.md) 참조.

| ID | 날짜 | 결정 | 대안 | 채택 사유 |
|---|---|---|---|---|
| D1 | 2026-08-07 | 라이브 재평가를 위해 **UE aggregator의 반응성 책임(R3: 의존+dirty+재평가)만 별도 컴포넌트로 분리**한다. R1(모디파이어 채널 소유)·R2(CurrentValue 계산)는 지금처럼 ASC에 둔다. | ① 현행 유지(반응성 없음) ② UE per-attribute `FAggregator` 전면 이식(R1/R2 포함) | 라이브 재평가는 "누가 무엇에 의존하는지 + 값 바뀌면 재평가"를 소유하는 곳이 없으면 **구조적으로 불가**하다 — 현재 magnitude는 apply 시점에 `EvaluatedMagnitude`로 고정돼 non-snapshot이 무력하다(코드 확인: `GameplayEffectSpec.CalculateModifierMagnitudes`가 생성·apply 2회만 호출). 그래서 반응성은 반드시 분리. 반면 R1/R2는 ASC가 이미 잘 대신하고 있어 전면 이식은 이득 대비 과함(과설계 회피 — CLAUDE.md). **(UE 대응)** `FAggregator`의 `Dependents`+`OnDirty`+`OnMagnitudeDependencyChange` 부분만 축소 재현. |
| D2 | 2026-08-07 | 구현은 **경량 형태**로 시작: ① 어트리뷰트 변경 이벤트(옵저버) ② 핸들러가 magnitude 재평가(라이브 캡처) ③ 재진입 억제(가드). 범위는 **cross-attribute·self-effect 라이브**. cross-actor 라이브·자기참조 fixed-point·태그 자격은 **후속**(태그는 seam만). | ① 처음부터 의존 레지스트리 + 본격 dirty 그래프 ② per-attribute aggregator 객체 | 유저 확인: "옵저빙만 추가로 어느정도 확장"이 목표. 현실적 버프(cross-attr·self)는 옵저버+재평가+가드로 정확히 커버되고, 이 규모에선 레지스트리 없이 변경 시 전량 재평가+가드로도 정합. 못 커버하는 건 cross-actor(cross-ASC 구독)·자기순환뿐이라 그 수요가 실제로 생길 때 승격. 태그(R5)는 미정이라 **얹을 seam**(자격 판정 함수 경계 + dirty 트리거 일반화)만 남기고 구현은 보류. |
