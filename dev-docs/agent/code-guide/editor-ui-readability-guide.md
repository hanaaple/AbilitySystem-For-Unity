# 에디터 UI 가독성 가이드

> Unity 에디터 UI(인스펙터·PropertyDrawer·EditorWindow, IMGUI)의 **가독성**을 높일 때 쓰는 진단표·패턴·구현 주의·적용 절차.
> 근거: 2026-10-16 Tag Query Editor 창 개선 1건(유저: "배열이 깊이 들어가면 가독성이 안좋아") + 웹 조사(디자인 시스템·제품 글 — 연구 결과가 아닌 2차 자료, 아래 '출처').
> **에디터 동작 검증 전(2026-10-16).** '(미확인)' 표시는 Unity 동작 기억에 기댄 것이다 — 에디터에서 확인되면 표시를 지운다.
> 상위 규약은 [`HARNESS.md`](HARNESS.md)(코드 작성 규약), 드로어 일반 규칙은 [`editor-drawer-guide.md`](editor-drawer-guide.md).

---

## 1. 언제 이 문서를 여나

- 에디터 창·인스펙터에 **중첩(트리)·목록·여러 줄짜리 칸**이 있는 UI를 새로 만들 때.
- 유저가 에디터 UI를 "보기 힘들다", "어디 속한 건지 모르겠다"처럼 지적했을 때.
- 기존 드로어에 접기·구분선·묶음을 더할 때.

---

## 2. 진단 — 무엇이 안 읽히는지 먼저 가린다

가독성 불만은 원인이 여럿이다. 유저가 말한 증상에 맞는 행부터 고친다. 증상이 애매하면 묻는다.

| 증상 | 의심 원인 | 패턴 |
|---|---|---|
| 어느 칸이 어느 묶음 소속인지 헷갈림 | 경계·안내선 없이 들여쓰기만으로 깊이를 표현 | 3.1 깊이 안내선, 3.2 묶음 상자 |
| 깊어질수록 라벨이 좁아지거나 잘림 | 깊이당 들여쓰기가 여러 번, 값 열이 고정 x | 3.3 깊이당 한 번 들여쓰기, 3.4 깊이를 따라가는 값 열 |
| 여러 줄짜리 칸이 이어져 경계가 흐림 | 형제 사이 구분 없음 | 3.5 형제 구분선 |
| 내용이 길어 전체가 안 보임 / 접으면 내용을 모름 | 접기 없음 / 접힌 줄에 정보 없음 | 3.6 접기 + 접힌 줄 요약 |
| 추가가 어느 목록에 들어가는지 모호 | 추가 버튼이 묶음 밖 머리 줄에 있음 | 3.7 묶음 안 추가 버튼 |
| 글자가 많아 정작 값이 안 보임 | "Index [0]"·"Expressions (n)" 같은 반복 라벨·머리 줄 | 3.8 라벨 군더더기 줄이기 |

---

## 3. 패턴

각 패턴은 **원칙(출처) → IMGUI 구현** 순이다. 출처가 없는 것은 에이전트 판단(유저 미확정)으로 표시한다.

### 3.1 깊이 안내선
- **원칙.** 깊이마다 세로 안내선을 그어 같은 깊이의 줄을 잇는다. 옅되 밝은·어두운 테마 모두에서 보여야 한다. (Primer TreeView, nesting lines 요청 글)
- **IMGUI.** 본문을 `EditorGUILayout.BeginVertical(style)`로 묶고, 돌려받은 Rect 왼쪽에 `EditorGUI.DrawRect`로 얇은 막대를 그린다. 막대는 위 줄의 접기 화살표 아래에 둔다.
- 깊이별 색 순환(멀리 떨어진 같은 깊이를 색으로 잇기)은 출처 없음 — 에이전트 판단.

### 3.2 묶음 상자
- **원칙.** 중첩 조건의 묶음을 경계로 감싸 관계를 한눈에 보이게 한다. (VMware 쿼리 빌더 개편 글)
- **IMGUI.** 3.1과 같은 Rect에 반투명 회색을 칠한다. 회색 반투명 하나로 두 스킨에 공용이고, 중첩되면 칠이 겹쳐 깊을수록 진해진다.

### 3.3 깊이당 한 번, 일정한 들여쓰기
- **원칙.** 깊이마다 일정하게 들여쓴다(GitLab Pajamas Tree). 깊은 트리는 가로 폭이 모자라므로 앞뒤 장식을 줄인다(Primer).
- **IMGUI.** 들여쓰기는 `EditorGUI.indentLevel` 대신 본문 style의 `padding.left`로만 준다. 묶음 머리 줄처럼 깊이를 한 번 더 먹는 줄은 없앤다.

### 3.4 깊이를 따라가는 값 열
- **문제.** `PrefixLabel`·`PropertyField`는 값 열을 `rect.x + labelWidth`에 둔다. indentLevel로 들여쓰면 라벨 칸만 줄어 깊을수록 좁아진다.
- **IMGUI.** 하위 줄은 짧은 고정 라벨 폭을 쓰고 줄 Rect를 직접 나눈다. 그러면 값 열이 본문 여백만큼 같이 밀린다. 한 줄 아래의 여러 줄짜리 값(태그 상자 등)은 위 줄 값 열에 맞춘다.

### 3.5 형제 구분선
- **근거.** 유저 요청("구분선도 넣고").
- **IMGUI.** 같은 목록의 칸 사이(첫 칸 앞은 없음)에 `EditorGUILayout.GetControlRect(false, 간격)`을 잡고 가운데에 1px `DrawRect`를 긋는다. 3.1·3.2가 있으면 여러 줄짜리 칸이 이어질 때 보조로 쓴다.

### 3.6 접기 + 접힌 줄 요약
- **원칙.** 펼침·접힘 상태를 다르게 보이게 한다(GitLab Pajamas Tree). 접기 자체는 유저 요청("접을 수 있게도").
- 접힌 줄에 내용 요약을 띄우는 것은 출처 없음 — 에이전트 판단.
- **IMGUI.** `EditorGUI.Foldout(rect, expanded, label, true)`. 요약은 폭을 고정한 컨트롤 뒤 남는 자리에 회색 `GUI.Label`(`clipping = TextClipping.Clip`)로 띄우고, 툴팁에 전문을 넣는다. 런타임에 설명 문자열이 이미 있으면 그것을 재사용한다.
- 접힘 상태를 무엇으로 기억할지는 4장 '접힘 상태 키'.

### 3.7 묶음 안 추가 버튼
- **원칙.** 묶음 추가와 항목 추가를 구분된 버튼으로 둔다(sysreview 쿼리 문서).
- 버튼을 묶음 안 끝에 두어 추가 위치를 자리로 드러내는 것은 에이전트 판단.
- **IMGUI.** 목록 끝, 본문 Rect 안에 "+ 항목" 미니 버튼을 둔다.

### 3.8 라벨 군더더기 줄이기
- "Index [0]" → "[0]"처럼 반복되는 접두를 줄이고, 개수만 보여 주는 머리 줄은 없앤다(3.3과 같이). 에이전트 판단.

---

## 4. IMGUI 구현 주의

- **indentLevel 이중 적용.** 라벨 없는 `EditorGUI.Popup(rect, …)` 같은 컨트롤은 indentLevel만큼 rect를 다시 들여쓴다(미확인). 줄 Rect를 직접 나눴다면 indentLevel을 쓰지 않거나 0으로 둔다.
- **전역 `EditorGUIUtility.labelWidth`는 바꾸지 않는다.** 원래 값이 "자동"일 때 읽은 값으로 되돌리면, 창 너비를 따라가지 않는 고정값이 될 수 있다(미확인). 대신 라벨은 직접 그리고, 드로어에는 `GUIContent.none`을 넘긴다. 높이는 `EditorGUI.GetPropertyHeight(prop, GUIContent.none, true)`로 잡는다.
- **접기를 누른 이벤트에서 레이아웃을 바꾸지 않는다.** 칸 수는 Layout 이벤트에서 정해진다. 누른 그 이벤트는 이전 상태로 마저 그리고, `Repaint()`로 다음 그리기에 반영한다(같은 이벤트에서 바꾸면 GUILayout 칸 수가 어긋난다 — 미확인).
  - 칸 추가·삭제·종류 변경 같은 구조 변경은 Apply 뒤 `GUIUtility.ExitGUI()`로 그 GUI를 끝낸다.
- **배경은 Repaint에서, 자식보다 먼저 그린다.** `BeginVertical`이 돌려주는 Rect는 Repaint에서만 유효하다(미확인). 칠이 자식 아래에 깔리도록 `BeginVertical` 바로 뒤에 그린다.
- **접힘 상태 키.**
  - `SerializedProperty.isExpanded`는 기본값이 접힘이다. 편집용 사본을 다시 만들면 상태가 사라진다(미확인).
  - 배열 인덱스 경로(`….Array.data[i]`)를 키로 쓰면 [`editor-drawer-guide.md`](editor-drawer-guide.md)의 '❌ 배열 인덱스 propertyPath를 상태 캐시 키로 사용'에 해당한다.
  - 안정적인 정체성을 먼저 검토한다. 예: `[SerializeReference]`면 `managedReferenceId`.
- **색.** 스킨 분기 대신 반투명 회색 하나로 두 스킨에 공용으로 쓴다. 강조색도 알파를 낮춘다.

---

## 5. 적용 절차

1. 2장 표로 증상과 원인을 가린다. 유저가 말한 증상이 우선이다.
2. 고칠 범위를 정한다. 다른 파일·타입까지 손대야 하면 착수 전에 표면화한다(→ [`HARNESS.md`](HARNESS.md) '설계 원칙').
3. 3장에서 패턴을 고르고, 4장 주의대로 구현한다.
4. 여백·폭·색 농도 같은 수치를 에이전트가 정했으면 worklog에 '유저 미확정'으로 남기고, 보고할 때 조정 여부를 묻는다.
5. 컴파일한 뒤 progress 검증 항목에 에디터 확인 사항을 적는다. 밝은·어두운 스킨, 깊은 중첩, 좁은 창, 접을 때 콘솔 오류.

---

## 6. 적용 사례

- `Assets/Scripts/Core/AbilitySystem/Tag/Editor/GameplayTagQueryEditorWindow.cs` — 3장 전부(2026-10-16, 에디터 검증 전). 경위는 gameplay-tag worklog 2026-10-16의 접기 항목·가독성 개선 항목.
  - 접힘 상태는 배열 경로를 키로 쓰고, 칸을 지울 때 상태를 옮기도록 구현했다. 4장 '접힘 상태 키'와 어긋나며 유저 확인을 기다리는 중이다.

---

## 출처

- [GitHub Primer — TreeView guidelines](https://primer.style/product/components/tree-view/guidelines)
- [GitLab Pajamas — Tree](https://design.gitlab.com/components/tree)
- [VMware — Workspace ONE Intelligence 쿼리 빌더 개편](https://blogs.vmware.com/euc/2022/06/see-the-new-query-builder-user-interface-in-workspace-one-intelligence.html)
- [sysreview — Designing queries](https://sysreview.readthedocs.io/en/latest/tutorials/draft_query.html)
- [Tree vertical nesting lines (기능 요청 글)](https://able.featurebase.app/p/tree-vertical-nesting-lines)
