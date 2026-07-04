# item-system — 전투 아이템 시스템

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: 2026-07-04

## 목표
아이템(무기·방어구)이 모듈 조합으로 동작을 획득하고, 장착 시 캐릭터 전투 루프에 반영되는 수직 슬라이스를 구현한다. 최종적으로 **코드 추가 없이 에셋 조합만으로** 신규 온히트 효과를 만들 수 있는 상태가 목표.

## 수용 기준 (Definition of Done)
- [ ] feature 전용 하네스 [HARNESS.md](HARNESS.md) §7 로드맵 S1~S10 슬라이스 전부 ☑
- [ ] 각 슬라이스가 HARNESS §4 3단 검증(V1 정적 / V2 기능 / V3 회귀) 통과
- [ ] HARNESS §2 불변 조건(INV-1~12) 위반 0

## 범위
### 포함
- ItemData(SO) / ItemInstance / ItemModule / ItemBehaviour 조합 구조
- 근접·총기 Strategy, 온히트 모듈, 콤보·패링, 기여(스탯) 배선, 저작 툴링
### 제외 (명시적으로 하지 않을 것)
- 등급 / 인벤토리 / 개수 / 네트워크 / 세이브 (HARNESS §1 스코프 밖)

## 설계 개요
- **구현 방향·불변 조건·검증·로드맵의 단일 진실:** 같은 폴더의 [HARNESS.md](HARNESS.md) (feature 전용 하네스).
- 아키텍처 문서: [item-equipment.md](../../../project/architecture/item-equipment.md)
- 코드: `Assets/Scripts/Core/ItemSystem/`

## 결정 기록
| 날짜 | 결정 | 대안 | 채택 사유 |
|---|---|---|---|
| 2026-07-04 | feature 전용 하네스/설계 문서를 `feature/<id>/` 폴더에 progress와 분리 배치 | 일반 HARNESS.md에 통합 | 아이템 시스템 고유의 불변 조건·로드맵이 방대해 feature 스코프로 격리, 프로세스 규약(일반 HARNESS)과 구현 규약(feature HARNESS)을 분리 |
| 2026-07-04 | **설계 권위: Item HARNESS 우선.** 상속 금지·POCO·모듈 조합(INV-2/3/4)이 이 시스템의 진실 | 아키텍처 문서(`item-equipment.md`)·기존 상속 기반 코드를 진실로 유지 | 포트폴리오상 "코드 0줄로 신규 효과 추가"(HARNESS §7 S9)를 증명하는 모듈 조합 설계가 핵심 산출물. 상속 구조로는 그 확장성 데모가 성립하지 않음 |

## 작업 로그
### 2026-07-04
- `feature/<feature-id>/` 폴더 구조 도입, Item 구현 하네스를 `HARNESS.md`로 배치
- feature-list에 `item-system` 등록
- 설계 충돌(상속 vs 모듈 조합) → **Item HARNESS 우선**으로 확정. 아키텍처 문서에 우선순위·이관 대상 명시

## 블로커
- 없음 (직전 [설계 충돌]은 Item HARNESS 우선으로 해소 — 결정 기록 참조).
- 단, 기존 상속 기반 코드/서술은 INV-2/3/4 위반 소지가 있어 **이관 대상**이다. (아래 다음 작업 1)

## 다음 작업
1. **INV 점검:** 현재 `Core/ItemSystem/` 및 `Assets/Scripts/Item/` 코드를 INV-2/3/4 기준으로 훑는다 — `ItemInstance`가 MonoBehaviour인지, `GunItemInstance`/`ShoesItemInstance` 등이 `ItemInstance`를 상속하는지 확인하고 위반 목록화. (harness §2 자가점검 형식으로 보고)
2. 위반이 있으면 모듈 조합/POCO 구조로 이관 계획을 세운 뒤, HARNESS §7 **S1(최소 전투 루프: 평타 1대로 적 HP 감소)** 부터 슬라이스 진행
