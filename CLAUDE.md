# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language

Always respond in Korean (한국어).

## 행동 원칙

모르거나 애매한 것은 추정하지 않는다. 막연한 추측으로 스스로 판단하고 진행하는 대신, 추가 정보를 요청하거나 질문한다.

- 외부 API·엔진 동작·설계 의도가 불확실하면 → 직접 조사(검색, 문서 확인)하거나 질문한다
- 요구사항이 여러 해석으로 읽히면 → 구현 전에 먼저 확인한다
- 확인 없이 진행했다가 틀리는 것보다 질문 한 번이 낫다

구현 완료 후 `read_console`로 컴파일 에러를 확인하지 않는다. 에디터 확인은 유저가 직접 한다.

## 개발 방향

포트폴리오를 목적으로 하므로, 설계 결정에는 **기술적 근거와 선택 이유**가 명확히 드러나야 한다.  
단, 기술적 완성도에 대한 집착으로 실제 게임 완성을 저해하는 과설계(over-engineering)는 지양한다.

- 좋음: "이 패턴을 쓴 이유를 한 문장으로 설명할 수 있다"
- 나쁨: "실제로 필요하지 않지만 기술적으로 인상적으로 보이기 위해 추가한다"

## 게임 개요

**Spectral-Raid** — 제한된 시야와 소리 기반 정보로 위험을 판단해 탈출을 결정하는 5~8분 세션 기반 탑다운 전술 액션 게임.

- 장르: Extraction-lite 액션 + Roguelite 성장 (싱글플레이, 멀티플레이 확장 고려 구조)
- 핵심 차별점: 강하게 제한된 시야 + 사운드 중심 정보 인지
- 세션 구조: 탐색 → 전투/레벨업 → 루팅 → 탈출 판단 (세션 타이머 5~8분)
- 세션 간 성장: Soul Shard로 영구 업그레이드, 스킬 해금

패키지:
- **Input System** v1.19.0 — 신 Input System (레거시 미사용)
- **Cinemachine** v2.10.7
- **URP** v17.3.0
- **UniTask** (Cysharp) — async/await
- **R3** (Cysharp) — 반응형 확장
- **AI Navigation** v2.0.11

## MVP 우선순위

| 시스템 | 구분 | 상태 |
|---|---|---|
| 이동 / Sprint | MVP | 진행 중 |
| AbilitySystem (GAS-like) | MVP | 구현됨 |
| ItemModule 구조 / 규칙 기반 EquipmentSystem | MVP | 구조 구현, 연동 진행 중 |
| 전투 / 스킬 시스템 | MVP | 예정 |
| 시야 시스템 / 적 AI / 스폰 | MVP | 예정 |
| 루팅 / 탈출 / 사운드 | MVP | 예정 |
| 장비 등급 / 영구 성장 / 미니맵 | 추가목표 | 예정 |

### 현재 목표

권총 1개를 동작 가능한 수준으로 구현한다. AbilitySystem·ItemSystem·EquipmentSystem 구조는 완성됐으며, 실제 게임 루프(전투·장비 장착 연동)를 붙이는 단계.

## 아키텍처

### AbilitySystem (GAS-like)

`Assets/Scripts/Core/AbilitySystem/`

캐릭터 수치 관리를 위한 경량 Ability System. UE5 GAS 개념을 Unity에 맞게 축소 적용.

#### 핵심 구조

```
AbilitySystemComponent (MonoBehaviour)
├── Dictionary<Type, AttributeSet>       ← Type당 하나만 등록 가능
└── Dictionary<AGEHandle, ActiveGE>      ← 활성 이펙트 목록

AttributeSet (abstract)                  ← 수치 컨테이너
├── CharacterAttributeSet               — health, maxHealth, stamina, maxStamina, speed
└── CombatAttributeSet                  — Damage

AttributeHandle (struct)                 ← 경량 식별자 (SetType + fieldName + FieldInfo 캐싱)
AttributeData (struct)                   ← BaseValue / CurrentValue 쌍

GameplayEffect (ScriptableObject)        ← 불변 정의
GameplayEffectSpec                       ← 런타임 인스턴스 (Level·Context 스냅샷)
ActiveGameplayEffect                     ← 활성 상태 (Handle, RemainingDuration, PeriodTimer)
```

#### AttributeHandle 규칙

- `CharacterAttributeSet`에 `static readonly AttributeHandle Health = new(typeof(...), nameof(...))` 형태로 선언
- Reflection 비용은 생성 시점 1회뿐 — 런타임 읽기/쓰기는 캐싱된 `FieldInfo` 사용
- 필드명은 camelCase(`health`), 핸들명은 PascalCase(`Health`)

#### AbilitySystemComponent 주요 API

| 메서드 | 용도 |
|---|---|
| `AddAttributeSet(set)` | AttributeSet 등록 (같은 타입 중복 불가) |
| `GetAttributeBaseValue(handle)` | BaseValue 읽기 |
| `GetAttributeCurrentValue(handle)` | CurrentValue 읽기 (모디파이어 누산 후, 캐싱값) |
| `SetBaseAttributeValue(handle, float)` | BaseValue 직접 설정 → CurrentValue 재계산 |
| `ApplyGameplayEffectToSelf(GE, ctx, level)` | GE 적용. Instant는 즉시 실행 후 Invalid Handle 반환 |
| `RemoveActiveGameplayEffect(handle)` | 핸들로 Duration/Infinite GE 해제 |

#### GameplayEffect 타입

- `Instant` — BaseValue 즉시 변경 후 종료. Active Effect 등록 없음.
- `Duration` — 지정 시간 후 자동 해제. Period 설정 시 주기마다 BaseValue 직접 변경.
- `Infinite` — 명시적 Remove 전까지 CurrentValue에 지속 반영.

#### GameplayEffectExecution (TODO: concrete 서브클래스)

추상 클래스·ASC 호출 프레임워크는 구현됨. 단순 Modifier로 표현 불가한 복합 계산(방어력 관통, 크리티컬 배율 등)을 서브클래스 `Execute()` 오버라이드로 처리. 아직 실제 서브클래스 없음.

#### ScriptableObject 초기화

`AttributeInitData` (SO) → `AttributeSetInitData[]` → `AttributeFieldInitData` (fieldName + BaseValue)  
`AbilitySystemComponent.Awake()`에서 Reflection으로 AttributeSet 인스턴스 생성 및 BaseValue 세팅.

---

### 컨트롤러-캐릭터 분리 (Possession 패턴)

```
ControllerBase (abstract)              CharacterBase (abstract)
├── _possessedCharacter (protected)    ├── PlayerCharacter
├── Possess(CharacterBase)             └── MonsterCharacter
├── HasAuthority() — 항상 true (멀티 TODO)
└── PlayerController
    └── OnEnable/OnDisable 입력 구독·해제
```

- `ControllerBase` — `_possessedCharacter`, `Possess()`, `UnPossess()`, `HasAuthority()`
- `PlayerController` — `OnEnable`/`OnDisable`에서 `PlayerInputActions` 구독·해제, 입력을 `PlayerCharacter` 메서드 호출로 전달
- 캐릭터가 이동 로직 소유 (`PlayerCharacter.MoveDelta(Vector3 normalizedDirection, float delta)`)

---

### ItemSystem / EquipmentSystem

`Assets/Scripts/Core/ItemSystem/`  
`Assets/Scripts/Core/EquipmentSystem/`

아이템 모듈 조합과 장비 슬롯 규칙을 분리한 구조. 세부 설계는 코드 직접 참고.

```
ItemArchetype (SO)          ← 아이템 정의 (모듈 목록 포함)
ItemModule (abstract SO)    ← 모듈 단위 (StatModifierModule, SlotBlockModule, InputModule 등)
ItemData                    ← 런타임 아이템 인스턴스

EquipmentComponent          ← MonoBehaviour, 장비 슬롯 관리
EquipmentRuleSet (SO)       ← 슬롯 제약 규칙 정의
EquipmentConstraintChecker  ← 장착 가능 여부 검사
EquipmentInstance           ← 장착 상태 런타임 표현
```

## 입력 액션 맵

`PlayerInputActions.inputactions`가 소스. 자동 생성된 `PlayerInputActions.cs`는 직접 수정 금지.  
`InputSystem_Actions.inputactions`는 중복 파일 — 추후 제거 예정.

| Action        | Map | 바인딩               | 타입 |
|---------------|---|-------------------|---|
| Move          | Gameplay | WASD              | Vector2 |
| Look          | Gameplay | Mouse Position    | Vector2 |
| Fire          | Gameplay | Mouse Left        | Button |
| Attack Melee  | Gameplay | F Key             | Button |
| Sprint        | Gameplay | Left Shift (Hold) | Button |
| Interact      | Gameplay | E Key (Tap)       | Button |
| Interact Hold | Gameplay | E Key (Hold 1s)   | Button |
| Inventory     | Gameplay + UI | Tab               | Button |
| Skill_Q       | Gameplay | Q                 | Button |
| Skill_R       | Gameplay | R                 | Button |
| UseItem_1~6   | Gameplay | 숫자키 1~6           | Button |

E키: `Interact(Tap)` = 아이템 획득 / `InteractHold(Hold 1s)` = 보물상자 채널링

## 씬 구성

| 씬 | 역할 | 전환 방식 |
|---|---|---|
| MainMenuScene | 타이틀 | Single Load → LobbyScene |
| LobbyScene | 스킬 슬롯 세팅, 업그레이드 진입 | Single Load |
    | GameScene | 메인 게임플레이 (세션마다 리로드) | Single Load |
    | ResultScene | 세션 결과 | Additive Load →  Unload |


## 개발 도구 설정

### Unity MCP (Claude Code 연동)

`mcp-for-unity` 패키지로 Claude Code에서 Unity 에디터를 직접 조작할 수 있음.

- 설정 파일: `.claude/settings.local.json` (git 제외, 개인 로컬 전용)
- Unity 에디터가 열려 있어야 MCP 서버가 연결됨
- Claude Code 세션 재시작 후 활성화됨
- 연결 확인: `manage_editor` → `telemetry_ping` 응답이 `success: true`이면 정상

**현재 로컬 설정:** `C:\Users\<username>\.local\bin\uvx.exe`

팀원이 사용하려면 `.claude/settings.local.json`을 아래 형식으로 직접 생성 (`<uvx 경로>`는 본인 환경에 맞게 수정):
```json
{
  "mcpServers": {
    "unityMCP": {
      "command": "C:\\Users\\<username>\\.local\\bin\\uvx.exe",
      "args": [
        "--prerelease",
        "explicit",
        "--from",
        "mcpforunityserver>=0.0.0a0",
        "mcp-for-unity",
        "--transport",
        "stdio"
      ]
    }
  }
}
```

**주요 MCP 도구:**

| 도구 | 용도 |
|---|---|
| `manage_editor` | 에디터 상태 조회, Play/Pause/Stop, 태그·레이어 추가 |
| `manage_gameobject` | GameObject CRUD (생성·수정·삭제·복제) |
| `manage_scene` | 씬 로드·저장·쿼리 |
| `manage_components` | 컴포넌트 추가·제거·프로퍼티 수정 |
| `manage_script` | 스크립트 생성·수정·삭제 |
| `read_console` | Unity 콘솔 로그 읽기 (컴파일 에러 확인) |
| `find_gameobjects` | 이름·태그·레이어·컴포넌트로 오브젝트 검색 |

## 카메라

### 씬 설정

- Perspective, FOV 40
- Follow = PlayerCharacter 자식 `CameraTarget` Transform, LookAt = null
- Body: Transposer, Offset (0, 10, -10), BindingMode = WorldSpace → 캐릭터 회전과 무관하게 카메라 방향 고정
- Aim: Do Nothing → 카메라 자체 회전 잠금


## 코드 컨벤션

`.editorconfig` 자동 적용. 상세 규칙은 `Readme.md` 참고.

| 대상 | 규칙 | 예시 |
|---|---|---|
| 클래스 / 구조체 | PascalCase | `PlayerController` |
| 인터페이스 | `I` + PascalCase | `IDamageable` |
| 메서드 / 프로퍼티 | PascalCase | `TakeDamage()` |
| public 필드 | PascalCase | `MoveSpeed` |
| private 필드 | `_` + camelCase | `_health` |
| 지역 변수 / 매개변수 | camelCase | `deltaTime` |
| 상수 | PascalCase | `MaxHealth` |
| 이벤트 | `On` + PascalCase | `OnDeath` |

- Allman 스타일 중괄호, 생략 금지
- 4스페이스 들여쓰기 (탭 금지)
- 접근 한정자 항상 명시
- `this.` 생략
- `var`는 우변에서 타입이 명확할 때만
- 메서드 시그니처는 한 줄로 작성 (매개변수가 많아도 줄넘김 금지)
- `switch` case는 항상 `{}` 블록 사용 (fall-through case도 동일)

MonoBehaviour 멤버 순서: 필드 → 프로퍼티 → Unity 생명주기 (Awake → OnEnable → Start → Update → …) → public 메서드 → private 메서드 → 이벤트 콜백

주석: public API는 XML 문서화(`/// <summary>`), 인라인 주석은 이유(WHY)만 설명.
