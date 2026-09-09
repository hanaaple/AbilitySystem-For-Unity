# AbilitySystem-For-Unity

UE5의 **Gameplay Ability System(GAS)**을 참고해, 이 프로젝트에 필요한 축만 골라 **Unity에 직접 재구현**한 Ability System. 라이브러리 이식이 아니라 개념의 선택적 재구현이며, "어느 부분을 왜 취하고 왜 뺐는지"의 판단을 문서로 남기는 **포트폴리오 프로젝트**다.

## 구현 현황

- **Attribute** — 타입드 `AttributeSet` + 런타임 핸들(FieldInfo 캐싱, string 탐색 없음), SO 기반 초기화
- **GameplayEffect** — SO 정의 / 런타임 Spec 분리, Modifier 6종 공식, Instant/Duration/Infinite·주기 실행, Execution·AttributeBased 캡처
- **Aggregator** — 어트리뷰트별 집계로 CurrentValue 산출 + 캡처 대상 변경 시 라이브 재평가(dirty/dependents)
- **GameplayAbility** — 계획 (수치 계층 위에 실행 계층을 올린다)

설계 문서(개념·방향 중심, **에이전트·개발자·외부 리뷰어 공용**) → [dev-docs/project/architecture/overview.md](dev-docs/project/architecture/overview.md)

## 데모 게임 (컨텍스트)

GAS는 **Spectral-Raid**(제한된 시야·사운드 기반의 탈출 세션 탑다운 액션) 데모에서 주요 기능이 한 씬에 엮여 작동하는 것으로 보인다. "완성된 게임"이 아니라 **GAS가 온전히 작동하는 데모(수직 슬라이스)**로 범위를 좁혔다. 전체 게임 비전(빙의·아이템·전투·씬 등)은 보관됨 → [dev-docs/_parked/project/design.md](dev-docs/_parked/project/design.md)

## 패키지

- **Input System** v1.19.0 — New Input System (레거시 미사용)
- **Cinemachine** v2.10.7
- **URP** v17.3.0
- **UniTask** (Cysharp) — async/await
- **R3** (Cysharp) — 반응형 확장
- **AI Navigation** v2.0.11

## 문서

작업용 문서는 [dev-docs/](dev-docs/)에 모여 있다. (`agent/` 에이전트 작업용, `project/` 프로젝트 자료, `_parked/` 비활성 보관)

- **아키텍처** — [dev-docs/project/architecture/overview.md](dev-docs/project/architecture/overview.md)
  - 시스템 구조·설계를 **개념·방향 중심**으로 정리한 문서. **에이전트·개발자·외부 리뷰어 공용** — 그 도메인을 알지만 이 프로젝트를 처음 보는 사람이 훑어 이해하는 수준을 지향한다. 작성 규약은 [architecture/HARNESS.md](dev-docs/project/architecture/HARNESS.md).
- GAS 데모 방향 — [dev-docs/project/design.md](dev-docs/project/design.md)
- 개발 도구(Unity MCP) 설정 — [dev-docs/project/dev-tools.md](dev-docs/project/dev-tools.md)
- 코드 컨벤션 — [dev-docs/project/CODE_CONVENTION.md](dev-docs/project/CODE_CONVENTION.md)
- 작업 규약 (Claude Code 가이드) — [CLAUDE.md](CLAUDE.md)

## 프로젝트 위키

기술 설계와 **선택 이유**를 한 페이지로 정리한 포트폴리오용 위키 → [dev-docs/project/wiki/index.html](dev-docs/project/wiki/index.html) (브라우저로 바로 열기)

- ⚠️ GAS 중심 재편 이전 스냅샷이라 최신 코드/문서와 어긋날 수 있음(수동 갱신).
