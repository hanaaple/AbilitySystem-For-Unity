# gameplay-tag — 작업 로그 (worklog)

> 시간순 "무엇을 왜 했나" 기록. 최신이 위로. 결정은 `(→D#)`(decisions.md)로 링크한다.
> 2026-10-23 정리로 검증 수치·조사 경로·확인 절차 경위를 걷어냈다 — 원문은 git 이력. 확인 절차는 [tests.md](tests.md).

## 2026-10-20 — GE·Tag 전체 회귀 + 코드상 빈 곳 점검
- 유저 "구현되어있는거 전반 다 잘 작동하나? TODO로 남은 것들 리스트로 싹 확인해봐", "코드상에 빈 것도". 회귀는 모두 기준값.
- 호출하면 예외가 나는 미구현 공개 API 5개와 남은 TODO 주석을 TODO-BOARD 'GE·Tag 남은 것'으로 모았다. `PreAttributeBaseChange`가 값으로 받아 클램프할 수 없다는 차이는 보고만(이후 attribute에서 수정).

## 2026-10-20 — 중첩 struct 역직렬화 콜백 실측
- `InternedName.Equals`는 참조 비교라 에셋에서 읽힌 이름도 `OnAfterDeserialize`에서 intern돼야 한다. `GameplayTag` 안의 `InternedName`(struct 속 struct)에도 콜백이 불리는지가 2026-09-28부터 미확인이었다.
- 임포트 직후·언로드 뒤 재로드·도메인 리로드 세 경로 모두 intern됨을 확인 → fallback(컨테이너 재-intern / `Ordinal` 비교) 불필요. 빌드 플레이어는 안 봄(같은 직렬화기라 같을 것으로 추론).

## 2026-10-20 — 태그 관리 창·대화창·레지스트리 자동 생성 확인 (tests.md §4·§5)
- 실제 마우스 클릭으로 피커를 MCP 밖에서 열자, MCP 안에선 안 뜨던 미등록 태그 대화창이 떴다.
- 찾아서 고친 버그 2건(`GameplayTagPicker`): 태그 추가 뒤 입력란이 포커스를 쥔 채 옛 글자를 보여 줌(UE도 추가 뒤 입력란을 되돌림) / Enter가 텍스트 필드까지 가서 편집을 끝냄 → 입력란에 포커스가 있을 때 Enter를 먼저 소비.
- 고치지 않은 것: 하위 태그 추가 때 Unity 텍스트 필드가 전체 선택돼 접두어가 지워진다(UE는 이어 칠 수 있음). 고치려면 internal 편집기를 건드려야 해 물었고, 유저 "그대로 둬".
- 레지스트리 자동 생성은 Refresh로는 안 되고 도메인 리로드 때 `[InitializeOnLoad]`가 만든다.

## 2026-10-20 — 태그로 활성 GE 찾기·제거 (세부 TODO 13)
- `GameplayEffectQuery`·ASC 질의 API·Remove Other. 경위·결정은 gameplay-effect worklog·D24·D25.

## 2026-10-19 — 태그 연동 PlayMode 테스트
- 에디터 셀프체크는 한 번에 호출해 보는 것이라 시간이 걸린 동작(만료로 태그가 빠지며 의존 GE가 꺼짐, 꺼진 주기 GE)이 빠져 있었다 → `TagRequirementsPlayModeTest`로 실제 프레임에서 확인.

## 2026-10-19 — Target Tag Requirements 판정
- UE `TargetTagRequirementsGameplayEffectComponent.cpp` 전체를 대조해 적용 거부(Application·Removal), Ongoing·Removal 태그 구독 → 켜고 끄기(inhibit)·제거, GE 제거 때 구독 해제를 옮겼다. 구독 해제 고리를 걸려고 `ActiveGameplayEffectEvents.OnEffectRemoved`(지금 쓰는 것만)·컨테이너 `Owner`를 더했다.
- 뺀 것: 네트워크 권한 분기(프로젝트 미채택), `IsDataValid` 에디터 오류(검증 체계 없음 — 유저 결정 대기).

## 2026-10-19 — Granted Tags Component
- 부여·회수 경로는 이미 UE 위치대로 있었고 빠진 건 GE 캐시를 채우는 Component였다 → `TargetTagsGameplayEffectComponent`(부모 없음, D30 그대로).

## 2026-10-19 — GE 인스펙터 Component 목록 + 에디터 하드코딩 점검
- 유저: Component를 드롭다운으로 고르게, Component마다 개별 드로어로, 순서 Duration → Components → Modifiers → Executions, "억지로 늘린거나 단순 Tooltip은 Attribute 쪽으로".
- 처음 만든 Asset Tags·Target Tag Requirements 전용 드로어는 라벨·툴팁만 덧붙이는 것이라 지우고, 공용 Component 드로어 + 필드 `[Tooltip]`으로. 같은 기준으로 Tag 에디터의 하드코딩 라벨 상수도 필드 이름·`[Tooltip]`을 읽게 바꿨다.
- 같은 종류 Component는 하나만(에이전트 판단 — GE가 Component를 종류로 찾는다).

## 2026-10-19 — Asset Tags Component (→D30)
- UE Asset Tags는 `FInheritedTagContainer`(부모 = 블루프린트 CDO)인데 SO 에셋엔 부모가 없다. 원문으로 "부모 결과에 쌓는 구조"를 설명한 뒤 유저 "부모 없이 가자 일단".
- Component가 GE 캐시에 쓰려면 소유 GE를 알아야 해 `GetOwner()` 추가(유저 "UE에 따라 당연히 추가해야지").
- 폴더: 유저 지시로 구체 Component를 `Core/AbilitySystem/GameplayEffectComponents/`로. 베이스·`InheritedTagContainer`는 UE처럼 폴더 밖(에이전트 판단).
- 같이 고침: Spec 생성 때 magnitude를 계산해 `Calculation Failed` 경고가 나던 것 — UE는 생성 때 계산하지 않는다.

## 2026-10-19 — GE Component 호출 골격
- 유저 "Concrete Class 구현말고 추상화된 상태에서 구현만". UE가 Component를 도는 함수 4개와 호출 위치를 그대로 옮겼다.
- inhibit을 같이 넣었다(에이전트 판단): `OnActiveGameplayEffectAdded` 반환값이 GE를 끌지 정하는데 끄는 경로가 없으면 쓸 데가 없어서. 꺼지면 modifier·부여 태그를 거두고 주기 실행을 건너뛴다.
- `CanApply`·`OnApplied`는 ASC가 아니라 컨테이너 적용 함수에서 부른다 — 우리 컨테이너가 UE ASC 적용과 컨테이너 적용을 합친 형태라서.

## 2026-10-18 — 목표 정리 + 태그만 주는 GE 허용 + 판정 경로 UE 대조
- 유저 목표 "GameplayTag & GameplayEffect 쪽을 온전히 개발하고 넘어가는거야 … 일반적인 Tag 기능만". UE `GameplayEffectComponents/` 10개와 대조해 일반 기능 6개를 골랐다(태그만 주는 GE → Asset Tags → Granted Tags → 인스펙터 목록 → Target Tag Requirements → 태그로 GE 제거). SetByCaller·스택·D3·부가 Component는 미룸.
- 실수: 목록만 묻는 질문에 1번 코드를 먼저 지웠다가 유저 지적("지금 당장 구현하라는 얘기 아니었어")으로 되돌림.
- 1번: Modifier·Execution이 없는 GE를 거부하던 블록 삭제 — UE엔 이 거부가 없고, 태그만 주는 GE에 필요하다.
- 유저 "모든 경로가 UE와 동일하게 작동하나?" → Instant·Periodic·지속형·캡처 평가 경로를 원문과 하나씩 맞추고 셀프체크 E로 고정. 함정: 자신에게 거는 GE는 Spec 생성 때의 소유 태그가 Source 태그가 된다.

## 2026-10-18 — CurrentValue를 UE로 되돌리고 GE Component 도입 (→D28·D29)
- 유저 "CurrentValue에 적용시 … Ongoing Tag Requirements 담당으로 넘기고 가보자. 기존에 선택 치우고", "UE 처럼 GE Component 도입", "빈 거로 넣어둬".
- UE는 CurrentValue를 빈 평가 문맥으로 계산하고 소유 태그 조건은 GE 단위 Component가 맡는다(`GameplayEffect.cpp:3311-3317`). D21(CurrentValue에도 modifier 조건)·D23(modifier 단위 태그 구독)을 코드째 걷어내고 Component 껍데기를 넣었다.

## 2026-10-18 — mod 자격 판정을 UE처럼 갱신·확인 두 단계로
- UE `FAggregatorMod`는 `UpdateQualifies`가 채우고 `Qualifies()`는 읽기만 한다. 계산 루프 안에서 바로 판정하던 것을 같은 구조로 나눴다. 결과는 같다.

## 2026-10-17 — 버그 수정 + 결정 정리 + `TokenStreamVersion`
- 유저 "단순 오류나 버그는 하고 결정해야되는거만". 수정안이 이미 정해진 버그는 시점 결정이 필요 없다고 보고 고쳤다: aggregator 없는 어트리뷰트의 Current 미갱신(aggregator D14가 정한 동작), `UpdateAggregatorMod`가 Spec modifier 인덱스를 aggregator 목록 인덱스로 쓰던 것, 주기 실행 때 magnitude를 다시 계산하지 않던 것(UE대로), 다중 선택 표시, 인스펙터 높이 계산.
- 결정: D24(접힘 상태 키=배열 경로), D25(None 하위 식은 UE처럼 `undefined` 저장), D26(형식 버전 저장 — 스트림 첫 토큰이 아니라 필드로, 저장된 질의를 옮기지 않으려고).
- Query 편집기: 열어 둔 채 리로드하면 매 repaint NullReference + 창 중복 → 상태를 잃으면 빈 편집기로(유저 "아예 새로 만들라는게 아니라 빈 Window의 Editor로").

## 2026-10-17 — 에디터 확인을 에이전트가 직접 (유저 "에디터 확인은 직접 해")
- 화면 보기: 화면 픽셀 읽기는 가린 창이 찍히고, `GUIView.GrabPixels`는 UI Toolkit 본문이 빠져, Windows `PrintWindow`로 Unity 창만 캡처하는 방법에 정착(→ test-harness).
- 이 확인에서 aggregator 없는 어트리뷰트의 Current 버그와 다중 선택 표시 버그를 찾았다(위 항목에서 수정).

## 2026-10-17 — 소유 태그 입구 (→D22, D23은 이후 되돌림)
- 처음 계획은 "UE `OnTagUpdated` 자리에서 알린다"였는데, 원문을 보니 그건 빈 가상 함수였고 5.6엔 modifier 단위 재계산이 없다 — 태그 반응은 GE 단위 Component가 태그별로 구독한다. 이걸 설명하고 다시 물어 구조 "UE와 같은 구조", 이벤트 "UE 두 종류 모두"를 받았다.
- `GameplayTagCountContainer`(UE 함수 단위 대응), ASC Loose 태그·조회·이벤트. 이벤트는 개수 갱신이 끝난 뒤 부른다(UE처럼 콜백이 갱신된 상태를 봄).

## 2026-10-16 — Modifier Source/Target Tags: 필드 → 판정 연결 → 셀프체크 (→D14)
- 유저 "Modifier에 Source Tags와 Target Tags 구현해보자", 이어 "ue 호출 경로나 위치 참고하면서" 판정 연결.
- 필드: UE `FGameplayTagRequirements` 전체 + Query 직렬화. `GameplayTagRequirements`는 class(필드 초기화로 null이 안 되게 — D7과 같은 선택), 위치는 UE처럼 GE 쪽(`Effect/`).
- 판정: Source 태그는 Spec 생성 때, Target 태그는 적용·실행마다. Instant/Periodic은 실행 때 건너뛰기, 지속형은 aggregator 평가 문맥. 당시 유저 지시로 CurrentValue에도 조건을 걸었으나(D21) 2026-10-18 되돌림.
- 막힌 곳: 에디터 어셈블리 컴파일이 옛 런타임 ref DLL을 참조해 바뀐 API를 못 봤다 → 런타임을 먼저 컴파일해 그 DLL로 교체(→ dev-tools '컴파일 확인').
- 유저 지적 "GameplayTagQuery는 뭘 변경한거야": Query 직렬화·동등 비교가 선택지 라벨에 드러나지 않았다. 유저는 "그대로 둠".
- 셀프체크 `ModifierTagSelfCheck`와 유저 확인 문서 [tests.md](tests.md)를 만들었다.

## 2026-10-16 — Query 편집기 + 가독성 (→D16·D17, 치운 결정)
- UE `SGameplayTagQueryWidget`처럼 별도 창·즉시 반영·Cancel 복원, 자동 설명은 편집 경로에서 저장.
- 변경 감지: 태그 비교가 인터닝 참조 비교라 값 비교로는 매 프레임 사본을 다시 만들 위험이 있어 `GetDirtyCount`로.
- 깊어지면 읽기 어렵다는 지적 → 접기·구분선 → 웹의 트리 뷰 패턴을 참고해 본문 묶음·깊이별 안내선·접은 줄 요약. 수치는 에이전트 판단. 이 과정을 `code-guide/editor-ui-readability-guide.md`로 정리(유저 요청).
- 접힘 상태를 배열 경로 키로 들어 가이드의 안티패턴을 다시 밟았다 → 드로어 가이드를 "에디터 GUI 코드를 고치기 전에도" 읽도록 규약을 고침.

## 2026-10-15 — 태그 드로어·피커·관리 창 (→D10·D11 치운 결정, →D12)
- 유저 "어느 클래스나 struct의 Field로 들어가도 작동 가능하게, 기능은 최대한 UE와 유사하게". 이전의 기억 기반 UE 모양을 원문(`GameplayTagsEditor`)으로 대체했다.
- 범위 질문에 유저가 넷 다 "포함/UE처럼" — 태그 관리(레지스트리 편집)는 에이전트 권장(제외)과 반대(D12).
- 기존 `AdvancedDropdown`은 단일 선택·체크박스 없음이라 UE 피커와 맞지 않아 `TreeView` 피커를 새로 짰다. 팝업 콜백 시점엔 `SerializedProperty`가 무효일 수 있어 대상·경로만 들고 다시 조회한다.
- 레지스트리 인스펙터는 읽기 전용(유저 지시, 이유 미기록) — 편집은 관리 창으로만.

## 2026-10-15 — GE·ASC 태그 연동 범위 질문
- 유저 "GE, ASC에 Tag 넣어봐 ue에 맞춰서". UE 5.3+ GE Component 구조(태그 설정이 Component로 옮겨짐)를 원문으로 정리해 GE 구조·범위·ASC 범위·TagQuery 포함 여부를 물었다(답은 D22·D29).

## 2026-10-14~15 — Tag API를 UE 이름·시그니처에 정렬 (→D8)
- 유저 "함수명이나 그런 거까지 따라가려는데 그게 되려 나를 붙잡는 거 같아" → 이름 맞추기(비용 거의 없음)와 UE 함수 전부 채우기(비용 누적)를 나눠 제시 → "기존 있는 함수들만" 맞춤. UE 대응 함수에 `<remarks>UE: …</remarks>` 주석.
- `MatchesTag`를 컨테이너 없이 평탄 배열로 바꿔 봤다가 유저가 "그냥 원래대로" → UE처럼 컨테이너 경유.
- `InternedName` 해시 캐시: C# string은 해시를 캐시하지 않아 조회마다 다시 해시한다(UE `FName`은 정수). 해시는 실행마다 다를 수 있어 저장하지 않는다.
- 쓸모 있는 UE 함수는 빈 시그니처로(치운 D9), `EmptyTag`.

## 2026-10-12 — 런타임 API 구현 + 생성 차단 + native 태그 (→D5·D6·D7)
- 유저 "미등록은 못하게 막자" → "생성자 막고 Request 경로만 내비두고 NativeCode 경로를 뚫자". UE 원문: native는 저장하지 않고 ini보다 먼저 트리에 넣는다. C# static 필드는 첫 접근 때 초기화돼 "로드되면 자동 등록"이 안 되므로 마커 필드를 탐색한다.
- 처음엔 팩토리 전달로 생성을 막았다가 유저 "괜히 복잡해" → `internal static Create` 하나(D5).
- 컨테이너는 class(D7): struct + List는 사본끼리 리스트를 공유해 노드 캐시가 조용히 오염된다. 부모 캐시는 저장하지 않고 역직렬화 때 이름 파싱으로 복원(콜백 안에선 매니저를 만들 수 없어서).

## 2026-10-11 — GameplayTagQuery 골격·구현 (→D4)
- UE 원문대로 토큰 스트림으로 구현(평가는 단락 평가 + 건너뛴 토큰도 끝까지 소비). 내부 표현(토큰 스트림 vs 식 트리)은 미확정으로 남김.
- 식은 class + 자기 반환 체이닝(유저). code-guide '연쇄 호출' 문구가 막는 듯했지만 유저 설명상 의도는 객체를 건너가며 파고드는 호출 금지 → 규약 문구를 의도대로 고침.

## 2026-10-03 — 레지스트리 에셋을 에디터 로드 때 자동 생성
- 유저 "고정 경로에 코드로 직접 만들고, 외부에서 Create 못 하게", 시점은 "플레이 말고 엔진 켜질 때". `CreateAsset`은 에디터 전용이라 `[InitializeOnLoad]`에서 보장하고 런타임은 읽기만(UE "정의는 영속·빌드는 읽기만"과 같은 꼴). 막기는 생성 메뉴 제거까지(완전 봉인은 과설계).

## 2026-10-01 — 노드 트리 + 매니저
- 트리 = 포인터 노드 + 태그→노드 맵. 매칭은 노드의 펼친 부모 캐시를 읽는다. 저장은 평면 점표기 문자열(편집·diff 때문)이고 리프만 적어도 중간 부모는 빌드가 만든다. 매니저는 일반 C# 싱글턴 + 씬 로드 전 빌드.
- 버린 것: 네트워크 인덱스, 정렬 삽입(대규모 네이티브 테이블용), 부모 역참조(조상은 캐시에 이미 펼침).

## 2026-09-30 — 태그 정의 소재: 단일 SO
- UE는 정의를 ini 텍스트로 두고 시작 때 트리를 만든다(정의는 영속, 트리는 휘발). 다중 SO 자동 발견은 빌드에서 `AssetDatabase`가 없어 함정이고, 은닉(codegen·커스텀 포맷)은 유저 "오바, 단순함 우선" → 고정 경로 단일 SO + 문자열 목록.

## 2026-09-28 — 태그 표현 타입: 인터닝 래퍼
- "FName처럼"과 "매니저 인덱스 맵"이 결국 같은 것(핸들 + 인터닝)이라는 데 합의. 유저 "string을 대체하되, intern string임을 타입으로 명시적으로" → struct 래퍼(지금 `InternedName`), 참조 비교. 정수 핸들은 유보 — 바꿀 지점은 `Intern` 한 곳.
