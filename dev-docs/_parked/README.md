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

## 되돌리기
1. 해당 파일/폴더를 `git mv`로 원 경로로 이동.
2. 분리 시 원본에서 제거했던 섹션(architecture overview 인덱스·design.md·feature-list)을 다시 통합.
3. `CLAUDE.md` 라우팅·활성 인덱스의 "보관" 안내 제거.
