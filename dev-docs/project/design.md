# design.md

Spectral-Raid의 게임 기획 문서. 게임 컨셉·세션 구조·MVP 로드맵·씬 흐름·조작 스펙을 정리한다.
기술 아키텍처·코드 컨벤션·개발 도구 설정은 `CLAUDE.md`를 참고.

## 게임 개요

**Spectral-Raid** — 제한된 시야와 소리 기반 정보로 위험을 판단해 탈출을 결정하는 5~8분 세션 기반 탑다운 전술 액션 게임.

- 장르: Extraction-lite 액션 + Roguelite 성장 (싱글플레이, 멀티플레이 확장 고려 구조)
- 핵심 차별점: 강하게 제한된 시야 + 사운드 중심 정보 인지
- 세션 구조: 탐색 → 전투/레벨업 → 루팅 → 탈출 판단 (세션 타이머 5~8분)
- 세션 간 성장: Soul Shard로 영구 업그레이드, 스킬 해금

## 핵심 기획 (큰 그림)

플레이어의 정체성과 게임 루프를 관통하는 다섯 축. (세부 수치·연출 등 개발 분기가 아닌 것은 여기 적지 않는다.)

1. **조종은 몸이 아니라 빙의(Possess)로 성립한다** — 플레이어는 특정 캐릭터에 종속되지 않는다. 몸(캐릭터)에 Possess하는 순간 즉시 조종 가능. (컨트롤러-캐릭터 분리 아키텍처와 일치)
2. **전투 정체성은 아이템이 정의한다** — 장착한 아이템에 따라 공격과 스킬이 달라진다. 몸 자체가 아니라 장비가 무엇을 할 수 있는지를 결정.
   - **무기는 다양하게, 대등하게.** 초기 "총 위주 + 근접 보조"가 아니라 총·검·특이 무기(변칙 공격 알고리즘)를 대등한 축으로 둔다. → **무기 종류마다 배타적인 주 행동 알고리즘(축 A)이 계속 늘어난다**는 뜻 = ItemSystem 구조는 "무기 추가 시 중앙 시스템을 건드리지 않고 자기 완결적으로 붙는" 확장성이 1순위 요구.
3. **적은 인간형 플레이어 캐릭터와 동일한 구조다** — 적과 플레이어 캐릭터는 같은 틀(몸 + 장비)로 만든다.
4. **성장은 아이템 획득 + 재화로 이뤄진다** — 루팅한 아이템과 재화(Soul Shard)를 통해 강해진다.
5. **캐릭터는 죽으면 끝이다 (타르코프류)** — 몸의 죽음은 되돌릴 수 없다.

### 결정·미결

- **[분기 ①] "죽으면 끝"과 "성장"의 대상** — *미결(보류).* 죽을 때 잃는 것이 이번 판의 몸/장비까지인지, 성장 자체가 리셋되는 진성 permadeath인지 아직 정하지 않는다.
- **[분기 ②] Possess의 위상 — 결정: 게임플레이 메커닉(적/다른 몸에도 빙의)을 지향하되, "무조건 들어가는 기능"으로 확정하지 않는다.** 이는 게임플레이 확정이 아니라 **아키텍처 불변조건**이다: 구조가 크로스-빙의를 막지 않아야 하고, 적 몸으로의 빙의를 나중에 *간단히* 붙일 수 있어야 한다.
  - 따라서 **몸의 능력(이동·전투·ASC)은 컨트롤러에 무관(controller-agnostic)**해야 한다. `PlayerCharacter`/`EnemyCharacter`로 나뉘더라도, 둘의 차이는 "누가 조종하는가"가 아니라 **기본 컨트롤러·초기 로드아웃 등 세팅 차이**여야 한다. 어떤 몸이든 `PlayerController`로 빙의되면 조종 가능해야 함.
  - 인간형 적은 플레이어 캐릭터와 **같은 몸 구조**를 공유한다(몸 + 장비 + ASC + 명령 구동 이동). "AI가 조종함"과 "플레이어가 조종함"의 차이는 Character 클래스가 아니라 Controller에 둔다.

### 시스템 큰 틀 (결정)

- **몸 능력의 소재 = 하이브리드(상속 뼈대 + 컴포넌트 능력).** `CharacterBase` 상속은 **빙의 수명주기·명령 인터페이스(`MoveDelta`/`Attack`)·생사(`IsAlive`/`OnDeath`)**의 뼈대에만 쓴다. 실제 능력인 **이동·장비·ASC는 컴포넌트**로 붙인다(`EquipmentComponent`·`AbilitySystemComponent`는 이미 컴포넌트). → 상속 깊이는 얕게(컨트롤러-캐릭터 seam은 클래스로 고정), 능력 조합은 컴포넌트로 유연하게.
- `PlayerCharacter`/`EnemyCharacter`는 `CharacterBase`를 상속한 **얇은 껍데기** — 컴포넌트 배선 + 기본 컨트롤러/초기 로드아웃 세팅만 다르고, 조종 가능 여부는 동일.
- **불변조건(빙의를 막지 않기):** ① 제어 경로의 명령 인터페이스는 `CharacterBase` 레벨(특정 파생 타입에 캐스팅 금지) → 어떤 몸이든 `PlayerController`로 조종 가능. ② 적 몸도 *명령 구동 이동*의 통로를 가진다(AI일 땐 AI가, 빙의 땐 플레이어가 같은 통로로 명령). — 현재 구조 간극은 [`architecture/controller-character.md`](architecture/controller-character.md) 참고.

## MVP 우선순위

| 시스템 | 구분 | 상태 |
|---|---|---|
| 이동 / Sprint | MVP | 진행 중 |
| AbilitySystem (GAS-like) | MVP | 구현됨 |
| ItemSystem / EquipmentSystem | MVP | ⚠ 구조 설계 미정 (재검토 중) |
| 전투 / 스킬 시스템 | MVP | 예정 |
| 시야 시스템 / 적 AI / 스폰 | MVP | 예정 |
| 루팅 / 탈출 / 사운드 | MVP | 예정 |
| 장비 등급 / 영구 성장 / 미니맵 | 추가목표 | 예정 |

### 현재 목표

권총 1개를 동작 가능한 수준으로 구현한다. AbilitySystem(어트리뷰트·게임플레이 이펙트)은 구현됐다.
**ItemSystem/EquipmentSystem의 구조는 확정된 것이 없다** — 2026-07-18 이전 설계·문서 전부 아카이브하고 **유저 주도로 무기 설계를 다시 시작**하는 중이다. 확정 요구사항·현황은 [`agent/feature/item-system/README.md`](../agent/feature/item-system/README.md). (아카이브는 열지 않는다.)

## 씬 구성

| 씬 | 역할 | 전환 방식 |
|---|---|---|
| MainMenuScene | 타이틀 | Single Load → LobbyScene |
| LobbyScene | 스킬 슬롯 세팅, 업그레이드 진입 | Single Load |
| GameScene | 메인 게임플레이 (세션마다 리로드) | Single Load |
| ResultScene | 세션 결과 | Additive Load → Unload |

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