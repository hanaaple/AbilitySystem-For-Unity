# item — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로. 과거 기록은 삭제·수정하지 않는다(오기 정정은 취소선).
> 결정을 가리킬 땐 `(→D#)`(decisions.md), 로드맵 작업은 `(S#)` 코드로 링크한다.

### 2026-07-08 — 에디터 저작 툴링: `ItemData` Module Add 팝업 버그픽스 + 다형 리스트 빌더 추출

**배경/의도.** `ItemData`(EquipItem/ConsumeItem) 인스펙터에서 Modules 리스트 `+`를 눌러도 타입 선택 팝업이 안 떴음. 겸사겸사 `AttributeInitDataDrawer`의 동일한 "타입 팝업 달린 add 리스트" 패턴과 공통분모를 정리(유저 요청).

**버그 원인·수정.** `[CustomEditor(typeof(ItemData))]`에 `editorForChildClasses`가 없어, abstract `ItemData`의 서브클래스 에셋(`EquipItem`/`ConsumeItem`) 선택 시 커스텀 에디터가 적용되지 않고 기본 인스펙터로 떨어짐 → `[SerializeReference]` 리스트 `+`가 팝업 없이 null 요소만 추가. → `editorForChildClasses: true` 추가로 해결. (비교: `AttributeInitData`는 sealed라 문제없었음.)

**추상화 판단(왜 이렇게).** 두 드로어(Item modules · AttributeInitData sets)가 이미 "ReorderableList + add-dropdown 타입 팝업" 구조를 중복 보유 → **두 번째 사용처 존재이므로** 공통 빌더 채택 정당(북극성 위반 아님). CREATE `Editor/Utility/TypeChoiceList.cs` — `Create(so, elements, header, typeProvider, onAdd, emptyMessage, drawElement?, elementHeight?)`가 리스트 배선·삽입·Update/Apply·빈 상태 안내 팝업까지 처리. 호출부는 (1)후보 타입 (2)`onAdd(element,type)`만 제공. 저장 방식 차이(모듈=`managedReferenceValue` SerializeReference / AttributeSet=`typeName` 문자열+ClearArray)는 콜백에 격리 — 이 차이는 데이터 모델상 정당. dedup(AttributeSet 타입당 1개)도 호출부 typeProvider에 잔류.
- 중간 산물 `TypeAddMenu`(팝업만)는 빌더로 흡수·삭제(작은 util보다 "리스트 통째" 편의가 유저 선호 — memory feedback).

**변경 파일.** CREATE `Editor/Utility/TypeChoiceList.cs` / MODIFY `Editor/ItemSystem/ItemDataDrawer.cs`(editorForChildClasses + 빌더 사용, BuildReorderableList·DrawHeader·OnAddDropdown 제거) / MODIFY `Editor/AbilitySystem/AttributeInitDataDrawer.cs`(빌더 사용, 동일 메서드 제거). AttributeSet add 메뉴 라벨이 nicify로 통일됨(의도).

**검증 상태.** V1 정합성 OK(참조·시그니처, 잔여 참조 0). V2 = 유저 Unity: EquipItem/ConsumeItem 에셋 → Modules `+` → 팝업(StatModifier/MeleeAttack/InputModule) → 추가·펼침. `[제안]` `GameplayEffectDrawer`는 인라인 Popup 방식이라 이 빌더 대상 아님(미변경).

### 2026-07-07 (2) — 표현 배치 재결정: `EquipmentVisual` 분리 → `EquipmentComponent` 병합 (→D10)

- **유저 재검토(2차 질문)로 병합 채택.** 아래 (1)에서 만든 별도 `EquipmentVisual`은 "장비=한 개념인데 컴포넌트 2개라 헷갈린다"는 이유로 기각 → **`EquipmentVisual.cs` 삭제**, 소켓 맵(`SocketBinding[]`)·소환/파괴(`SpawnVisual`/`DespawnVisual`)를 `EquipmentComponent`로 흡수(Equip/Unequip 내부 호출). `ItemBehaviour`·`ItemData.CreateBehaviourRuntime` 제거는 (1)과 동일 유지. `OnEquipped/OnUnequipped` 이벤트는 유일 구독자(EquipmentVisual) 사라져 현재 무구독 — UI 씸으로 존치(제거는 별도 판단).
- **D2 취지 정정(유저).** 내가 D10 근거로 쓴 "ASC 의존 → 적 재사용"은 각색이었음. equipment D2 "PlayerCharacter 미의존"의 실제 취지 = **장비 상태를 캐릭터 사망·씬 전환에도 유지**. 코드 주석·D10에서 ASC/적 재사용 프레이밍 제거.
- **검증 상태.** V1 정합성 OK(참조·시그니처, 이벤트 무구독은 컴파일 무관). Unity 배선은 (1)과 동일하되 **`EquipmentVisual` 추가 대신 `Player.prefab`의 기존 `EquipmentComponent` sockets 필드에 Weapon→소켓 매핑**. play는 유저.

### 2026-07-07 — 장착 표현 슬라이스: ItemBehaviour ↔ Instance ↔ 소켓 프리팹 (코드 완료, play 검증 대기)
> ※ 위 (2)로 대체됨 — 여기서 만든 `EquipmentVisual`은 삭제, 로직은 `EquipmentComponent`로 병합.

**배경/의도.** 유저 당장 목표 "Item Instance – Item Behaviour, Socket에 Item prefab 장착까지". 지금까지 표현 계층이 비어 있었음(`ItemBehaviour` 빈 abstract 스텁, `OnEquipped`만 발행하고 소환 주체 없음, 소켓 개념 부재). 장착 시 손 등 소켓에 무기 프리팹이 뜨고 해제 시 사라지는 **표현 수직 슬라이스**. (로드맵 S1~S10엔 없는 유저 주입 미니 슬라이스 — 범위 내 표현 작업.)

**설계 판단(왜 이렇게) (→D10).** 유저 Q1/Q2 확인 후:
- **소환 주체 = 전투와 분리된 `EquipmentVisual` 컴포넌트.** `EquipmentComponent`(ASC 전투, D2)에 씬/프리팹 관심사(INV-3)를 섞지 않고, `OnEquipped`/`OnUnequipped` **구독**(INV-8)으로 소환/파괴. 소켓 맵(`SlotType→Transform`)도 여기 보유. (대안: EquipmentComponent 직접 소환 — 관심사 혼입으로 기각.)
- **`ItemBehaviour` = 표현 앵커(구체, 로직 0 INV-4).** `ItemInstance Item`+`Bind`만. abstract 유지는 파생 0에서의 선추상화라 북극성 위반 → 구체 1개.
- **생성 = 프리팹 보유 + Bind(Q2).** 프리팹이 `ItemBehaviour`를 달고, 소환 후 `GetComponent().Bind(instance)`. 실동작 불가(MonoBehaviour `new` 못 함)한 `ItemData.CreateBehaviourRuntime` 팩토리 제거.

**변경 파일.**
- MODIFY `Core/ItemSystem/ItemBehaviour.cs` — abstract 빈 스텁 → 구체 `ItemBehaviour`(`ItemInstance Item{get;}` + `Bind`).
- MODIFY `Core/ItemSystem/ItemData.cs` — `CreateBehaviourRuntime` 제거(호출부 0 확인). `prefab` 필드 유지.
- CREATE `EquipmentVisual.cs`(`Assets/Scripts/`, EquipmentComponent 옆·동일 전역 ns) — `[RequireComponent(EquipmentComponent)]`, 소켓 맵(SocketBinding[] 직렬화→Dict), OnEnable/OnDisable 구독, `HandleEquipped`(prefab null·소켓 없음 가드→`Instantiate(prefab, socket, false)`→로컬 0→`TryGetComponent<ItemBehaviour>().Bind`→슬롯별 추적), `HandleUnequipped`(추적 GO Destroy). 새 폴더 신설 없음(§3-2).

**검증 상태.** V1 코드 정합성 OK(참조·시그니처·ns, `CreateBehaviourRuntime` 호출부 0). V2 play는 유저 몫 — 남은 Unity 배선: (a) 무기 표현 프리팹 생성(메시+`ItemBehaviour`) 후 `Weapon_TestSword.prefab`에 할당, (b) `Player.prefab`에 `EquipmentVisual` 추가 + 소켓 Transform(예: model 하위 `WeaponSocket`) 만들어 Weapon 슬롯에 매핑. V3 회귀: EquipmentComponent 전투 경로 미변경(이벤트만 구독 추가) → S1 재현 영향 없음 예상.

**§5 보고.** `[추상화 제안] EquipmentVisual`(표현을 전투와 분리 — 대안: EquipmentComponent 직접 소환). 사용처 1이지만 관심사 분리(INV-3/8)가 근거. `[제안]` 소켓 부착 오프셋이 아이템별로 갈리면 소켓 맵을 ItemData로 이동(D10 재평가 트리거).

### 2026-07-06 (4) — EquipmentComponent 런타임 디버깅 드로어
- **`Editor/ItemSystem/EquipmentComponentDrawer.cs`**(에디터 전용): `AbilitySystemComponentDrawer`와 동일 방식 — `[CustomEditor(typeof(EquipmentComponent))]`, private `_equipped`를 리플렉션으로 읽어 **Play 모드에서 슬롯 → 장착 아이템 → 모듈 목록**을 인스펙터에 표시. `RequiresConstantRepaint`로 런타임 갱신. (유저 확인: Active Effect 추가·이동속도 감소 정상.)

### 2026-07-06 (3) — S1 PASS + 모듈 상태 lifecycle(S3/S4 부분) + 픽업

- **S1 검증 완료(play):** 유저 확인 — 인벤→장비 로그 후 LMB로 적 Health 감소. HARNESS §7 **S1 ☑**. (에셋/씬 GUID·재현은 → [test-harness-state.md](../../test-harness-state.md))
- **모듈 lifecycle에 상태 동반(→D9):** 유저 요구 "장착 시 이동속도 −5"가 회수 핸들(per-item 상태)을 필요로 함 → `OnEquip/OnUnEquip(ModuleContext, IModuleState)`로 확장, 캐스트는 `ItemModule<TState>` 제네릭 베이스 1곳(INV-5). 순회·상태 정렬은 `ItemInstance.OnEquip/OnUnEquip`이 담당(상태 배열 캡슐화). EquipmentComponent는 `instance.OnEquip(ctx)` 호출로 단순화. → 로드맵 S3(모듈 인프라)·S4(상태 모듈) **부분 선착수**(§7 순서 변경 보고).
- **`StatModifierModule` 구현:** 장착 시 Infinite GE를 소유자 ASC에 적용, 해제 시 핸들로 회수. 핸들은 `StatModifierState`(per-item, INV-5). `GE_EquipSpeedDown`(Infinite, Speed −5)을 무기 2번째 모듈로 추가 → 한 무기에 **모듈 조합**(공격+스탯) 데모.
- **픽업 프리팹(`ItemPickup`):** 트리거 진입 시 대상의 `InventoryComponent`에 Add + `EquipmentComponent`로 장착 후 소멸. 플레이어 **초기 노 item**으로 전환(startingItems 비움, EquipmentTestBootstrap 제거) → 획득 흐름이 픽업 기반.
- **GE 오소링 문서화(유저 요청):** `architecture/ability-system/gameplay-effect.md`에 "에셋 생성·세팅" 절 추가(필드·operation 값·MCP/YAML 레시피·실제 예시). Instant 검증됨을 반영해 "GE 타입별 실전 상태" 절 정정.
- **정합성:** V1(참조·시그니처) OK. 모듈 lifecycle 변경 영향은 MeleeAttack(무상태·미override)·StatModifier뿐. play 재검증은 유저.

### 2026-07-06 (2) — S1 착수: Inventory → Equip → Item 수직 슬라이스 (코드 완료, play 검증 대기)

**배경/의도.** S1("평타 1대가 적 HP를 깎는다")을 단독 전투 루프가 아니라 **`Inventory → Equip → Item` 체인을 씬에서 실제로 굴려 item 코어를 눈으로 검증**하는 슬라이스로 착수(유저 확정). inventory worklog의 "인벤/장비 = item 코어 검증용 실험 도구" 취지를 그대로 실행. 데미지는 기존 GAS(GameplayEffect) 재사용, equip은 최소로 실배선(play 모드 검증). 즉 **item S1 + equipment E1 겸함**, inventory는 얇은 부트스트랩으로만 물림(I2 블로커 미접촉).

**착수 전 확인(정합성).** 코어·전투·입력·씬 배선 통독으로 아래를 확정: ① HP는 `CharacterAttributeSet.Health`(ASC+GE)에 있고 `CharacterBase.TakeDamage(int)`는 빈 스텁 → GE 경로가 정답(유저 Q2도 GE 재사용). ② ASC엔 `ApplyGameplayEffectToSelf`만 있음 → 대상에겐 **대상 ASC의 이 메서드를 직접 호출**(신규 API 불필요, Instant GE로 Health BaseValue 감소). ③ `AbilitySystemModuleContext : ModuleContext`(소유자 ASC 전달)가 이미 있어 OnEquip 컨텍스트로 재사용 가능. ④ `Attack` 입력 액션은 존재하나 `PlayerController`는 Move만 배선.

**설계 판단(왜 이렇게).**
- **공격 = 능력 인터페이스 `IWeaponAttack`**(데미지 GE + 사거리)로 선언, EquipmentComponent는 구체 모듈이 아니라 이 능력으로 공격을 요구(INV-11). (→D7)
- **결정/실행 분리(INV-3/4):** 모듈은 "무엇을 적용할지"(데미지 효과·사거리)만 보유하고 씬을 모름. 오버랩 감지·GE 적용 등 씬 접근은 **EquipmentComponent(시스템)** 가 수행 — ItemBehaviour 아님(INV-4는 Behaviour에 로직 금지). S1엔 표현 없어 ItemBehaviour 불필요. (→D8)
- **무상태 공격(INV-5):** S1 평타는 쿨다운·콤보 없음(그건 S6) → `MeleeAttackModule = StatelessModule`, per-item 상태 인프라 미도입. `StatModifierModule`(장착 시 스탯 기여)은 회수용 per-item 핸들 상태가 필요해 **S7/E2로 미룸** — 이번엔 손대지 않음(북극성: 트리거 전 복잡화 금지).
- **equip 소유(equipment D2/D3):** `EquipmentComponent`은 `PlayerCharacter` 무참조, 같은 GO의 ASC만 GetComponent → 적·NPC도 장착 가능. slot은 `Dictionary<SlotType, ItemInstance>` 필드(D3).

**변경 파일.**
- CREATE `Item/Module/MeleeAttackModule.cs` — `IWeaponAttack{ DamageEffect, Range }` + `MeleeAttackModule : StatelessModule, IWeaponAttack`(직렬화 `damageEffect`·`range`).
- MODIFY `EquipmentComponent.cs`(죽은코드 전량 대체) — `[RequireComponent(ASC)]`, `Equip(ItemInstance)`(IEquippable 판별→슬롯 저장→모듈 `OnEquip(AbilitySystemModuleContext)`), `Unequip(SlotType)`, `TriggerAttack()`(Weapon 슬롯의 `IWeaponAttack`→`Physics.OverlapSphereNonAlloc` 전방→자신 제외, 각 타겟 ASC에 `ApplyGameplayEffectToSelf(damageEffect)`), `OnEquipped/OnUnequipped` 이벤트.
- CREATE `Item/EquipmentTestBootstrap.cs`(임시 스캐폴드) — `new Inventory()`→`Add(startingWeapon)`→`Query(d=>d is IEquippable)` 첫 엔트리→`equipment.Equip(entry.Instance)`+`Remove`. **Inventory→Equip 홉**. (I2 정식 배선 아님을 주석 명시.)
- MODIFY `PlayerCharacter.cs` — `Attack()` 추가(캐시한 EquipmentComponent.TriggerAttack 호출), Awake에서 `_equipment` 캐시.
- MODIFY `PlayerController.cs` — Possess/UnPossess에 `Player.Attack.performed ± OnAttackInput`, `OnAttackInput→_playerCharacter.Attack()`. (생성 래퍼에 `Player.Attack` 존재 확인.)
- DELETE `TestItem.cs`·`TestModule.cs`·`Assets/Data/Item/Test.asset`(+각 meta) — 어느 prefab/scene에서도 GUID 미참조 확인 후 제거(스캐폴드 정리).

**검증 상태.** V1 코드 정합성 완료(참조·시그니처·네임스페이스). 컴파일/`play` V2는 유저 몫 — 남은 것: (a) 데미지 GE 에셋(Instant, Health AddBase −10), (b) 무기 EquipItem 에셋(Slot=Weapon, modules=[MeleeAttackModule{그 GE, range≈2}]), (c) 적에 ASC+Health, (d) 플레이어에 EquipmentTestBootstrap. 에셋/씬 셋업은 MCP로 시도 중(도메인 리로드 중 연결 끊김 → 컴파일 완료 후 재시도).

**§5 보고.** `[추상화 제안] IWeaponAttack`(공격을 능력으로 — 대안: EquipmentComponent가 구체 MeleeAttackModule 하드코딩) / `[제안]` 장기적으로 공격 실행을 EquipmentComponent→별도 전투 드라이버로 분리 가능(S6 콤보 도입 시), S1은 최소로 EquipmentComponent에 둠.

### 2026-07-06 — INV-2/3/4 자가점검 (다음작업 1 완료)
- `Core/ItemSystem/` + `Assets/Scripts/Item/` 전수 점검. **능동 위반 0.** INV-2: `ItemInstance` 상속 클래스 0(구 `WeaponInstance`·`ItemBase`는 이미 삭제됨 — 이관은 파일 레벨에서 완료). `EquipItem`/`ConsumeItem`은 카테고리 축(D4 승인). INV-3: Module 3종 씬/GameObject 접근 0. INV-4: `ItemBehaviour` 로직 0줄.
- 경미 스멜만: `TestItemData`(카테고리 없는 테스트 스캐폴드)·`EquipmentComponent`(삭제된 `WeaponInstance` 참조 죽은코드) → S1에서 실 아이템 대체하며 정리로 처리(블로커 아님).
- 결과로 블로커의 "상속 코드 이관 대상" 전제 = 낡음 → 해소. 다음작업을 S1로 재지정.
- (process) "하던거 하자" 재개가 매번 어긋나던 문제 → `NOW.md` 단일 재개 지점 도입 + SessionStart 훅/CLAUDE.md/HARNESS §3 재배선. 상세는 [TODO-BOARD](../../../TODO-BOARD.md) Done.

### 2026-07-05 (2) — inventory 세션발 교차 편집
- **[교차 feature] `IStackable{ int MaxStack }` 능력 인터페이스 + `ConsumeItem` 구현 추가** (`ConsumeItem.cs`). 사유: `inventory` I1이 데이터 주도 스택을 요구 — 스택을 전 아이템 공통 필드로 두지 않고 능력으로(장비 `EquipItem`은 미구현→스택 불가, INV-11). `IEquippable`과 동형. `maxStack` 기본 99, `OnValidate`로 최소 1 클램프. 상세 근거는 inventory decisions D4. ConsumeItem의 낡은 "개수/인벤토리 범위 밖" 주석 제거(스코프 확장 반영).

### 2026-07-05
- **아이템 전용 자유형 로직 = `ItemRuntime`(합성 멤버)** 로 확정. 상속(INV-2) 아님, per-instance 생성(INV-1/5). 구현은 **첫 트리거 아이템까지 보류**(현재 0). (→D6)
- **[대기]** HARNESS INV-10 문구 개정 필요 — 현재 "행동이 아닌 구조적 특수함에만"이 자유형 로직 Runtime을 배제. 유저 승인 후 반영 예정. (D6 전제)

### 2026-07-04
- `feature/<feature-id>/` 폴더 구조 도입, Item 구현 하네스를 `HARNESS.md`로 배치 (→D1)
- feature-list에 `item-system` 등록
- 설계 충돌(상속 vs 모듈 조합) → **Item HARNESS 우선**으로 확정. 아키텍처 문서에 우선순위·이관 대상 명시 (→D2)
- 부위(slot) 표현 방식 결정: **필드**(Module 아님). 근거·뒤집는 트리거는 decisions 참조 (→D3)
- 아이템 종류(장비/소비/중요) 축이 스코프 내로 확정 → `ItemData` 서브클래스(`EquipItem`/`ConsumeItem`)로 분리, 능력 인터페이스로 시스템 판별(INV-11). slot은 base→`EquipItem`으로 이동. 스코프 가드: 개수·충전·인벤토리·세이브는 여전히 밖 (→D4·D5)
- 코드: base `ItemData`에서 slot/SlotType 제거, `EquipItem`(+`IEquippable`)·`ConsumeItem` 신설, `ItemInstance` 최소 구현(모듈 상태 병렬 배열 + `GetState<T>` 단일 캐스트) (D3·D4 구현)
