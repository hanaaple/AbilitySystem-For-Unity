# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.
세션 시작 시 자동으로 로드되는 최상위 진입 문서다. 세부 규약·문서는 아래에서 연결한다.

## Language

Always respond in Korean (한국어).

## 행동 원칙

모르거나 애매한 것은 추정하지 않는다. 막연한 추측으로 스스로 판단하고 진행하는 대신, 추가 정보를 요청하거나 질문한다.

- 외부 API·엔진 동작·설계 의도가 불확실하면 → 직접 조사(검색, 문서 확인)하거나 질문한다
- 요구사항이 여러 해석으로 읽히면 → 구현 전에 먼저 확인한다
- 확인 없이 진행했다가 틀리는 것보다 질문 한 번이 낫다

## 이어하기 (세션 재개)

유저가 "하던거 하자"류로 이어하기를 요청하면 → **`dev-docs/agent/NOW.md` 하나만 읽고 곧장 그 액션부터 실행한다.** feature-list·progress·worklog·코드를 미리 훑거나 전체를 확인하지 않는다(막힐 때만 NOW.md의 링크로 연다). 세션 종료 시 NOW.md를 다음 재개 지점으로 갱신한다. — 상세 프로토콜은 `dev-docs/agent/HARNESS.md` §3.

## 빌드·검증

이 프로젝트에는 CLI 빌드·테스트가 없다. 컴파일·플레이 검증은 **유저가 Unity 에디터에서 수행한다.**
에이전트는 코드 정합성(참조·시그니처·구조)까지만 책임지며, `read_console`로 컴파일 에러를 확인하지 않는다.

## 개발 방향

포트폴리오를 목적으로 하므로, 설계 결정에는 **기술적 근거와 선택 이유**가 명확히 드러나야 한다.  
단, 기술적 완성도에 대한 집착으로 실제 게임 완성을 저해하는 과설계(over-engineering)는 지양한다.

- 좋음: "이 패턴을 쓴 이유를 한 문장으로 설명할 수 있다"
- 나쁨: "실제로 필요하지 않지만 기술적으로 인상적으로 보이기 위해 추가한다"

## 코드 컨벤션

`.editorconfig` 자동 적용. 상세 규칙은 `dev-docs/project/CODE_CONVENTION.md` 참고.

## 관련 문서

작업 규약·프로젝트 내용·설계 문서는 루트를 깔끔히 유지하기 위해 전부 `dev-docs/`에 모아둔다 (루트에는 `CLAUDE.md`·`Readme.md`만). `dev-docs/`는 `agent/`(에이전트 작업용)와 `project/`(프로젝트 자료)로 나뉜다.
필요할 때만 열고, 코드 구조·설계를 바꿨으면 **같은 세션에서** 관련 `project/` 문서(특히 architecture)도 갱신한다.

**탐색 규율 (토큰 낭비 방지 — 이 원칙 하나로 통일):** 문서·코드 탐색은 지금 작업에 필요한 범위로만 한다. ① 무관한 문서를 미리 열지 않는다(아래 각 항목의 '언제 여는지' 조건에서만 연다). ② 필요 이상으로 광범위한 검색·파일 전체 읽기 등 **과한 탐색을 하지 않는다** — 목표를 좁혀 최소한으로 조회한다.

- Agent 작업 규약 (세션 프로토콜·기록 규칙) — `dev-docs/agent/HARNESS.md` (구현 작업 착수 전 읽는다)
- 잡다한 작업·개인 TODO·이슈 보드 — `dev-docs/agent/TODO-BOARD.md` (feature-list와 별개 레이어. `TODO/진행 중/Done` 3칸. 채팅·작업 중 나온 할 일을 놓치지 않게 잡아 두고 상태를 옮긴다 — HARNESS.md §7)
- feature 현황·진행 추적 — `dev-docs/agent/feature-list.md` (대시보드) · `dev-docs/agent/feature/<feature-id>/progress.md` (feature별 진행 문서; 구현 방향·불변조건·로드맵이 있으면 같은 폴더의 `HARNESS.md` = feature 전용 하네스/설계 문서). **해당 feature 구현 작업에 착수할 때만** 그 feature 폴더 문서를 연다 — 무관한 작업에선 열지 않는다
- 프로젝트 소개·패키지 — `Readme.md` (외부인이 프로젝트를 처음 볼 때를 위한 소개용. wiki 등 하위 링크는 작업에 불필요하므로 열어보지 않는다)
- 게임 기획 (컨셉·세션 구조·MVP 로드맵·씬 흐름·조작 스펙) — `dev-docs/project/design.md` (필요할 때만 참고. feature 문서(`feature-list.md`·progress)로 충분하면 깊이 확인하지 않는다)
- 아키텍처 (시스템 구조·설계) — `dev-docs/project/architecture/overview.md`. **해당 시스템 코드를 실제로 건드릴 때만** 그 시스템 문서를 연다 — 무관한 작업·평소엔 열지 않는다
- 개발 도구(Unity MCP) 설정 — `dev-docs/project/dev-tools.md`
- 커밋·PR 규약 — `dev-docs/project/git-convention.md` (커밋 타입·메시지·PR 작성. **커밋/push 전 참고** — 특히 refactor(동작 불변) vs feat 구분)
- 포트폴리오 wiki 편집 규약 — `wiki/HARNESS.md`. **wiki를 실제로 편집할 때만** 연다. 핵심: 에이전트는 wiki를 자동 갱신하지 않고 **유저가 명시적으로 요청할 때만** 편집한다 (그 외엔 열지 않는다)
