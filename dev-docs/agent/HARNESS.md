# HARNESS.md — Agent 작업 규약

> 이 문서는 **feature 현황 추적과 세션 작업 프로토콜**을 정의한다. 진입점은 루트의 `CLAUDE.md`이며, 실제 구현 작업(feature 착수·진행·완료)에 들어가기 전 이 문서를 읽는다.
> 에이전트 작업 문서는 전부 `dev-docs/agent/`에 있다.

---

## 1. 핵심 원칙

1. **문서가 곧 상태(State)다.** Agent의 컨텍스트는 세션마다 초기화된다. 세션 간 유일한 기억은 `dev-docs/` 문서이므로, 문서에 기록되지 않은 작업은 없었던 작업으로 간주한다.
2. **읽기 → 작업 → 기록.** 모든 세션은 이 3단계를 따른다. 기록 없이 세션을 종료하지 않는다.
3. **하나의 세션, 하나의 feature.** 한 세션에서는 원칙적으로 하나의 feature에만 집중한다. 다른 feature의 코드를 수정해야 한다면 해당 feature의 progress에 사유를 기록한다.
4. **추측 금지.** progress 문서와 실제 코드가 불일치하면, 코드를 진실로 보고 progress 문서를 수정한 뒤 작업을 시작한다.

---

## 2. 디렉토리 구조

```
dev-docs/
├── agent/                      # 에이전트 작업용 (본 규약이 관리하는 대상)
│   ├── HARNESS.md              # 본 문서 (작업 규약)
│   ├── feature-list.md         # 전체 feature 목록 및 상태 대시보드
│   ├── feature-progress-TEMPLATE.md
│   └── feature/
│       ├── <feature-id>-progress.md   # feature별 진행 문서
│       ├── combat-core-progress.md    # 예시
│       └── item-system-progress.md    # 예시
└── project/                    # 프로젝트 자료 (작업 규약 대상 아님)
    ├── design.md               # 게임 기획
    ├── CODE_CONVENTION.md      # 코드 컨벤션
    ├── dev-tools.md            # 개발 도구 설정
    └── architecture/           # 시스템 구조·설계 문서
```

- feature-id는 **kebab-case 영문 소문자**로 짓는다. (예: `combat-core`, `enemy-ai`, `save-load`)
- progress 파일명은 반드시 `<feature-id>-progress.md` 형식을 지킨다.
- `feature-list.md`에 없는 feature의 progress 파일을 만들지 않는다. (등록이 먼저다)

---

## 3. 세션 프로토콜 (Agent 필수 절차)

### 3.1 세션 시작 시

1. `dev-docs/agent/HARNESS.md` (본 문서)를 읽는다.
2. `dev-docs/agent/feature-list.md`를 읽고 전체 현황을 파악한다.
3. 작업 대상 feature의 `dev-docs/agent/feature/<feature-id>-progress.md`를 읽는다.
4. progress의 `## 다음 작업` 섹션과 사용자의 지시를 대조한다.
   - 충돌 시 사용자 지시가 우선하며, 변경 사유를 progress에 기록한다.
5. progress 내용과 실제 코드의 정합성을 간단히 확인한다. (핵심 파일 존재 여부, 최근 작업 반영 여부)

### 3.2 작업 중

- 설계 결정(트레이드오프가 있는 선택)을 내리면 즉시 progress의 `## 결정 기록`에 남긴다.
- 작업이 막히면(외부 의존성, 미확정 스펙 등) `## 블로커`에 기록하고 사용자에게 보고한다.
- feature 범위가 커지면 임의로 확장하지 않고, 분리 제안을 사용자에게 보고한다.
- 컨텍스트가 길어지면 세션 종료 전이라도 progress `## 작업 로그`·`## 다음 작업`에 중간 요약을 남긴다. (컨텍스트 압축·리셋으로 진행 내용이 유실되지 않게)

### 3.3 세션 종료 시 (필수)

1. progress의 `## 작업 로그`에 오늘 날짜로 수행한 작업을 요약 기록한다.
2. `## 다음 작업`을 갱신한다. — **다음 세션의 Agent가 이 섹션만 읽고도 이어서 작업할 수 있어야 한다.**
3. feature 상태가 변했으면 progress 상단의 상태와 `feature-list.md`의 상태를 **둘 다** 갱신한다. (단일 갱신 금지 — 항상 동기화)

---

## 4. feature-list.md 규약

`feature-list.md`는 프로젝트의 대시보드이며, 아래 형식을 따른다.

### 4.1 상태 정의

| 상태 | 의미 |
|---|---|
| `📋 PLANNED` | 등록만 됨. 설계/구현 미착수 |
| `🔧 IN-PROGRESS` | 현재 작업 중 |
| `⏸️ BLOCKED` | 블로커로 중단됨 (progress에 사유 필수) |
| `✅ DONE` | 완료. 수용 기준 충족 확인됨 |
| `🗄️ ARCHIVED` | 폐기 또는 통합됨 (사유 필수) |

### 4.2 상태 전이 규칙

```
PLANNED → IN-PROGRESS → DONE
              ↕
           BLOCKED
(모든 상태 → ARCHIVED 가능, 사유 필수)
```

- `DONE` 처리는 progress의 `## 수용 기준`이 전부 체크된 경우에만 허용한다.
- `DONE`인 feature를 다시 수정해야 하면 `IN-PROGRESS`로 되돌리고 사유를 기록한다.

### 4.3 목록 형식

```markdown
| ID | Feature | 상태 | 우선순위 | 의존 | Progress |
|---|---|---|---|---|---|
| combat-core | 근접 전투 코어 | 🔧 IN-PROGRESS | P0 | - | [링크](feature/combat-core-progress.md) |
```

- 우선순위: `P0`(필수) / `P1`(중요) / `P2`(여유 시)
- `의존` 열에는 선행되어야 하는 feature-id를 적는다.

---

## 5. progress.md 규약

각 progress 파일은 아래 템플릿 구조를 유지한다. **섹션을 임의로 삭제하지 않는다.**

```markdown
# <feature-id> — <Feature 이름>

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: YYYY-MM-DD

## 목표
이 feature가 완성되면 무엇이 가능해지는가. (1~3문장)

## 수용 기준 (Definition of Done)
- [ ] 검증 가능한 조건 1
- [ ] 검증 가능한 조건 2

## 범위
### 포함
### 제외 (명시적으로 하지 않을 것)

## 설계 개요
핵심 구조, 관련 클래스/파일, 데이터 흐름 요약.

## 결정 기록
| 날짜 | 결정 | 대안 | 채택 사유 |
|---|---|---|---|

## 작업 로그
### YYYY-MM-DD
- 수행한 작업 요약
- 변경된 주요 파일

## 블로커
(없으면 "없음"으로 명시)

## 다음 작업
1. 가장 먼저 할 일 (구체적으로: 파일, 클래스, 목표 단위)
2. 그 다음 할 일
```

### progress 작성 규칙

- **작업 로그는 최신이 위로** 오도록 쌓는다.
- 로그는 "무엇을 왜 했는가" 중심으로 3~7줄 이내로 요약한다. 코드 전문을 붙여넣지 않는다.
- `결정 기록`에는 트레이드오프가 있었던 결정만 남긴다. (사소한 네이밍 등은 제외)
- `다음 작업`은 항상 **실행 가능한 수준**으로 구체화한다.
  - 나쁜 예: "전투 시스템 개선"
  - 좋은 예: "`AttackState.cs`에 콤보 입력 버퍼(0.3s) 추가 후 `PlayerStateMachine` 전이 연결"

---

## 6. Feature 생성 / 완료 / 폐기 절차

### 신규 feature 등록
1. `feature-list.md`에 행 추가 (`📋 PLANNED`)
2. 템플릿 기반으로 `dev-docs/agent/feature/<feature-id>-progress.md` 생성
3. `목표`, `수용 기준`, `범위`를 반드시 채운 후 작업 착수

### 완료 처리
1. `수용 기준` 전 항목 체크 확인
2. progress 상태 → `✅ DONE`, 최종 로그 기록
3. `feature-list.md` 상태 동기화

### 폐기/통합 처리
1. progress 최상단에 폐기 사유 및 (통합 시) 대상 feature-id 기록
2. 양쪽 문서 상태를 `🗄️ ARCHIVED`로 동기화

---

## 7. 금지 사항

- ❌ progress 갱신 없이 세션 종료
- ❌ `feature-list.md`와 progress 상태 불일치 방치
- ❌ 등록되지 않은 feature에 대한 구현 착수
- ❌ 다른 feature의 progress를 해당 feature 세션 외부에서 무단 수정
- ❌ 작업 로그에서 과거 기록 삭제/수정 (오기 정정 시 취소선 + 정정 표기)
