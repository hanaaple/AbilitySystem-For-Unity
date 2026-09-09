# CLAUDE.md

세션 시작 시 자동으로 로드되는 최상위 진입 문서다. 세부 규약·문서는 아래에서 연결한다.

## 행동 원칙

모르거나 사용자의 의도가 부정확한 상태에서 추정하지 않는다.

막연한 추측으로 스스로 판단하고 진행하는 대신, 추가 정보를 요청하거나 질문한다.


## 빌드·검증

이 프로젝트에는 CLI 빌드·테스트가 없다. 에이전트는 코드 정합성(참조·시그니처·구조)까지만 책임진다.

## 개발 방향

포트폴리오를 목적으로 하므로, 설계 결정에는 **기술적 근거와 선택 이유**가 명확히 드러나야 한다.  
단, 기술적 완성도에 대한 집착으로 실제 게임 완성을 저해하는 과설계(over-engineering)는 지양한다.

## Routing Table

```
./ (Root)
├── CLAUDE.md (최상위 하네스)
├── Readme.md (프로젝트 소개)
└── dev-docs/ (작업 규약·프로젝트 내용·설계 문서)
    ├── agent/ (에이전트 작업 자료)
    │   └── feature/ (프로젝트 작업 피처)
    ├── project/ (프로젝트 정리 자료)
    │   └── architecture/
    │   └── wiki/
    └── _parked/ (보관 — 비활성 비-GAS 인게임 문서. 원 경로 미러링. 되돌릴 때 원위치)
```
- 각 파일을 확인 시, Root로부터 해당 경로까지의 `HARNESS.md`들을 우선하여 따른다.
- **`_parked/`는 비활성 보관**이다 — 현재 작업(GAS 데모)에선 열지 않는다. 되돌릴 때만 참조.
- 현재 필요하지 않은 문서를 미리 열지 않는다.
- 필요 이상으로 광범위한 검색·파일 전체 읽기 등 **과한 탐색을 하지 않는다** — 목표를 좁혀 최소한으로 조회한다.

### 관련 문서
- 프로젝트 소개 — `Readme.md`
- 최상위 하네스 — `CLAUDE.md`
- 코드 컨벤션 — `.editorconfig` 자동 적용. `dev-docs/project/CODE_CONVENTION.md`
- feature 현황·진행 추적 — `dev-docs/agent/feature/feature-list.md` (대시보드) · `dev-docs/agent/feature/<feature-id>`
- 잡다한 작업·개인 TODO·이슈 보드 — `dev-docs/agent/TODO-BOARD.md`
- 게임 기획 — `dev-docs/project/design.md`
- 아키텍처 — `dev-docs/project/architecture` (overview.md가 인덱스). 시스템 구조·설계를 개념·방향 중심으로 정리한 **에이전트·개발자·외부 공용** 문서. 작성 규약은 `architecture/HARNESS.md`.
- 개발 도구 설정 — `dev-docs/project/dev-tools.md`
- 커밋·PR 등 git 관련 규약 — `dev-docs/project/git-convention.md`
- 세션 프로토콜 — `dev-docs/agent/session-protocol.md`.
- wiki 편집 규약 — `wiki/HARNESS.md`.
- 보관(비활성 비-GAS) — `dev-docs/_parked/` (README: `dev-docs/_parked/README.md`).


### 세부 탐색 규율
#### dev-docs/agent/HARNESS.md
 - 프로젝트 개발에 해당하는 내용을 다루기 시작하면 — 구현이 아니어도 읽는다.** 코드 작성뿐 아니라 **질문 답변·개념 설명·설계 논의**도 포함한다.
 - `dev-docs/` 문서를 작성·수정하기 직전
 - 프로젝트 개발, 설계 등 프로젝트와 관련된 대화 시작 시. (검색, 해석, 판단 등)
