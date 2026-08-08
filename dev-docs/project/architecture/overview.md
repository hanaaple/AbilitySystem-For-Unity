# 아키텍처 개요

Spectral-Raid의 런타임 구조와 시스템 간 관계를 정리한다.
각 시스템의 상세 설계는 아래 개별 문서를 참고.

## 전체 구조

```
ControllerBase ──possess──> CharacterBase
                              ├── AbilitySystemComponent   (수치 관리)
                              └── EquipmentComponent        (장비 슬롯)
                                    └── ItemInstance        (장착 런타임 — ⚠ 구조 미정)
```

- **컨트롤러-캐릭터 분리 (Possession)** — 입력/제어 주체(Controller)와 피제어 객체(Character)를 분리. 멀티플레이·AI 빙의 확장을 염두에 둔 구조.
- **AbilitySystem (GAS-like)** — 캐릭터 수치(체력·스태미나·데미지 등)를 Attribute/GameplayEffect로 관리.
- **ItemSystem / EquipmentSystem** — ⚠ **설계 미정 (재검토 중).** 아이템을 무엇으로 정의할지(모듈 조합 / 종류별 런타임 / 어빌리티) 확정되지 않았다. 이전의 "모듈 조합" 서술은 폐기 — [item-equipment.md](item-equipment.md) 참조.

핵심 시스템 코드는 `Assets/Scripts/Core/` 아래에 위치.

## 시스템 문서

- [AbilitySystem (GAS-like)](ability-system/overview.md) — [Attribute](ability-system/attribute.md) · [GameplayEffect](ability-system/gameplay-effect.md) · [GameplayAbility](ability-system/gameplay-ability.md)
- [컨트롤러-캐릭터 분리 (Possession)](controller-character.md)
- [ItemSystem / EquipmentSystem](item-equipment.md)
- [카메라](camera.md)
- 에디터 툴: [New Script (서브클래스 생성기)](editor-new-subclass-script.md)

## 관련 문서

- 개발 방향·코딩 규약 — [`CLAUDE.md`](../../../CLAUDE.md)
- 게임 기획 — [`design.md`](../design.md)
- 코드 컨벤션 — [`CODE_CONVENTION.md`](../CODE_CONVENTION.md)
- 개발 도구(Unity MCP) — [`dev-tools.md`](../dev-tools.md)