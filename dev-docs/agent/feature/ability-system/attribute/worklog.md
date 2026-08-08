# attribute — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-08-07 — 에디터 툴: Attribute Set 셀렉터 "New Script..." (템플릿 생성 + 자동 배정) (→D4)

**요청.** Attribute Set "+" 드롭다운에서 (Add Component→New Script처럼) 이름을 적어 새 Set을 만들면, 항상 `AttributeSet`을 상속한 템플릿으로 생성되게. 생성 후 그 필드에 자동으로 들어가면 좋겠다(유저가 자동 배정 선택).

**구현.** (→D4)
- `NewSubclassScriptPopup`(신규, `Core/Common/Editor`): Add Component 느낌의 인라인 이름 입력 팝업(`ShowAsDropDown`). baseType의 빈 서브클래스 .cs를 템플릿 생성, 네임스페이스는 폴더 경로에서 유도(`Assets/Scripts/X` → `X`). 이름 유효성·중복·파일 존재 검증. "무엇을 어디에 배정할지"는 몰라도 되게 `Action<className, scriptPath> onCreated` 콜백만 받음.
- `PendingSubclassAssignment`(같은 파일): 리로드로 팝업·SerializedProperty가 무효화 → 재배정 정보 `SessionState`, 대상 `GlobalObjectId` stash. `[DidReloadScripts]`+`delayCall`에서 타입 해석 후 배정. **두 모드**: Field(필드에 AQN) / Add(배열에 요소 삽입 후 AQN·attributes clear). 새 스크립트 ping.
- `SubclassAdvancedDropdown`: 범용화 — `(title, types, includeNone, onSelected, onNewScript, emptyMessage)`. onNewScript non-null이면 맨 아래 `New Script...`. 검색형 팝업을 필드/리스트가 공유.
- `SubclassSelectorDrawer`: `allowCreateNew`(기본 false)면 필드 "+"에 New Script(Field 모드). 후보 필터는 드로어가 `excluded`로 적용해 명시 목록으로 넘김.
- `TypeChoiceList`: 리스트 add 팝업을 `GenericMenu`→검색형 `SubclassAdvancedDropdown`으로 교체. `newScriptBaseType`(opt-in)이면 add "+"에 New Script(Add 모드, `newScriptTypeRelPath`·`newScriptClearArrayRelPath`로 새 요소 세팅). Item 모듈 리스트도 검색 가능해짐(New Script는 미노출).
- `AttributeDefinitionAssetDrawer`: `newScriptBaseType: typeof(AttributeSet)` 등으로 리스트 "+"에 opt-in. `AttributeSetDefinitionDrawer`: 필드 "+"에 `allowCreateNew: true`.

**과정 메모(수정 이력).** 처음엔 필드 셀렉터 "+"에만 넣었으나, 유저가 새 Set을 만드는 곳은 **리스트 헤더 "Attribute Sets +"**여서 안 보였다 → 리스트 add로 진입점 이동(필드에도 유지). 이어 add 팝업도 검색 가능하게(요청) `AdvancedDropdown`으로 통일. (중간에 존재 불확실한 `GUIUtility.GUIToScreenRect` 대신 `GUIToScreenPoint`로 교체 — §1.1 API 단정 회피.)

**구조 정리(SoC 리팩터, 유저 요청).** 남아 있던 호출부 배선 반복(`delayCall`+`Show`+`Stash…`)을 파사드로 흡수하고 책임별로 파일 분리. 의존은 한 방향: 드로어 → `NewSubclassScript`(파사드: 진입점, `delayCall` 넘김) → `NewSubclassScriptPopup`(UI 표현부, 입력만) → `SubclassScriptTemplate`(순수 저작: 네임스페이스 유도·소스 조립·검증·파일 생성) / `PendingSubclassAssignment`(리로드 생존·자동 배정). 드로어는 이제 `NewSubclassScript.OpenForField/OpenForListAdd` 한 줄만 부르고 내부(`SessionState`·`GlobalObjectId`·`delayCall`)를 모른다. UI 창은 배정 로직을 모르고, 저작 로직은 UI를 모른다.

**검증.** 코드 정합성만(에이전트 범위 — CLAUDE.md). 컴파일/플레이 확인은 유저가 Unity에서: `AttributeDefinitionAsset` → Attribute Sets "+" → (검색) New Script → 이름 입력 → 생성 → 리로드 후 새 요소로 자동 추가.

### 2026-07-08 — 버그픽스: `AttributeSetInitDataDrawer` Attributes 필드 간헐 미렌더

**증상(유저 리포트).** `AttributeInitData`에서 Attribute Set Add/Remove를 반복하면 가끔 특정 요소의 "Attributes" 라벨 아래 필드가 안 그려짐(빈칸). 지웠다 다시 하면 보이기도 하는 간헐성.

**원인.** `SyncAttributeFieldsIfChanged`가 `_lastSyncedTypeName`(static)에 **배열 인덱스 경로(`...data[i]...`)를 키로** "이미 sync함"을 캐싱 → Add/Remove로 요소가 밀리면 같은 경로가 다른 요소를 가리키는데도 sync를 스킵. `AttributeInitDataDrawer`의 add가 새 요소의 attributes를 `ClearArray()`하므로, "같은 인덱스·같은 타입" 재생성 시 캐시가 일치로 오판 → 빈 attributes가 그대로 렌더. 높이는 리플렉션 필드 수로 계산돼 빈칸만 남음(증상 일치).

**수정.** 인덱스 경로 캐시(`_lastSyncedTypeName`)와 `SyncAttributeFieldsIfChanged` 제거 → OnGUI에서 `SyncAttributeFields` 직접 호출. 이 메서드는 불일치가 있을 때만 배열을 변형하는 self-guard라 이미 맞으면 no-op → 매 프레임 호출해도 안전·저비용(에디터·소형 리스트). 성급한 최적화가 낳은 버그라 단순한 쪽으로 환원. 변경: `Editor/AbilitySystem/AttributeSetInitDataDrawer.cs` 1개.

**검증.** V1 정합성 OK(잔여 참조 0, 미사용 심볼 제거). V2 = 유저: 같은 타입 Add→Remove→같은 인덱스 Add 반복 시 매번 필드 즉시 렌더 확인. (선재 버그 — 같은 세션 다른 작업인 다형 리스트 빌더 리팩터와 무관, item worklog 2026-07-08 참조.)

### 2026-05-26 — PR #2 (Ability System - Attribute)
- Attribute 계층 구현: `AttributeSet`(빈 마커)·`AttributeData`(Base/Current)·`AttributeHandle`(FieldInfo 캐싱 struct), `CharacterAttributeSet`·`CombatAttributeSet` (→D1·D2)
- SO 초기화 파이프라인(`AttributeInitData`→`AttributeSetInitData`→`AttributeFieldInitData`) + `ASC.Awake` Reflection 세팅 (→D3)
- Editor 드로어: `AttributeInitDataDrawer`·`AttributeSetInitDataDrawer`·`AttributeReflectionUtility`
- ASC 최소 API: `AddAttributeSet`/`RemoveAttributeSet`, `GetAttributeBaseValue`, `SetBaseAttributeValue` (초기 109줄)
