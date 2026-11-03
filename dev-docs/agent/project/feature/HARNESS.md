# feature/HARNESS — feature 규약 파일(공개) 허브

feature 작업의 **설계·불변조건 하네스**(그룹·feature 전용)는 여기 공개 폴더에 둔다. feature **내용 문서**(`feature-list.md`·progress·worklog·decisions·보조 문서)와 그 구조·생명주기·기록 규약은 비공개 — [private/feature/HARNESS.md](../private/feature/HARNESS.md) ([project/HARNESS.md](../HARNESS.md) '하위 문서·폴더').

---

## 폴더 구조·네이밍

```
agent/feature/                          # 규약 (공개)
├── HARNESS.md                          # 이 문서
├── <group-id>.harness.md               # 그룹 공유 하네스/설계 (선택) — 그룹 폴더 옆, 하위 전체 지배 (예: `ability-system.harness.md`)
└── <feature-id>.harness.md             # feature 전용 하네스/설계 (선택) — feature 폴더 옆 (그룹 하위면 `<group-id>/<feature-id>.harness.md`)
```

- **그룹 하네스:** 하위 전체를 지배하는 공유 불변조건·설계가 있으면 `<group-id>.harness.md`를 둔다(하위는 참조, 중복 정의 금지).
- **feature 전용 하네스 (선택):** 구현 방향·불변 조건·로드맵/설계가 방대하면 `<feature-id>.harness.md`(그룹 하위면 `<group-id>/<feature-id>.harness.md`)로 둔다. 일반 `HARNESS.md`(agent/)는 **프로세스 규약**, feature/그룹 하네스는 **그 feature의 구현 규약·설계** — 충돌 시 프로세스 규약이 우선하고 충돌을 보고한다.
- feature-id·group-id 네이밍, feature 폴더·그룹 폴더, 생성/완료/폐기 절차, 작업 중 기록, 결정 기록 기준 → [private/feature/HARNESS.md](../private/feature/HARNESS.md).
