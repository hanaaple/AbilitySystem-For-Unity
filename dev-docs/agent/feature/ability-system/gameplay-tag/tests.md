# gameplay-tag — 검증 목록

> 이 feature의 동작을 확인하는 셀프체크·PlayMode 테스트와 에디터 화면 확인 절차. 진행 문서: [progress.md](progress.md). 최종 갱신: 2026-10-23 (KST)
> 실행 방법(메뉴 실행·결과 줄 읽기·PlayMode 붙이기·화면 확인)은 → [test-harness.md](../../../test-harness.md). 피커·우클릭 메뉴·OS 메뉴·키 입력·툴팁은 Unity가 맨 앞에 있어야 하고, `EditorUtility.DisplayDialog`는 MCP 명령 안에서 띄우면 화면에 안 뜨고 바로 OK로 돌아온다 — 대화창이 걸린 확인은 MCP 밖에서 실제 클릭으로 연다.
> 아래 화면 확인(§2~§5)은 2026-10-20까지 모두 통과했다.

---

## 0. 테스트용 태그

- native(코드 정의, 에디터에서만): `Test.ModifierTag.Native`, `Test.ModifierTag.Native.Enraged` — `Assets/Scripts/Test/Editor/ModifierTagSelfCheck.cs`.
- 레지스트리(에셋에 저장, 빌드에도 포함): `Test.ModifierTag.Registry.Stunned` — `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`.

---

## 1. Modifier Source/Target 태그 판정

관련 결정: D14(modifier 태그 판정=UE 경로), D22(소유 태그=UE `FGameplayTagCountContainer` 구조+Loose·이벤트), D28(CurrentValue=UE처럼 빈 문맥·소유 태그 on/off는 Ongoing Tag Requirements).

소유 태그는 코드(`AddLooseGameplayTag`)로 넣는다. 셀프체크 C는 소유 태그가 빈 상태, D·E는 Loose 태그를 넣고 뺀 상태를 본다. Source 조건은 Spec에 태그를 직접 넣어서 본다.

### 1-1. 셀프체크 실행

- 메뉴 `Tools ▸ Ability System ▸ Run Modifier Tag Self-Check`(Play 모드 필요 없음) → 결과 줄 `[ModifierTag Self-Check] passed 65 / failed 0`.
  - 노란 경고 `[GameplayTagCountContainer] … 명시로 더한 태그가 아닙니다` 1줄은 정상이다(D-4가 일부러 냄, UE도 같은 경고 — `GameplayEffectTypes.cpp:543`).
  - `테스트 태그 준비 실패로 중단`이 나오면 1-2의 0번 항목부터 본다. 실패한 줄은 그 ID를 아래 표에서 찾아 "실패 시 의심"을 본다.
  - E-12는 직전 Play 모드 때문에 에디터 `Time.time`이 0이 아니면 1초 경계 Tick이 빗나가 실패할 수 있다(TODO-BOARD '검증').

### 1-2. 셀프체크 항목별 의미

값은 모두 Health 기준값(Base) 100에서 시작한다. "문맥"은 aggregator를 평가할 때 넘기는 Source·Target 태그다.

**0. 테스트 태그 준비**

| ID | 확인 내용 | 기대 | 실패 시 의심 |
|---|---|---|---|
| 0-1 | native `Test.ModifierTag.Native.Enraged` 유효 | PASS | native 태그 수집(`NativeGameplayTag.GetRegisteredNativeTags`)이 에디터 어셈블리 필드를 못 찾음. 스크립트 재컴파일 후 다시 실행. |
| 0-2 | native 부모 `Test.ModifierTag.Native` 유효 | PASS | 위와 같음. |
| 0-3 | 레지스트리 `Test.ModifierTag.Registry.Stunned` 유효 | PASS | 레지스트리 에셋에 줄이 없음 / 매니저가 에셋 변경 전에 만들어짐 → Unity 재시작 또는 스크립트 재컴파일 후 재실행. |
| 0-4 | 새 ASC의 소유 태그가 비어 있음 | PASS | ASC가 생성 때 태그를 넣게 바뀌었다면 C 기대값을 다시 봐야 한다. |

**A. `GameplayTagRequirements` 판정 (조건 객체 단독)**

| ID | 확인 내용 | 기대 |
|---|---|---|
| A-1 | Require Enraged, Enraged 보유 | 충족 |
| A-2 | Require Enraged, 빈 태그 | 불충족 |
| A-3 | Require 부모 `Test.ModifierTag.Native`, 자식 Enraged 보유 | 충족(계층 매칭) |
| A-4 | Ignore Stunned(레지스트리 태그), Stunned 보유 | 불충족 |
| A-5 | Ignore Stunned, 빈 태그 | 충족 |
| A-6 | TagQuery "Stunned 중 하나", Stunned 보유 | 충족 |
| A-7 | 같은 TagQuery, Enraged만 보유 | 불충족 |
| A-8 | 아무 조건도 없는 객체 | `IsEmpty` true |

실패 시 의심: `GameplayTagRequirements`·`GameplayTagContainer`·`GameplayTagQuery` 자체.

**B. `AttributeAggregator` 자격 판정 (aggregator 단독, 태그를 직접 넘김)**

| ID | 확인 내용 | 기대 | 의미 |
|---|---|---|---|
| B-1 | mod +50, Target Require Enraged / 문맥 Target=Enraged | 150 | 캡처 평가 경로(`Evaluate`)가 Target 조건을 본다 |
| B-2 | 같은 mod / 문맥 비어 있음 | 100 | 조건 못 맞추면 빠진다 |
| B-3 | 같은 mod / Enraged가 문맥 Source에만 | 100 | Target 조건은 Target 태그로만 판정 |
| B-4 | mod +50, Source Ignore Stunned / 문맥 Source=Stunned | 100 | Source 조건 |
| B-5 | 같은 mod / 문맥 비어 있음 | 150 | Ignore만 있으면 빈 태그에서 통과 |
| B-6 | 조건 없는 mod(기존 호출 형태) / 아무 문맥 | 150 | **기존 GE 회귀 없음** |
| B-10 | Source Require Enraged mod 3개(GE 3개) / 문맥 Source=Enraged·빈 | 170·100 | 여러 GE의 mod를 평가 문맥 하나로 판정(UE 그대로) |
| B-11 | B-1 aggregator의 snapshot / 문맥 Enraged·빈 | 150·100 | snapshot이 조건까지 복사 |
| B-12 | Override 7, Target Require Enraged / 문맥 빈·Enraged | 100·7 | Override도 조건 못 맞추면 무시 |

실패 시 의심: `Assets/Scripts/Core/AbilitySystem/Effect/AttributeAggregator.cs`의 `Mod.UpdateQualifies`·`EvaluateQualificationForAllMods`(갱신한 복사본을 목록에 다시 넣는지)·`EvaluateWithBase`, `TakeSnapshotOf`.

**C. 실제 ASC에 GE 적용 (메모리에서 만든 GE, 에셋 파일은 안 생김)**

| ID | 확인 내용 | 기대 | 의미 |
|---|---|---|---|
| C-1 | Instant +30, Target Require Enraged | Base 100 | 실행 시 조건 못 맞춘 modifier 건너뛰기(UE `ExecuteActiveEffectsFrom`) |
| C-2 | Instant +30, Target Ignore Stunned | Base 130 | Ignore만 있으면 적용 |
| C-3 | Instant +30, Source Require Enraged, Source 태그 빈 | Base 100 | Source 조건 |
| C-4 | 같은 GE, Spec의 Source 태그에 Enraged를 직접 넣고 적용 | Base 130 | Source 조건 충족 시 적용 |
| C-5 | Infinite +50, Target Require Enraged | 핸들 유효, Current 100 | 등록은 되고 CurrentValue에선 빠짐 |
| C-6 | Infinite +50, Target Ignore Stunned | Current 150 | |
| C-7 | C-6 제거 | Current 100 | |
| C-8 | Infinite +50, Source Require Enraged, Source 빈 | Current 100 | |
| C-9 | 같은 GE, Source에 Enraged 넣고 적용 | Current 100 | CurrentValue는 UE처럼 빈 문맥(D28) — Source 태그를 보지 않는다 |
| C-10 | C-9 제거 | Current 100 | |
| C-11 | Health 버프(Infinite +100, Source Require Enraged, Source에 Enraged) | Health Current 100 | 빈 문맥이라 버프가 CurrentValue에 안 들어간다 |
| C-12 | Speed GE(Speed += Health.current × 1, Target 캡처·non-snapshot, Source 태그 빈) 적용 | Speed 100 | 캡처 평가는 Speed GE의 Source 태그로 판정해 버프가 빠진다 |
| C-13 | 같은 Speed GE, Source에 Enraged | Speed 200 | 캡처하는 GE의 태그로 버프가 켜진다 |
| C-14 | 정리 후 | Health 100, Speed 0 | |

실패 시 의심: `ActiveGameplayEffectsContainer`(`ExecuteGameplayEffect`·`CaptureTargetActorTags`·`AddSpecMods`·`OnAttributeAggregatorDirty`), `GameplayEffectSpec`(Source 태그 캡처·복제), `AttributeBasedMagnitude.Evaluate`.

**D. 소유 태그(Loose 태그)** — "저장값"은 `GetAttributeCurrentValue`(조회 때마다 다시 계산)가 아니라 AttributeSet에 저장된 CurrentValue다.

| ID | 확인 내용 | 기대 |
|---|---|---|
| D-1 | Loose Enraged 추가 | Enraged·부모 모두 매칭, 부모 개수 1 |
| D-2 | Enraged 2회 추가·1회 제거 | 남음, 개수 1 |
| D-3 | 한 번 더 제거 | Enraged·부모 모두 없음, 소유 태그 빈 |
| D-4 | Enraged가 있을 때 부모 태그를 빼려 함 | 무시(경고 1줄), Enraged 그대로 |
| D-5 | `SetLooseGameplayTagCount` 3 → 0 | 개수 3 → 없음 |
| D-6 | 부모에 `NewOrRemoved`, Enraged에 `AnyCountChange` 등록 후 추가·추가·제거·제거 | 부모 이벤트 2회(1, 0), Enraged 이벤트 4회 |
| D-7 | 두 이벤트 해제 후 태그 변경 | 해제 true, 콜백 없음 |
| D-8 | Infinite +50 Target Require Enraged → Enraged 추가 → 제거 | 저장값 100 그대로(modifier 조건은 CurrentValue에 안 씀, D28) |
| D-14 | Enraged 보유 중 Instant +30 Target Require Enraged | Base 130 |
| D-15 | 정리 | 소유 태그 빈, Health 100 |
| D-16 | Modifier·Execution 없는 Infinite GE 적용 → 제거 | 핸들 유효, 제거 true, Health 100 (태그만 주는 GE의 형태) |

실패 시 의심: D-1~D-7은 `GameplayTagCountContainer`(`UpdateExplicitTags`·`GatherTagChangeDelegates`), D-8은 `ActiveGameplayEffectsContainer.OnAttributeAggregatorDirty`(빈 문맥으로 평가하는지), D-16은 `ApplyGameplayEffectSpec` 입구.

**E. GE 종류별 Base·Current의 Source·Target 조건 판정** — 유저 "Base/Current Value 계산시 Source/Target Tag 검사하는 모든 경로가 UE와 동일하게 작동하나?"를 셀프체크로 고정. UE 원문 대조는 worklog 2026-10-18 '판정 경로 UE 대조'. Source 태그는 Spec 생성 때 Instigator(여기선 자신)의 소유 태그다 — "Spec 생성 뒤 태그"는 Spec을 먼저 만들고 태그를 넣은 경우다.

| ID | GE / 상황 | 기대 | UE 경로 |
|---|---|---|---|
| E-1 | Instant +30 Target Require Enraged, 소유 태그 빈 | Base·Current 100 | `ExecuteActiveEffectsFrom` — 적용 순간 소유 태그(액터 태그) |
| E-2 | 같은 GE, 소유 태그 Enraged | 130 | |
| E-3 | Instant +30 Target Ignore Stunned, 소유 태그 Stunned | 100 | |
| E-4 | Instant +30 Source Require Enraged, Spec Source에 Enraged | 130 | Source = Spec의 Source 액터 태그 |
| E-5 | 같은 GE, Spec 생성 뒤 소유 태그 Enraged | 100 | Source는 생성 때 캡처, 소유 태그를 보지 않음 |
| E-6 | Duration·Infinite(period 0) +50 Target Require Enraged, 소유 태그 Enraged | Base 100, Current 100 | CurrentValue = 빈 평가 문맥(`OnAttributeAggregatorDirty`) |
| E-7 | Duration·Infinite +50 Target Ignore Stunned, 소유 태그 Stunned | Base 100, Current 150 | 빈 문맥이라 Ignore만 있으면 통과 |
| E-8 | Duration·Infinite +50 Source Require Enraged, Spec Source에 Enraged | Base 100, Current 100 | |
| E-9 | Health 버프(Infinite +50 Target Require Enraged) + Speed GE(Speed += Health.current, Target 캡처·non-snapshot), 적용 때 소유 태그 빈 | Health 100, Speed 100 | 캡처 평가 = Spec의 합친 태그(`FAttributeBasedFloat::CalculateMagnitude`) |
| E-10 | 같은 버프 + Speed GE, 적용 때 소유 태그 Enraged | Health 100, Speed 150 | Speed GE의 Target 태그로 버프가 켜짐 |
| E-11 | Duration·Infinite Period 1 +10 Target Require Enraged: 적용 실행 → Enraged 추가 후 주기 → 제거 후 주기 | 100 → 110 → 110 | 주기 실행마다 소유 태그를 다시 읽음 |
| E-12 | Infinite Period 1 +10 Target Ignore Stunned: 적용 → Stunned 추가 후 주기 → 제거 후 주기 | 110 → 110 → 120 | |
| E-13 | Infinite Period 1 +10 Source Require Enraged, Spec Source에 Enraged: 적용 + 주기 1회 | 120 | |
| E-14 | 같은 GE, Spec 생성 뒤 소유 태그 Enraged: 적용 + 주기 1회 | 100 | 주기마다 Source를 다시 캡처하지 않음 |
| E-15 | 정리 | 소유 태그 빈, Health 100 | |

E-6·E-7·E-8·E-11은 Duration·Infinite 두 번씩 나온다(PASS 줄 4개 추가). 실패 시 의심: E-1~E-5·E-11~E-14는 `ActiveGameplayEffectsContainer.ExecuteActiveEffectsFrom`·`CaptureTargetActorTags`, E-6~E-8은 `OnAttributeAggregatorDirty`·`GetAttributeCurrentValue`(`Evaluate(default)`), E-9·E-10은 `AttributeBasedMagnitude.Evaluate`·`ApplyGameplayEffectSpec`의 Target 태그 캡처 순서.

### 1-3. 기존 기능 회귀

- Aggregator 셀프체크(`Run Aggregator Self-Check`)와 PlayMode `AggregatorPlayModeTest` — 기준값은 [gameplay-effect tests](../gameplay-effect/tests.md).

### 1-7. UE(5.6)와 다른 동작

| # | 내용 | UE | 우리 | 상태 |
|---|---|---|---|---|
| 3 | 평가 파라미터 | `FAggregatorEvaluateParameters`에 `AppliedSourceTagFilter`·`AppliedTargetTagFilter`·`IgnoreHandles`·`IncludePredictiveMods` | Source·Target 태그만 | 필터는 progress 세부 TODO 9. 나머지는 예측·네트워크용이라 해당 없음. |
| 4 | Instigator 태그를 얻는 법 | `FGameplayEffectContext::GetOwnedGameplayTags`가 Instigator 액터의 `IGameplayTagAssetInterface`를 먼저 보고, 없으면 ASC(`GameplayEffectTypes.cpp:368-379`) | ASC만 | 액터 태그 인터페이스가 없다. `specTagContainer` 인자는 자리만(UE 기본 구현도 안 채움). |
| 5 | `FTagContainerAggregator` | `ScopedTags`(5.3부터 deprecated) 포함 | 없음 | 쓰지 않는 필드. |

(행 번호는 worklog가 참조해서 유지한다. 1·2·6·7은 UE와 같아져 지웠다.)

---

## 2. Query 편집기

관련 결정: D16(Query 자동 설명=UE처럼 저장·Build 때 생성)·D17(Query 편집기=UE처럼 별도 창·8종·Categories 전달) — 둘 다 치운 결정. 확인 위치: GE 에셋 → Modifier 펼치기 → `Source Tags` 또는 `Target Tags` 펼치기 → `Tag Query` 줄.

- **2-1. 인스펙터 줄 모양.** 회색 `Empty` + 오른쪽 `Edit` 버튼 + `×` 버튼이 한 줄.
- **2-2. 창 열기.** `Empty` 또는 `Edit` → 제목 `Tag Query Editor: Tag Query {에셋 이름}` 창이 줄 왼쪽 조금 위에 뜬다. 다시 누르면 기존 창이 닫히고 새로 열린다(창 하나).
- **2-3. 태그 식.** `Root Expression` → `Any Tags Match` → `Tags`에 태그 추가 → **창을 닫지 않아도** 인스펙터 줄이 ` ANY( 태그 )`로 바뀐다.
- **2-4. 하위 식.** `All Expressions Match` → `+ Expression` 두 번 → `[0]` Any+A, `[1]` None+B → ` ALL(  ANY( A ),  NONE( B ) )`.
- **2-5. 사용자 설명.** `User Description`을 넣으면 인스펙터 줄이 그 글자로, 지우면 자동 설명으로.
- **2-6. Cancel.** 고친 뒤 `Cancel` → 창을 열었을 때 값으로 돌아간다.
- **2-7. OK·저장.** `OK` → 값 유지 → 저장 → 다시 읽어도 유지.
- **2-8. Undo.** 창을 연 채로 `Ctrl+Z` → 인스펙터 줄과 창 내용이 함께 되돌아간다.
- **2-9. Clear.** 창을 연 채로 인스펙터 `×` → 창도 빈 질의.
- **2-10. 삭제.** 하위 식 `−` → 그 칸이 목록에서 사라진다.
- **2-11. 창 안 피커.** `Tags` 상자에서 피커가 열리고, 고르면 창이 바로 다시 그려진다.
- **2-12. 가독성.** 접기 화살표(`None` 줄은 화살표 없이 글자 정렬), 깊이별 색 안내선과 옅은 칠, 접은 줄 끝 회색 요약 + 툴팁, 태그 상자가 위 줄 팝업과 같은 열, 칸 사이 가로줄, 앞 칸을 지워도 접힘 유지, GUI 레이아웃 에러 0, Light 테마에서도 보임.
- **2-13. 창을 열어 둔 채 재컴파일·대상 삭제.** 오류 없이 빈 편집기(제목 `Tag Query Editor`, Root `None`)가 되고, 빈 편집기에서 고쳐도 이전 대상에 안 써진다.

---

## 3. Modifier Source/Target Tags 인스펙터

관련 결정: D13(Modifier Source/Target Tags=UE Requirements 전체)·D15(Query=직렬화+판정) — 둘 다 치운 결정.

- **3-1.** GE 에셋의 Modifier를 펼치면 Magnitude 아래에 `Source Tags`·`Target Tags` 접기 두 개.
- **3-2.** 각각 펼치면 `Require Tags`·`Ignore Tags`(태그 상자 + 피커)·`Tag Query`(2번 항목의 줄). 라벨은 UE 표시 이름(Must Have Tags 등)이 아니라 필드 이름이다(에이전트 판단, 바꾸고 싶으면 말해 줌).
- **3-3.** 태그를 넣고 저장 → 다시 읽어도 그대로.
- **3-4.** Modifier가 여러 개일 때 요소 높이가 맞고 겹치지 않는다.
- **3-5.** 접기 화살표가 드래그 핸들과 안 겹치고, 값 열이 `Modifier Op` 줄의 값 열과 같은 세로선.

---

## 4. 태그 드로어·피커·태그 관리 창

관련 결정: D12(태그 관리 포함=레지스트리 편집). 확인용 에셋은 `Create ▸ Test ▸ Gameplay Tag Test`(`Assets/Scripts/Test/GameplayTagTestAsset.cs` — 단일·List·Container 필드)로 만든다. **4-1·4-2는 Unity를 맨 앞에 띄워야 한다.** 레지스트리를 고치므로 백업 후 진행하고, 끝나면 추가한 태그를 지운 뒤 백업과 비교한다.

- **4-1. 관리 창.** `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset` 선택 → 인스펙터 `Manage Gameplay Tags...` → 창의 `+`로 `Test.A`·`Test.A.B`·`Test.C` 추가 → 레지스트리 에셋 목록에 저장되는지. 추가 뒤 입력란이 비고, Enter로 추가한 뒤에도 다음 태그를 이어 칠 수 있는지.
- **4-2. 행 메뉴.** 관리 창 행의 메뉴에서 Rename·Delete가 동작하는지. native 태그(`Test.ModifierTag.Native…`) 행은 Rename·Delete가 회색(비활성)인지.
- **4-3. 필드별 피커.** 단일·List 요소·컨테이너 필드마다 피커가 열리고, 체크·해제(형제가 없으면 부모가 들어감), 칩의 `×`, 칩 우클릭 Copy/Paste/Clear가 된다.
- **4-4. 저장.** 값을 넣고 저장 → 다시 읽어도 유지(5-3과 겹침).
- **4-5. 다중 선택.** 값이 다른 두 에셋을 같이 선택 → `Multiple Values`, 일부에만 있는 태그는 흐린 칩, 피커 체크박스는 미정 상태.
- **4-6. 위험 지점 — 대화창.** 미등록 태그(예: `No.Such.Tag`)가 든 필드에서 피커를 열 때, 미등록 태그 경고 대화창 뒤에 피커 팝업이 뜨는지(대화창과 피커의 앞뒤). 기대: 대화창(`Invalid Tags that have been removed: No.Such.Tag`)이 먼저 뜨고, OK 뒤 피커가 맨 앞에 열리며 필드는 None. 피커 안의 행 ▾·우클릭·설정 메뉴를 열어도 피커가 남는다.
- 참고: 중첩 struct/class 필드·`[GameplayTagCategories]` 확인용 필드는 테스트 에셋에 없다(유저 요청으로 지움) — 보려면 확인용 필드를 따로 둔다.

---

## 5. Tag 코어

- **5-1. native 태그 — 피커 화면.** 레지스트리에 native와 같은 경로의 하위 태그(예: `Test.ModifierTag.Native.Calm`)를 넣으면 피커에서 native 노드 아래로 합쳐 보이는지. Unity를 맨 앞에 띄워야 하고, 끝나면 넣은 태그를 지운다.
- **5-2. 계층 매칭.** `RequestGameplayTagParents`·`RequestGameplayTagChildren`·`RequestGameplayTagDirectParent`·`GameplayTagsMatchDepth`·`MatchesTag`/`MatchesTagExact`·`IsValidGameplayTagString` 모두 UE 의미대로.
- **5-3. 저장 후 매칭.** 컨테이너에 태그를 저장 → 다시 읽어 부모 `HasTag` true, `HasTagExact(부모)` false.
- **5-4. 레지스트리 자동 생성.** 레지스트리 에셋을 백업 후 지우고 Unity를 다시 열면 `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`이 자동으로 생기는지. 끝나면 백업을 되돌린다. 참고: 지운 뒤 Refresh만으로는 안 생기고, 도메인 리로드(`EditorUtility.RequestScriptReload`) 직후 `_tags: []`로 생김. 재시작 대신 리로드로 확인(`[InitializeOnLoad]`는 둘 다에서 돈다).
- **5-5. 미등록 이름.** `GameplayTag.RequestGameplayTag("No.Such.Tag")`를 두 번 불러도 에러 로그는 **이름당 한 번**.

---

## 6. GE Component 호출·inhibit

관련 결정: D29(GE Component 도입), D30(Asset Tags=UE `FInheritedTagContainer` 구조, 부모 없음). A~D는 호출을 기록하는 테스트 전용 Component(`RecordingComponent`, 셀프체크 파일 안)를 GE에 넣어 보고, E는 실제 `AssetTagsGameplayEffectComponent`를 쓴다.

- 메뉴 `Tools ▸ Ability System ▸ Run GE Component Self-Check`(Play 모드 필요 없음) → 결과 줄 `[GEComponent Self-Check] passed N / failed 0`. 같은 셀프체크의 S(GE 스택)는 gameplay-effect 소관.

| ID | 확인 내용 | 기대 | UE 근거 |
|---|---|---|---|
| A-1·2 | Instant 적용 | CanApply → Executed → Applied, Invalid 핸들 | `AbilitySystemComponent.cpp:844`·`:950-957`, `GameplayEffect.cpp:3224` |
| A-3·4 | 호출 인자 | CanApply는 넘긴 Spec 원본, Executed·Applied는 적용 때 만든 같은 복사본, 모든 호출이 이 ASC의 컨테이너 | CanApply는 Spec 복사 전(`:844`), OnApplied는 `OurCopyOfSpec`(`:957`) |
| A-5~7 | Infinite(period 0) | CanApply → Added → Applied(Executed 없음), Added가 받은 활성 GE는 이미 목록에 있고 반환 핸들과 같음, 켜진 채 시작 | `GameplayEffect.cpp:4305` |
| A-8 | 제거 | Component 호출 없음 | 베이스에 제거 훅이 없다(`GameplayEffectComponent.h:47-74`) |
| A-9·10 | Periodic(적용 시 실행) | CanApply → Added → Applied → Executed, 주기마다 Executed만 | UE는 적용 시 실행을 다음 틱으로 미뤄 Applied가 먼저 |
| A-11 | Component 여러 개 + 빈 칸 | 함수마다 목록 순서, 빈 칸 건너뜀 | `GameplayEffect.cpp:881-935` |
| A-12 | `OnGameplayEffectChanged` | Component마다 전달 | |
| B-1·2 | CanApply false | 적용 안 됨, 뒤 Component의 CanApply도 안 부름(첫 false에서 끝) | `GameplayEffect.cpp:883-889` |
| C-1 | Added에서 한 Component가 false | 나머지 Component의 Added도 부름, GE는 꺼진 채 시작 | `:898-903` |
| C-2·3 | 꺼진 채 시작 | 활성 목록에 있음, modifier·부여 태그 안 걸림 | `:4306-4309` |
| C-4~6 | `SetActiveGameplayEffectInhibit` 켜기·다시 켜기·끄기 | 같은 핸들 반환, 켜면 걸고 끄면 거둠, 같은 상태로 다시 부르면 변화 없음 | `AbilitySystemComponent.cpp:297-315` |
| C-7 | 꺼진 GE 제거 | 거둔 효과를 또 거두지 않음(같은 Loose 태그 1개가 그대로) | `GameplayEffect.cpp:4635` |
| C-8 | 켜진 GE의 부여 태그 | 적용 중 1 → 제거 후 0 | |
| C-9 | 켜는 중 부여 태그 이벤트가 GE를 제거 | Invalid 반환 | `AbilitySystemComponent.cpp:324-327` |
| D-1~3 | 꺼진 채 시작한 주기 GE | Applied는 부르지만 적용 시·주기 실행 안 함, 켠 뒤 주기에 실행 | `GameplayEffect.cpp:4467` |
| E-1 | `InheritedTagContainer` 부모 없음 | Combined = Added | `GameplayEffect.cpp:6046-6075` |
| E-2·3 | 부모 있음(코드로만 — 에셋엔 부모가 없다, D30) | Removed는 부모 태그를 계층으로 뺌, Added는 Removed에 정확히 같은 태그만 막힘 | 같은 곳 |
| E-4 | `AddTag`·`RemoveTag` | 서로의 목록에서 지우고 Combined도 고침 | `:6106-6118` |
| E-5·6 | `ApplyTo` | 기존 컨테이너: Removed에 걸리는 태그를 빼고 Removed와 같은 Added는 안 넣음 / 빈 컨테이너: Combined 그대로 | `:6082-6104` |
| E-7~9 | Asset Tags Component + GE 캐시 | `GetOwner()`가 그 GE, `GetAssetTags()` = 설정, 다시 모아도 중복 없음 | `AssetTagsGameplayEffectComponent.cpp`, `UGameplayEffect::OnGameplayEffectChanged` |
| E-10·11 | Spec | Asset Tags가 Source Spec 태그·합친 태그에 들어가고 액터 태그엔 없음, `GetAllAssetTags` | `GameplayEffect.cpp:1627`·`:2191-2198` |
| E-12 | 적용 | Asset Tags는 대상 소유 태그에 안 들어감 | Asset Tags 정의(헤더 주석) |
| E-13 | Instant modifier Source Require = GE 자신의 Asset Tag | 건너뜀 — 실행 판정은 액터 태그만 | `:3102` |
| E-14 | 캡처 평가 | 캡처하는 GE의 Asset Tag가 버프의 Source Require를 맞춤(100 → 150) | `:983`(합친 태그) |

실패 시 의심(E): `InheritedTagContainer`, `AssetTagsGameplayEffectComponent`, `GameplayEffectAsset.OnGameplayEffectChanged`(`SetOwner`·캐시 비우기), `GameplayEffectSpec.Initialize`(Spec 태그).

| ID | 확인 내용 | 기대 | UE 근거 |
|---|---|---|---|
| F-1~3 | Target Tags Component + GE 캐시 + Spec | `GetOwner()`, `GetGrantedTags()` = 설정(다시 모아도 중복 없음), `Spec.GetAllGrantedTags` | `TargetTagsGameplayEffectComponent.cpp`, `GameplayEffect.cpp:2174-2181` |
| F-4 | Infinite 적용 | 대상 소유 태그 1개 | `GameplayEffect.cpp:4373` |
| F-5 | 같은 태그를 두 GE가 부여 | 2 → 하나 제거 1 → 0 (참조 카운트) | `:4373`·`:4670` |
| F-6 | 자식 태그 부여 | 부모 태그로도 매칭 | 소유 태그 컨테이너가 부모 개수를 센다 |
| F-7 | Instant | 주지 않음(활성 목록에 안 남음) | UE는 에디터 검증에서 오류(`TargetTagsGameplayEffectComponent.cpp` `IsDataValid`) — 우리는 검증 없음 |
| F-8 | 주기 GE | 활성인 동안 1 → 제거 0 | 부여는 period와 무관(`:4373`) |
| F-9 | 꺼진 채 시작 | 꺼진 동안 0 → 켜면 1 → 제거 0 | `AbilitySystemComponent.cpp:307-315` |

실패 시 의심(F): `TargetTagsGameplayEffectComponent`, `GameplayEffectAsset.CachedGrantedTags`, `ActiveGameplayEffectsContainer.Add/RemoveActiveGameplayEffectGrantedTagsAndModifiers`, `AbilitySystemComponent.UpdateTagMap`.

| ID | 확인 내용 | 기대 | UE 근거 |
|---|---|---|---|
| G-1·2 | Application Require / Ignore | 조건을 못 맞추면 적용 안 됨(Instant·Infinite) | `TargetTagRequirementsGameplayEffectComponent.cpp` `CanGameplayEffectApply` |
| G-3 | Removal 조건을 이미 맞춤 | 적용도 막힘 | 같은 함수 |
| G-4 | Ongoing Require | 조건 없이 적용 → 꺼진 채 / 태그 생김 → 켜짐 / 없어짐 → 꺼짐(제거 아님) | `OnActiveGameplayEffectAdded` 반환값, `OnTagChanged` |
| G-5 | GE 제거 뒤 태그 변화 | 아무 일 없음(구독 해제) | `OnGameplayEffectRemoved` |
| G-6 | Removal Require | 적용 중 태그 생김 → 제거 | `OnTagChanged` |
| G-7 | 다른 GE의 Granted Tag로 Ongoing 충족 | 부여 GE 적용 → 켜짐, 제거 → 꺼짐 | 태그 연동 전체 흐름 |
| G-8 | Ongoing TagQuery | 질의에 나오는 태그도 구독해 켜짐 | 구독 태그에 `TagQuery.GetGameplayTagArray` 포함 |
| G-9 | 정리 | Health 100, 태그 0 | |

실패 시 의심(G): `TargetTagRequirementsGameplayEffectComponent`, `ActiveGameplayEffectEvents`(제거 알림), `ActiveGameplayEffectsContainer.RemoveActiveGameplayEffect`(알림 순서)·`SetActiveGameplayEffectInhibit`, `GameplayTagCountContainer` 이벤트.

| ID | 확인 내용 | 기대 | UE 근거 |
|---|---|---|---|
| H-1 | `IsEmpty` | 빈 질의 true, 태그 질의 false | `GameplayEffect.cpp` `FGameplayEffectQuery::IsEmpty` |
| H-2~8 | 태그 질의 4종 | Effect=Asset Tags(부모 태그로도 맞음), Owning=Asset+Granted, Source Spec 태그엔 Asset Tags도 들어 있음 | `FGameplayEffectQuery::Matches(Spec)` `:5792-5845` |
| H-9·10 | ModifyingAttribute·EffectDefinition | 그 어트리뷰트를 바꾸는 GE·그 에셋의 GE만 | 같은 함수 `:5848-5885` |
| H-11·12 | IgnoreHandles·CustomMatchDelegate | 무시 핸들은 빠짐, 델리게이트만 채워도 비어 있지 않음 | `Matches(ActiveGE)` `:5752-5781` |
| H-13 | `GetAggregatedStackCount` | 켜진 GE만 셈(스택 없어 GE당 1) | `GetActiveEffectCount` `:5394` |
| H-14·15 | 시간 조회 | 지속·남은 시간, 가장 늦게 끝나는 시각, 끝없는 GE가 섞이면 -1 | `:5210-5338` |
| H-16 | 레벨 변경(질의) | 맞는 GE만 레벨이 바뀜 | `SetActiveGameplayEffectLevel` `:3679` |
| H-17~20 | 제거 API | 지운 수 반환, 효과·부여 태그 회수 | `RemoveActiveEffects` `:5375`, ASC `RemoveActiveEffectsWith*` |
| H-21 | 연쇄 제거 | 지우다 다른 GE가 스스로 빠져도 예외 없음 | UE는 scope lock — 우리는 핸들을 먼저 모음 |
| I-1~3 | Remove Other | 맞는 다른 GE 제거, 자기·먼저 걸린 같은 GE는 남김, 에셋 질의 그대로 | `RemoveOtherGameplayEffectComponent.cpp:16-50` |
| I-4 | 질의 둘 | 둘째 질의에서도 자기는 남음 | UE와 다름 → gameplay-effect D24 |
| I-5·6 | 빈 질의·Instant | 빈 질의 건너뜀, Instant도 적용 때 지움 | 같은 함수 |
| I-7 | 정리 | Health 100, 활성 GE 0 | |

실패 시 의심(H·I): `GameplayEffectQuery`, `ActiveGameplayEffectsContainer` 질의 절, `ActiveGameplayEffect.GetDuration·GetTimeRemaining·GetEndTime`, `RemoveOtherGameplayEffectComponent`.

실패 시 의심: A·B는 `ActiveGameplayEffectsContainer.ApplyGameplayEffectSpec`·`ExecuteActiveEffectsFrom` 끝과 `GameplayEffectAsset`의 `CanApply`·`OnAddedToActiveContainer`·`OnExecuted`·`OnApplied`, C·D는 `InternalOnActiveGameplayEffectAdded`·`SetActiveGameplayEffectInhibit`·`RemoveActiveGameplayEffect`·`InternalExecutePeriodicGameplayEffect`.

---

### 6-2. 시간이 걸린 동작 — PlayMode

`Assets/Scripts/Test/TagRequirementsPlayModeTest.cs`. 저장 안 하는 씬에 이 컴포넌트만 붙이고 Play(에디터에서 — native 테스트 태그가 에디터 어셈블리에 있다) → Console `[TagPlayMode Self-Check] passed 9 / failed 0`. ASC.Update가 도는 실제 프레임 위에서 만료·주기 실행과 태그 연동이 맞물리는지 본다.

- **6-2-1.** P-1·2 부여 GE(Duration 0.3초) 만료 → 태그 없어짐 → 그 태그로 켜져 있던 GE 꺼짐(제거 아님) / P-3~5 꺼진 주기 GE는 시간이 지나도 실행 안 함, 켜지면 재개, 다시 꺼지면 멈춤 / P-6·7 주기 GE가 도는 중 Removal 태그 → 즉시 제거, 그 뒤 실행 없음 / P-8 꺼진 Duration GE도 시간이 되면 만료 / P-9 정리.

---

## 7. 테스트용으로 추가한 것 (정리 대상)

| 것 | 위치 | 지울 때 |
|---|---|---|
| 셀프체크 코드 + native 테스트 태그 2개 | `Assets/Scripts/Test/Editor/ModifierTagSelfCheck.cs` | 파일 삭제 → native 태그도 사라짐 |
| PlayMode 태그 연동 테스트(GE는 메모리에서 만듦) | `Assets/Scripts/Test/TagRequirementsPlayModeTest.cs` | 파일 삭제 |
| GE Component 셀프체크 + native 테스트 태그 `Test.GEComponent.Granted`·`Test.GEComponent.Asset`·`Test.GEComponent.Asset.Child`·`Test.GEComponent.Other` | `Assets/Scripts/Test/Editor/GameplayEffectComponentSelfCheck.cs` | 파일 삭제 → native 태그도 사라짐 |
| 레지스트리 테스트 태그 | `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`의 `Test.ModifierTag.Registry.Stunned` | 관리 창에서 Delete 또는 줄 삭제. 지우면 셀프체크 0-3이 실패한다 |
