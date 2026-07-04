# design.md

Spectral-Raid의 게임 기획 문서. 게임 컨셉·세션 구조·MVP 로드맵·씬 흐름·조작 스펙을 정리한다.
기술 아키텍처·코드 컨벤션·개발 도구 설정은 `CLAUDE.md`를 참고.

## 게임 개요

**Spectral-Raid** — 제한된 시야와 소리 기반 정보로 위험을 판단해 탈출을 결정하는 5~8분 세션 기반 탑다운 전술 액션 게임.

- 장르: Extraction-lite 액션 + Roguelite 성장 (싱글플레이, 멀티플레이 확장 고려 구조)
- 핵심 차별점: 강하게 제한된 시야 + 사운드 중심 정보 인지
- 세션 구조: 탐색 → 전투/레벨업 → 루팅 → 탈출 판단 (세션 타이머 5~8분)
- 세션 간 성장: Soul Shard로 영구 업그레이드, 스킬 해금

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