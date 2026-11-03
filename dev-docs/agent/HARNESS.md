# HARNESS.md — Agent 작업 규약 (인덱스)

**agent 작동 시 필수 규약의 입구.** 두 갈래로 나뉜다.

| 폴더 | 내용 | 언제 읽나 |
|---|---|---|
| [`core/`](core/HARNESS.md) | **에이전트 자체** — 핵심 원칙(추측 금지·근거·해석 규율·"왜"), 문서 write, 세션 시작·종료, 대화, 작업 범위, git·테스트 원칙 | 프로젝트 관련 대화·작업을 시작할 때 항상 |
| [`project/`](project/HARNESS.md) | **이 프로젝트** — 프로젝트 핵심 원칙(판단의 질·과설계 금지·"왜"의 근거), 코드 작성, 테스트 실행, git 세부, feature 규약, 비공개 문서(`private/`) | 코드·테스트·feature 문서를 다룰 때 |

- 규약 문서 네이밍(`HARNESS.md` / `<폴더>.harness.md` / `<파일>.harness.md` / `<이름>-harness.md`)은 [project/HARNESS.md](project/HARNESS.md) '하위 문서·폴더'.
- 재개 메모 `dev-docs/temp/NOW.md`(로컬)는 [core/HARNESS.md](core/HARNESS.md) '상태 문서'.
