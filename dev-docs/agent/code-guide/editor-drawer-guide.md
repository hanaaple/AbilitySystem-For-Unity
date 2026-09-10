# 에디터 드로어 구현 가이드

> Unity 커스텀 인스펙터/PropertyDrawer를 만들 때의 실전 규칙. 2026-07-08 에디터 툴링 정리에서
> 실제로 터진 버그와 거기서 정착시킨 재사용 유틸을 근거로 한다. **새 드로어를 짜기 전에 이 문서를 먼저 본다.**
> 상위 규약(설계 원칙·범용/전용 분리)은 [`HARNESS.md`](HARNESS.md)(코드 작성 규약) 참고. 스타일은 [`../../project/CODE_CONVENTION.md`](../../project/CODE_CONVENTION.md).

---

## 1. 먼저 재사용할 것 (있으면 새로 만들지 않는다)

공용 에디터 헬퍼는 **`Assets/Scripts/Core/Common/Editor/`** 에 있다(런타임 참조가 필요한 속성만 `Editor` 폴더 밖 `Core.Common`에). 아래로 되는 일은 직접 배선하지 않는다.

| 유틸 | 용도 |
|---|---|
| `TypeChoiceList.Create(...)` | 타입 선택 팝업이 달린 다형 리스트를 `ReorderableList`로 통째 생성. 삽입·Update/Apply·빈 상태 팝업까지 처리. 호출부는 (1)후보 타입 (2)`onAdd(element,type)`만 준다. |
| `EditorTypeUtility.GetConcreteSubclasses(baseType)` | non-abstract·non-generic·무인자 생성자 구체 서브클래스를 이름순으로(캐시). |
| `EditorTypeUtility.FindScript(type)` | 타입 → 소스 `MonoScript`(파일명=클래스명 관례로 검색, 캐시). ping/open·아이콘 표시용. |
| `SubclassSelectorDrawer` + `Core.Common.SubclassSelectorAttribute` | AQN 문자열 필드를 특정 베이스의 서브클래스 선택 UI로. **자동 경로**: 필드에 `[SubclassSelector(typeof(Base))]` 한 줄이면 끝. UI는 선택된 타입의 스크립트를 오브젝트 필드처럼 표시(단일클릭 ping / 더블클릭 open) + 우측 검색 팝업. |
| `SubclassSelectorDrawer.DrawSelector / CollectArrayValues / CollectSiblingValues` | **직접 경로** 재사용 조각. 중복 제외 등 정책이 필요한 전용 드로어가 `excluded`를 모아(`CollectSiblingValues`/`CollectArrayValues`) `DrawSelector`에 넘겨 조립한다. |
| `SubclassAdvancedDropdown` | 검색 가능한 네이티브 타입 선택 팝업(Add Component 창과 동일). `SubclassSelectorDrawer`가 내부적으로 사용. |
| `EditorDrawUtility` | Foldout Rect/볼드 스타일 등 반복 GUI 헬퍼. |

---

## 2. DON'T — 실제로 터진 안티패턴

### ❌ abstract를 타깃하는 `CustomEditor`에서 `editorForChildClasses` 누락
`[CustomEditor(typeof(BaseSO))]`만 쓰면 그 에디터는 **정확히 그 타입에만** 붙는다. 베이스가 abstract이고
실제 에셋이 서브클래스면(예: `ItemData` → `EquipItem`/`ConsumeItem`) 커스텀 에디터가 적용되지 않고
기본 인스펙터로 떨어진다 → `[SerializeReference]` 리스트의 `+`가 타입 팝업 없이 null만 추가.
```csharp
[CustomEditor(typeof(ItemData), editorForChildClasses: true)]   // abstract면 반드시
```

### ❌ 배열 인덱스 propertyPath를 상태 캐시 키로 사용
`something.Array.data[i]` 경로는 **요소의 안정적 정체성이 아니다.** Add/Remove로 요소가 밀리면 같은
경로가 다른 요소를 가리킨다. "이 경로는 이미 sync했다"를 이걸로 캐싱하면 stale → 잘못된 스킵.
(AttributeSet 드로어에서 Attributes 필드가 간헐적으로 안 그려지던 원인.)
> 동기화는 **인덱스 캐시 대신 self-guard로 매번 호출**한다: 불일치가 있을 때만 배열을 변형하고 이미
> 맞으면 no-op이 되게 짜면, 매 OnGUI 호출해도 안전하고 값싸다.

### ❌ 타입만 다른 드로어 서브클래스를 N개 생성
피커 대상 타입이 다를 뿐인 드로어를 타입마다 만들지 않는다. **속성이 `Type`을 들고**(=
`SubclassSelectorAttribute(Type)`), 드로어는 하나로 두고 `[CustomPropertyDrawer(..., useForChildren: true)]`로
자식 별칭 속성까지 처리한다. `PropertyDrawer`는 `attribute` 필드로 자기 속성 인스턴스를 읽을 수 있다.

### ❌ 범용 드로어에 특정 사용처 정책을 심기
범용 드로어에 "이 데이터는 중복 금지" 같은 특정 정책 플래그(`uniqueInList` 등)를 넣지 않는다. 범용은
**결과(`excluded` 목록)를 인자로 받아 그리기만** 하고, "무엇을 제외/필터할지"는 그 데이터의 **전용 드로어**가
조립한다. (한 번 `uniqueInList` 플래그로 넣었다가, 특수 요구임을 인지하고 `DrawSelector(excluded)` + 전용
드로어 조립으로 되돌렸다.)

### ❌ `GUI.Button` + `clickCount`로 더블클릭 감지
`GUI.Button`은 **MouseUp에 발동**하는데 그 시점 `Event.current.clickCount`가 더블클릭을 신뢰성 있게 담지
못한다(단일 ping은 되는데 더블 open은 놓침). **`EventType.MouseDown` 이벤트에서 직접** `clickCount`를 보고
`Event.current.Use()`로 소비한다.

### ❌ 필요 없는 범용화(좁은 스코프를 미리 일반화)
실제 유스케이스가 요구하지 않는 확장은 과설계다. 예: "리스트 내 중복 제외"를 "리스트 외부"까지 일반화하려다
실제론 자기 배열 스코프면 충분했던 경우 — 범용 훅(`excluded` 인자)만 열어두고 전용 케이스는 전용 드로어에서
조립한다. 스코프·정책이 갈리면 먼저 확인한다([`../session-protocol.md`](../session-protocol.md) '작업 중').

### ❌ 직렬화 필드 rename 시 데이터 유실 방치
필드 이름이 곧 직렬화 키다. 이름을 바꾸면 기존 에셋에 저장된 값의 매핑이 끊긴다.
값을 지켜야 하면 `[FormerlySerializedAs("이전이름")]`을 붙이고, 지켜야 할 값이 없으면(초기 개발) 유실을
**명시적으로 인지**하고 넘어간다.

---

## 3. DO — 기본기

- **공용 헬퍼는 `Core/Common/Editor/`에**, 그중 런타임에서 참조해야 하는 것(속성 등)은 **`Editor` 폴더 밖**의
  런타임 어셈블리(예: `Core.Common`)에 둔다 — 드로잉 로직만 `Editor`에.
- **에디터 스크립트는 feature-local `Editor/` 폴더**에 둔다(대상 코드 옆, 서브시스템 레벨. 예:
  `Core/AbilitySystem/Attribute/Editor/`). 상단에 `Editor/` 트리를 따로 두지 않는다.
- **폴더 = 네임스페이스**를 지킨다(신설·이동 폴더는 네임스페이스도 맞춘다).
- **범용 드로어는 정책을 모른다.** 전용 드로어가 `excluded` 등 정책을 모아 재사용 진입점(`DrawSelector`)에 넘긴다.
- **런타임/에디터 경계.** 런타임 직렬화 데이터(`[Serializable]` 값 클래스)는 부모·형제를 모른다(부모 참조 없음).
  형제·외부를 훑는 로직은 에디터의 `SerializedProperty`(`serializedObject`로 전체 트리 접근)에서 `propertyPath`를
  파싱해 한다(`CollectSiblingValues` 참고).
- **메뉴/드롭다운 라벨은 `ObjectNames.NicifyVariableName`** 으로 통일.
- **콜백(GenericMenu·AdvancedDropdown) 안에서는 `SerializedProperty`를 재조회**한다(`serializedObject` +
  `propertyPath`). 콜백 시점에 원래 프로퍼티가 무효화될 수 있다.
- **높이(`GetPropertyHeight`)와 그리기(`OnGUI`)의 소스를 일치**시킨다. 높이는 리플렉션 필드 수로,
  그리기는 배열로 계산하는 식으로 어긋나면 "공간은 잡히는데 내용은 빈" 증상이 난다.

---

## 4. 참고 — 정리로 생긴 자산
- 빌더: `TypeChoiceList` / 열거·스크립트 조회: `EditorTypeUtility`(`GetConcreteSubclasses`·`FindScript`)
- 타입 피커: `SubclassSelectorDrawer`(+`SubclassSelectorAttribute`) · 검색 팝업 `SubclassAdvancedDropdown`
- 재사용 진입점: `DrawSelector`(그리기) · `CollectArrayValues`/`CollectSiblingValues`(제외 수집 코어)
- 위치: 공용은 `Core/Common/Editor/`, 필드 속성은 `Core/Common/`
- 버그픽스 근거: item worklog · attribute worklog 2026-07-08 항목.
