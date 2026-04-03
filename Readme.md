# Code Convention

Unity C# 코드 컨벤션 문서입니다.  
네이밍/포맷팅 규칙은 `.editorconfig`에 의해 자동 적용되며, 이 문서는 그 기준을 설명합니다.

---

## 네이밍

| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스 / 구조체 | PascalCase | `PlayerController` |
| 인터페이스 | `I` + PascalCase | `IDamageable` |
| 메서드 | PascalCase | `TakeDamage()` |
| 프로퍼티 | PascalCase | `IsGrounded` |
| public 필드 | PascalCase | `MoveSpeed` |
| private 필드 | `_` + camelCase | `_health`, `_rigidbody` |
| 지역 변수 | camelCase | `deltaTime` |
| 매개 변수 | camelCase | `damage` |
| 상수 | PascalCase | `MaxHealth` |
| 이벤트 | `On` + PascalCase | `OnDeath`, `OnDamaged` |
| 열거형 | PascalCase | `GameState.Playing` |

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

## 포맷팅

### 중괄호 — Allman 스타일

```csharp
// ✅
private void Update()
{
    if (IsGrounded)
    {
        Jump();
    }
}

// ❌
private void Update() {
    if (IsGrounded) {
        Jump();
    }
}
```

### 중괄호 생략 금지

```csharp
// ✅
if (isDead)
{
    return;
}

// ❌
if (isDead)
    return;
```

### var 사용

타입이 명확한 경우 외에는 `var` 사용을 권장하지 않습니다.

```csharp
// ✅
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

### MonoBehaviour 메서드 순서

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

## 주석

### 공개 API는 XML 주석 사용

```csharp
/// <summary>
/// 플레이어에게 데미지를 줍니다.
/// </summary>
/// <param name="damage">입힐 데미지 양</param>
public void TakeDamage(int damage) { }
```

### 인라인 주석은 이유를 설명

```csharp
// ✅ 이유 설명
// Rigidbody 이동은 FixedUpdate에서 처리해야 물리 연산이 정확함
private void FixedUpdate() { }

// ❌ 코드 반복
// FixedUpdate 함수
private void FixedUpdate() { }
```