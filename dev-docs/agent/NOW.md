# NOW — 단일 재개 지점

> 휘발성 재개 메모. 진실은 코드 + progress/decisions. 세션 종료 시 통째 재작성.
> 최종 갱신: 2026-09-08 (KST)

## ▶ 지금 할 일 — 하네스/문서 간결화 (계속)

dev-docs 하네스를 **인덱싱·중복 제거** 방향으로 정리 중. 이번 세션 완료분:
- `HARNESS.md 금지 사항` 순수 인덱스화 + feature 스코프 금지 → `feature/HARNESS.md`로 이관
- `session-protocol '작업 중'` 압축
- `HARNESS.md 관련 하위 문서 및 폴더 (Indexing)` 재구성(자기설명 인덱스 + 가드 한 줄)
- feature **폴더 구조·네이밍 규약** → `feature/HARNESS.md`로 이관(참조 4곳 갱신)
- **cases 시스템 전면 제거** — 3개 `*.cases.md` 삭제 + `→ 사례 [C-NN]` 포인터·"규칙↔사례 분리" 블록쿼트 전부 제거
- `NOW.md — 무엇인가` 절 제거(인덱스 자기설명화 + 규칙은 `session-protocol '세션 종료'`로 이관)
- **핵심 원칙 1~8 평면 번호화**(판단·해석·읽기트리거·왜는지어내지를 번호 원칙 5~8로 승격)

**다음 액션:** ① `HARNESS.md 포트폴리오·면접 대비` 섹션 트림 ② (선택) 핵심원칙 5·6 더 압축 ③ `session-protocol 대화 규율`(Grice 표·Gordon 로드블록) 압축 검토.

## ⚠ 미커밋 · 환경 (반드시 확인)

- **이 세션 산출물 전부 미커밋** — 하네스 doc 편집(CLAUDE·HARNESS·session-protocol·feature/HARNESS·TODO-BOARD·design) + `*.cases.md` 3파일 삭제 + feature-list. 커밋 여부·타입 유저 판단.
- **GA 작업은 `stash@{0}`**(GitHub Desktop `!!GitHub_Desktop<main>`)에 있음 — GA 스크립트 4개(`GameplayAbilityAsset`/`Spec`/`SpecContainer`/`Handle`) + `ASC.GiveAbility` 스켈레톤 + `GA_New.asset` + **이전 NOW.md 수정본**. 유저가 만든 stash라 건드리지 않음.
  - ⚠ **이 NOW.md를 방금 재작성**했으므로 그 stash를 `pop`하면 NOW.md에서 충돌 가능(stash 쪽 NOW.md 수정과 겹침) — pop 시 이 최신본 유지 쪽으로 해결.
- **feature-list vs progress 상태 불일치:** attribute·aggregator·gameplay-effect를 `feature-list.md`에선 `✅ 임시완료`로 표기했으나 각 `progress.md` 상단은 아직 `🔧 IN-PROGRESS`. 동기화 유저 판단 대기. (`✅ 임시완료`는 정식 상태 아님 — 유지 시 `feature/HARNESS` 상태 정의에 추가 필요.)
