# 아키텍처 개요 (보관 — 비활성 비-GAS 시스템)

> **비활성/보관.** GAS 데모 집중을 위해 잠시 치워둔 비-GAS 인게임 시스템의 구조. 되돌릴 때 이 내용을 `project/architecture/overview.md`로 통합한다.
> 활성 아키텍처는 [project/architecture/overview.md](../../../project/architecture/overview.md).

## 전체 구조

```
ControllerBase ──possess──> CharacterBase
                              ├── AbilitySystemComponent   (수치 관리 — GAS, 활성)
                              └── EquipmentComponent        (장비 슬롯)
                                    └── ItemInstance        (장착 런타임 — ⚠ 구조 미정)
```

- **컨트롤러-캐릭터 분리 (Possession)** — 입력/제어 주체(Controller)와 피제어 객체(Character)를 분리. 멀티플레이·AI 빙의 확장을 염두에 둔 구조. → [controller-character.md](controller-character.md)
- **ItemSystem / EquipmentSystem** — ⚠ 설계 미정(재검토 중)에서 보관됨. → [item-equipment.md](item-equipment.md)
- **카메라** — → [camera.md](camera.md)

핵심 시스템 코드는 `Assets/Scripts/Core/` 아래에 위치.
