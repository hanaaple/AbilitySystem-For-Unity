# feature/HARNESS — feature 문서 작성 규약

문서끼리는 번호가 아니라 **절 이름**으로 참조한다.

---

## 폴더 구조·네이밍

```
feature/
├── HARNESS.md              # 본 문서 (feature 문서 작성 규약) — feature/ 전체 지배
├── feature-list.md         # 전체 feature 목록·상태 대시보드
├── <feature-id>/           # 단일 feature (그룹 불필요 시)
│   ├── progress.md          # 진행 문서 (필수)
│   ├── worklog.md           # 작업 로그 (아카이브)
│   ├── decisions.md         # 결정 기록 상세
│   └── HARNESS.md           # feature 전용 하네스/설계 (선택)
└── <group-id>/             # feature 그룹 (대분류)
    ├── HARNESS.md           # 그룹 공유 하네스/설계 (선택, 하위 전체 지배)
    └── <feature-id>/        # 하위 feature
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

## feature-list.md 규약

`feature-list.md`는 프로젝트의 대시보드이며, 아래 형식을 따른다.

### 상태 정의

| 상태 | 의미 |
|---|---|
| `📋 PLANNED` | 등록만 됨. 설계/구현 미착수 |
| `🔧 IN-PROGRESS` | 현재 작업 중 |
| `⏸️ BLOCKED` | 블로커로 중단됨 (progress에 사유 필수) |
| `✅ DONE` | 완료. 수용 기준 충족 확인됨 |
| `🗄️ ARCHIVED` | 폐기 또는 통합됨 (사유 필수) |

### 상태 전이 규칙

```
PLANNED → IN-PROGRESS ⇄ DONE
              ↕
           BLOCKED
(모든 상태 → ARCHIVED 가능, 사유 필수)
```

- `DONE` 처리는 progress의 `## 수용 기준`이 전부 체크된 경우에만 허용한다.
- `DONE`인 feature를 다시 수정해야 하면 `IN-PROGRESS`로 되돌리고 사유를 기록한다.

### 목록 형식

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

---

## progress.md 규약

각 progress 파일은 아래 템플릿 구조를 유지한다. **섹션을 임의로 삭제하지 않는다.**
**이 블록이 유일한 템플릿이다** — 별도 템플릿 파일을 두지 않는다(두 벌이 되면 갈라진다). 새 progress는 여기서 복사해 만든다.

```markdown
# <feature-id> — <Feature 이름>

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: YYYY-MM-DD  (KST)

## 목표
이 feature가 완성되면 무엇이 가능해지는가. (1~3문장)

## 수용 기준 (Definition of Done)
가능하면 **유저가 Unity 에디터에서 직접 확인 가능한** 조건으로 쓴다 (Play 모드 동작·Inspector 값·에디트 모드 동작 등). CLI 자동 테스트는 없다.
- [ ] 검증 가능한 조건 1
- [ ] 검증 가능한 조건 2

## 범위
### 포함
### 제외 (명시적으로 하지 않을 것)

## 설계 개요
핵심 구조, 관련 클래스/파일, 데이터 흐름 요약.

## 세부 TODO (구현 체크리스트)
이 feature를 완성하는 데 필요한 구현 작업을 순서대로 나열한다. 진행하며 ☑ 한다.
- [ ] 1. 작업 항목
- [ ] 2. 작업 항목

## 결정 기록
→ [decisions.md](decisions.md) (상세). progress엔 요지 인덱스만 (decisions.md와 같은 `D#` 공유).
| ID | 날짜 | 결정 (요지) |
|---|---|---|

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력은 별 파일). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
(없으면 "없음"으로 명시)

## 다음 작업
1. 가장 먼저 할 일 (구체적으로: 파일, 클래스, 목표 단위)
2. 그 다음 할 일
```

### progress 작성 규칙

- **세부 TODO vs 수용 기준 vs 다음 작업** (혼동 금지):
  - `수용 기준` = 완료 판정(검증 가능한 **결과**). `세부 TODO` = 그 결과를 만들기 위한 **구현 작업 backlog**(과정). `다음 작업` = 세부 TODO 중 **다음 세션에 착수할 항목**을 구체 실행 지시로 지목.
  - 세부 TODO 항목을 끝내면 ☑ 하고, 필요하면 `다음 작업`이 그다음 미완 항목을 가리키게 갱신한다.
  - feature에 **전용 `HARNESS.md` 로드맵**(예: item의 S1~S10)이 있으면 그것이 세부 TODO 역할을 하므로, progress의 `## 세부 TODO`는 그 로드맵을 **가리키기만** 하고 중복 나열하지 않는다.
  - 이 세부 TODO는 feature 내부 작업용이다. feature와 무관한 잡다한 할 일은 여기가 아니라 `TODO-BOARD.md`(작업 보드 규약)에 둔다.
- **작업 로그는 feature 폴더의 `worklog.md`에 별도 보관**한다 (progress엔 `## 작업 로그` 헤더 + 포인터 링크만). 로그는 계속 자라는 아카이브라 매 세션 재로드 비용을 분리하려는 것 — 재개 앵커는 progress `## 다음 작업`이다. worklog는 **최신이 위로** 쌓는다.
- **worklog의 과거 기록은 삭제·수정하지 않는다** — 시간순 이력 아카이브라 되돌리지 않는다. 오기 정정 시 원문을 지우지 말고 **취소선 + 정정 표기**로 남긴다.
- 로그는 "무엇을 왜 했는가" 중심으로 3~7줄 이내로 요약한다. 코드 전문을 붙여넣지 않는다.
- `결정 기록` **상세는 feature 폴더 `decisions.md`**에 둔다(아래 '결정 기록 품질 기준': 맥락·대안·근거·결과·재평가 트리거). progress `## 결정 기록`엔 **요지 인덱스 + 링크만** (worklog와 같은 이유로 분리 — 해당 영역 건들 때 decisions.md 확인). 트레이드오프가 있었던 결정만, 사소한 네이밍 등은 제외.
- **`D#`은 유저가 확정한 결정에만 부여한다.** 에이전트가 스스로 내린 판단·제안 단계 결론을 D로 박지 않는다 — 결정 기록은 *유저의 판단*을 남기는 자리다. 제안 단계에서는 `worklog.md`·progress에 **"제안"으로 적고**, 유저가 확정한 뒤에 D를 부여한다. (판단 근거·대안 정리는 제안 단계에서 해도 되지만, 그게 곧 결정은 아니다.)
- **식별 코드:** 각 결정엔 feature 내 고유한 `D1`·`D2`…(순번, 재번호·재사용 금지)를 부여한다. decisions.md와 progress 인덱스가 같은 ID를 공유하고, worklog·`다음 작업`에서 결정을 가리킬 땐 `(→D3)`처럼 코드로 링크한다. 로드맵 슬라이스(`S#`/`I#`/`E#` 등)도 worklog에서 같은 방식으로 참조해 **"언제(worklog) → 무슨 작업(S/I/E) → 왜(D)"**가 코드로 이어지게 한다.
- **`최종 갱신`은 날짜만**(`YYYY-MM-DD`, KST — 시:분 없음) 적고, 문서를 바꿀 때마다 그날 날짜로 갱신한다 — 하류 문서(architecture·wiki) 동기화 판별의 기준(session-protocol '최종 갱신 타임스탬프').
- `다음 작업`은 항상 **실행 가능한 수준**으로 구체화한다.
  - 나쁜 예: "전투 시스템 개선"
  - 좋은 예: "`AttackState.cs`에 콤보 입력 버퍼(0.3s) 추가 후 `PlayerStateMachine` 전이 연결"

---

## Feature 생성 / 완료 / 폐기 절차

### 신규 feature 등록
1. `feature-list.md`에 행 추가 (`📋 PLANNED`)
2. `feature/<feature-id>/` 폴더를 만들고, 'progress.md 규약'의 템플릿을 복사해 `progress.md` 생성 (구현 방향·불변 조건·로드맵이 방대하면 같은 폴더에 `HARNESS.md`도 작성)
3. `목표`, `수용 기준`, `범위`를 반드시 채운 후 작업 착수

### 완료 처리
1. `수용 기준` 전 항목 체크 확인
2. progress 상태 → `✅ DONE`, 최종 로그 기록
3. `feature-list.md` 상태 동기화
4. **wiki 갱신 리마인드(마일스톤):** feature를 `✅ DONE` 처리했으면, 포트폴리오 wiki(`dev-docs/project/wiki/`)가 낡았을 수 있음을 유저에게 **한 줄 리마인드**한다. — wiki를 직접 고치지는 않는다(갱신은 요청 시에만 — `dev-docs/project/wiki/HARNESS.md`의 "요청 시에만 갱신" 규칙). 어긋날 만한 섹션이 짐작되면 함께 짚어준다. (예: "item-system 완료 — wiki의 `arch`/`todo` 섹션이 코드와 어긋날 수 있으니 갱신할까요?")

### 폐기/통합 처리
1. progress 최상단에 폐기 사유 및 (통합 시) 대상 feature-id 기록
2. 양쪽 문서 상태를 `🗄️ ARCHIVED`로 동기화

---

## 결정 기록 품질 기준

트레이드오프가 있는 결정은 feature 폴더 `decisions.md`에 아래를 담아 **상세히** 남긴다 (progress `## 결정 기록`엔 요지 인덱스 + 링크만 — 'progress.md 규약'). 표 `채택 사유` 칸에 서술로 압축. (자명한 선택·네이밍은 제외 — 과잉 기록은 유지 부담)

- **맥락(forces):** 왜 이 결정이 필요했나 — 충돌한 제약·요구.
- **대안:** 진지하게 검토한 후보 + 각 장단 (버린 것도 이유와 함께).
- **근거:** 왜 이걸 택했나, 그리고 **왜 대안이 아니었나**.
- **결과·트레이드오프:** 무엇을 얻고 **무엇을 포기했나** — 부정적 결과를 반드시 포함.
- **재평가 트리거:** 이 결정을 뒤집을 조건
