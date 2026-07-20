# item — 아이템 코어 (데이터·인스턴스·모듈·런타임)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: 2026-07-10 (KST)

> **구조(2026-07-05):** `item-system` 그룹의 하위 feature다 — 이 `item`(코어) · [`inventory`](../inventory/progress.md) · [`equipment`](../equipment/progress.md)(미착수·제외). 공유 불변조건은 그룹 폴더의 [`HARNESS.md`](../HARNESS.md)이며 세 하위가 모두 이를 따른다.

## 목표
아이템(무기·방어구)이 모듈 조합으로 동작을 획득하고, 장착 시 캐릭터 전투 루프에 반영되는 수직 슬라이스를 구현한다. 최종적으로 **코드 추가 없이 에셋 조합만으로** 신규 온히트 효과를 만들 수 있는 상태가 목표.

## 수용 기준 (Definition of Done)
> ⚠ **2026-07-12 규약 백지화** — 이전 수용 기준(로드맵 S1~S10 / 3단 검증 / INV-1~12)은 [HARNESS 아카이브](../archive/HARNESS.archived-2026-07-12.md)로 이관. 새 수용 기준은 설계 재정립 후 다시 세운다.
- [ ] (미정 — 새 설계 확정 후 작성)

## 범위
### 포함
- ItemData(SO) / ItemInstance / ItemModule / ItemBehaviour 조합 구조
- 근접·총기 Strategy, 온히트 모듈, 콤보·패링, 기여(스탯) 배선, 저작 툴링
### 제외 (명시적으로 하지 않을 것)
- 등급 / 인벤토리 / 개수 / 네트워크 / 세이브 (HARNESS §1 스코프 밖)

## 설계 개요
- **판단 기준·방향성:** 그룹 폴더의 [HARNESS.md](../HARNESS.md) (2026-07-12 규약 백지화 — 이전 불변조건·로드맵은 [아카이브](../archive/HARNESS.archived-2026-07-12.md)).
- 아키텍처 문서: [item-equipment.md](../../../../project/architecture/item-equipment.md) ⚠ 백지화 이전 설계 서술 — 재정립 후 갱신 대상.
- 코드: `Assets/Scripts/Core/ItemSystem/`

## 세부 TODO (구현 체크리스트)
> ⚠ 이전 로드맵(HARNESS §7 S1~S10)은 백지화로 아카이브. 새 backlog는 설계 재정립 후 작성.

## 결정 기록
→ [decisions.md](decisions.md). **2026-07-12 규약 백지화 — 이전 D1~D12는 [아카이브](../archive/decisions.archived-2026-07-12.md)로 이관(구속력 없음).** 새 결정은 재정립 후 처음부터 다시 쌓는다.

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음. (직전 [설계 충돌]은 Item HARNESS 우선으로 해소.)
- ~~기존 상속 기반 코드/서술 INV-2/3/4 위반 소지 → 이관 대상~~ **해소(2026-07-06):** 상속 기반 구코드(`WeaponInstance`·`ItemBase` 등)는 이미 삭제됨. INV-2/3/4 자가점검 완료 — 능동 위반 0(`ItemInstance` 상속 클래스 0, Module의 씬/GameObject 접근 0, `ItemBehaviour` 로직 0). 경미 스멜: `TestItemData`(테스트 스캐폴드)·`EquipmentComponent` 죽은코드 — S1에서 실 아이템으로 대체하며 정리.

## 다음 작업
※ **(2026-07-10) 아이템 시스템 신규 설계 진행 중 — 유저 숙고, 결론 전 구현 금지.** 목적·현황은 TODO-BOARD "진행 중" 참조. 아래 0번 검증 포함 아이템 작업 전체가 설계 결론에 종속(현재 모듈 lifecycle 미호출 상태 — worklog 2026-07-10).
0. **(코드 완료·play 검증 대기 → 설계 결론까지 보류) S5 총기 히트스캔(D11) + 공격 실행 분리(D12), 2026-07-09:** `GunAttackModule`+`IHitscanAttack`, 공격 판정은 신설 `WeaponAttackComponent`(Player.prefab 배선 완료)가 능력 분기로 수행, `Weapon_TestGun.asset`(GE_MeleeDamage, range 20). **검증**: 씬 ItemPickup의 item에 Weapon_TestGun 할당 → Play → 픽업 → 원거리에서 LMB → TestEnemy HP −10. ⚠ 별건: `Weapon_TestSword.asset` modules가 비어 있음(문서 불일치 — 검 평타 불능 상태, 유저 확인 필요).
1. **(완료) 장착 표현 슬라이스 play 검증 PASS(2026-07-09, D10).** 배선: `Weapon_TestSword.prefab`→`Tset Weapon.prefab`, `Player.prefab` sockets Weapon→`Model (1)` Transform — 상세는 [test-harness-state.md](../../test-harness-state.md). 부수 수정: `ItemDataAssetDrawer`에 `prefab` 필드 표시 추가(드로어가 필드를 명시 나열 — 새 직렬화 필드는 드로어에도 등록 필요). 개선 여지: 소켓을 전용 `WeaponSocket` 빈 GO로 교체(현재 모델 Transform 직결).
2. **(완료) S1 play 검증 PASS(2026-07-06).** + 모듈 상태 lifecycle(D9, S3/S4 부분)·`StatModifierModule`(장착 시 Speed −5)·`ItemPickup` 프리팹까지.
3. **S2 — WeaponData + 근접 Strategy:** 무기 에셋 교체로 데미지·범위가 달라지게. (다음 초점)
4. **S3/S4 마무리:** lifecycle 기반은 섰으니, "발화 온히트 모듈"(S3)·"차지샷 상태"(S4) 실제 예제로 완결.
