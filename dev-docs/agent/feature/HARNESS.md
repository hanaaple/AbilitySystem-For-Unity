# feature/HARNESS — feature 문서 규약 허브

feature/ 폴더 개요와 생명주기 절차. 문서 작성 규약은 별도 파일에 있다.

- [feature-list.harness.md](feature-list.harness.md) — 상태 정의·전이·목록 형식
- [progress.harness.md](progress.harness.md) — progress 템플릿·작성 규칙

---

## 폴더 구조·네이밍

```
feature/
├── HARNESS.md                    # 규약 허브 (폴더 구조·생명주기) — feature/ 전체 지배
├── feature-list.harness.md       # feature-list.md 작성 규약
├── progress.harness.md           # progress/decisions 작성 규약 (progress.md 종류 전체)
├── feature-list.md               # 전체 feature 목록·상태 대시보드
├── <feature-id>/                 # 단일 feature (그룹 불필요 시)
│   ├── progress.md                # 진행 문서 (필수)
│   ├── worklog.md                 # 작업 로그 (아카이브)
│   ├── decisions.md               # 결정 기록 상세
│   └── HARNESS.md                 # feature 전용 하네스/설계 (선택)
└── <group-id>/                   # feature 그룹 (대분류)
    ├── HARNESS.md                 # 그룹 공유 하네스/설계 (선택, 하위 전체 지배)
    └── <feature-id>/              # 하위 feature
        ├── progress.md
        ├── worklog.md
        └── decisions.md
```

- **네이밍:** feature-id·group-id는 **kebab-case 영문 소문자** (예: `combat-core`, `enemy-ai`, `item-system`).
- **위치:** 각 feature는 폴더를 가지며 진행 문서는 그 안에 `progress.md`로 둔다. 단일 feature는 `feature/<feature-id>/`, 그룹 하위는 `feature/<group-id>/<feature-id>/`.
- **그룹(대분류):** 한 도메인의 하위 feature가 여럿으로 늘면 `feature/<group-id>/`로 묶는다(예: `item-system` → `item`·`inventory`·`equipment`). 그룹 자체는 progress를 갖지 않으며, 하위 전체를 지배하는 공유 불변조건·설계가 있으면 그룹 폴더에 `HARNESS.md`를 둔다(하위는 참조, 중복 정의 금지).
- **feature 전용 `HARNESS.md` (선택):** 구현 방향·불변 조건·로드맵/설계가 방대하면 같은 폴더에 둔다. 일반 `HARNESS.md`(agent/)는 **프로세스 규약**, feature/그룹 `HARNESS.md`는 **그 feature의 구현 규약·설계** — 충돌 시 프로세스 규약이 우선하고 충돌을 보고한다.
- `feature-list.md`에 없는 feature의 폴더를 만들지 않는다(등록이 먼저 — 'Feature 생성 / 완료 / 폐기 절차').

---

## Feature 생성 / 완료 / 폐기 절차

### 신규 feature 등록
1. `feature-list.md`에 행 추가 (`📋 PLANNED`)
2. `feature/<feature-id>/` 폴더를 만들고, `progress.harness.md`의 템플릿을 복사해 `progress.md` 생성 (구현 방향·불변 조건·로드맵이 방대하면 같은 폴더에 `HARNESS.md`도 작성)
3. `목표`, `수용 기준`, `범위`를 반드시 채운 후 작업 착수

### 완료 처리
1. `수용 기준` 전 항목 체크 확인
2. progress 상태 → `✅ DONE`, 최종 로그 기록
3. `feature-list.md` 상태 동기화
4. **wiki 갱신 리마인드(마일스톤):** feature를 `✅ DONE` 처리했으면, 포트폴리오 wiki(`dev-docs/project/wiki/`)가 낡았을 수 있음을 유저에게 **한 줄 리마인드**한다. — wiki를 직접 고치지는 않는다(갱신은 요청 시에만 — `dev-docs/project/wiki/HARNESS.md`의 "요청 시에만 갱신" 규칙). 어긋날 만한 섹션이 짐작되면 함께 짚어준다. (예: "item-system 완료 — wiki의 `arch`/`todo` 섹션이 코드와 어긋날 수 있으니 갱신할까요?")

### 폐기/통합 처리
1. progress 최상단에 폐기 사유 및 (통합 시) 대상 feature-id 기록
2. 양쪽 문서 상태를 `🗄️ ARCHIVED`로 동기화

## 결정 기록 품질 기준

트레이드오프가 있는 결정은 feature 폴더 `decisions.md`에 아래를 담아 **상세히** 남긴다. 표 `채택 사유` 칸에 서술로 압축. (자명한 선택·네이밍은 제외 — 과잉 기록은 유지 부담)

- **맥락(forces):** 왜 이 결정이 필요했나 — 충돌한 제약·요구.
- **대안:** 진지하게 검토한 후보 + 각 장단 (버린 것도 이유와 함께).
- **근거:** 왜 이걸 택했나, 그리고 **왜 대안이 아니었나**.
- **결과·트레이드오프:** 무엇을 얻고 **무엇을 포기했나** — 부정적 결과를 반드시 포함.
- **재평가 트리거:** 이 결정을 뒤집을 조건
