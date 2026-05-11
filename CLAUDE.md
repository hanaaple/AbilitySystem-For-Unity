# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language

Always respond in Korean (한국어).

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
| StatModifier 통합 / 기본 장비 (권총 1개) | MVP | 진행 예정 |
| 전투 / 스킬 시스템 | MVP | 예정 |
| 시야 시스템 / 적 AI / 스폰 | MVP | 예정 |
| 루팅 / 탈출 / 사운드 | MVP | 예정 |
| 장비 등급 / 영구 성장 / 미니맵 | 추가목표 | 예정 |

### 현재 목표

장비·패시브·액티브 스킬·스탯의 확장성을 고려한 구조를 갖추면서, 권총 1개를 동작 가능한 수준으로 구현한다.

## 아키텍처

### 컨트롤러-캐릭터 분리 (Possession 패턴)

```
ControllerBase (abstract)          CharacterBase (abstract)
└── PlayerController               ├── PlayerCharacter
    - _possessedCharacter          └── MonsterCharacter
    - Input 구독/해제
```

- `Controller.cs` (`ControllerBase`) — `Possess(CharacterBase)`, `HasAuthority()` (현재 항상 true, 멀티 대비 TODO)
- `PlayerController` — `OnEnable`/`OnDisable`에서 `PlayerInputActions` 구독·해제, 입력을 `PlayerCharacter` 메서드 호출로 전달
- 캐릭터가 이동 로직 소유 (`PlayerCharacter.MoveTo(Vector3)`)

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

MonoBehaviour 멤버 순서: 필드 → 프로퍼티 → Unity 생명주기 (Awake → OnEnable → Start → Update → …) → public 메서드 → private 메서드 → 이벤트 콜백

주석: public API는 XML 문서화(`/// <summary>`), 인라인 주석은 이유(WHY)만 설명.
