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

## 결정 기록 기준 (decisions.md)

> 유저(2026-10-18): "크게 중요하지도 않는 것들, 사소한 것들을 모두 기록하는게 아니라 진짜로 중요한 UE의 기능을 가져오면서 선택해야되는 순간이나 새로운 게 나오거나 설계를 하거나 등에 대한 것을 적는거지. 그리고 그것은 간단 명료해야해."

### 무엇을 적나 — 진짜 갈림길만
- **적는다:**
  1. UE 기능을 가져오면서 고른 것 — UE와 다르게 가거나, 일부만 가져오거나, 방식이 둘 이상이라 하나를 고른 경우.
  2. 새로 생기는 구조·개념 — UE에 없는 것, 새 레이어·타입.
  3. 설계 — 데이터 모델, 소유·책임 경계.
- **안 적는다(→ worklog):** UE를 그대로 옮겨 고를 것이 없던 것, 단계·순서·잠정 계획, 버그 수정, 네이밍, 에디터 UI 세부, 작은 구현 선택.
- 뒤집히거나 낡은 결정은 본문을 지우고 파일 끝 `치운 결정`에 한 줄로 남긴다(코드는 재사용하지 않는다 — progress.harness '식별 코드').

### 어떻게 적나 — 간단명료
한 결정 = 아래 항목, 항목마다 1~2줄. 경위·원문 인용·긴 설명은 worklog에 둔다.

```
## D# — 무엇을 골랐나 (한 줄)
- 날짜:
- 결정: 무엇을 하기로 했나.
- 왜: 유저가 말한 근거(인용). 없으면 `근거 미기록`. UE 근거는 원문 위치 하나만.
- 대안: 진지하게 본 다른 방식만, 한 줄씩(얻는 것 / 잃는 것).
- 대가: 포기한 것, UE와 달라진 점.
- 재검토: 뒤집을 조건(있을 때만).
```

- `대안`: 채택안을 뒤집기만 한 것("안 한다"·"제외"·"지금대로"·"덜 한다")은 적지 않는다. 다른 방식이 없으면 항목을 뺀다. (유저 2026-10-18: "decision에 쓸데없는 선택들은 좀 적지말고 지워")
- `왜`는 지어내지 않는다 → [agent/HARNESS.md](../HARNESS.md) 에이전트 원칙 8. 에이전트 판단이 섞이면 `에이전트 판단(유저 미확정):` 한 줄로 따로 둔다.
