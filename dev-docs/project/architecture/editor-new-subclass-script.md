# Editor Tooling — New Script (서브클래스 생성기)

타입 선택 드롭다운에서 **"New Script..."**로 베이스 타입을 상속한 새 클래스를 바로 만들고, 컴파일 후 그 자리에 자동으로 꽂아 주는 에디터 공용 툴. Unity의 *Add Component → New Script* 흐름을 흉내낸다.

- 코드: `Assets/Scripts/Core/Common/Editor/`
- 첫 사용처: Attribute Set 셀렉터 (`AttributeDefinitionAsset`). 결정 근거는 feature `ability-system/attribute` [decisions.md](../../agent/feature/ability-system/attribute/decisions.md) **D4**.
- **반영 기준:** feature `ability-system/attribute` @ 2026-08-07 (KST) — HARNESS §3.4.

> 바깥에서 안으로 읽는다. 1·2절만 봐도 "무엇을·어떻게 쓰나"는 끝난다. 3절부터는 내부, 5절이 가장 깊다.

---

## 1. 겉에서 본 동작 (에디터 사용자 시점)

`AttributeDefinitionAsset`을 열고 **Attribute Sets 리스트의 "+"**를 누르면 타입을 고르는 검색 드롭다운이 뜬다. 맨 아래 **"New Script..."**를 고르면:

```
"New Script..." 클릭
   → 작은 창: 이름(과 폴더) 입력
   → [Create and Assign]
   → 잠깐 컴파일
   → 새로 만든 AttributeSet이 리스트에 항목으로 추가돼 있음 (직접 다시 고를 필요 없음)
```

만들어지는 파일은 항상 베이스 타입을 상속한다 — 예를 들어 `AttributeSet`이면:

```csharp
using Core.AbilitySystem.Attribute;

public class MyNewSet : AttributeSet
{
}
```

즉 사용자는 "이름만 적으면 상속까지 된 빈 클래스가 생기고, 그 자리에 바로 꽂힌다"만 알면 된다.

---

## 2. Entry Point — 붙이는 법 (개발자 인터페이스)

이 툴을 **다른 셀렉터에 켜려는 개발자**가 만지는 면은 딱 이것뿐이다. 진입점은 `NewSubclassScript` 하나이고, 두 메서드가 전부다:

```csharp
// 기존 요소(필드)의 타입을 새 스크립트로 바꾼다  — 셀렉터 필드 "+"에서 사용
NewSubclassScript.OpenForField(rect, baseType, target, propertyPath);

// 배열에 새 요소를 추가하며 타입을 새 스크립트로 채운다  — 리스트 헤더 "+"에서 사용
NewSubclassScript.OpenForListAdd(rect, baseType, target, arrayPath, typeRelPath, clearArrayRelPath);
```

`baseType`을 넘기는 대로 그 타입을 상속하는 스크립트가 만들어진다(`AttributeSet`에 묶여 있지 않다). 팝업 열기·컴파일·자동 배정 같은 내부는 **몰라도 된다** — 넘기는 건 "어디에 꽂을지"(target·경로)뿐이다.

실제로 켜는 건 **한 줄**이다. Attribute Sets 리스트는 리스트 빌더에 opt-in 인자만 넘겨 켠다:

```csharp
// AttributeDefinitionAssetDrawer
TypeChoiceList.Create( … ,
    newScriptBaseType: typeof(AttributeSet),      // 이걸 주면 add 팝업에 "New Script..."가 생김
    newScriptTypeRelPath: "attributeSetTypeName", // 만든 타입을 새 요소의 어디에 쓸지
    newScriptClearArrayRelPath: "attributes");
```

안 켠 셀렉터/리스트는 검색만 되고 "New Script..." 항목은 아예 안 뜬다.

---

## 3. 부르고 나면 — 전체 흐름

`OpenFor…`를 부른 뒤 벌어지는 일을 큰 단위로만:

```
1. 이름 입력 창을 띄운다
2. 사용자가 이름을 적고 Create
3. 그 이름으로 .cs 파일을 만든다        (상속 골격까지 채워서)
4. 컴파일이 돈다 → 새 타입이 생김
5. 컴파일이 끝나면, 만든 타입을 원래 그 필드/리스트에 자동으로 꽂는다
```

여기서 4번이 문제의 핵심이다. **컴파일은 도메인 리로드를 일으키고, 그 순간 창도 "지금 편집 중이던 그 필드"도 전부 사라진다.** 그래서 5번(자동 배정)은 리로드 *전*에 그냥 이어서 할 수 없다 — 리로드를 건너뛰어야 한다. 이걸 어떻게 하는지가 5절이다.

---

## 4. 흐름을 나눈 조각들 (내부 구조)

3절의 각 단계를 누가 맡는지로 나눴다. 의존은 **한 방향** — 위가 아래를 알고, 아래는 위를 모른다.

```
드로어 (SubclassSelectorDrawer · TypeChoiceList)
   │   2절의 OpenFor… 를 부른다
   ▼
NewSubclassScript ·············· 진입점(파사드). 팝업 열기·타이밍 처리를 감춘다
   ├─▼ NewSubclassScriptPopup ··· 입력 창(1·2·3단계). 이름/폴더만 받는다
   │      └─▼ SubclassScriptTemplate ··· 순수 저작(3단계). 소스 조립·검증·파일 쓰기
   └─▼ PendingSubclassAssignment ······ 리로드 후 배정(5단계)
```

| 조각 | 맡는 것 | 모르는 것 |
|---|---|---|
| `NewSubclassScript` | 진입점. `OpenForField` / `OpenForListAdd` | UI·저작·배정 세부 |
| `NewSubclassScriptPopup` | 이름/폴더 입력 창 (표현부) | 배정 로직 |
| `SubclassScriptTemplate` | 네임스페이스 유도·소스 조립·이름 검증·`.cs` 생성 | UI·리로드 |
| `PendingSubclassAssignment` | 리로드를 넘겨 만든 타입을 원위치에 배정 | UI |

이렇게 나눠서, 드로어는 진입점 한 줄만 알고 UI 창은 배정 방식을 모르고 저작 로직은 UI를 모른다.

---

## 5. 가장 안쪽 — 리로드를 어떻게 넘기나

3절 4번의 도메인 리로드를 건너뛰는 방법. 배정을 **리로드 전에 예약**하고 **리로드 후에 실행**한다.

```
[리로드 전]                                [리로드]           [리로드 후]
· .cs 생성                                 · 컴파일           · 예약을 읽어 만든 타입을 배정
· "무엇을·어디에 꽂을지"를 예약해 둠   ──►  · 도메인 리로드 ──►  · 예약은 한 번 쓰고 즉시 비움
· Refresh() 로 컴파일 태움                                    · 만든 스크립트를 Project에 ping
```

리로드를 넘기는 데 필요한 두 가지 도구:

- **`SessionState`** — 도메인 리로드를 넘겨 살아남는 임시 저장소. "무엇을·어디에 배정할지"(대상·경로·타입명)를 여기 적어 두면, 리로드 후 `[DidReloadScripts]` 콜백이 읽어서 배정하고 곧바로 지운다(재적용 방지).
- **`GlobalObjectId`** — 리로드를 넘겨도 같은 에셋을 다시 찾게 해 주는 ID. 보통의 오브젝트 참조는 리로드로 끊기므로, "그 필드가 있던 대상"을 이 ID로 저장해 두고 리로드 후 되찾는다.

두 진입점(`OpenForField` / `OpenForListAdd`)의 차이도 결국 여기서만 갈린다 — 리로드 후 **기존 필드에 값을 넣느냐**, **배열에 새 요소를 추가하고 채우느냐**. 그 앞 단계(창·생성·리로드 넘김)는 완전히 같다.
