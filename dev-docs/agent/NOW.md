# NOW — 지금 이어서 할 일 (단일 재개 지점)

> **"하던거 하자" = 이 파일 하나만 읽고 곧장 `## ▶ 지금 할 일`부터 실행한다.**
> feature-list·progress·worklog·코드를 **미리 훑지 않는다** — 필요할 때만 링크로 연다.
> 세션 종료 시 이 파일을 현재 상태로 갱신한다(§3.3). 그래야 다음 세션이 이것만 읽고 재개한다.
>
> 최종 갱신: 2026-07-08 (KST)

---

## ▶ 지금 할 일

**⚠ 먼저: 이번 세션 리팩터 컴파일 확인(2026-07-08).** ① 에디터 스크립트를 상단 `Editor/` 트리 → **feature-local `Editor/`**(각 feature 옆, 서브시스템 레벨)로 통일. ② 시스템 컴포넌트 `EquipmentComponent`를 루트 → **`Core.ItemSystem.Equipment` 서브시스템**으로 승격(`Core/ItemSystem/Equipment/`). 능력 인터페이스 `IEquippable`·`SlotType`은 `ConsumeItem.cs`의 `IStackable`과 대칭 유지 위해 `Core.ItemSystem`(EquipItem.cs)에 그대로 둠. namespace 다수·using 변경, `.meta`/GUID 보존(프리팹 참조 유지). ③ **`SubclassSelector` 드로어 개선** — 선택 타입의 스크립트를 오브젝트 필드처럼(단일클릭 ping/더블클릭 open) + 검색 팝업(`SubclassAdvancedDropdown`), 리스트 내 중복 제외를 **범용 드로어에서 빼** 전용 드로어가 `DrawSelector(excluded)`로 조립(수집 코어 `CollectArrayValues` 공유). `AttributeSetInitData`는 `[SubclassSelector]` 떼고 `AttributeSetInitDataDrawer`가 직접 그린다. → **Unity에서 컴파일·인스펙터 동작 확인.** 규칙: `dev-docs/project/CODE_CONVENTION.md`(설계 원칙·에디터 확장) · 상세 [`editor-drawer-guide.md`](../project/editor-drawer-guide.md).

**장착 표현 슬라이스 — 코드 완료, 유저 Unity 배선·play 검증 대기(2026-07-07, D10).**
`ItemBehaviour`(빈 스텁→구체 표현 앵커, Instance+Bind). 소켓 소환은 **`EquipmentComponent`가 겸함**(소켓 맵 필드 + Equip/Unequip에서 프리팹 소환/파괴 + `ItemBehaviour.Bind`). `ItemData.CreateBehaviourRuntime`(실동작 불가 팩토리) 제거. (별도 `EquipmentVisual`을 먼저 만들었다 발견성 이유로 병합·삭제 — D10.)

→ **다음 액션 = 유저 Unity 배선 후 눈으로 검증.** (a) 무기 표현 프리팹 생성(메시+`ItemBehaviour` 컴포넌트) 후 `Weapon_TestSword.prefab` 필드에 할당, (b) `Player.prefab`의 기존 `EquipmentComponent` **sockets 필드**에 Weapon→소켓 Transform(예: `PlayerCharacter.model` 하위 `WeaponSocket`) 매핑. 검증: 픽업 장착 시 손에 프리팹 뜸 / 해제 시 사라짐. 배선 후 [test-harness-state.md](feature/item-system/test-harness-state.md)에 프리팹·소켓·GUID 반영.
→ 그다음 item 코어 후보: **S2**(WeaponData + 근접 Strategy) 또는 S3/S4 예제 완결. item [progress](feature/item-system/item/progress.md) `## 다음 작업` 참조.
→ **inventory · equipment = 개발 일시 중단**(유저 결정) — 지금 손대지 않는다.
→ 현재 씬/에셋 상태·GUID·재현은 [test-harness-state.md](feature/item-system/test-harness-state.md).
