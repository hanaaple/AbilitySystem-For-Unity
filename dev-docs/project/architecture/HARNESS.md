# HARNESS.md — architecture 문서 규약

`dev-docs/project/architecture/`(하위 폴더 포함)에 적용. 인덱스는 [overview.md](overview.md).

## 작성 규약
- **문서 상단에 `> **최종 갱신:** YYYY-MM-DD (KST)`** 를 둔다 — 문서를 실제로 바꿀 때마다 그날 날짜로 갱신. 이전 업데이트 여부(낡음) 확인용. 날짜는 **셸로 확인**해 쓰고 지어내지 않는다(모르면 git 최종 커밋 날짜: `git log -1 --format=%cd --date=format-local:%Y-%m-%d -- <file>`). 오늘: Bash `TZ=Asia/Seoul date +%Y-%m-%d` / PowerShell `(Get-Date).ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd")`.
- 개념·역할·시스템 간 관계를 쓴다.
- 원본(UE 등) 대비 채택/생략과 근거를 밝힌다 — 한 결정은 한 문장으로 "왜"까지.
- 흐름·생명주기·재계산은 다이어그램/표로 남긴다.
- 구현 / 미구현(계획)을 범례로 구분한다.
- 확정된 구조·동작·설계 판단의 **결론**만 쓴다.
- 이름·구조·동작은 실제 코드 기준. 다이어그램의 내부 함수/타입명은 현재 코드와 일치시키고, 원본 대응은 `(UE: ...)`로 병기한다.
