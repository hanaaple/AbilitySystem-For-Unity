# ItemSystem / EquipmentSystem

`Assets/Scripts/Core/ItemSystem/`  
`Assets/Scripts/Core/EquipmentSystem/`

아이템 모듈 조합과 장비 슬롯 규칙을 분리한 구조. 세부 설계는 코드 직접 참고.

```
ItemData (SO)               ← 아이템 정의 (모듈 목록 포함)
ItemModule (abstract)       ← 모듈 단위 데이터 (StatModifierModule, SlotBlockModule, InputModule 등)
ItemInstance (MonoBehaviour) ← 장착 시 생성되는 런타임 인스턴스
└── WeaponInstance          ← 입력 처리·발사 로직 등 무기 고유 동작

EquipmentComponent          ← MonoBehaviour, 장비 슬롯 관리
EquipmentRuleSet (SO)       ← 슬롯 제약 규칙 정의
EquipmentConstraintChecker  ← 장착 가능 여부 검사
EquipmentInstance           ← 장착 상태 런타임 표현
```

## 설계 원칙

- `ItemModule`은 **데이터 정의**만 담당 — 어떤 입력을 받는지, 어떤 스탯을 수정하는지 선언
- 런타임 동작은 `ItemInstance` 및 서브클래스가 장착 시 모듈을 읽어 처리
- 기획이 확정되지 않은 시점에서 과도한 추상화 없이 상속으로 시작; 아이템 종류가 늘어나면 공통 로직을 모듈 동작으로 옮기는 방향으로 확장