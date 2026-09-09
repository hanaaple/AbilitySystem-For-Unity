# Feature List

> 프로젝트 전체 feature 현황 대시보드.
> 상태 정의 및 전이 규칙은 [feature-list-convention.md](feature-list-convention.md) 참조.
> **이 표와 각 progress 문서의 상태는 항상 동기화되어야 한다.**

최종 갱신: 2026-09-09 (KST)

> **`✅ 임시완료`**: 유저가 임시 완료로 지정한 feature. 수용 기준 최종 확인 전이라 정식 `✅ DONE`과 구분한다. (2026-09-08 유저 지시)

## 현황

> feature는 **그룹(대분류)** 아래 하위 feature로 묶는다. 그룹 폴더에 공유 하네스/설계가 있으면 그룹 헤더에 링크한다. (구조 규약: [HARNESS.md](HARNESS.md)의 '폴더 구조·네이밍')

### 🧩 ability-system — 어빌리티 시스템 그룹 (GAS-like)
아키텍처: [overview](../../project/architecture/ability-system/overview.md)

| ID | Feature | 상태 | 우선순위 | 의존 | Progress |
|---|---|---|---|---|---|
| attribute | 어트리뷰트 (수치·Set·Handle·SO 초기화) | ✅ 임시완료 | P0 | - | [progress](ability-system/attribute/progress.md) |
| gameplay-effect | 게임플레이 이펙트 (GE·Spec·Active·Modifier) | ✅ 임시완료 | P0 | attribute | [progress](ability-system/gameplay-effect/progress.md) |
| gameplay-ability | 게임플레이 어빌리티 (GA·스킬 실행) | 📋 PLANNED | P1 | attribute, gameplay-effect | [progress](ability-system/gameplay-ability/progress.md) |
| aggregator | 어트리뷰트 반응성 / Aggregator (라이브 재평가) | ✅ 임시완료 | P1 | attribute, gameplay-effect | [progress](ability-system/aggregator/progress.md) |

> **비-GAS 인게임 feature는 보관(비활성) 처리** — item-system 그룹은 GAS 데모 집중을 위해 잠시 치워뒀다. → [_parked/agent/feature/feature-list.md](../../_parked/agent/feature/feature-list.md).

## 상태 요약

- 📋 PLANNED: 1
- 🔧 IN-PROGRESS: 0
- ✅ 임시완료: 3
- ⏸️ BLOCKED: 0
- ✅ DONE: 0
- 🗄️ ARCHIVED: 0
