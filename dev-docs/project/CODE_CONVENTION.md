# Code Convention

Unity C# 코드 **스타일** 규약(네이밍·포맷팅·멤버 순서). `.editorconfig`로 자동 적용된다.

> 코드를 **어떻게 짤지**(설계 원칙·연쇄 호출·주석·에디터 확장)는 → [`../agent/code-guide/HARNESS.md`](../agent/code-guide/HARNESS.md) (코드 작성 규약).

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
| private 필드 | `_` + camelCase | `_health` |
| 지역 변수 / 매개 변수 | camelCase | `deltaTime` |
| 이벤트 | `On` + PascalCase | `OnDeath` |

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

- namespace = 폴더 경로(`Assets/Scripts/` 기준). 폴더 이동 시 namespace도 맞춘다.
- `Core/` = 재사용 가능한 시스템 구조만(장착·인벤토리·어빌리티). 게임플레이 구현부(공격 드라이버·픽업)는 Core 밖(`Item/`·`Character/`)에 둔다.
- 에디터 스크립트는 feature-local `Editor/` 폴더에 둔다. 상단에 `Editor/` 트리를 따로 두지 않는다.

---

## 포맷팅

### 들여쓰기

스페이스 4칸 (탭 금지).

### 중괄호 — Allman 스타일, 생략 금지

여는 중괄호는 항상 다음 줄. 한 줄짜리 블록도 중괄호를 생략하지 않는다.

```csharp
// ✅
private void Update()
{
    if (IsGrounded)
    {
        Jump();
    }
}

// ❌ 같은 줄 중괄호 / 중괄호 생략
private void Update() {
    if (IsGrounded)
        Jump();
}
```

### 표현식 본문(`=>`) — 프로퍼티/인덱서 getter에만

단일 식 프로퍼티·인덱서 getter에만 허용. 메서드는 블록 본문 + 명시적 `return`.

```csharp
// ✅ 프로퍼티/인덱서 getter
public bool IsValid => _field != null;
public int Count => _list.Count;

// ✅ 메서드는 블록 본문
private AttributeAggregator FindAggregator(GameplayAttributeHandle handle)
{
    return _attributeAggregatorMap.GetValueOrDefault(handle);
}

// ❌ 메서드를 => 로 축약
private AttributeAggregator FindAggregator(GameplayAttributeHandle handle)
    => _attributeAggregatorMap.GetValueOrDefault(handle);
```

### var — 우변에 타입이 드러날 때만

우변에 타입이 드러날 때만(`new T()`, `(T)x`, `x as T`, `GetComponent<T>()`). 타입이 숨으면 명시.

```csharp
// ✅ 타입이 우변에 보임
var player = GetComponent<PlayerController>();
var list = new List<int>();
var enemy = (Enemy)obj;

// ❌ 타입이 숨음 → 명시
Dictionary<int, float> scoreMap = GetScoreMap();
```

### this 생략

```csharp
// ✅
_health = 100;

// ❌
this._health = 100;
```

### 긴 줄 — 한 줄 유지

길이가 길어도 한 줄로 유지한다(시그니처·호출·조건식 전반). 예외: LINQ류 쿼리 메서드 체인만 줄을 나눈다. (연쇄 호출 자체의 규칙은 코드 작성 규약 '연쇄 호출' 참고.)

```csharp
// ✅ 길어도 한 줄
private void Apply(GameplayEffectSpec spec, AttributeAggregator agg, GameplayTagContainer tags, float magnitude)
{
}

// ✅ 예외 — LINQ 쿼리 체인은 줄 나눔 허용
var actives = effects
    .Where(e => e.IsActive)
    .Select(e => e.Handle);
```

### switch

`case`는 fall-through 포함 항상 `{}` 블록.

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

항상 명시한다.

```csharp
// ✅
private int _health;
private void HandleInput() { }

// ❌
int _health;
void HandleInput() { }
```

---

## 멤버 순서

MonoBehaviour든 일반 클래스든 아래 순서로 배치한다.

```csharp
public class Example
{
    // 1. 상수 / static 필드
    // 2. 필드
    // 3. 프로퍼티
    // 4. 생성자 (MonoBehaviour면 Unity Lifecycle: Awake → OnEnable → Start → Update → ...)
    // 5. abstract / virtual / override 메서드
    // 6. public 메서드
    // 7. protected 메서드
    // 8. private 메서드
    // 9. 이벤트 콜백 (구독 핸들러)
}
```
