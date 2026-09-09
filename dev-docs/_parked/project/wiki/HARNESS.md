# HARNESS.md — wiki 비-GAS 섹션 보관 (parked)

> **비활성 보관.** GAS 데모 중심 재편 과정에서 활성 `wiki/HARNESS.md`의 **비-GAS 인게임 섹션 문서**만 여기로 분리했다. 원 경로 미러: `dev-docs/project/wiki/HARNESS.md`.
> wiki `index.html`/`src`는 아직 GAS+비-GAS **혼합 빌드 산출물**이라 그대로 있다(html 재작업은 추후). html을 GAS 중심으로 정리할 때 아래 섹션들을 실제로 제거·이관하며, 그 시점에 이 파일을 참조한다.
> 되돌릴 때는 아래 내용을 활성 `wiki/HARNESS.md` §2~§2.1에 원위치.

---

## 분리된 비-GAS 섹션 (wiki `index.html`의 `<section id>`)

**사이드바 NAV — 아키텍처 그룹의 비-GAS 항목:** `arch` · `scenes` · `camera` · `input`
(GAS 항목 `ability-system`은 활성 HARNESS.md에 남김.)

| # | section id | 제목 | 내용·구성 | 하위 앵커/탭 | `SECTION_META` |
|---|---|---|---|---|---|
| 04 | `arch` | 컨트롤러·캐릭터 분리 (Possession) | 클래스 트리 2(ControllerBase→PlayerController / CharacterBase→PlayerCharacter·MonsterCharacter) + 이동로직 소유 callout | - | decisions · sources=Controller/PlayerController/PlayerCharacter |
| 06 | `scenes` | 씬 구성 | MainMenu→Lobby→Game→Result 플로우 | - | decisions · sources=`Scenes/` |
| 07 | `camera` | 카메라 | info-card(Projection/FOV·Follow·Body·Aim) + BindingMode 설명 | - | decisions · sources=`Prefabs/Player.prefab` |
| 08 | `input` | 입력 액션맵 | Action/Map/바인딩/타입 표 | - | decisions · sources=`Input/PlayerInputActions.inputactions` |

**대응 코드/문서(대부분 `_parked`로 이동됨):**
- `arch` ↔ `dev-docs/_parked/project/architecture/controller-character.md`
- `camera` ↔ `dev-docs/_parked/project/architecture/camera.md`
- `scenes`·`input` ↔ 씬/입력 에셋(게임 전용, GAS 데모 범위 밖)

> 이 섹션들의 §2.1 편집 규약(섹션 id 대응·메타 동기화·동적 요소 등)은 활성 `wiki/HARNESS.md`의 일반 규약(§0·§1·§3·§4)을 그대로 따른다 — 여기엔 위치 정보만 보관한다.
