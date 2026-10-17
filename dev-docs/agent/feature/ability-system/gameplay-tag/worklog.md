# gameplay-tag — 작업 로그 (worklog)

> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다. 시간순 "판단의 이야기"로 쓴다.
> (주의: 아래 기존 두 항목은 오름차순이나, 규약은 '최신이 위로'. 이 항목부터 최신을 위에 둔다.)

## 2026-10-16 — 세션 회고 후 규약 개선 (유저 승인)

- **요청(유저).** 세션 회고의 규약 개선 제안 두 개에 "규약 개선해".
- **반영.**
  - 드로어 가이드 읽는 조건 확장: `code-guide/HARNESS.md` 하위 문서 표와 `editor-drawer-guide.md` 머리말을 "새 드로어 전" → "에디터 GUI 코드(PropertyDrawer·CustomEditor·EditorWindow)를 새로 짜거나 고치기 전(기존 파일 수정 포함)"으로. 계기: Query 편집기 창을 고치며 이 가이드를 안 읽어 배열 인덱스 경로 상태 키 안티패턴을 다시 밟았다.
  - 컴파일 확인 기법: `dev-docs/project/dev-tools.md`에 '컴파일 확인 — Unity 없이' 절(재료·절차 6단계·명령 예·한계). 작성 중 확인한 사실: Unity 응답 파일에는 `-refout:` 줄이 있어 지우지 않으면 Library의 ref DLL을 덮어쓴다. `CLAUDE.md` '빌드·검증'에 한 줄 링크.
- 결정 대기 목록의 ⑦(컴파일 기법 기록)은 해결로 표시(TODO-BOARD·progress·NOW).

## 2026-10-16 — 세션 종료: 다음 할 일 순서 정리

- **요청(유저).** "정확하게 다음에 해야될거 리스트를 정리하고 세션 종료하자", "now.md 말고도 todo list로".
- **정리.** 같은 순서를 세 곳에 맞췄다: NOW.md(로컬) / TODO-BOARD '당장 목표'(gameplay-tag 항목 5개: verify·decision·소유 태그·GE·ASC 연동·태그 후보) / progress `다음 작업` 머리의 순서 줄. 순서: ① 유저 에디터 확인(tests.md) ② 유저 결정 7개 ③ 소유 태그 입구 + CurrentValue 재계산 ④ GE·ASC 태그 연동 ⑤ 태그 후보.

## 2026-10-16 — Modifier 태그 셀프체크 + 유저 확인 문서(tests.md) + 후속 TODO 정리

- **요청(유저).** "태그가 바뀌면 CurrentValue 다시 계산하도록 하는게 맞고, 기존 버그는 TODO로 자세히, UE와 다른 동작도 자세히, 동작을 확인해야 되는 것은 테스트할 수 있는 코드 만들어주고. 내가 확인해야되는거 자세히 문서로 써놔." 이어서 "내 확인이 필요한 부분 나중에 그대로 확인할 거니까 아주 자세히 문서로 써놔."
- **질문 → 답.**
  - 소유 태그 입구: "문서로 자세히 써놔 다음에 하게" → 이번엔 구현하지 않고 progress 다음 작업 3에 UE 구조(위치 포함)와 착수 순서를 적었다. CurrentValue 재계산도 그 계획 안에 넣었다.
  - 테스트 형태: "에디터 메뉴 셀프체크".
  - 테스트 태그: "native, 레지스트리 어셋 둘 다".
- **작성.**
  - `Assets/Scripts/Test/Editor/ModifierTagSelfCheck.cs` — 메뉴 `Tools/Ability System/Run Modifier Tag Self-Check`, 38항목(0 태그 준비 / A 조건 객체 / B aggregator 자격 / C 실제 ASC 적용).
    - 소유 태그가 비어 있어 Target 조건 통과는 aggregator에 태그를 직접 넘겨(B), Source 조건은 Spec의 Source 액터 태그에 직접 넣어(C) 확인한다.
    - C-11~C-13은 D21 트레이드오프(CurrentValue 200 vs 캡처 평가 100)를 "현재 설계"로 기록한다.
    - 헬퍼(GE·modifier 생성, 리플렉션)는 `AggregatorSelfCheck`와 같은 모양으로 복사했다(그 파일을 고치지 않으려고).
  - native 테스트 태그 2개(셀프체크 파일 안), 레지스트리 테스트 태그 `Test.ModifierTag.Registry.Stunned`(`GameplayTagRegistry.asset` 끝 줄).
  - [tests.md](tests.md) 신설 — 이 feature에서 유저가 확인할 것 전부(§1 이번 판정, §2 Query 편집기, §3 Modifier 태그 인스펙터, §4 드로어·피커·관리 창, §5 Tag 코어, §6 정리 대상). progress 검증 항목마다 해당 절을 링크했다.
  - TODO-BOARD: (bug) `UpdateAggregatorMod` — 원인·증상 예 2개·UE 방식(`GameplayEffectAggregator.cpp:503-521`)·수정안·검증안. (code) 주기 실행 magnitude 재계산 — UE 흐름(`GameplayEffect.cpp:3065-3110`)·차이 예·수정안·검증안. aggregator·gameplay-effect progress에서 링크.
- **함께 발견.** `AttributeAggregator.Evaluate` 요약 주석 "마지막 Override 우선"은 틀렸다(코드와 UE `EvaluateWithBase` `:76-84` 모두 먼저 만난 Override). aggregator 소관이라 tests.md §1-7에만 적고 고치지 않았다.
- **컴파일 확인.** 셀프체크를 넣은 에디터 어셈블리를 새 런타임 DLL에 대해 컴파일 → 에러 0, 이 파일 경고 0. 실행은 안 함 → tests.md §1.

## 2026-10-16 — Modifier Source/Target Tags 판정 연결 (UE 호출 위치대로)

- **요청(유저).** "modifier에 gameplaytagRequirements SourceTags, TargetTags 구현 들어가자. ue 호출 경로나 위치 참고하면서".
- **UE 원문 확인(5.6, `GameplayAbilities` 플러그인).**
  - Source 태그: Spec 생성 시 `CaptureDataFromSource` → `RecaptureSourceActorTags`가 컨텍스트로 Instigator 소유 태그를 받는다(`GameplayEffect.cpp:1753-1779`, `GameplayEffectTypes.cpp:368`).
  - Target 태그: 적용(`ApplyGameplayEffectSpec`, `:4153-4154`)과 실행(`ExecuteActiveEffectsFrom`, `:3081-3082`)마다 Owner에서 다시 읽는다.
  - Instant/Periodic: 조건을 못 맞춘 modifier를 건너뛴다(`:3102-3110`, 액터 태그만).
  - 지속형: mod를 조건과 함께 aggregator에 넣고(`:4350`), `Evaluate(평가 파라미터)`가 자격을 판정한다(`GameplayEffectAggregator.cpp:28-74`·`:379-383`).
    - CurrentValue는 빈 파라미터로 평가한다(`GameplayEffect.cpp:3317`·`:3358`).
    - 캡처 평가는 캡처하는 Spec의 합친 태그로 한다(`:983-984`).
- **질문 → 결정.**
  - ASC에 소유 태그가 없어 출처·Execution·Spec 구조를 물었다 → D18(판정 태그 출처=ASC 조회 함수만·빈 값), D19(Execution 캡처 조회=UE처럼 평가 파라미터 인자), D20(Spec 캡처 태그=UE `FTagContainerAggregator` 구조).
  - D14의 CurrentValue 결과를 자세히 설명했다. 그러자 유저가 "Current Value에 대해 GameplayTagRequirements가 정상적으로 작동하지 않는 것은 ue에서도 의도에 맞지 않는다고 해. 적용되도록 해" → D21(지속형 태그 조건을 CurrentValue에도 적용).
- **구현.**
  - 새 파일: `Effect/TagContainerAggregator.cs`(UE `FTagContainerAggregator`), `Effect/AggregatorEvaluateParameters.cs`(UE `FAggregatorEvaluateParameters`, 필터는 세부 TODO 9로).
  - ASC: `GetOwnedGameplayTags`(채우기·읽기 두 형태). 지금은 늘 빈 값.
  - 컨텍스트: `GetOwnedGameplayTags`. Spec: `CapturedSourceTags`·`CapturedTargetTags`, `RecaptureSourceActorTags`, 복제 시 깊은 복사.
  - 컨테이너: 적용·실행 때 Target 태그 캡처(적용은 magnitude 계산 전), Instant/Periodic 건너뛰기, mod 등록 시 조건과 GE Source 액터 태그 전달, CurrentValue는 `EvaluateCurrentValue(소유 태그)`.
  - aggregator: `Mod`에 조건, `Qualifies`, `Evaluate(평가 파라미터)`(UE 경로), `EvaluateCurrentValue`(D21).
  - 캡처 조회·`AttributeBasedMagnitude`·`GameplayEffectExecutionParameters`·`SpeedBoostExecution`·`AbilitySystemInspectorGUI`·`AggregatorSelfCheck`를 새 시그니처로 바꿨다. 다른 feature 수정 사유는 gameplay-effect·aggregator progress에 남겼다.
- **막힌 곳 → 해결.**
  - **D19 범위 정정.** BaseValue 조회는 UE에서 평가 파라미터를 받지 않는다(`GameplayEffectExecutionCalculation.h:85`). 그래서 Magnitude 쪽에만 인자를 더했고, D19 문구를 정정했다.
  - **에디터 어셈블리 검증.** 지난번 방식의 에디터 응답 파일이 Unity가 만든 런타임 ref DLL(옛 API)을 참조해 바뀐 API를 못 봤다. 런타임을 먼저 scratchpad에 컴파일하고, 에디터 응답 파일의 `Assembly-CSharp.ref.dll` 참조를 그 DLL로 바꿔 컴파일했다 → 둘 다 에러 0, 손댄 파일 경고 0.
- **에이전트 판단(유저 미확정).**
  - CurrentValue의 Source 조건은 그 mod를 건 GE의 Source 액터 태그로 판정한다. Instant/Periodic 경로와 같은 데이터다.
  - 태그 변경 시 CurrentValue 재계산은 세부 TODO 3으로 미뤘다(지금은 태그가 바뀔 일이 없음).
  - 판정 헬퍼에서 조건 객체가 null이면 조건 없음으로 본다(UE도 포인터 null을 조건 없음으로 본다).
- **발견(미수정, 유저 확인 대기).**
  - `AttributeAggregator.UpdateAggregatorMod`가 Spec modifier 인덱스를 aggregator `_mods` 인덱스로 쓴다 → 여러 GE의 mod가 섞이면 다른 칸을 덮어쓸 수 있다.
  - 주기 실행 때 UE는 magnitude를 다시 계산하지만 우리는 하지 않는다.
- **동작 미검증.** 소유 태그가 비어 있어 Require 조건은 늘 실패한다. 확인 항목 → progress 다음 작업 11.

## 2026-10-16 — 에디터 UI 가독성 가이드 문서화

- **요청(유저).** "UI 가독성에 대한 부분을 따로 문서로 정리해둬. Skill 처럼 사용할 수 있게 해둬".
- **질문 → 답.** 형태는 "code-guide 문서만"(Claude Code 스킬은 만들지 않음), 범위는 "Unity 에디터(IMGUI)만".
- **작성.** `dev-docs/agent/code-guide/editor-ui-readability-guide.md`를 새로 만들었다. 스킬처럼 쓰도록 언제 여나 → 진단표 → 패턴 → IMGUI 주의 → 적용 절차 순으로 썼다. `code-guide/HARNESS.md` 하위 문서 표에 한 줄 등록했다.
  - 패턴마다 출처를 달았다. 출처 없는 것은 에이전트 판단으로, Unity 동작 기억에 기댄 주의점은 '(미확인)'으로 표시했다.
- **작성 중 발견.** 기존 `editor-drawer-guide.md`에 '❌ 배열 인덱스 propertyPath를 상태 캐시 키로 사용'이 있다. 앞 항목의 접힘 상태가 바로 이 방식(경로 키 + 삭제 시 옮기기)이다. 창 안에서의 삭제는 옮기지만, Undo로 구조가 되돌아가면 다른 칸이 접힐 수 있다(영향은 접힘 표시뿐). 대안은 `managedReferenceId` 키(사본을 다시 만들면 상태가 사라짐). 유저에게 물음.

## 2026-10-16 — Tag Query Editor 창: 가독성 개선(본문 묶음·안내선·설명 요약)

- **요청(유저).** 접기·구분선 뒤 "여전히 가독성이 부족해. 가독성을 높여봐. UI에 대한 가독성을 웹에 검색해도 좋아". 방식은 에이전트에게 맡겼다.
- **웹 조사(참고한 패턴).**
  - 트리 뷰의 깊이별 안내선: GitHub Primer TreeView 가이드라인. 들여쓰기는 깊이마다 일정하게, 펼침·접힘 상태는 다르게 보이게: GitLab Pajamas Tree.
  - 중첩 조건의 묶음을 괄호·경계로 감싸 관계를 한눈에: VMware Workspace ONE Intelligence 쿼리 빌더 개편 글.
  - 묶음 안에 추가 버튼을 따로 두기: sysreview 쿼리 작성 문서(+Group/+Rule).
  - 안내선은 두 테마(밝음·어두움) 모두에서 옅지만 보이게: tree vertical nesting lines 기능 요청 글.
- **구현(`GameplayTagQueryEditorWindow`만 수정).**
  - **본문 묶음.** 식의 내용(Tags나 하위 식 목록)을 옅은 회색 칠 상자에 넣었다. 왼쪽에 깊이별 색 안내선(파랑·초록·주황·보라 순환)을 접기 화살표 아래로 그었다. 칠은 겹칠수록 진해져 깊이가 드러난다.
  - **들여쓰기를 깊이당 한 번으로.** "Expressions (n)" 머리 줄을 없앴다. 들여쓰기는 `indentLevel` 대신 본문 여백(16px)으로만 준다.
  - **라벨 축소.** "Index [i]" → "[i]". 하위 식 줄의 라벨 칸을 56px로 줄였다. 팝업이 깊이를 따라 오른쪽으로 옮겨 가므로, 깊어도 라벨이 찌그러지지 않는다. 루트 줄만 User Description과 같은 라벨 폭을 쓴다.
  - **추가 버튼.** "+ Expression" 버튼을 목록 끝, 본문 안에 둔다.
  - **접힌 줄 요약.** 종류 팝업을 170px로 고정하고, 접힌 식은 남는 자리에 설명(` ALL( ANY( A ), NONE( B ) )` 꼴, 질의 자동 설명과 같은 문구)을 흐리게 띄운다. 툴팁은 전체 문구다.
  - **태그 상자.** 위 줄의 종류 팝업과 같은 열에서 시작한다. 전역 `labelWidth`를 건드리지 않으려고 "Tags" 라벨은 직접 그리고, 컨테이너 드로어엔 빈 라벨을 넘긴다.
  - 칸 사이 구분선은 유지했다.
- **에이전트 판단(유저 미확정).** 위 구현의 수치 전부: 색 4개, 칠 6%, 여백 16px, 라벨 56px, 팝업 170px. 그리고 요약을 접혔을 때만 보이는 것.
- **컴파일 확인 / 동작 미검증.** 같은 방법으로 에디터 어셈블리를 컴파일했다 → 에러 0, 이 파일 경고 0. `SerializedProperty.managedReferenceValue` 읽기가 Unity 6에서 컴파일되는 것도 확인했다. 에디터 동작은 progress 다음 작업 12의 7.

## 2026-10-16 — Tag Query Editor 창: 식 접기 + 칸 사이 구분선

- **요청(유저).** "Tag Query Editor 배열이 깊이 들어가면 가독성이 안좋아. 접을 수 있게도 하고 구분선도 넣고 해줘".
- **구현(`GameplayTagQueryEditorWindow`만 수정).**
  - 종류를 고른 식 줄(Root Expression·Index [i])에 접기 화살표를 달았다. 접으면 그 아래 Tags나 하위 식 목록을 숨긴다. 종류가 None인 줄은 화살표가 없고, 글자 시작만 화살표 있는 줄에 맞춘다.
  - 같은 목록의 칸 사이에 가로줄을 긋는다. 줄은 그 칸의 들여쓰기에서 시작해서, 줄 시작 위치로 깊이를 구분할 수 있다.
  - 이를 위해 식 줄을 `GUILayout` 가로 묶음에서 한 줄 Rect를 직접 나누는 방식(라벨·팝업·− 버튼)으로 바꿨다.
- **막힌 곳 → 해결.**
  - **접은 상태 저장 위치.** `SerializedProperty.isExpanded`는 기본이 접힘이다. 게다가 사본을 다시 만들면(Undo·인스펙터 Clear) 새 오브젝트라 상태가 사라진다. 그래서 창이 "접은 식의 속성 경로" 집합을 들고 있다. 경로는 사본을 다시 만들어도 같다.
  - 대신 칸을 지우면 뒤 칸의 경로 인덱스가 당겨지므로, 접힘 상태도 같이 옮기고 지운 칸의 상태는 버린다. 종류를 바꾸면 새 노드라 그 칸과 하위 식의 상태를 버린다.
  - **접은 순간의 레이아웃.** 접기를 누른 이벤트에서 바로 칸 수를 바꾸면 레이아웃 단계에서 잡은 칸 수와 어긋난다. 그래서 그 이벤트는 이전 상태로 마저 그리고 `Repaint`로 다음 그리기부터 반영한다.
- **에이전트 판단(유저 미확정).**
  - 처음 열 때와 새로 고른 식은 펼친 상태다.
  - 접힘 상태는 창을 닫으면 사라진다.
  - "Expressions (n)" 머리 줄은 따로 접지 않는다(식 줄을 접으면 함께 숨는다).
  - 구분선은 같은 목록의 칸 사이에만 긋는다(User Description과 Root 사이, 머리 줄 아래는 없음).
  - 색은 반투명 회색이다(두 스킨 공용).
- **컴파일 확인 / 동작 미검증.** 위 Query 편집기 항목과 같은 방법(Unity 응답 파일 + 동봉 Roslyn)으로 에디터 어셈블리를 컴파일했다 → 에러 0, 이 파일 경고 0. 에디터 동작은 progress 다음 작업 12의 7.

## 2026-10-16 — Query 편집기(인스펙터 줄 + Tag Query Editor 창) + 자동 설명

- **요청(유저).** "이제 TagQuery Editor 구현해봐".
- **UE 원문 확인(5.6, `Plugins/Editor/GameplayTagsEditor`·`Runtime/GameplayTags`).** 인스펙터 줄은 `SGameplayTagQueryEntryBox`가 그린다(설명 버튼·Edit·Clear). 창은 `SGameplayTagQueryWidget`이다. 편집용 UObject(`UEditableGameplayTagQuery`, 식 6종)를 디테일 뷰로 보여 준다. 고치면 즉시 반영하고, Cancel은 원래 값으로 되돌린다(`SGameplayTagQueryWidget.cpp:146-215`). 창은 하나만 열고, 크기 600×400, 줄 왼쪽에 띄운다. 자동 설명은 편집 경로(`BuildFromEditableQuery`)에서만 만든다(`GameplayTagContainer.cpp:1980-2190`).
- **질문 → 결정.** 자동 설명·식 종류·편집 형태·Categories를 물었다. 각 선택지에 고치게 될 기존 파일을 적었다. 결과 → D16(Query 자동 설명=UE처럼 저장·Build 때 생성), D17(Query 편집기=UE처럼 별도 창·8종·Categories 전달).
- **구현.**
  - 런타임: `GameplayTagQuery`에 `_autoDescription`·`GetDescription` 대체·`Equals`(두 설명 비교)·`Build` 설명 생성을 넣었다. `GameplayTagQueryExpression.EmitTokens`에 `StringBuilder description` 인자를 더했다.
  - 에디터 새 파일: `EditableGameplayTagQuery`(ScriptableObject, DontSave)·`EditableGameplayTagQueryExpression`(식 종류 enum + 태그 컨테이너 + `[SerializeReference]` 하위 식)·`GameplayTagQueryEditorWindow`.
  - 에디터 수정: `GameplayTagQueryPropertyDrawer`(엔트리 박스), `GameplayTagDrawerUtility`(Categories 덮어쓰기 입구).
- **막힌 곳 → 해결.**
  - **사용자 설명 읽기.** `GameplayTagQuery`엔 사용자 설명만 읽는 공개 함수가 없다(UE도 private). 런타임 API를 늘리지 않으려고 창이 직렬화 필드 `_userDescription`을 직접 읽는다. 이 상호 의존을 `GameplayTagQuery`에 주석으로 적었다.
  - **변경 감지.** 처음엔 질의 값 비교로 짰다. 그런데 태그 비교는 인터닝된 이름의 참조 비교다(`InternedName.cs:23`). `boxedValue`로 읽은 값에도 인터닝 콜백이 도는지는 확인할 수 없었다. 돌지 않으면 열기만 해도 매 프레임 사본을 다시 만들 수 있어서 `EditorUtility.GetDirtyCount`로 바꿨다(대상·사본 각각). Cancel 복원은 "이 창이 한 번이라도 썼는가" 플래그로 정한다.
- **에이전트 판단(유저 미확정).**
  - Exact 설명 문구 `" ANY EXACT("`·`" ALL EXACT("`.
  - 편집 노드는 클래스 하나 + 식 종류 enum이다(UE는 종류마다 클래스). 종류를 바꾸면 노드를 새로 만든다(UE와 같음).
  - 읽기 전용 창은 넣지 않았다.
  - Edit·Clear는 아이콘 대신 글자(`Edit`·`×`)다.
  - 창 위치는 왼쪽·위 화면 끝만 막는다.
  - 즉시 반영 쓰기는 Undo 그룹을 새로 열지 않고 사본 변경과 묶는다.
- **컴파일 확인 / 동작 미검증.**
  - Unity의 마지막 자동 컴파일(18:45)엔 새 에디터 파일 일부만 들어갔다.
  - 그래서 Unity가 남긴 컴파일러 응답 파일(`Library/Bee/artifacts/<dag>/Assembly-CSharp(-Editor).rsp`)에서 출력 경로만 scratchpad로 바꾸고 빠진 새 파일을 더했다. 이걸 Unity 동봉 Roslyn(`Editor/Data/DotNetSdkRoslyn/csc.dll`)으로 런타임·에디터 어셈블리 둘 다 컴파일했다 → 에러 0, 우리 파일 경고 0.
  - 에디터 동작은 실행 안 함 → progress 다음 작업 12.
  - D15 트리거(`TokenStreamVersion`)는 유저에게 아직 안 물었다.

## 2026-10-16 — Modifier Source/Target Tags 필드 + `GameplayTagRequirements` + Query 직렬화

- **요청(유저).** "Modifier에 Source Tags와 Target Tags 구현해보자. 이번에 한거로". 에이전트는 "이번에 한거"를 이번에 만든 컨테이너·드로어로 읽었다(유저 정정 없음).
- **UE 원문 확인(5.6).** `FGameplayModifierInfo::SourceTags/TargetTags`=`FGameplayTagRequirements`(`GameplayEffect.h:566-570`). 판정은 Instant/Periodic이 실행 시 건너뛰기(`GameplayEffect.cpp:2950-2980`, cvar `AbilitySystem.Fix.UseModTagReqsOnAllGE` 기본 true·5.4 수정), 지속형이 aggregator 자격 판정(`GameplayEffectAggregator.cpp:28-74`)이다. CurrentValue는 빈 태그로 평가한다(`GameplayEffect.cpp:3311-3315`). Source 태그는 Spec 생성 때 Instigator에서 캡처한다(`:1776-1779`).
- **질문 → 결정.** 소유 태그 출처·지속형 판정·요구 구성을 물었다. 첫 답 "UE에 따를건데 일단, Editor쪽 모양새 채우는 멤버 변수만 채워둬"가 두 갈래(ASC 멤버 / Modifier 필드만)로 읽혀 되물었다. 결과 → D13(Modifier Source/Target Tags=UE Requirements 전체, 이번엔 판정 미연결), D14(지속형 판정=UE 그대로, 다음 단계), D15(Query=직렬화+판정, 편집 UI 나중).
- **구현.**
  - `Effect/GameplayTagRequirements.cs` 신설 — UE 함수 전부(`RequirementsMet`·`IsEmpty`·`ToString`·`Equals`(operator==)·`ConvertTagFieldsToTagQuery`).
  - `GameplayTagQuery` — `[Serializable]`, 세 필드 `[SerializeField]`, 동등 비교(`Equals`·`==`·`!=`, 빈 목록=null). Requirements의 `operator==`가 Query 비교를 요구해 추가했다.
  - `GameplayModifier` — `sourceTags`·`targetTags` + 읽기 프로퍼티, 판정 미연결 TODO.
  - `GameplayEffectAssetDrawer` — Magnitude 아래에 Source/Target Tags를 그린다. 하위 필드는 컨테이너·Query 드로어에 맡긴다.
  - `Tag/Editor/GameplayTagQueryPropertyDrawer.cs` 신설 — 읽기 전용 설명 한 줄.
- **에이전트 판단(유저 미확정).**
  - `GameplayTagRequirements`를 UE의 struct가 아니라 class로 했다. 필드 초기화(`= new()`)가 코드 생성 경로에서 돌아 컨테이너가 null이 되지 않는다. D7(Container=class)·`AttributeBasedMagnitude`와 같은 선택이다.
  - 위치는 `Effect/`(namespace `Core.AbilitySystem.Effect`)로 했다. UE가 GameplayTags 모듈이 아니라 GameplayAbilities의 `GameplayEffectTypes.h`에 둬서다.
  - 필드 이름은 새 class는 규약대로 `_requireTags`, `GameplayModifier`는 주변 필드에 맞춰 `sourceTags`(밑줄 없음)로 했다.
  - 인스펙터 라벨은 필드 이름 그대로(Require Tags·Ignore Tags·Tag Query)다. UE는 DisplayName(Must Have Tags·Must Not Have Tags·Query Must Match)을 쓴다.
  - 드로어의 태그 영역은 요소 헤더 foldout처럼 10px 밀고 라벨 폭을 그만큼 줄였다(값 열 정렬).
- **⚠ 미검증.** Unity 컴파일·실행 안 함. 새 `.cs` 2개의 `.meta`는 Unity가 만든다.
- **유저 지적 → 유지.** 유저: "GameplayTagQuery는 뭘 변경한거야 GameplayTagRequirements만 변경하기로 한거 아냐?". 직렬화는 D15 선택지 설명에 적었지만 라벨("필드+판정, 편집 UI는 나중")엔 Query 타입을 고친다는 게 드러나지 않았다. Query 동등 비교는 묻지 않고 넣은 것이다(Requirements `operator==` 때문). 되돌릴 범위를 묻자 유저는 "그대로 둠"을 골랐다.

## 2026-10-15 — GE·ASC 태그 연동 요청 → 구조·범위 질문 대기

- **요청(유저).** "GE, AbilitySystemCopmponent에 Tag 넣어봐 ue에 맞춰서". progress 결정 대기 2(GE 연동 범위)에 해당한다. 범위와 구조가 갈려 착수 전에 물었다. 코드 변경 없음.
- **UE 원문 확인(5.6, `Plugins/Runtime/GameplayAbilities`).**
  - GE 태그 설정은 5.3부터 GE Component로 옮겨졌다. `UGameplayEffect::GEComponents`(`GameplayEffect.h:2423`)에 컴포넌트를 붙이고, 본체 태그 필드는 deprecated다(`:2311-2365`).
  - 태그 관련 컴포넌트: `AssetTags`·`TargetTags`(부여 태그)·`TargetTagRequirements`(Application·Ongoing·Removal, `TargetTagRequirementsGameplayEffectComponent.h:48-57`)·`RemoveOther`(`RemoveGameplayEffectQueries`)·`Immunity`(`ImmunityQueries`)·`BlockAbilityTags`.
  - 컴포넌트 훅은 `CanGameplayEffectApply`·`OnActiveGameplayEffectAdded`·`OnGameplayEffectApplied/Executed`·`OnGameplayEffectChanged`(`GameplayEffectComponent.h:47-74`).
  - 컴포넌트는 GE의 캐시(`CachedAssetTags`·`CachedGrantedTags`)만 채운다(`TargetTagsGameplayEffectComponent.cpp:73`, `AssetTagsGameplayEffectComponent.cpp:63`). 대상에 태그를 붙이고 떼는 건 활성 GE 컨테이너다(`GameplayEffect.cpp:4373-4374`·`4670-4671`, 정의의 부여 태그 + Spec `DynamicGrantedTags`).
  - Modifiers·Executions·Duration·Period·Stacking은 본체에 남았다. 원문 주석: 컴포넌트는 에셋당 하나라 인스턴스별 런타임 상태를 둘 수 없어 일부 기능이 본체에 남았다(`GameplayEffectComponent.h:23-27`). 도입 이유는 "GEComponents are what define how a GameplayEffect behaves" 외에 적혀 있지 않다.
  - `FGameplayTagRequirements` = `RequireTags`·`IgnoreTags`·`TagQuery`(`GameplayEffectTypes.h:1426-1440`).
  - ASC: `FGameplayTagCountContainer GameplayTagCountContainer`(`AbilitySystemComponent.h:1906`), 조회(`HasMatchingGameplayTag`·`HasAll/AnyMatchingGameplayTags`·`GetOwnedGameplayTags`·`GetTagCount`), `UpdateTagMap`, Loose 태그 API, `RegisterGameplayTagEvent`·`RegisterGenericGameplayTagEvent`, 태그로 GE 조회·제거(`:576-870`). `BlockedAbilityTags`(`:1892`)는 GA 범위.
- **현재 코드(확인).** `GameplayEffectAsset`은 평면 필드(modifiers·executions)이고 태그 필드가 없다. ASC에도 태그가 없다.
- **유저에게 물은 것(답 전).** ① GE 구조: 평면 필드 vs GE Component 도입(에이전트 권장: 평면 필드 — 기존 에셋이 평면이라 변경이 작음, 대신 데이터 배치는 5.2 이전 UE) ② GE 범위: Asset·Granted 태그 / 적용·제거 요구 / 유지 요구 / 다른 GE 제거·면역 ③ ASC 범위: 카운트 컨테이너+조회 / Loose 태그 / 태그 변경 이벤트 / 태그로 GE 조회·제거 ④ 요구 조건의 TagQuery 포함 여부(에이전트 권장: 지금은 뺌 — Query 미검증·편집기 없음). 유저는 ①에서 "GE Component가 뭔데"를 물었고 위 원문으로 설명했다. 답은 다음 세션.
- **같은 날 다른 문답.** 에디터에서 추가한 태그가 레지스트리 에셋 `_tags`에 저장되는지 → 코드로 확인(저장됨). `[GameplayTagCategories]`가 UE에선 전용 attribute가 아니라 `UPROPERTY(meta = (Categories = ...))`라는 점 설명 → `[Categories]`로 이름을 바꿀지 제안, 답 없음.

## 2026-10-15 — GameplayTag·Container 드로어 + 피커 + 태그 관리 (UE 에디터 동작 미러)

- **요청(유저).** "GameplayTag, TagContainer Inspector Drawer가 필요해. 그것도 어느 클래스나 struct의 Field로 들어가도 작동 가능하게. 기능은 최대한 UE와 유사하게".
- **UE 원문 확인(5.6, `Plugins/Editor/GameplayTagsEditor`).** 지난 항목의 "UE 모양(기억 기반)"을 원문으로 대체했다. `GameplayTagCustomization`·`GameplayTagContainerCustomization`은 헤더 행에 콤보만 둔다. `SGameplayTagCombo`는 칩·×·우클릭(Copy/Paste/Clear), `SGameplayTagContainerCombo`는 칩 목록·Empty·이름순·오브젝트별 개수를 맡는다. `SGameplayTagPicker`는 검색·트리·체크 규칙(`SGameplayTagPicker.cpp:784-879`)·펼침 저장·Expand/Collapse·행 메뉴(`:1098-1210`)·열 때 미등록 태그 제거(`:1735-1779`)를 맡는다. 태그 추가·삭제·이름 변경은 `SAddNewGameplayTagWidget`·`GameplayTagsEditorModule`(`AddNewGameplayTagToINI`·`DeleteTagFromINI`·`RenameTagInINI`)에 있다. 중간 부모 노드도 자손의 출처 이름을 받는다(`GameplayTagsManager.cpp` `InsertTagIntoNodeArray` 끝의 `SourceNames.AddUnique`).
- **범위 질문 → 결정.** 범위가 갈리는 네 가지를 물었다. 루트 입구(→D11 피커 루트 입구=UE `GetFilteredGameplayRootTags`), Categories·다중 편집(→D10 태그 드로어=UE 동작 최대 미러), 태그 관리(→D12 태그 관리 포함=레지스트리 편집). 유저는 넷 다 "포함/UE처럼"을 골랐다. 태그 관리는 에이전트 권장(이번엔 제외)과 반대.
- **런타임 변경.**
  - 매니저 `IsValidGameplayTagString`을 구현했다(D9 빈 시그니처 → UE 규칙: 빈 문자열, 앞뒤 `.`·공백, 금지 문자 `"',\r\n\t`). 우리만 `..`(빈 구간)을 거부한다. `GameplayTag.IsValidGameplayTagString`은 UE처럼 매니저에 위임한다.
  - 에디터 전용으로 `GetFilteredGameplayRootTags`·`EditorRefreshGameplayTagTree`·`OnEditorRefreshGameplayTagTree`를 추가했다.
  - `GameplayTagCategoriesAttribute`를 신설했다. 일반 `Attribute`라 타입 드로어가 그대로 선택되고, 드로어가 `fieldInfo`로 읽는다. 그래서 List 필드에 붙이면 요소 전체에 적용된다.
  - 에디터가 직렬화 필드 이름으로 접근하는 세 곳(`GameplayTag.name`·`GameplayTagContainer._gameplayTags`·`GameplayTagRegistryAsset._tags`)에 상호 의존 주석을 달았다.
- **에디터 구현(`Tag/Editor/`).**
  - `GameplayTagPropertyDrawer`·`GameplayTagContainerPropertyDrawer` — 타입 드로어라 어느 class·struct 필드, List 요소에도 붙는다.
  - `GameplayTagPropertyHandle` — UE PropertyHandle 역할. 대상 오브젝트와 경로만 들고 오브젝트별 `SerializedObject`로 읽고 쓴다. 팝업·메뉴 콜백 시점엔 원래 `SerializedProperty`가 무효일 수 있어서다(editor-drawer-guide '콜백 안에서는 재조회'). 쓰기는 선택된 오브젝트 전체를 Undo 한 번으로 묶는다.
  - `GameplayTagPicker` — IMGUI `TreeView<int>` + `SearchField`. 필드 팝업(`GameplayTagPickerPopup`, 선택 모드)과 관리 창(`GameplayTagManagerWindow`, 관리 모드)이 같은 피커를 담는다. Unity 6000.3에 제네릭 `TreeView<TIdentifier>`가 있음은 설치본 XML·DLL로 확인했다.
  - `GameplayTagChip`·`GameplayTagRenameWindow`·`GameplayTagRegistryEditing`(레지스트리 편집·출처 조회)·`GameplayTagEditorUtility`(클립보드·정렬·메뉴)·`GameplayTagDrawerUtility`.
  - 레지스트리 인스펙터에 "Manage Gameplay Tags..." 버튼을 넣었다(목록 직접 편집은 계속 막음).
- **재사용하지 않은 것.** editor-drawer-guide의 `SubclassAdvancedDropdown`(AdvancedDropdown)은 단일 선택이고 고르면 닫히며 체크박스가 없다. UE 피커(다중 체크·미정 상태·트리 펼침 유지)에 맞지 않아 쓰지 않았다.
- **UE와 다르게 한 것(에이전트 판단, 유저 미확정).** 칩·콤보 모양은 Slate 스타일 대신 IMGUI 기본 스타일(`miniButton`·`helpBox`)로 그렸다. 행을 누르면 체크박스+이름 영역만 토글된다(UE는 행 전체). 실패 알림은 토스트 대신 피커 안 HelpBox로 띄운다. 펼침 상태는 UE 프로젝트별 ini 대신 EditorPrefs에 저장하고, 키에 프로젝트 경로를 넣었다. 정렬은 `EditorUtility.NaturalCompare`로 했다(UE `CompareWithNumericSuffix`와 같은 결과인지 미확인). 태그 비교는 D3(대소문자=무시비교+표기보존) 미구현이라 대소문자를 구분한다.
- **테스트 에셋 확장 → 제거.** `GameplayTagTestAsset`에 중첩 struct·class·struct List·Categories 필드를 더했다("어느 class·struct 필드든" 확인용, 요청 없이 추가). 같은 날 유저 요청("테스트용 코드는 지우자" → 범위 질문에 "오늘 추가한 필드만")으로 지웠다. 지운 이유는 유저 미언급. 테스트 에셋은 `_tag`·`_tagList`·`_tagContainer`로 돌아갔다.
- **⚠ 미검증.** Unity 컴파일·실행 안 함(이번 세션 Unity MCP 연결 실패). 위험 지점: ① 팝업 안에서 GenericMenu(행 ▾·우클릭·설정)를 열면 팝업이 포커스를 잃고 닫힐 수 있다 ② `PopupWindow.Show` 직전의 미등록 태그 경고 대화창 뒤에 팝업이 정상으로 뜨는지 ③ TreeView 행 안 `ToggleLeft` 클릭이 행 선택과 겹치지 않는지.

## 2026-10-15 — 드로어 준비: 테스트 SO + UE 에디터 모양 정리

- **테스트 에셋.** `Assets/Scripts/Test/GameplayTagTestAsset.cs`(`[CreateAssetMenu("Test/Gameplay Tag Test")]`, 필드 `_tag`·`_tagList`·`_tagContainer` — 단일·리스트 요소·컨테이너에서 드로어가 다르게 그려질 수 있어 셋 다). 에셋 `Assets/_Test/GameplayTagTest.asset`은 Unity MCP 연결 실패로 YAML 수기 작성(스크립트 `.meta` GUID 사용, 값 비움 — 레지스트리에 태그가 없어 이름을 넣으면 미등록 태그). 폴더 위치는 에이전트가 정함(`_PlayModeTest`는 Aggregator 셋업 전용이라 분리).
- **UE 에디터 모양(유저: "코드로 보지 말고").** 에이전트 기억으로 정리 — 단일 태그는 콤보 + 검색·계층 트리 피커, 컨테이너는 선택 목록 + 체크박스 다중 선택, 태그 관리는 Project Settings·관리 창, 필드별 하위 트리 필터(meta `Categories`), Query는 별도 편집 창. **원문·스크린샷 미확인.**
- **범위(유저).** 1·2(단일·컨테이너 드로어)가 필요하고, 어느 class·struct 필드에 들어가도 동작해야 함. 다음 세션 착수 → progress 다음 작업 9.

## 2026-10-15 — 레지스트리 인스펙터 읽기 전용

- **요청(유저).** Editor 쪽 구현 논의 시작. "GameplayTagRegistry 쪽은 우선 직접 값 변경은 못하게 하고 readonly로". 막는 이유는 유저 미언급(근거 미기록).
- **변경.** `Tag/Editor/GameplayTagRegistryAssetEditor.cs` 신설 — `[CustomEditor(typeof(GameplayTagRegistryAsset))]`, 태그 수 + 각 태그를 `DisabledScope` 안의 `TextField`로 표시. 네임스페이스 `...Tag.Editor`가 `Editor` 이름을 가리므로 베이스는 `UnityEditor.Editor`로 명시.
- **한계.** 막는 건 기본 인스펙터 편집뿐. Debug 인스펙터 모드·`.asset` YAML 직접 편집·코드(`SerializedObject`)는 그대로 가능.
- **결과.** 지금은 레지스트리에 태그를 넣는 경로가 없다 — 편집 도구가 생길 때까지 태그 정의는 native(→D6 native=코드 정의+저장 안 함)뿐.
- **⚠ 미검증.** Unity 컴파일 안 함.

## 2026-10-15 — UE 5.6 대조 후 쓸모 있는 함수 빈 시그니처 추가

- **계기(유저).** "UE 피처는 다 가져온건가" → 에이전트가 UE 5.6 GameplayTags 모듈 헤더와 우리 public API 이름을 대조(정규식 추출, 이름만 — 동작·여러 줄 선언 누락 가능). 핵심(식별·매칭·컨테이너·Query·노드 트리)은 거의 일치, 빠진 건 검증·전체 조회·분할·native 완료 콜백·정렬과 모듈 부가 기능(태그 소스·리다이렉터·restricted·`IGameplayTagAssetInterface`).
- **결정(→D9 쓸모 있는 UE 함수=빈 시그니처).** 유저: "쓸모있는건 빈 함수로 시그니처만". `*ExactMatch`는 UE 5.6 이후 버전에 있어 유지(유저 진술, 원문 미확인).
- **변경.** `GameplayTag`: `operator <`/`>`·`IsValidGameplayTagString`(2 오버로드). 매니저: `IsValidGameplayTagString`(2)·`DoneAddingNativeTags`·`CallOrRegister_OnDoneAddingNativeTagsDelegate(Action)`·`SplitGameplayTagFName`·`RequestAllGameplayTags`. 본문은 `NotImplementedException`(인자 없는 오버로드와 `>`는 위임).
- **정정.** 처음엔 `RemoveTagByExplicitName`도 쓸모 있다고 제시했으나, 원문 확인 결과 protected·리다이렉터 전용(`GameplayTagContainer.h:684-690`)이라 제외.
- **⚠ 미검증.** Unity 컴파일 안 함.

## 2026-10-15 — GameplayTag.EmptyTag 추가

- **요청(유저).** UE `GetCompleteTag`가 `FGameplayTag::EmptyTag`를 쓰는 걸 보고 "우리도 그런 식으로".
- **변경.** `public static readonly GameplayTag EmptyTag`(UE `GameplayTagContainer.h:224`). `GameplayTagNode.GetCompleteTag`를 UE 형태(`Num() > 0 ? [0] : EmptyTag`, `GameplayTagsManager.h:193`)로. UE 모듈에서 `EmptyTag`를 쓰는 곳은 여기뿐이라 다른 `default` 반환(`First`·`GetByIndex`·`RequestGameplayTag` 등, UE도 `FGameplayTag()`)은 그대로 둔다.
- **참고.** UE의 static은 `const FGameplayTag&` 반환이 가리킬 수명 긴 객체가 필요해서다. C#은 값 반환이라 `default`와 동작 차이는 없다.

## 2026-10-15 — InternedName 해시 캐시

- **계기(유저).** "C# 특성상 비효율적일 게 꽤 있어 보이는데" → 에이전트가 비효율 후보를 정리(해시 재계산·`GameplayTagsMatchDepth` List 할당·`IReadOnlyList` foreach 박싱·`List.Contains` 비교자·컨테이너 반환 할당). 그중 해시 캐시를 유저가 적용 결정.
- **이유.** C# `string`은 해시를 캐시하지 않아 `FindTagNode`(Dictionary 조회)마다 태그 문자열 전체를 다시 해시한다. UE `FName`은 정수 인덱스라 이 비용이 없다.
- **변경.** `InternedName`에 `[NonSerialized] _hash` 추가. 생성자와 `OnAfterDeserialize`(생성자 우회 경로)에서 계산하고, `GetHashCode()`는 그 값을 반환. null은 0(`default`와 일치). `string.GetHashCode`는 런타임·플랫폼마다 다를 수 있어 저장하지 않는다.
- **감수.** 크기 8→16바이트(64비트). `GameplayTag`·컨테이너 List·질의 태그 목록도 같이 커진다.
- **남은 후보(미적용).** `GameplayTagsMatchDepth` List 할당(UE는 `TInlineAllocator<32>`, `GameplayTagsManager.cpp:2640-2670`), 매니저 `:90`·`:283`의 `IReadOnlyList` foreach 박싱.
- **⚠ 미검증.** Unity 컴파일·실행 안 함.

## 2026-10-14 — MatchesTag 컨테이너 우회 시도 → 되돌림

- **요청(유저).** `MatchesTag`에서 "TagContainer로 가지 말고 할 수 있게 해봐, 캐시히트 높이게".
- **시도.** 노드에 자기 태그+부모를 담은 평탄 배열을 두고 `GameplayTagNode.IsSelfOrParent`가 그 배열만 훑게 했다(UE는 `TagNode->GetSingleTagContainer().HasTag(...)`, `GameplayTagContainer.cpp:1160-1176`).
- **되돌림(유저).** "dictionary 대신 array로 순차탐색하네 … 그냥 원래대로 가자" → UE처럼 컨테이너 경유로 복귀. 참고: 원래 컨테이너 `HasTag`도 해시가 아닌 `List.Contains` 순차 탐색이다.

## 2026-10-14 — 구현된 Tag API를 UE 이름·시그니처에 정렬

- **계기(유저).** "함수명이나 그런 거까지 따라가려는데 그게 되려 나를 붙잡는 거 같아" → 에이전트가 "이름 맞추기(비용 거의 없음)와 UE 함수 전부 채우기(비용 누적)는 다르다"고 구분해 제시 → 유저: "지금 구현해둔 거 UE 함수에 맞춰서, 안 되면 빈 껍데기로, 특히 기존 함수는 매칭되게". 범위는 "기존 있는 함수들만"(UE 전용 함수 80개 이상은 추가 안 함).
- **결정(→D8 기존 API=UE 이름·시그니처).** 껍데기는 `NotImplementedException`(유저 선택).
- **변경.** `GameplayTagsManager.Instance` → `Get()`, `GameplayTag.TagName` → `GetTagName()`, `NativeGameplayTag.GameplayTag` → `GetTag()`, `MakeQueryMatch*` → `MakeQuery_Match*`, `GetQueryExpr()` → `GetQueryExpr(out)`(UE `Read`가 식을 새로 채우므로 out이 같은 의미), `RequestGameplayTag(..., errorIfNotFound = true)`(UE처럼 이름당 1회 에러), `Container.Reset(slack = 0)`, `AddChildrenTags(..., recurseAll = true, onlyIncludeDictionaryTags = false)`(후자 true면 `NotImplementedException`), 노드 생성자 `(inTag, inFullTag, inParentNode)`(노드가 `GameplayTag.Create`로 태그 생성). `GameplayTag.cs`의 오타 `dfg`(컴파일 에러) 제거.
- **맞추지 않은 것.** UE 대응 없는 헬퍼, 노드 에디터 전용 플래그, `Construct/DestroyGameplayTagTree` 접근 수준, `Container.ToString()` 형식 → D8 '맞추지 않은 것'.
- **⚠ 미검증.** Unity 컴파일·실행 안 함. 참조는 grep으로만 확인(구 이름 잔여 없음).
- **UE 대응 주석(유저 요청).** "UE::함수명 이런 식으로 대응해서 써줘" → 위치는 코드 주석(유저 선택). UE 대응이 있는 함수 116개에 `/// <remarks>UE: 클래스::함수</remarks>`를 달았다(태그 20·컨테이너 35·노드 10·질의 19·식 13·매니저 15·native 4). 대응이 원문에서 확인되는 것만 달았다: 질의 평가 private 함수는 UE `FQueryEvaluator`(`GameplayTagContainer.cpp:189-491`), `CreateManager`는 `UGameplayTagsManager::InitializeManager`(`GameplayTagsManager.cpp:1058-1067`)에 대응한다. UE에 없는 것(`*ExactMatch` 2종, 우리 헬퍼)에는 달지 않았다. code-guide '외부 포인터 금지'와 충돌해 규약에 예외를 추가했다(→ code-guide/HARNESS '주석').

## 2026-10-12 — Tag 네임스페이스 런타임 API 구현(에디터 제외)

- **요청(유저).** "GameplayTag namespace 내부 다 구현해봐, editor 제외하고".
- **UE 원문 기준(5.6).** `FGameplayTag`·`FGameplayTagContainer`(`GameplayTagContainer.h` 인라인 `Has*`, `.cpp` `AddTag`·`AddLeafTag`·`RemoveTag`·`FillParentTags`·`Filter`·`AppendTags`·`ParseParentTags`·`GetTagLeafName`), `FGameplayTagNode` 접근자·`ResetNode`, 매니저 `RequestGameplayTagParents/Children/DirectParent`·`ExtractParentTags`·`GameplayTagsMatchDepth`·`GetNumberOfTagNodes`·`GetNumGameplayTagNodes`.
- **결정(→D7 Container=class).** struct+public List는 사본 간 리스트 공유로 노드 캐시를 오염시킬 수 있어 class로(유저 선택). 노드 캐시는 읽기 전용 약속으로 반환, `GameplayTag.GetSingleTagContainer()`는 사본.
- **구현.** 컨테이너: 매칭(`HasTag/HasTagExact/HasAny/HasAnyExact/HasAll/HasAllExact`)·변경(`AddTag/AddTagFast/AddLeafTag/RemoveTag(defer)/RemoveTags/AppendTags/AppendMatchingTags/Reset`)·`Filter(Exact)`·`GetGameplayTagParents`·`MatchesQuery`·순서 무관 `Equals`·`ToStringSimple`·인덱스 접근·할당 없는 `foreach`. 태그: `IsValid`·`MatchesTag(Exact/Depth)`·`MatchesAny(Exact)`·`RequestDirectParent`·`GetGameplayTagParents`·`ParseParentTags`·`GetTagLeafName`·`==`. 노드: 부모 참조(루트 센티널은 부모로 안 침)·접근자·`ResetNode`. Query의 임시 `HasTag` 헬퍼를 컨테이너로 이동.
- **UE와 다르게 한 것(에이전트 판단, 유저 미확정).** ① 부모 태그 캐시는 저장 안 하고(`[NonSerialized]`) **역직렬화 시 이름 파싱(`ParseParentTags`)으로 복원** — 역직렬화 콜백 안에선 매니저(내부 `Resources.Load`)를 만들 수 없어서. 등록 태그의 부모는 항상 트리에 있어 노드 캐시와 결과가 같다(UE `ExtractParentTags`의 미등록 폴백도 같은 파싱). ② UE ensure는 `Debug.LogError`로(미등록 태그로 매칭 시). ③ `IsValid()`·`IsEmpty()`·`Num()`은 UE·기존 Query와 맞춰 메서드(프로젝트 다른 핸들의 `IsValid` 프로퍼티와는 다름). ④ 직렬화·네트워크·텍스트 import/export·deprecated·`EmptyContainer`(가변 class라 공유 static 위험) 제외.
- **⚠ 미검증.** Unity 컴파일·실행 안 함.

## 2026-10-12 — D5 개정: 팩토리 전달 → internal 생성 함수 하나

- **요청(유저).** static 생성자 설명을 들은 뒤 "이렇게 하지 말고 함수 하나만 뚫어놓고 나중에 바꾸는 거로 — 지금거 괜히 복잡해".
- **변경(→D5 생성 차단=Request 입구+internal 생성 함수).** `GameplayTag`의 static 생성자·매니저의 `RegisterTagFactory`·`_tagFactory`·`RunClassConstructor` 제거. `GameplayTag.Create(InternedName)`(`internal static`)를 열고 매니저의 트리 구성·`RequestGameplayTag`가 이를 호출. 트리 구성 중 재진입 예외(`_isConstructing`)는 native 재귀 방지용이라 유지.
- **감수.** asmdef 분리 전까진 `Create`를 같은 어셈블리 어디서든 부를 수 있음 — 분리 시 코드 변경 없이 실효.
- 아래 항목의 '팩토리 전달' 서술은 당시 구현 기록으로 그대로 둔다.

## 2026-10-12 — GameplayTag 생성 차단 + native 태그 등록 경로

- **요청(유저).** "GameplayTag를 native code에서 등록, 에디터 타임에 매니저로 등록". 논의 중 "미등록은 못하게 막자" → "생성자 막고 Request 경로만 내비두고 NativeCode 경로를 뚫자".
- **UE 확인(5.6 원문).** 생성자 protected+friend, `RequestGameplayTag`는 미등록이면 빈 태그+ensure, 텍스트 입력은 거부, 로드된 미등록 이름은 보존+에디터 경고, 매칭은 false+ensure. native는 `RF_Transient`(저장 안 함)·ini보다 먼저 트리에. 에디터 피커는 매니저 트리를 그대로 보여 native도 뜨고 수정·삭제만 `.ini` 출처로 제한. native 아래 하위 태그 추가는 허용되며 ini에 저장, native를 지우면 그 경로는 암시 노드로 남아 `RequestGameplayTag`도 유효. ini는 명시 태그만 전체 경로 한 줄씩 평면 저장(Lyra `DefaultGameplayTags.ini`로 확인).
- **구현(→D5 생성 차단=Request 입구+팩토리 전달, →D6 native=코드 정의+저장 안 함).** `GameplayTag` private 생성자 + static 생성자에서 팩토리 등록 + `RequestGameplayTag(InternedName/string)`·`TagName`. 매니저: `RegisterTagFactory`(최초만), `Instance` 지연 생성(에디터에도 존재), `CreateManager`가 `RunClassConstructor`로 팩토리 강제 + 구성 중 재진입 시 예외(무한 재귀 방지), 노드 맵 키를 `InternedName`으로, 트리 구성은 native → 레지스트리 순(`AddTagToTree` 추출, 에셋 없어도 native는 구성). `GameplayTagNode`는 완성 태그를 받음. 신규 `NativeTagAttribute`·`NativeGameplayTag`(이름만 저장, `Tag`는 Request로, implicit 변환, `GetRegisteredNativeTags`). 탐색은 에디터 `TypeCache.GetFieldsWithAttribute`(6000.3.12f1 문서 XML로 존재 확인), 빌드는 전 어셈블리 리플렉션(**에이전트 제안**, 유저 명시 확정 없음). `Test.cs`는 Request로 교체.
- **경로 정리.** 매니저의 주석 처리된 `AddNativeGameplayTag` 스텁 제거(native 경로로 대체).
- **⚠ 미검증.** Unity 컴파일·실행 안 함. IL2CPP 코드 제거 시 마커 필드 유실 가능성, 빌드 탐색 비용 미측정. 에디터 매니저는 레지스트리 편집 후 재컴파일·플레이 전까지 낡음.
- **미룬 것.** 노드 출처·명시/암시 기록, 드롭다운 드로어, 로드 시 미등록 경고, 태그 코멘트.

## 2026-10-11 — GameplayTagQuery 대략 구현(토큰 스트림, UE FQueryEvaluator 미러)

- **요청(유저).** "GameplayTagQuery 쪽 대충 구현해봐". 내부 표현이 미확정(토큰 스트림 vs 경량 식 트리)인 채로, **이미 채운 필드(토큰 스트림)를 그대로 써서** 구현 — 표현 결정은 여전히 열림.
- **UE 원문 기준(5.6.0 `GameplayTagContainer.cpp`).** 평가 `FQueryEvaluator::Eval*`(:189-510, 단락 평가 + 건너뛴 토큰도 끝까지 소비), 기록 `EmitTokens`(:2200), `Matches`/`IsEmpty`/`Clear`/`Build`/`MakeQuery_*`(:1694-1810), 매칭 의미 `HasTag`=명시+부모 / `HasTagExact`=명시만(`GameplayTagContainer.h:313-338`).
- **UE와 다르게 한 것.** ① 스트림 머리의 버전·루트 유무 바이트 생략(직렬화·`TokenStreamVersion` 미정이라) — 빈 스트림=빈 질의. ② `Undefined`도 `[유형][0]`으로 기록해 읽는 쪽이 항상 `[유형][개수]`를 소비. ③ `_autoDescription` 제거 — UE에선 에디터 편집 질의(`BuildFromEditableQuery`)에서만 채워져 우리에겐 쓸 곳이 없음. ④ `ExactMatch` 2종은 UE 5.6에 없어 `HasTagExact`로 유추 구현 + Expression에 설정 메서드 추가. ⑤ Query는 struct라 복사본끼리 리스트를 공유 → `Build`·`ReplaceTag*Fast`는 리스트를 제자리 수정하지 않고 새로 만들어 교체.
- **임시.** 컨테이너 매칭(`HasTag`/`HasTagExact`)을 Query 안 private 헬퍼로 둠 — `GameplayTagContainer`에 매칭이 생기면(세부 TODO 2) 옮김(TODO 주석).
- **⚠ 미검증.** 빌드·실행 안 함. `AddTag(InternedName)`은 `GameplayTagsManager.Instance`(플레이 시작 시 Bootstrap)에 의존 — 에디터 비플레이 상태에선 null.

## 2026-10-11 — GameplayTagQuery·Expression 골격(시그니처·필드) + Expression=class

- **요청(유저).** "UE 코드 확인해서 GameplayTagQuery 시그니처만 해둔 빈 함수" → 이어서 Expression 빈 struct + `Build` 시그니처, Expression 함수 시그니처, 필드 채우기.
- **UE 원문 기준(5.6.0 `GameplayTagContainer.h:761-1030`).** 공개 API를 C#으로 옮기되 C++/UE 전용(복사·이동 생성자·대입, `Serialize(FArchive&)`, 에디터 `EditableQuery`, Json 변환)은 제외. 본문은 전부 `NotImplementedException`. 이름은 `MakeQuery_X`→`MakeQueryX`(PascalCase). `AddTag(FName)`→`AddTag(InternedName)`(유저 지정).
- **enum.** 유저가 Container 안에 private으로 둔 `GameplayTagQueryExprType`을 별도 파일 public으로 이동(유저 선택). `AnyTagsExactMatch`·`AllTagsExactMatch`는 UE 5.6 원본에 없지만 유지(유저 선택).
- **struct vs class 논의.** C# struct는 UE 값 의미론(`TArray` 깊은 복사·`Expr&` 체이닝)을 못 옮김(List 참조 공유, mutable struct 사본 함정). 유저가 GC 흐름 불편을 제기 → Span/Memory(뷰일 뿐·ref struct 중첩 불가), stackalloc+ArrayPool `ref struct` writer(가능하나 평면 API로 이탈·`GameplayTag` managed라 stackalloc 불가)까지 검토. 결론은 class 잠정. 이어서 유저가 체이닝 요청 → 수정 메서드 반환 타입을 자기 자신으로(→D4 질의 식=class+자기 반환 체이닝). code-guide "연쇄 호출" 규약 문구("LINQ 외 fluent 금지")와 충돌해 보였으나, 유저 설명상 규약 의도는 `GetComponent().Item.GetPlayer().GetTarget()`처럼 객체를 건너가며 파고드는 호출 금지 → 문구를 의도대로 수정(LINQ·빌더식 fluent 허용).
- **⚠ 열린 것.** Query 필드를 UE대로 토큰 스트림으로 채웠는데 progress '다음 작업 7'의 '경량 식 트리' 기울기와 상충 — 내부 표현 미확정. Query 직렬화(`[Serializable]`) 여부·`TokenStreamVersion` 미정.

## 2026-10-03 — 레지스트리 에셋: 에디터 로드 시 자동 생성 + 수동 Create 메뉴 제거

- **방향(유저).** "고정 경로에 레지스트리 에셋을 코드로 직접 만들고, 외부에서 Create 못 하게 막자"(`~해봐`). 처음엔 "매니저 생성 시"로 지시했으나, 이어서 **"플레이 말고 엔진 켜질 때"**로 트리거를 조정 → 생성 시점을 **에디터 로드/재컴파일**로 옮김. (그 외 근거는 유저 미언급.)
- **핵심 제약.** `AssetDatabase.CreateAsset`은 **에디터 전용**(빌드에 없음). 엔진 로드·재컴파일마다 도는 에디터 훅 = `[InitializeOnLoad]`. 여기서 에셋을 보장해 두면 런타임(플레이·빌드)은 `Load()`로 **읽기만**. UE식 "정의는 영속·빌드는 읽기만"(→worklog 2026-09-30)과 동형.
- **왜 플레이(RuntimeInitializeOnLoad)가 아니라 InitializeOnLoad인가.** 유저 요청 + 설계상 이점: 플레이 진입 시 생성은 "`CreateAsset`→같은 프레임 `Load`" 타이밍이 애매하고 플레이해야만 생김. 에디터 로드 훅은 켜두면 항상 보장되고 런타임 경로를 순수 읽기로 유지.
- **구현.**
  - `GameplayTagRegistryAsset`: `[CreateAssetMenu]` 제거(수동 생성 메뉴 차단), 에디터 전용 `LoadOrCreate()`(Load 실패 시 `EditorAssetPath="Assets/Resources/"+ResourcesPath+".asset"`에 생성·저장) + `EnsureParentFolders()`(CreateAsset은 부모 폴더가 AssetDatabase에 있어야 해 `Assets`부터 한 단계씩 `CreateFolder`).
  - **`Tag/Editor/GameplayTagRegistryInitializer.cs` 신설** — `[InitializeOnLoad]` static 클래스, static 생성자에서 `LoadOrCreate()` 호출. feature-local `Editor/` 폴더라 에디터 어셈블리로 자동 컴파일(asmdef 없음 → Assembly-CSharp-Editor). `#if` 불필요.
  - `GameplayTagsManager.ConstructGameplayTagTree`: 다시 `Load()`만(생성 책임 이관). 런타임은 읽기 전용.
  - 경로는 기존 `ResourcesPath`("AbilitySystem/GameplayTagRegistry") + 기존 `Assets/Resources/` 폴더로 **확정**. 최종 `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`.
  - **차단 범위(절제).** "막기"는 **메뉴 노출 제거**까지(`sealed` 유지). 코드의 `CreateInstance`는 여전히 가능하나 노출 안 됨 — 솔로 규모에 완전 봉인은 과설계라 미도입.
- **선행 이슈 해소 관측.** 2026-10-01 worklog의 '어셈블리 컴파일 불가'(비-void 빈 스텁 4개)는 **현재 코드에서 주석 처리돼 해소**된 상태(이번 세션 밖 변경).
- **⚠ 미검증(확신 낮음).** 에디터 실측 안 함 — Unity MCP 연결 막혀 에이전트가 트리거 못 함(유저가 Unity 포커스 시 재컴파일 → 생성됨). 확인할 것: 에셋이 `Assets/Resources/AbilitySystem/`에 실제 생성되는지, 폴더 자동 생성(`EnsureParentFolders`). **D# 정식화 대기:** 유저가 접근만 지정하고 근거 미언급 → decisions 미등록(원칙 8).

## 2026-10-01 — Tag 트리 코어: Node 생성자 + Manager 빌드·싱글톤 + 조회 API

- **설계 논의(UE 5.6.0 원문 대조).** 트리는 포인터 링크 노드 + 태그→노드 맵(**맵 값이 곧 노드**, 별개 두 트리 아님). 매칭은 트리를 안 타고 노드의 `CompleteTagWithParents` 캐시를 맵으로 읽음. 저장은 **평면 점표기 문자열 유지**(편집·diff·병합 때문, 효율 아님) + **리프만 적어도 중간 부모는 빌드가 파생**. 데이터지향 평면배열은 안 내는 비용(순회 지역성) 최적화라 기각.
- **`GameplayTagNode`.** 생성자 책임 분담을 UE대로 — `(짧은이름, 전체이름, 부모)`를 받고 노드는 분할·부모탐색 안 함(매니저가 함). `CompleteTagWithParents`(완전태그[0] + 펼친 부모 캐시) 구성. **루트=빈 센티널**(`_tag`=default ≈ NAME_None), 부모 체인 가드 `!parent._tag.Equals(default)`로 루트 제외(UE `GetSimpleTagName() != NAME_None` 대응). `TryFindChild`/`AddChildNode`. **버림(근거):** NetIndex(복제 미채택)·에디터 충돌추적·정렬삽입(대규모 네이티브 테이블 성능용)·`_parentNode` 역참조(조상은 캐시에 이미 펼침).
- **`GameplayTagsManager`.** 문자열 파싱 빌드(`split('.')` + 레벨 dedup으로 부모 파생). **싱글톤 = 플레인 C#**(MonoBehaviour 아님, UE `Get()` 대응) + `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`로 startup 결정적 빌드(도메인리로드 OFF여도 매 재생 새 인스턴스). 기존 `Start()`는 비-MonoBehaviour라 안 불려서 생성자로 이관. 레지스트리 배선: `GameplayTagRegistryAsset.Load()` + null 가드. 조회 API 채움: `FindTagNode`×2·`RequestGameplayTag`(등록된 것만, 미등록 default)·`DestroyGameplayTagTree`.
- **절제 판단.** `accumulatedPath` StringBuilder 최적화 검토 → 기각(빌드 1회성·인터닝은 string 필요·범용 유틸 오염). 루트 접근자 **bounds 가드(②)**: 노드에 완전태그 조회 접근자 추가 시 `Num()>0` 가드 필요(빈 루트 보호) — 아직 접근자 없어 미도입.
- **열린 질문(→progress 결정대기 3).** 태그 조회 입력 ergonomics: implicit `string→InternedName` vs `(string)` 오버로드 vs `new InternedName()` 명시. 명시 쪽 제안, 유저 확정 전.
- **⚠ 알려진 이슈.** 매니저 비-void 빈 스텁 4개(`AddNativeGameplayTag`·`RequestGameplayTagParents`·`RequestGameplayTagChildren`·`RequestGameplayTagDirectParent`)가 반환 누락으로 **어셈블리 컴파일 불가**. `RequestGameplayTagDirectParent()`는 파라미터 누락(시그니처 깨짐). 다음 세션 선행 해결.

## 2026-09-28 — 태그 표현 타입 확정: `InternedString` 래퍼

- **논의 흐름:** 내부 표현 후보 "FName처럼" vs "TagsManager index map"을 두고 봤더니 **결국 같은 것**(둘 다 struct가 핸들만 들고 뒤에서 string→인덱스 인터닝)이라는 데 합의. 이어 "int 핸들 안 하고 문자열 통째 들어도, intern만 보장되면 참조 비교·중복 제거가 성립 = FName 실질 대체"까지 정리.
- **`string.Intern` 확인:** 리터럴은 자동 인터닝, 런타임 문자열은 `string.Intern` 호출 + **반환값 대입** 필요. 전역 풀이라 앱 수명 내 GC 안 됨(태그는 유한이라 무해).
- **결정(유저):** 내부 문자열을 **`InternedString` struct 래퍼**로 대체. 래퍼가 인터닝 책임을 캡슐화. **근거(유저):** "string을 대체하되, intern string임을 타입으로 명시적으로." 동등성은 `ReferenceEquals`(유저 선택 — "둘 다 interned라 참조 비교로 충분").
  - **정수 핸들 유보(회색지대):** 밀집 int 인덱싱이 필요해지면 자체 레지스트리로 교체. **교체 지점은 `InternedString.Intern` 한 곳**으로 국소화(호출측·GameplayTag 무영향).
- **구현:**
  - `InternedString.cs` 신설 — struct, 생성자 `string.Intern` 정규화, `ReferenceEquals` 동등성, `ISerializationCallbackReceiver.OnAfterDeserialize`에서 재-intern(역직렬화가 생성자 우회하므로).
  - `GameplayTag.cs` — 임시 `string _tagName` → `[SerializeField] InternedString _name`. 동등성·해시 래퍼 위임.
- **⚠ 미검증(확신 낮음):** 참조 비교 정확성은 "역직렬화된 태그도 intern됨"에 의존. **중첩 struct의 `ISerializationCallbackReceiver` 콜백이 실제로 불리는지 미확인** — 안 불리면 인스펙터/에셋 로드 태그가 intern 안 돼 참조 비교가 조용히 깨짐. **에디터에서 실측 필요.** fallback: ① 컨테이너 레벨 재-intern, ② `Equals`를 `Ordinal` 문자열 비교로 강등(intern=성능 최적화로).

## 2026-09-30 — 태그 정의 소재: 단일 SO(고정 위치)

- **문제:** 에디터에서 추가/제거한 태그를 **빌드·에디터 동일하게 startup에 로드**할 소재가 필요. UE는 이걸 어떻게 하나 확인(로컬 5.6.0 원문) — 정의는 config `DefaultGameplayTags.ini`(+플러그인 ini·DataTable 여러 개)에 텍스트로 저장, 시작 시 매니저가 모아 트리 빌드. **정의는 영속(텍스트), 런타임 트리·인덱스는 휘발(재구성)**. 편집은 Project Settings 전용 UI, 빌드는 읽기만.
- **논의 흐름:** 발견 방식(다중 SO)·은닉(Addressable/codegen)까지 넓혔다가 좁힘.
  - 다중 SO 자동 발견은 `AssetDatabase`가 빌드에서 안 되는 함정 → 런타임 발견은 `Resources.LoadAll`/Addressables/루트참조만 가능. 하지만 **"단일 트리"만 성립하면 되지 단일 파일일 필요는 없음** — 지금 규모엔 단일 SO면 자명하게 충족(다중 병합 기계는 과설계).
  - "은닉·못 건드림"은 런타임 로드(빌드 포함 에셋=Project 창 노출)와 상충. 완전 은닉은 codegen(숨은 저장처→C# 상수 생성)이어야 하나 **오바로 판단**. custom 텍스트 포맷도 파서·검증·편집 UX를 SO 재구현이라 이득 없음(솔로·외부편집 불요).
- **결정(유저):** 정의 소재 = **단일 ScriptableObject(고정 위치, 단순 string 리스트)**, startup에 로드해 트리 빌드. 커스텀 Inspector·codegen·custom 포맷 **미채택**. **근거(유저):** "codegen·custom format은 오바, 단순함 우선." Inspector는 나중에 따로.
  - 포기한 것(의식적): 은닉/손댐 방지(SO는 Project 창 노출), 오타 방지·자동완성(문자열 기반). 필요해지면 매니저의 수집 지점 한 곳만 바꿔 드로어/codegen 확장.
- **구현:** `GameplayTagRegistryAsset.cs` 신설 — `sealed ScriptableObject`, 고정 경로 `ResourcesPath`("AbilitySystem/GameplayTagRegistry") 상수, `List<string> _tags`, `static Load()`(Resources 로드).
- **다음:** ① Resources 하위에 에셋 인스턴스 생성(에디터: Create ▸ Ability System ▸ Gameplay Tag Registry → `Resources/AbilitySystem/`로 이동). ② 매니저 startup 로드 + 트리 빌드 배선(단, `GameplayTagNode` 생성자 미완이 선행). ③ 결정을 `decisions.md`에 D#로 정식화할지 유저 확인.
