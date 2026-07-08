# 아키텍처 개요

Spectral-Raid의 런타임 구조와 시스템 간 관계를 정리한다.
각 시스템의 상세 설계는 아래 개별 문서를 참고.

## 전체 구조

```
ControllerBase ──possess──> CharacterBase
                              ├── AbilitySystemComponent   (수치 관리)
                              └── EquipmentComponent        (장비 슬롯)
                                    └── ItemInstance        (장착 런타임, 모듈 동작)
```

- **컨트롤러-캐릭터 분리 (Possession)** — 입력/제어 주체(Controller)와 피제어 객체(Character)를 분리. 멀티플레이·AI 빙의 확장을 염두에 둔 구조.
- **AbilitySystem (GAS-like)** — 캐릭터 수치(체력·스태미나·데미지 등)를 Attribute/GameplayEffect로 관리.
- **ItemSystem / EquipmentSystem** — 아이템을 모듈 조합으로 정의하고, 슬롯 규칙에 따라 장착. 장착 시 런타임 인스턴스가 모듈을 읽어 동작.

핵심 시스템 코드는 `Assets/Scripts/Core/` 아래에 위치.

## 시스템 문서

- [AbilitySystem (GAS-like)](ability-system/overview.md) — [Attribute](ability-system/attribute.md) · [GameplayEffect](ability-system/gameplay-effect.md) · [GameplayAbility](ability-system/gameplay-ability.md)
- [컨트롤러-캐릭터 분리 (Possession)](controller-character.md)
- [ItemSystem / EquipmentSystem](item-equipment.md)
- [카메라](camera.md)

## 관련 문서

- 개발 방향·코딩 규약 — [`CLAUDE.md`](../../../CLAUDE.md)
- 게임 기획 — [`design.md`](../design.md)
- 코드 컨벤션 — [`CODE_CONVENTION.md`](../CODE_CONVENTION.md)
- 개발 도구(Unity MCP) — [`dev-tools.md`](../dev-tools.md)