# aggregator — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-08-07 — feature 신설 + 스캐폴딩 (구현 착수 준비)
- **경위:** GE_BoxSelfEffect의 non-snapshot 모디파이어가 초기값으로만 적용되는 걸 계기로, "라이브 재평가"의 UE 메커니즘을 원문으로 확인(→ `RegisterLinkedAggregatorCallback`의 `bSnapshot==false` 시 `AddDependent`, `FAggregator::OnDirty.Broadcast`, `ASC->OnMagnitudeDependencyChange`, snapshot은 `TakeSnapshotOf`). 유저 방향 확정: **라이브 재평가를 제대로**, 태그는 미정(seam만).
- **feature 배치:** attribute(계산)·gameplay-effect(모디파이어/캡처)를 가로지르는 반응성 계층이라 둘 중 하나에 넣지 않고 **ability-system 그룹의 새 하위 feature `aggregator`**로 분리(의존: attribute, gameplay-effect). feature-list 등록(🔧 IN-PROGRESS).
- **결정(→D1/D2):** 반응성 책임만 분리(R1/R2는 ASC 유지). 구현은 경량(옵저버+재평가+재진입 억제), 범위 cross-attr·self.
- **스캐폴딩 생성:** progress·decisions·worklog(본 파일) / architecture 스켈레톤 `architecture/ability-system/aggregator.md` / 앵커 스텁 `Assets/Scripts/Core/AbilitySystem/Aggregator/AttributeAggregator.cs`(빈 셸). 구현 로직은 아직 없음.
- **다음:** progress `## 다음 작업` — ASC에 어트리뷰트 변경 이벤트부터.
