# ItemSystem / EquipmentSystem

`Assets/Scripts/Core/ItemSystem/` · `Assets/Scripts/Item/`

> **설계 권위:** 이 시스템의 설계 방향은 [`feature/item-system/HARNESS.md`](../../agent/feature/item-system/HARNESS.md)(feature 전용 하네스)가 **단일 진실**이다. 이 문서는 그 설계를 아키텍처 altitude로 요약할 뿐이며, 불변 조건(INV)·검증·로드맵의 상세는 harness를 본다. 문서·코드가 harness와 어긋나면 harness가 우선.

아이템을 **모듈 조합**으로 정의하고, 장착 시 런타임 인스턴스가 모듈을 읽어 동작한다. 아이템별 상속 없이 — 특수화는 Module·인터페이스 조합으로만 — 확장한다.

## 계층 (단방향 흐름)

```
ItemData (ScriptableObject)    무상태 정의 — 모듈 목록·기본값. 런타임 값 쓰기 금지
   │  (모듈 목록 참조)
ItemModule (abstract)          재사용 가능한 동작 단위. 무상태·씬/UI 미접근 — "결정"만 한다
   │
ItemInstance (POCO)            장착 시 생성. per-item 상태 소유(IModuleState[] 병렬 배열).
   │                           아이템별 상속 없음 — 상태 캐스트는 제네릭 베이스 한 곳에만
   ▼
소유자 (CombatActor)           공유 상태(콤보·스태미나·타깃) 소유
   ▼
ItemBehaviour (MonoBehaviour)  표현·이벤트 중계만. 동작 로직 없음 — UI는 이벤트 구독으로 갱신
```

흐름은 **Module → Instance → 소유자 → Behaviour/UI 단방향**이며 역방향 참조를 두지 않는다.
호출 단위로만 필요한 값은 상태로 보관하지 않고 `ModuleContext` 파라미터로 전달한다.

## 핵심 규칙 (harness INV 요약 — 상세·전체는 HARNESS §2)

- **상속 금지:** `ItemData`/`ItemInstance`에 아이템별 상속을 두지 않는다. 특수화는 Module·인터페이스 조합.
- **무상태 경계:** `ItemData`(SO)와 `ItemModule`은 무상태. per-item 상태는 `ItemInstance`, 공유 상태는 `CombatActor`가 소유.
- **표현 분리:** Module·Instance는 씬/UI/GameObject를 모른다. 표현은 `ItemBehaviour`·시스템으로 넘긴다.
- **능력은 인터페이스로:** 시스템의 필수 요구는 구체 Module이 아니라 인터페이스 능력(`I~Provider`/`I~Contributor`)으로 선언·검증한다.
- **재사용 판별식:** 여러 아이템에 붙으면 Module, 한 아이템 전용이면 통짜 Module 또는 전용 Runtime.

## 코드 위치

- **코어:** `Assets/Scripts/Core/ItemSystem/` — `ItemData` · `ItemInstance` · `ItemBehaviour` · `Module/ItemModule` · `Module/ModuleContext`
- **모듈·아이템 정의:** `Assets/Scripts/Item/` — `Module/`(StatModifier·Input 등), 아이템별 데이터
- **장비 슬롯:** `EquipmentComponent` — 장착 슬롯·규칙 관리. *아이템 모듈 조합*과 *슬롯 규칙*은 분리된 축이다. (세부는 코드 참고)

> **이관 중:** 현재 코드에는 상속 기반 잔재(아이템별 `~ItemInstance` 클래스 등)가 남아 있을 수 있으며, 이는 INV-2 위반으로 **제거·이관 대상**이다. 목표는 상속이 아닌 모듈 조합 + POCO `ItemInstance`. 진행 상황은 [`feature/item-system/progress.md`](../../agent/feature/item-system/progress.md) 참조.
