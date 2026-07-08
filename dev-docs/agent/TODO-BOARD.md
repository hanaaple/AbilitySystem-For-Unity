# TODO-BOARD — 작업 현황 보드

> feature 단위 추적은 `feature-list.md`가 담당한다. **이 보드는 그보다 잘게 쪼개진 잡다한 작업·개인 TODO·이슈**를 모은다 — 문서 정비, 인프라(GitHub 설정 등), 실험, 리팩터, 아이디어 등 feature로 묶기 애매한 것들.
> 관리 규약은 `HARNESS.md` §7. 항목은 상태가 바뀌면 아래 칸 사이를 이동한다.

- 최종 갱신: 2026-07-08 (KST)

---



## 📋 TODO (당장 목표)
- [ ] Item Instance - Item Behaviour, Socket에 Item prefab 장착까지
- [ ] Ability System에 Attribute 쪽에 Class 명이 헷갈리게 만들어놨는데 어떻게 해야 좋지

## 📋 TODO (순서 무관 우선순위 낮음)

- [ ] **(editor)** `SubclassSelector` 중복 제외 **다른 스코프** 지원 — 현재 구조: 범용 `SubclassSelectorDrawer`는 `excluded`를 받아 **그리기만**(`DrawSelector`), 수집은 **호출측(전용 드로어)**이 `CollectSiblingValues`(자기 배열)로 트리거. 리스트 외부(같은 오브젝트 등) 스코프가 필요하면 **전용 드로어가 다른 수집 로직**(`SerializedProperty` 순회 등)으로 `excluded`를 만들어 넘기면 됨 — 범용 드로어 변경 불필요. 지금은 List로 충분해 추가 수집기 미구현. _(2026-07-08 등록)_
- [ ] Inventory/Equipment System 구현
- [ ] **(code)** `GameplayEffectType` Instant/Duration 실전 상태 확정 — enum 주석은 '미구현'이나 ASC에 실행 경로 존재. **유저가 직접 확인 예정.** 확정 후 `architecture/ability-system/gameplay-effect.md` §확인 필요 + `overview.md` 요약 표 반영. (feature `gameplay-effect` 블로커에도 추적) _(2026-07-05 등록 — GAS 문서화 중 발견)_
- [ ] **(wiki)** 설계 문서 상호 연결 — 각 섹션 요약에서 `dev-docs/project/architecture/*` 심화 문서로 링크. 로컬/Pages에선 `.md`가 raw로 뜨므로 **GitHub blob URL** 사용. 저장소 public 전제. `SECTION_META.sources`도 같은 방식으로 클릭 가능하게. _(2026-07-05 등록)_
- [ ] GameplayAbility (GA) 구현
- [ ] GE Execution 구현
- [ ] GE 스택 구현
- [ ] Gameplay Cue 구현


## 🔧 진행 중

(없음)

## ✅ Done

- [x] **(docs)** 코드 설계 규칙 문서화 — `CODE_CONVENTION.md`에 ① **설계 원칙**(필요할 때만 범용화·과설계 지양, 좁은 스코프/설계 갈림은 착수 전 확인, 범용 코드는 정책 모름) ② **에디터 확장(드로어) 가이드**(범용/전용 분리, `SubclassSelector` 예시, 런타임/에디터 경계) 섹션 신설. _(2026-07-08)_
- [x] **(editor)** `SubclassSelector` 필드를 오브젝트 참조 필드처럼 개선 — 선택된 타입의 소스 스크립트를 `ObjectField` 스타일로 표시(단일클릭 ping / 더블클릭 open), 타입 선택은 우측 버튼 → **검색 가능한 네이티브 팝업**(`AdvancedDropdown`, Add Component 창과 동일). `EditorTypeUtility.FindScript`(타입→MonoScript, 캐시) + `SubclassAdvancedDropdown` 추가. 범용이라 ItemData `runtimeClass` 등 모든 `[SubclassSelector]`에 적용. (New Class 생성은 안 함 — 유저 결정) 이후 `uniqueInList` 옵션 추가(리스트 내 이미 쓰인 타입 제외 — 형제 배열 훑는 로직 범용화)하고, `AttributeSetInitData.attributeSetTypeName`을 커스텀 `EditorGUI.Popup` → `[SubclassSelector(typeof(AttributeSet), uniqueInList:true)]`로 전환(드로어 죽은코드 `DrawAttributeSetPopup`·`GetUsedTypeNamesByOthers` 제거). _(2026-07-08)_
- [x] **(refactor)** 에디터 스크립트 배치를 `Editor/<feature>` 미러 구조 → **feature-local `Core/<feature>/.../Editor`** 로 통일. 입도는 **서브시스템 레벨**(기존 `ItemSystem/Inventory/Editor` 선례와 일치): 드로어를 대상 타입 옆에 둠(Attribute·Effect·ItemSystem 각각 Editor). feature 무관 공통 에디터 유틸(ConditionalShow·SubclassSelectorDrawer·EditorDrawUtility·EditorTypeUtility·TypeChoiceList)은 `Core/Common/Editor`. `.meta`(폴더 meta 포함) 그대로 이동해 GUID 보존, asmdef 없어 참조 영향 없음. _(2026-07-08)_
- [x] **(docs)** 기존 구현분 정리 — **Player Character 계층 설계**(Controller · Character 분리, Possession 패턴) 문서화. 기존 `controller-character.md`가 얇은 스텁+코드 불일치라 재작성: 설계 의도(왜 분리), Possession 생명주기(사망 시 자동 언포제스·재빙의), 시스템 연결점(possessEffect·Speed Attribute·EquipmentComponent 위임), MonsterCharacter까지 반영. _(2026-07-07)_
- [x] **(infra/docs)** "하던거 하자" 재개가 매번 어긋나는 문제 수정 — 재개 지점이 모호(IN-PROGRESS 3개 + TODO-BOARD 진행중 공백)·낡음(item 다음작업 stale)·선행조건 미기록(Unity MCP)이 원인. **단일 재개 파일 `dev-docs/agent/NOW.md` 도입**: 이어하기 시 이것 하나만 읽고 즉시 실행(전체 확인 안 함). SessionStart 훅을 NOW.md 기반으로 교체, `CLAUDE.md`·`HARNESS.md §3.1/3.3` 재배선(시작=NOW만 읽기 / 종료=NOW 최우선 갱신 + 선행조건 명시). _(2026-07-06)_
- [x] **(docs)** GAS를 feature로 등록 — `ability-system` 그룹 신설(attribute ✅ / gameplay-effect 🔧 / gameplay-ability 📋), PR#2·#3 기준 progress·decisions·worklog 작성, feature-list 동기화. _(2026-07-05)_
- [x] **(docs)** architecture `ability-system.md` → `ability-system/` 폴더 4분할(overview·attribute·gameplay-effect·gameplay-ability), `overview.md` 링크 갱신. _(2026-07-05)_
- [x] **(docs)** GAS 문서화 — `architecture/ability-system.md`를 구현됨/미구현(계획)으로 개편, Modifier 연산·CurrentValue 공식·Execution 프레임워크·SO 초기화 반영, 미구현 로드맵(GA·스택·Cue·Execution concrete·TakeDamage 배선) 통합. ⚠️ GE 타입 enum 불일치 1건은 후속 TODO로 분리. _(2026-07-05)_
- [x] **(docs)** wiki 편집 하네스 규약 작성 — `wiki/HARNESS.md` 신설, `CLAUDE.md` 링크, feature `✅ DONE` 시 wiki 갱신 리마인드(`dev-docs/agent/HARNESS.md` §6) 추가. _(2026-07-05)_
