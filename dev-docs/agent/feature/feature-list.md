# Feature List

> 프로젝트 전체 feature 현황 대시보드.
> 상태 정의 및 전이 규칙은 [feature/HARNESS.md](HARNESS.md)의 'feature-list.md 규약' 참조.
> **이 표와 각 progress 문서의 상태는 항상 동기화되어야 한다.**

최종 갱신: 2026-08-07 (KST)

## 현황

> feature는 **그룹(대분류)** 아래 하위 feature로 묶는다. 그룹 폴더에 공유 하네스/설계가 있으면 그룹 헤더에 링크한다. (구조 규약: [HARNESS.md](../HARNESS.md)의 '디렉토리 구조')

### 🧩 ability-system — 어빌리티 시스템 그룹 (GAS-like)
아키텍처: [overview](../../project/architecture/ability-system/overview.md)

| ID | Feature | 상태 | 우선순위 | 의존 | Progress |
|---|---|---|---|---|---|
| attribute | 어트리뷰트 (수치·Set·Handle·SO 초기화) | 🔧 IN-PROGRESS | P0 | - | [progress](ability-system/attribute/progress.md) |
| gameplay-effect | 게임플레이 이펙트 (GE·Spec·Active·Modifier) | 🔧 IN-PROGRESS | P0 | attribute | [progress](ability-system/gameplay-effect/progress.md) |
| gameplay-ability | 게임플레이 어빌리티 (GA·스킬 실행) | 📋 PLANNED | P1 | attribute, gameplay-effect | [progress](ability-system/gameplay-ability/progress.md) |
| aggregator | 어트리뷰트 반응성 / Aggregator (라이브 재평가) | 🔧 IN-PROGRESS | P1 | attribute, gameplay-effect | [progress](ability-system/aggregator/progress.md) |

### 🧩 item-system — 아이템 시스템 그룹
공유 하네스: [HARNESS](item-system/HARNESS.md)

| ID | Feature | 상태 | 우선순위 | 의존 | Progress |
|---|---|---|---|---|---|
| item | 아이템 코어 (데이터·인스턴스·모듈·런타임) | 🔧 IN-PROGRESS | P0 | - | [progress](item-system/item/progress.md) |
| inventory | 인벤토리 (개수·스택) | 🔧 IN-PROGRESS | P1 | item | [progress](item-system/inventory/progress.md) |
| equipment | 장비 장착 | 🔧 IN-PROGRESS | P1 | item, inventory | [progress](item-system/equipment/progress.md) |

## 상태 요약

- 📋 PLANNED: 1
- 🔧 IN-PROGRESS: 6
- ⏸️ BLOCKED: 0
- ✅ DONE: 0
- 🗄️ ARCHIVED: 0
