# Code Convention

Unity C# 코드 컨벤션 문서입니다.  
네이밍/포맷팅 규칙은 `.editorconfig`에 의해 자동 적용되며, 이 문서는 그 기준을 설명합니다.

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

## 주석

### 공개 API는 XML 주석 사용

```csharp
/// <summary>
/// 플레이어에게 데미지를 줍니다.
/// </summary>
/// <param name="damage">입힐 데미지 양</param>
public void TakeDamage(int damage) { }
```

### 인라인 주석은 이유(WHY)를 설명

```csharp
// ✅ 이유 설명
// Rigidbody 이동은 FixedUpdate에서 처리해야 물리 연산이 정확함
private void FixedUpdate() { }

// ❌ 코드 반복
// FixedUpdate 함수
private void FixedUpdate() { }
```