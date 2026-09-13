# docs.harness — 문서 작성·수정(write) 규약

**모든 `dev-docs/` 문서를 작성·수정(write)하기 직전에 따른다.** 상위: [HARNESS.md](HARNESS.md).
(같은 종류의 파일 전체에 대한 규약 = `<이름>.harness.md`. 여기선 "문서 write"라는 행위에 대한 규약.)

---

## 원칙

1. **문서 ≠ 규약 분리 — 한 파일에 섞지 않는다.**
   - **문서** = *내용(무엇을)*: 요구사항·목표·범위·진행상태·설계 서술 등. (예: `requirements.md`, `progress.md`, architecture 문서)
   - **규약** = *규칙(어떻게)*: 작성 방식·프로세스·불변조건. (예: `HARNESS.md`, `<파일>.harness.md`)
   - 요구사항을 HARNESS에 적거나, 작성 규칙을 요구사항 문서에 적지 않는다 — 섞였으면 각자 자리로 가른다.

2. **상위 하네스 재진술 금지 — 참조만 한다.**
   - 상위(루트→해당 경로)의 규약에 **이미 있는 내용을 하위 문서에서 다시 쓰지 않는다.** 복사·재진술 대신 **참조**(`→ 원칙N`·링크)로 가리킨다.
   - 하위 문서엔 **그 층에서 새로 생기는 것만** 적는다(그 feature/폴더 고유의 규칙·내용). 상위와 같은 말을 쓰고 있으면 지우고 링크로 바꾼다.
   - 목적: 규약이 바뀌면 **한 곳만** 고치면 되게 — 사본이 갈라지지 않게.

3. **문서에 필요한 내용만 적는다.**

---

## 날짜 작성 방법

- **형식:** `YYYY-MM-DD` (KST), 날짜만.
- **갱신 시점:** 문서를 실제로 바꿀 때마다 그날로 갱신.
- **값(셸로 확인):** 오늘 — Bash `TZ=Asia/Seoul date +%Y-%m-%d` / PowerShell `(Get-Date).ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd")`. 파일 마지막 변경일 — `git log -1 --format=%cd --date=format-local:%Y-%m-%d -- <file>`.

---

## 적용

- write 직전, 이 문서 + 대상 경로에 걸리는 `HARNESS.md`/`<파일>.harness.md`를 확인한다.
- 서술 규율·근거 규약 등 개별 규칙은 상위에 있으면 **여기서 재진술하지 않고 참조**한다(예: 서술 근거·확신도는 agent/HARNESS 원칙5·8, architecture 서술 규율은 architecture/HARNESS).
