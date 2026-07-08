# 컨트롤러-캐릭터 분리 (Possession 패턴)

입력/제어의 **주체**(Controller)와 **피제어 객체**(Character)를 분리하고, 둘을 런타임에 `Possess`로 연결한다. Unreal의 Controller–Pawn 관계에서 착안했다.

## 왜 분리했나

- **제어 주체 교체** — 같은 `PlayerCharacter`를 사람 입력(`PlayerController`)이든 AI든 빙의시킬 수 있다. 캐릭터는 "누가 조종하는지" 모른 채 명령(`MoveDelta`, `Attack`)만 받는다.
- **멀티플레이 확장 여지** — `HasAuthority()` 자리를 미리 만들어 두어, 소유권 판정을 나중에 붙일 지점을 고정.
- **입력 코드와 캐릭터 로직 분리** — 입력 바인딩·구독은 Controller에, 이동·전투 실행은 Character에 둔다.

## 구조

```
ControllerBase (abstract)                 CharacterBase (abstract)
├── _possessedCharacter                   ├── _controller
├── PossessedCharacter (get)              ├── IsAlive / OnDeath
├── Possess(CharacterBase)                ├── MoveDelta / StopMoving (abstract)
├── UnPossess()                           ├── TakeDamage / Die
├── HasAuthority()  ※항상 true (멀티 TODO) ├── OnPossessed / OnUnPossessed
├── OnPossessedCharacterDied              │
│                                         ├── PlayerCharacter
└── PlayerController                      └── MonsterCharacter
```

## 책임 분담

### ControllerBase
- `Possess(character)` — 이전 캐릭터가 있으면 먼저 `UnPossess()`, 그 뒤 `_possessedCharacter` 지정 → 캐릭터의 `OnDeath` 구독 → 캐릭터의 `OnPossessed(this)` 호출.
- `UnPossess()` — `OnDeath` 구독 해제 → 캐릭터의 `OnUnPossessed()` 호출 → 참조 해제.
- `OnPossessedCharacterDied` — 피제어 캐릭터 사망 시 자동으로 `UnPossess()` (빙의 대상이 죽으면 연결을 끊는다).

### CharacterBase
- 이동·정지·피해의 **실행 주체**. `MoveDelta`/`StopMoving`은 추상 — 캐릭터별로 구현.
- `OnPossessed`/`OnUnPossessed`는 빙의 시점의 훅. 컨트롤러 참조를 잡고, 파생 클래스가 빙의 부수효과를 붙일 지점.
- 사망은 `Die()` → `IsAlive=false` → `OnDeath` 발행. 컨트롤러가 이 이벤트로 언포제스한다.

## 구체 구현

### PlayerController → PlayerCharacter
- **입력 활성화**: `OnEnable`/`OnDisable`에서 `PlayerInputActions`의 액션맵을 Enable/Disable.
- **입력 구독**: 실제 콜백(`Move.performed/canceled`, `Attack.performed`) 구독·해제는 **`Possess`/`UnPossess` 시점**에 한다. 빙의된 캐릭터가 없을 때 입력을 흘려보내지 않기 위함.
- **이동 전달**: `Update`에서 매 프레임 `_moveInput`을 읽어 `PlayerCharacter.MoveDelta(dir, Time.deltaTime)` 호출(입력 0이면 `StopMoving`). 이동 계산 자체는 캐릭터가 소유.
- **평타**: `Attack.performed` → `PlayerCharacter.Attack()` → `EquipmentComponent.TriggerAttack()`. 실제 공격 판정은 장착 무기(장비 시스템)가 수행.

### PlayerCharacter의 빙의 부수효과 (시스템 연결점)
- `OnPossessed` 시 `possessEffect`(GameplayEffect)를 자기 자신에게 적용(`ApplyGameplayEffectToSelf`)하고 핸들 보관, `OnUnPossessed` 시 제거. → **Possession과 AbilitySystem이 만나는 지점**.
- `MoveDelta`는 ASC의 `Speed` Attribute(`GetAttributeCurrentValue`)를 읽어 속도를 결정. 모델 회전은 `rotationSmoothing`으로 Slerp. → 이동 속도가 Attribute/GameplayEffect의 영향을 받는다.
- `[RequireComponent]`로 `EquipmentComponent`, `AbilitySystemComponent`를 강제.

### MonsterCharacter
- `NavMeshAgent` 기반. `Chase(worldPosition)`로 목적지 설정, `StopMoving`은 `ResetPath`. `MoveDelta`는 현재 비어 있음(NavMesh가 이동 주도).

## 관련 문서
- [AbilitySystem](ability-system/overview.md) — 빙의 이펙트·Speed Attribute의 근거
- [ItemSystem / EquipmentSystem](item-equipment.md) — `Attack()`이 위임하는 장비 동작
