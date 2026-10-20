# _parked — 보관 (비활성)

GAS(Ability System) 데모에 집중하기 위해 **잠시 치워둔 비-GAS 인게임 문서**. 삭제가 아니라 비활성 보관이며, 필요할 때 원위치로 되돌린다.

## 원칙
- **현재 작업(GAS 데모)에선 열지 않는다.** 되돌릴 때만 참조.
- **원 경로를 미러링**해 보관한다(`_parked/<원래 경로>`) — 되돌리기는 `git mv`로 원위치.
- 여기 문서 안의 상대 링크는 `_parked/` 깊이만큼 어긋날 수 있다(보관이라 방치). 되돌리면 원래 경로 기준으로 복원된다.

## 보관 내용

| 원 경로 | 내용 |
|---|---|
| `project/architecture/camera.md` | 카메라 |
| `project/architecture/controller-character.md` | 컨트롤러-캐릭터 분리(Possession) |
| `project/architecture/item-equipment.md` | ItemSystem / EquipmentSystem |
| `project/architecture/overview.md` | 비-GAS 아키텍처 개요(전체 구조) — 활성 인덱스에서 분리 |
| `project/design.md` | 전체 게임 기획(컨셉·핵심기획·씬·입력) — 활성 GAS 데모 방향에서 분리 |
| `agent/feature/feature-list.md` | item-system feature 대시보드 행 |
| `agent/feature/item-system/` | 아이템·인벤토리·장비 feature 문서(폴더 통째, 내부는 이미 archive) |
| `agent/TODO-BOARD.md` | TODO-BOARD의 게임 쪽(아이템·캐릭터·아트) 항목 — 2026-10-20 보드에서 분리. 되돌릴 땐 파일째가 아니라 항목을 원래 칸에 다시 붙인다 |

## 코드 보관 — `Assets/Scripts/_Parked/`

문서와 짝으로, 비-GAS 스크립트도 `Assets/Scripts/_Parked/<원래 경로>`에 보관한다(2026-10-20). `.meta`를 함께 옮겨 GUID를 유지하고, asmdef가 없어 그대로 컴파일된다. namespace는 원래 그대로다.

| 원 경로 (`Assets/Scripts/` 기준) | 내용 |
|---|---|
| `Core/ItemSystem/` | 아이템·인벤토리·장비 코어 |
| `Item/` | 아이템 게임플레이 구현(픽업·모듈) |
| `CharacterBase.cs` · `ControllerBase.cs` · `MonsterCharacter.cs` · `PlayerController.cs` | 컨트롤러-캐릭터 계층 |
| `SequenceManager.cs` | 시퀀스 매니저 |

- 여기 보관 문서 속 코드 경로는 **원래 경로** 기준이다(코드를 되돌리면 맞는다).

## 되돌리기
1. 해당 파일/폴더를 `git mv`로 원 경로로 이동(코드는 `.meta`와 함께).
2. 분리 시 원본에서 제거했던 섹션(architecture overview 인덱스·design.md·feature-list)을 다시 통합.
3. `CLAUDE.md` 라우팅·활성 인덱스의 "보관" 안내 제거.
