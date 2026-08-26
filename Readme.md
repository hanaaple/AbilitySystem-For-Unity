# Spectral-Raid

**Spectral-Raid** — 제한된 시야와 소리 기반 정보로 위험을 판단해 탈출을 결정하는 5~8분 세션 기반 탑다운 전술 액션 게임.

- 장르: Extraction-lite 액션 + Roguelite 성장 (싱글플레이, 멀티플레이 확장 고려 구조)
- 핵심 차별점: 강하게 제한된 시야 + 사운드 중심 정보 인지
- 세션 구조: 탐색 → 전투/레벨업 → 루팅 → 탈출 판단 (세션 타이머 5~8분)
- 세션 간 성장: Soul Shard로 영구 업그레이드, 스킬 해금

## 패키지

- **Input System** v1.19.0 — 신 Input System (레거시 미사용)
- **Cinemachine** v2.10.7
- **URP** v17.3.0
- **UniTask** (Cysharp) — async/await
- **R3** (Cysharp) — 반응형 확장
- **AI Navigation** v2.0.11

## 프로젝트 위키

기술 설계와 **선택 이유**를 한 페이지로 정리한 포트폴리오용 위키 → [dev-docs/project/wiki/index.html](dev-docs/project/wiki/index.html) (브라우저로 바로 열기)

- 마지막 갱신: 2026-06-13
- ⚠️ 진행 중인 ItemSystem / EquipmentSystem은 아직 미반영 (수동 갱신)

## 문서

작업용 문서는 [dev-docs/](dev-docs/)에 모여 있다. (`agent/` 에이전트 작업용, `project/` 프로젝트 자료)

- 게임 기획 (세션 구조·MVP 로드맵·씬 흐름·조작 스펙) — [dev-docs/project/design.md](dev-docs/project/design.md)
- 아키텍처 (시스템 구조·설계) — [dev-docs/project/architecture/overview.md](dev-docs/project/architecture/overview.md)
- 개발 도구(Unity MCP) 설정 — [dev-docs/project/dev-tools.md](dev-docs/project/dev-tools.md)
- 코드 컨벤션 — [dev-docs/project/CODE_CONVENTION.md](dev-docs/project/CODE_CONVENTION.md)
- 작업 규약 (Claude Code 가이드) — [CLAUDE.md](CLAUDE.md)