# Code Convention

Unity C# 코드 컨벤션 문서입니다.  
네이밍/포맷팅 규칙은 `.editorconfig`에 의해 자동 적용되며, 이 문서는 그 기준을 설명합니다.

---

## 설계 원칙

`CLAUDE.md`의 "과설계 지양"을 코드 단위로 구체화한다.

- **필요할 때만 범용화한다.** 실제 유스케이스가 요구하지 않는 일반화는 과설계다. 지금 케이스가 실제로 요구하는 범위를 먼저 확인하고 그 범위만 구현한다. 범용 훅(확장점)은 열어두되, 특수 케이스는 특수한 곳에서 조립한다. *(예: "리스트 내 중복 제외"를 "리스트 외부"까지 일반화하려다, 실제론 자기 배열 스코프면 충분했던 경우 — 범용 드로어엔 훅만 두고 전용 드로어가 조립.)*
- **좁은 스코프·설계 갈림은 착수 전에 표면화·확인한다.** 구현이 특정 좁은 가정 위에서만 성립하거나, 스코프·정책이 여러 갈래로 갈리면, 한 쪽을 임의로 정하지 말고 그 한정성/갈림을 먼저 드러내 확인한다. (작업 프로세스 규약은 [`../agent/HARNESS.md`](../agent/HARNESS.md) §3.2.)
- **범용 코드는 정책을 모른다.** 재사용 컴포넌트(범용 드로어·유틸)에 특정 사용처의 정책(무엇을 제외/우선할지 등)을 심지 않는다. 범용 쪽은 결과를 **인자로 받아 처리**만 하고, "무엇을"은 호출측이 정해 넘긴다.

---

## 네이밍

| 대상 | 규칙 | 예시 |
|---|---|---|
| 파일명 | PascalCase | `PlayerController.cs` |
| 클래스 / 구조체 | PascalCase | `PlayerController` |
| 인터페이스 | `I` + PascalCase | `IDamageable` |
| 열거형 | PascalCase | `GameState.Playing` |
| 메서드 | PascalCase | `TakeDamage()` |
| 프로퍼티 | PascalCase | `IsGrounded` |
| public 필드 | PascalCase | `MoveSpeed` |
| 정적 필드 | PascalCase | `InstanceCount` |
| 상수 | PascalCase | `MaxHealth` |
| private 필드 | `_` + camelCase | `_health`, `_rigidbody` |
| 지역 변수 / 매개 변수 | camelCase | `deltaTime`, `damage` |
| 이벤트 | `On` + PascalCase | `OnDeath`, `OnDamaged` |

```csharp
public class PlayerController : MonoBehaviour
{
    public float MoveSpeed = 5f;

    [SerializeField] private float _jumpForce = 8f;
    private Rigidbody _rigidbody;

    public event Action OnDeath;

    public void TakeDamage(int damage)
    {
        _health -= damage;
    }
}
```

---

## 네임스페이스 · 폴더 구조

- **namespace = 폴더 경로** (`Assets/Scripts/` 기준). 예: `Assets/Scripts/Core/ItemSystem/Inventory/` → `Core.ItemSystem.Inventory`. 폴더를 옮기면 namespace도 함께 맞춘다.
- **에디터 스크립트는 feature-local `Editor/` 폴더**에 둔다 — 상단에 `Editor/` 트리를 따로 두지 않고, 대상 코드 옆 서브시스템 레벨에 배치한다. 예: `Core/AbilitySystem/Attribute/Editor/`, `Core/ItemSystem/Equipment/Editor/`. (Unity는 이름이 `Editor`인 폴더를 위치·깊이와 무관하게 에디터 전용 어셈블리로 컴파일하므로, 배치는 순수하게 응집도 기준으로 정한다.)

---

## 포맷팅

### 들여쓰기

스페이스 4칸 (탭 금지).

### 중괄호 — Allman 스타일, 생략 금지

여는 중괄호는 항상 다음 줄에 두고, 한 줄짜리 블록도 중괄호를 생략하지 않습니다.

```csharp
// ✅
private void Update()
{
    if (IsGrounded)
    {
        Jump();
    }
}

// ❌ 같은 줄 중괄호
private void Update() {
    if (IsGrounded) {
        Jump();
    }
}

// ❌ 중괄호 생략
if (isDead)
    return;
```

### var 사용

우변에서 타입이 명확한 경우 외에는 `var` 사용을 권장하지 않습니다.

```csharp
// ✅ 타입이 명확
var player = GetComponent<PlayerController>();

// ❌ 타입이 불명확한 경우 명시
var scoreMap = GetScoreMap();
```

### this 생략

```csharp
// ✅
_health = 100;

// ❌
this._health = 100;
```

### 메서드 시그니처

매개변수가 많아도 줄넘김하지 않고 한 줄로 작성합니다.

### switch

`case`는 fall-through 포함 항상 `{}` 블록을 사용합니다.

```csharp
switch (state)
{
    case GameState.Playing:
    {
        Tick();
        break;
    }
    default:
    {
        break;
    }
}
```

---

## 접근 한정자

접근 한정자는 항상 명시합니다.

```csharp
// ✅
private int _health;
private void HandleInput() { }

// ❌
int _health;
void HandleInput() { }
```

---

## Unity 특이사항

### MonoBehaviour 멤버 순서

```csharp
public class Example : MonoBehaviour
{
    // 1. Fields
    // 2. Properties
    // 3. Unity Lifecycle (Awake → OnEnable → Start → Update → ...)
    // 4. public Methods
    // 5. private Methods
    // 6. Event Callbacks
}
```

---

## 에디터 확장 (드로어 · 인스펙터)

> 실전 규칙·안티패턴·재사용 유틸 목록은 [`editor-drawer-guide.md`](editor-drawer-guide.md)에 상세히 있다. 새 드로어를 짜기 전에 그 문서를 먼저 본다.

- **에디터 스크립트는 feature-local `Editor/` 폴더**에 둔다 (위 [네임스페이스 · 폴더 구조](#네임스페이스--폴더-구조) 참고).
- **범용 드로어와 전용 정책을 분리한다.** 범용 `PropertyDrawer`는 "그리기·입력"만 담당하고, 특정 데이터의 정책(중복 제외 스코프, 필터, 정렬 등)은 그 데이터의 **전용 드로어**가 조립한다. 범용 드로어는 결과(예: 제외 목록)를 **인자로 받을 뿐**, 스스로 수집·판단하지 않는다. 범용 쪽에 특정 사용처 전용 플래그를 심지 않는다.
  - *예 — 타입 선택 `SubclassSelector`:* 범용 `SubclassSelectorDrawer`는 그리기 `DrawSelector(rect, prop, baseType, excluded, label)` 와 수집 헬퍼 `CollectSiblingValues(prop)`를 **노출만** 한다. "AttributeSet은 같은 리스트에서 중복 금지" 같은 정책은 전용 `AttributeSetInitDataDrawer`가 `CollectSiblingValues`로 모아 `DrawSelector`의 `excluded`로 넘겨 조립한다. (범용 드로어에 `uniqueInList` 같은 특수 플래그를 넣지 않는다.)
- **런타임 / 에디터 경계.** 런타임 직렬화 데이터(`[Serializable]` 값 클래스)는 부모·외부 구조를 모른다(부모 참조가 없다). "형제·외부 요소를 참조하는" 로직은 런타임이 아니라 **에디터의 `SerializedProperty`**(`serializedObject`로 전체 트리 접근 가능)에서 한다. `propertyPath`를 파싱해 배열/부모/형제로 이동한다.

---

## 주석 — 사람과 AI 에이전트를 함께 대상으로

이 저장소의 코드는 사람과 AI 에이전트가 **둘 다** 읽고 수정한다. 좋은 주석은 사람에겐 맥락을, 에이전트에겐 정확한 생성·검색 앵커를 준다. 관통 원칙 하나: **코드가 스스로 말하지 못하는 "왜"만, 낡지 않게, 근거에 앵커해서 적는다.** (주석이 길어지면 코드가 불명확하다는 신호 — 주석보다 코드·이름을 먼저 고친다.)

### 이름이 먼저, 주석은 그다음

자기설명적 이름으로 "무엇"을 없애고, 주석은 이름이 담을 수 없는 "왜"에만 쓴다. (에이전트·사람 모두 긴 서술형 이름을 낡은 주석보다 신뢰한다.)

```csharp
// ❌ 이름이 약해 주석으로 때움
// 구매액에 대한 멤버십 할인 계산
float Calc(User u, float amount)

// ✅ 이름이 '무엇'을 말함 → 주석 불필요
float CalculateMembershipDiscount(User user, float purchaseAmount)
```

### "왜"를 적고, "무엇"은 적지 않는다

코드를 기계적으로 되뇌는 주석은 소음이다. 제약·트레이드오프·엣지케이스·비직관적 선택의 **이유**를 적는다.

```csharp
// ✅ 이유 설명
// Rigidbody 이동은 FixedUpdate에서 처리해야 물리 스텝과 어긋나지 않음
private void FixedUpdate() { }

// ❌ 코드 반복
// FixedUpdate 함수
private void FixedUpdate() { }
```

### "왜"는 근거 문서에 앵커한다 (이 저장소의 하우스 스타일)

결정의 근거가 불변조건·설계 결정에 있으면 그 **식별 코드**(`INV-N`, 결정 `D#`, 로드맵 슬라이스 `S#`/`I#`)를 주석 앞에 박는다. 사람은 권위 문서로 점프하고, 에이전트는 관련 맥락을 정확히 검색·준수한다.

```csharp
// INV-5: IModuleState → 구체 상태 캐스트는 이 제네릭 접근자 한 곳에만 둔다.
public TState GetState<TState>(int moduleIndex) where TState : class, IModuleState { }
```

### 상호 의존은 명시적으로 (cross-reference)

"여길 바꾸면 저기도"인 숨은 결합은 주석으로 드러낸다 — 에이전트가 함께 고칠 지점을 놓치지 않게, 사람이 파급을 예측하게.

```csharp
// 이 스택 병합 규칙을 바꾸면 InventoryTests의 스택 케이스도 같이 갱신.
```

### 변하기 쉬운 값을 문자로 복제하지 않는다 (주석 드리프트 방지)

임계값·조건을 주석에 그대로 베끼면 코드만 바뀔 때 주석이 거짓말이 된다. **의도**를 적고 경계값은 코드에 맡긴다.

```csharp
// ❌ "상한 초과 시 분리" — 코드가 >= 로 바뀌면 주석이 틀림
// ✅ "상한을 채우면 새 엔트리로 분리" (정확한 경계는 코드가 진실)
```

### 주석 = 왜 / 테스트 = 무엇

동작 명세는 주석이 아니라 테스트로 고정한다. 주석은 이유를, 테스트는 기대 동작을 책임진다.

### 공개 API는 XML 주석 사용

```csharp
/// <summary>
/// 플레이어에게 데미지를 줍니다.
/// </summary>
/// <param name="damage">입힐 데미지 양</param>
public void TakeDamage(int damage) { }
```

### 유지: 코드를 바꾸면 닿은 주석도 함께 갱신

낡은 주석은 없는 주석보다 위험하다. AI가 생성한, 코드를 그대로 되뇌거나 실제 동작과 어긋나는 주석은 **병합 전에 지운다**.

> 참고(2026-07 조사): [Stack Overflow — Coding guidelines for AI agents (and people too)](https://stackoverflow.blog/2026/03/26/coding-guidelines-for-ai-agents-and-people-too/) · [Tusk — The case for comment-driven development](https://www.usetusk.ai/resources/the-case-for-comment-driven-development) · [TechTarget — Code comment best practices](https://www.techtarget.com/searchsoftwarequality/tip/Code-comment-best-practices-every-developer-should-know)