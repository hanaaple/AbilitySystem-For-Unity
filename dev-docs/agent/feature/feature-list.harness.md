# feature-list.harness — feature-list.md 규약

`feature-list.md`는 프로젝트의 대시보드이며, 아래 형식을 따른다.

## 상태 정의

| 상태 | 의미 |
|---|---|
| `📋 PLANNED` | 등록만 됨. 설계/구현 미착수 |
| `🔧 IN-PROGRESS` | 현재 작업 중 |
| `⏸️ BLOCKED` | 블로커로 중단됨 (progress에 사유 필수) |
| `✅ DONE` | 완료. 수용 기준 충족 확인됨 |
| `🗄️ ARCHIVED` | 폐기 또는 통합됨 (사유 필수) |

## 상태 전이 규칙

```
PLANNED → IN-PROGRESS ⇄ DONE
              ↕
           BLOCKED
(모든 상태 → ARCHIVED 가능, 사유 필수)
```

- `DONE` 처리는 progress의 `## 수용 기준`이 전부 체크된 경우에만 허용한다.
- `DONE`인 feature를 다시 수정해야 하면 `IN-PROGRESS`로 되돌리고 사유를 기록한다.

## 목록 형식

그룹이 있으면 `###` 헤더로 묶고, 그룹 공유 하네스가 있으면 헤더 아래 링크한다. 그룹이 필요 없는 단일 feature는 헤더 없이 표 행으로 둔다.

```markdown
### 🧩 <group-id> — <그룹 이름>
공유 하네스: [HARNESS](feature/<group-id>/HARNESS.md)

| ID | Feature | 상태 | 우선순위 | 의존 | Progress |
|---|---|---|---|---|---|
| combat-core | 근접 전투 코어 | 🔧 IN-PROGRESS | P0 | - | [progress](feature/<group-id>/combat-core/progress.md) |
```

- 우선순위: `P0`(필수) / `P1`(중요) / `P2`(여유 시)
- `의존` 열에는 선행되어야 하는 feature-id를 적는다.
- 하위 feature ID는 그룹 안에서만 유일하면 된다(그룹이 네임스페이스). 표기는 짧게 `item`·`inventory`처럼.
- **`최종 갱신`:** 상단에 날짜만(`YYYY-MM-DD`, KST). 표를 바꿀 때마다 그날 날짜로 갱신한다. 날짜는 **셸로 확인**해 쓰고 지어내지 않는다. (오늘: Bash `TZ=Asia/Seoul date +%Y-%m-%d` / PowerShell `(Get-Date).ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd")`.)
