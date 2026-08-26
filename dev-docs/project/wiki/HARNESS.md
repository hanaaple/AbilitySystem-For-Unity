# HARNESS.md — wiki 작업 규약

> 이 문서는 **포트폴리오 wiki(`wiki/`)를 편집할 때의 규약**을 정의한다. wiki를 실제로 건드릴 때만 읽는다 — 그 외 작업에선 열지 않는다(토큰 낭비 금지).
> 프로세스 일반 규약은 `dev-docs/agent/HARNESS.md`가, 진입점은 루트 `CLAUDE.md`가 담당한다.

---

## 0. 가장 중요한 규칙 — 요청 시에만 편집

**에이전트는 wiki를 자동으로 갱신하지 않는다.** 코드·아키텍처·문서를 아무리 크게 바꿔도, 유저가 명시적으로 "wiki 갱신/수정"을 요청하지 않는 한 `wiki/` 아래 파일을 건드리지 않는다.

- wiki는 유저가 편집권을 가진 **큐레이션된 포트폴리오 산출물**이며, 유저가 직접 손보기도 한다.
- **wiki는 dev-docs·project·코드보다 늦게 갱신되는 스냅샷이다.** 최신 코드/문서와 어긋나 있는 것이 비정상이 아니다 — 버그로 보고 "고쳐야 할 것"으로 취급하지 않는다.
- 코드 변경으로 wiki가 낡았다고 판단되더라도 → **고치지 말고**, 요청이 있을 때만 반영한다. (원하면 "wiki의 X 섹션이 현재 코드와 어긋남" 정도만 가볍게 보고 가능)
- 유저가 갱신을 요청하면, 그때 이 문서의 1~5번 규약을 따른다.
- **마일스톤 리마인드:** feature가 `✅ DONE` 될 때, 에이전트는 wiki가 낡았을 수 있음을 **한 줄 리마인드**한다(편집이 아니라 알림; `dev-docs/agent/feature/HARNESS.md`의 'Feature 생성/완료/폐기 절차' 참조). 갱신 여부는 유저가 결정한다.

---

## 1. wiki의 목적과 성격

- **목적:** 기술 설계와 **선택 이유(왜 이 결정을 했는가)**를 한 페이지로 정리한 포트폴리오. 대상 독자는 코드를 처음 보는 외부인(리뷰어·면접관)이다.
- **성격:** 프레임워크·빌드 없는 **정적 단일 페이지**. 손으로 작성한 콘텐츠이며, 코드에서 자동 생성되지 않는다.
- 브라우저로 `wiki/index.html`을 바로 열어 확인한다. (별도 서버·빌드 불필요)

---

## 2. 파일 구조와 콘텐츠 위치

```
wiki/
├── index.html      # 섹션별 서술 콘텐츠(마크업). 모든 텍스트/구조의 본체
├── HARNESS.md      # 본 문서
└── src/
    ├── main.js     # SECTION_META(섹션별 핵심 결정·근거·sources), todo 체크리스트 로직, meta 카드
    └── style.css   # 스타일
```

콘텐츠가 **세 곳에 나뉘어** 있으므로, 한 섹션을 수정할 때 대응 지점을 모두 확인한다.

| 요소 | 위치 |
|---|---|
| 섹션 본문(제목·설명·표·아코디언) | `index.html`의 `<section id="...">` |
| 섹션 우측 메타(핵심 결정 목록 + `sources` 코드 경로) | `src/main.js`의 `SECTION_META[<섹션id>]` |
| todo 체크리스트 항목·상태 | `index.html`의 `data-id`/`data-group` + `src/main.js` |
| 시각 스타일 | `src/style.css` |

- **섹션 id 대응:** `index.html`의 `<section id="X">`와 `main.js`의 `SECTION_META['X']`는 같은 id로 짝지어진다. 한쪽만 추가/삭제하면 메타 카드가 어긋난다 — 항상 동기화한다.
- 현재 섹션 id: `overview` · `mvp` · `packages` · `arch` · `ability-system` · `scenes` · `camera` · `input` · `convention`. (섹션별 상세는 아래 §2.1)

## 2.1 섹션별 콘텐츠 맵 — index.html/main.js 재파악 없이 편집하기

> 편집 대상 `<section id>`·탭·아코디언·앵커와 대응 메타 위치를 이 맵에서 바로 찾는다. **index.html 전체(≈800줄)를 다시 읽지 말 것.** 구조가 바뀌면(섹션·탭·앵커 추가/삭제) **같은 커밋에서 이 맵도 갱신**한다.

**사이드바 NAV 그룹(main.js `NAV`):** ① 개요 = `overview`·`mvp`·`packages` · ② 아키텍처 = `arch`·`ability-system`(하위 `attr-system`·`ge-system`)·`scenes`·`camera`·`input` · ③ 개발 가이드 = `convention`(하위 `convention-naming`·`convention-style`).

| # | section id | 제목 | 내용·구성 | 하위 앵커/탭 | `SECTION_META` |
|---|---|---|---|---|---|
| 01 | `overview` | 게임 개요 | info-card 4(핵심차별점·세션길이·루프·성장) + 포트폴리오 방향 callout | - | `null` |
| 02 | `mvp` | MVP 현황 | 시스템별 MVP/추가목표 상태 표(badge) + 현재목표 callout | - | `null` |
| 03 | `packages` | 패키지 | pkg-card 6(Input System·Cinemachine·URP·UniTask·R3·AI Navigation) | - | decisions · sources=`Packages/manifest.json` |
| 04 | `arch` | 컨트롤러·캐릭터 분리 (Possession) | 클래스 트리 2(ControllerBase→PlayerController / CharacterBase→PlayerCharacter·MonsterCharacter) + 이동로직 소유 callout | - | decisions · sources=Controller/PlayerController/PlayerCharacter |
| 05 | `ability-system` | AbilitySystem — GAS-like | 선택적 이식 인트로 + 설계의도 callout + **UE GAS 대비 채택/생략 표** + **탭 3개**(탭바 `ability-tabs`). 상세는 아래 | 탭 `attr`(앵커 `attr-system`)·`ge`(앵커 `ge-system`)·`api` | decisions(4) · sources=`Scripts/Core/AbilitySystem/` · synced |
| 06 | `scenes` | 씬 구성 | MainMenu→Lobby→Game→Result 플로우 | - | decisions · sources=`Scenes/` |
| 07 | `camera` | 카메라 | info-card(Projection/FOV·Follow·Body·Aim) + BindingMode 설명 | - | decisions · sources=`Prefabs/Player.prefab` |
| 08 | `input` | 입력 액션맵 | Action/Map/바인딩/타입 표 | - | decisions · sources=`Input/PlayerInputActions.inputactions` |
| 09 | `convention` | 코드 컨벤션 | 네이밍표·스타일 diff·멤버순서 코드블록(아코디언 3) | 앵커 `convention-naming`·`convention-style` | decisions · sources=`.editorconfig` |

**`ability-system` 섹션 내부(가장 복잡 — 탭바 `ability-tabs`):**
- 탭 `attr` (Attribute, 앵커 `attr-system`): 레이어 구조 · BaseValue/CurrentValue 이중 구조 · AttributeHandle Reflection 격리 · SO 초기화 흐름
- 탭 `ge` (GameplayEffect, 앵커 `ge-system`): 에셋/런타임 레이어 분리 · GameplayEffectType(Instant/Infinite/Duration) · Apply 흐름 · ModifierOperation(CurrentValue 공식) · GameplayEffectContext · GameplayEffectExecution(`서브클래스 없음` badge)
- 탭 `api`: ASC 주요 API 표

**메타 렌더링:** `SECTION_META[id]`의 `decisions[]`(결정+이유)·`sources[]`(코드 경로)만 우측 카드로 렌더된다(main.js `metaCard`). `null`이면 카드 숨김. → 결정/소스 수정은 index.html이 아니라 **main.js `SECTION_META`**를 고친다.

**동적 요소(HTML엔 클래스·`data-*`만, 동작은 main.js):** 탭(`.tab-bar`/`.tab-panel`·`data-tabs`/`data-panel`) · 아코디언(`details.accordion`, 탭 전환 시 첫 개만 open) · 코드 복사버튼 · dot-nav · 사이드바(`NAV`에서 생성). SPA 라우팅은 `#<id>` 해시 기반.

---

## 3. 갱신 요청을 받았을 때 — 절차

wiki는 뒤늦게 따라잡는 문서이므로, 갱신은 곧 **이미 확정된 상태를 wiki에 반영하는 작업**이다.

1. **진실의 출처를 먼저 읽는다.** wiki 자체가 아니라 ① 실제 코드, ② dev-docs(`project/architecture/*`, `project/design.md`, `agent/feature/*/progress.md`)가 기준이다. wiki의 기존 서술을 근거로 삼지 않는다.
2. **요청 범위만 최소 변경한다.** 유저가 직접 큐레이션·편집한 문서이므로, 요청받지 않은 섹션·문구·톤·레이아웃을 임의로 손대거나 전면 재작성하지 않는다.
3. 대응 지점(§2 표)을 함께 맞춘다 — 섹션 본문(`index.html`) · 메타(`main.js`) · `sources` 경로.
4. 반영한 범위와, 확정됐지만 이번에 반영하지 않은 부분이 있으면 유저에게 간단히 보고한다.
5. **반영 기준 타임스탬프(동기화 판별):** wiki는 하류 문서다. 각 섹션 메타(`SECTION_META[<id>]`)에 **`반영 기준: <상류 feature-id> @ <ts>`**(`YYYY-MM-DD` KST — 시:분 없이 날짜만)를 두고, 그 섹션을 갱신할 때 그날 날짜로 stamp한다. 상류 feature `progress.md`의 `최종 갱신`보다 오래된 섹션이 **낡은 섹션**(갱신 후보)이다 — 판별 규약은 `dev-docs/agent/HARNESS.md` §3.4. 섹션 id는 상류 feature-id에 매핑한다(예: `ability-system` 섹션 ↔ feature 그룹 `ability-system`).

## 4. 콘텐츠 작성 규약 (갱신 요청을 받았을 때)

### 4.1 `SECTION_META`의 `decisions`
- **"결정 + 이유"** 형식으로 쓴다. 이유 없는 사실 나열 금지. (`CLAUDE.md` 개발 방향과 동일 원칙)
  - 좋음: `"dirty-on-write 전략 — CurrentValue는 Effect 변경 시점에만 재계산, 읽기 비용 0"`
  - 나쁨: `"AttributeSet을 사용한다"`
- 항목은 섹션당 2~4개로 유지한다. 핵심 결정만 남기고 사소한 것은 빼서 포트폴리오 가독성을 지킨다.

### 4.2 `SECTION_META`의 `sources`
- 그 섹션의 근거가 되는 **실제 코드 경로**를 적는다. 경로가 실재하는지 확인 후 기재한다(존재하지 않는 파일 링크 금지).
- 코드 이동/리네임으로 경로가 바뀌면 여기도 함께 고친다.

### 4.3 서술 콘텐츠(`index.html`)
- 외부인이 읽는 문서다. 내부 약어·미설명 용어를 남발하지 않는다.
- 코드 스니펫을 넣을 때는 실제 코드와 일치해야 한다(발췌라도 시그니처·동작이 어긋나면 안 됨).

### 4.4 todo 섹션 (제거됨 — 2026-07-05)
- wiki의 `todo` 섹션은 **2026-07-05 제거**됐다. 작업 추적은 `dev-docs/agent/feature/feature-list.md`·`TODO-BOARD.md`가 담당하며, wiki는 설계 쇼케이스로만 유지한다. **요청 없는 한 todo 섹션을 다시 만들지 않는다.**
- (정리 메모) 제거 후 `src/style.css`의 `.todo-list`·`.todo-group*` 클래스는 미사용(dead)로 남아 있다 — 지우려면 별도 요청 시. `.badge--todo`는 `mvp` 표의 '예정' 뱃지에 계속 쓰이므로 **유지**.

---

## 5. 금지 사항

- ❌ 유저 요청 없이 `wiki/` 편집 (0번 규칙 위반)
- ❌ `index.html` 섹션과 `main.js`의 `SECTION_META` id 불일치 방치
- ❌ `sources`에 실재하지 않는 코드 경로 기재
- ❌ 이유 없는 결정 나열, 또는 섹션당 결정 항목 과다(가독성 저하)
- ❌ 외부 CDN·폰트·스크립트 의존 추가 (정적 단일 페이지 원칙 유지)
