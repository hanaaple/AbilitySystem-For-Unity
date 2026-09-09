# 아키텍처 개요

Spectral-Raid의 런타임 구조와 시스템 간 관계를 정리한다. 상세 설계는 아래 개별 문서를 참고.

> **현재 활성 = AbilitySystem(GAS) 중심.** 비-GAS 인게임 시스템(컨트롤러-캐릭터/Possession·ItemSystem·EquipmentSystem·카메라)은 GAS 데모 집중을 위해 **보관(비활성)** 처리됐다 → [_parked/project/architecture/overview.md](../../_parked/project/architecture/overview.md).

## 시스템 문서

- [AbilitySystem (GAS-like)](ability-system/overview.md) — [Attribute](ability-system/attribute.md) · [GameplayEffect](ability-system/gameplay-effect.md) · [Aggregator](ability-system/aggregator.md) · [GameplayAbility](ability-system/gameplay-ability.md)
- 에디터 툴: [New Script (서브클래스 생성기)](editor-new-subclass-script.md)

## 관련 문서

- 이 폴더 문서 작성 규약 — [`HARNESS.md`](HARNESS.md)
- 개발 방향·코딩 규약 — [`CLAUDE.md`](../../../CLAUDE.md)
- 게임 기획 — [`design.md`](../design.md)
- 코드 컨벤션 — [`CODE_CONVENTION.md`](../CODE_CONVENTION.md)
- 개발 도구(Unity MCP) — [`dev-tools.md`](../dev-tools.md)
