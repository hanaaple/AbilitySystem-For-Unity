# TODO-BOARD (보관) — 비-GAS 항목

> 원 경로: `dev-docs/agent/TODO-BOARD.md`. 2026-10-20 유저 "TODO 쪽에 이전에 GAS 제외한 TODO인 부분들 다 치우자. 아카이빙은 해두고", "_parked로 옮겨" — GAS 데모 집중을 위해 게임 쪽(아이템·캐릭터·아트) 항목을 보드에서 뺐다. 항목 본문은 옮기기 전 그대로다(상대 링크는 `_parked/` 깊이만큼 어긋날 수 있음 — README '원칙').
> 되돌리기: 항목을 원래 칸(아래 소제목)으로 다시 붙인다.

## '뒤로 미룸' — 2026-10-18 목표에서 미룬 것

- [ ] **(idea)** **게임 쪽 태그 적용** — ① `InputModule`의 입력 enum(`GameplayInputAction`·`ItemInputAction`)을 InputTag로 바꿀지(Lyra 패턴, 에이전트 기억이며 원문 미확인) ② 아이템 종류·진영·데미지 타입 분류 태그. 적용 여부는 유저 미정. _(2026-10-16 등록 — 태그 후보 조사)_

## '뒤로 미룸' — 이전 당장 목표·진행 중이던 것

- [ ] Item Instance - Item Behaviour, Socket에 Item prefab 장착까지
- [ ] **(design)** ⭐ **기획안 확정 (선행 — 아이템 설계보다 먼저)** — 아이템 구조를 못 정하는 이유가 결국 **기획이 안 정해져서**임이 드러났다(2026-07-13). 전투가 콤보·탄약·재장전·패링을 실제로 요구하는지에 따라 구조 결론이 뒤집힌다(design-draft의 미결 D-1). `dev-docs/project/design.md`를 전투 중심으로 확실히 세운 뒤 아이템 설계로 돌아온다. _(2026-07-13 등록)_
- [ ] **(design)** 아이템 시스템 신규 설계 — **기획안 확정 대기로 보류.** 유저와 설계 논의 중, 결론 전 구현 금지. 아래 유저 의도가 목적:
  - 어떤 캐릭터든 아이템 장착에 따라 **알아서** 작동 (캐릭터 특화 없이)
  - 아이템에 종속되는 로직이 아이템 외부에 작성되지 않을 것
  - Gun·Melee 등 종류별 로직이 들어갈 자리가 있되, 아이템별 클래스 분리·상속을 강제하지 않을 것 (전략 패턴 방향은 유저 언급)
  - 아이템을 구현할 때 알아야 하는 것(컨텍스트, 외부 접근 방법, 시스템 전체 이해)이 복잡하지 않을 것 — 복잡도 대비 이득 없는 과설계 배제
  - 표현부(ItemBehaviour·GameplayCue 활용 포함)의 역할과 Combat 로직과의 연동 방식은 재검토 대상
  - **진행(2026-07-12):** 규약 백지화 완료(이전 INV·D·로드맵 → `feature/item-system/archive/`). 새 판단 기준 4축 채택(feature/HARNESS '폴더 구조·네이밍', Nystrom Type Object vs Subclass Sandbox).
  - **진행(2026-07-13):** 설계도 백지에서 재작성 → [`feature/item-system/design-draft.md`](feature/item-system/design-draft.md)(확정 아님). `ItemModule`·`ItemInstance`·"모듈 조합"은 **권위 없음**으로 강등하고, 그렇게 서술하던 project 문서(architecture/item-equipment.md·overview.md, design.md)를 **전부 미정 처리**. 후보 3안(A: 무기=Runtime 서브클래스 / B: 현행 시스템이 알고리즘 보유 / C: 어빌리티) — GAS엔 GameplayAbility가 없음(코드 확인). **결론은 기획안(위 ⭐)이 서야 남** — 콤보/탄약/패링 채택 여부가 A vs B를 가름. _(2026-07-10 등록, 07-13 갱신)_

## '뒤로 미룸' — 이전 우선순위 낮음

- [ ] **(chore)** **아트 에셋 폴더 관례 적용** — 관례 확정(2026-08-05, 유저): **오브젝트 단위 co-locate** `Assets/Art/<모델>/`에 mesh·texture·material 다 같이(모델 통째 임포트에 강함), **여러 곳 공유하는 것만** `Assets/Art/_Shared/`. 미실행 — 필요 시 폴더 생성 + 루트에 뜬 `Lit.mat`을 공용이면 `_Shared/`로 이관(Room 전용이면 `Art/Room/`에 새로). _(2026-08-05 등록)_
- [ ] Inventory/Equipment System 구현
- [ ] **(code)** **Player 이동 구동 방식 교체** — 현재 `PlayerCharacter.MoveDelta`가 `transform.position`에 직접 대입해 **충돌을 전부 무시한다**(벽 통과, 중력 없음). `CharacterController.Move()`로 교체하면 충돌·계단(`stepOffset`)·경사(`slopeLimit`)가 해결된다. 중력은 별도로 y속도 누적 + `isGrounded` 필요(`Move`는 중력을 넣지 않음). 교체 후 `transform.position` 직접 대입과 혼용 금지(컨트롤러 내부 상태와 어긋남).
  - **부수 정리:** Player 루트에 `CapsuleCollider`·`CharacterController`·`NavMeshAgent`가 붙어 있었으나 **코드 참조 0건**(전부 미사용, 첫 커밋 `c49afa1` 프로토타입부터 존재).
    - ✅ **`CapsuleCollider` 제거 완료(2026-07-20, 유저)** — `CharacterController`가 `Collider` 상속이라 형상까지 중복(둘 다 r0.5/h2/center0)이었고 **트리거 콜백 2회의 원인**이었다. 해소됨.
    - 남음: `NavMeshAgent`는 `MonsterCharacter`만 사용하므로 Player에선 불필요. `CharacterController`는 아직 미사용(위 교체 시 사용하게 됨).
  - 필드명 주의: `CharacterBase._controller`는 possess 하는 `ControllerBase`라 이름이 겹친다.
  - 관련: 이동 모드가 상태로 갈리는 장르(구르기·공격 중 이동·피격 경직)라 GA 착수 시 "어빌리티가 이동을 어떻게 점유하는가" 경계와 함께 봐야 한다. _(2026-07-20 등록 — GE 픽업 트리거 2회 원인 추적 중 발견)_
- [ ] **(code)** `ItemPickup.OnTriggerEnter` **중복 적용 버그** — `GameplayEffectPickup`과 동일 원인(콜라이더 단위 콜백 + `Destroy` 프레임 지연)으로 인벤토리에 아이템이 2번 들어간다. `GameplayEffectPickup`에는 프레임+대상 가드를 넣었으나 `ItemPickup`은 파킹 중이라 손대지 않았다. 무기 재설계 때 함께 처리. _(2026-07-20 등록)_

## wiki — 2026-10-23 wiki 보관과 함께 보드에서 분리

- [ ] **(wiki)** 설계 문서 상호 연결 — 각 섹션 요약에서 `dev-docs/project/architecture/*` 심화 문서로 링크. 로컬/Pages에선 `.md`가 raw로 뜨므로 **GitHub blob URL** 사용. 저장소 public 전제. `SECTION_META.sources`도 같은 방식으로 클릭 가능하게. _(2026-07-05 등록)_
