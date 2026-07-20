# TODO-BOARD — 작업 현황 보드

> feature 단위 추적은 `feature-list.md`가 담당한다. **이 보드는 그보다 잘게 쪼개진 잡다한 작업·개인 TODO·이슈**를 모은다 — 문서 정비, 인프라(GitHub 설정 등), 실험, 리팩터, 아이디어 등 feature로 묶기 애매한 것들.
> 관리 규약은 `HARNESS.md` §7. 항목은 상태가 바뀌면 아래 칸 사이를 이동한다.

- 최종 갱신: 2026-07-19 (KST — HARNESS §3.4)

---



## 📋 TODO (당장 목표)
- [ ] Item Instance - Item Behaviour, Socket에 Item prefab 장착까지

## 📋 TODO (순서 무관 우선순위 낮음)

- [ ] Inventory/Equipment System 구현
- [ ] **(code)** `GameplayEffectType` Instant/Duration 실전 상태 확정 — enum 주석은 '미구현'이나 ASC에 실행 경로 존재. **유저가 직접 확인 예정.** 확정 후 `architecture/ability-system/gameplay-effect.md` §확인 필요 + `overview.md` 요약 표 반영. (feature `gameplay-effect` 블로커에도 추적) _(2026-07-05 등록 — GAS 문서화 중 발견)_
- [ ] **(wiki)** 설계 문서 상호 연결 — 각 섹션 요약에서 `dev-docs/project/architecture/*` 심화 문서로 링크. 로컬/Pages에선 `.md`가 raw로 뜨므로 **GitHub blob URL** 사용. 저장소 public 전제. `SECTION_META.sources`도 같은 방식으로 클릭 가능하게. _(2026-07-05 등록)_
- [ ] GameplayAbility (GA) 구현
- [ ] GE Execution 구현
- [ ] **(code)** GE **Magnitude — AttributeBased 런타임 평가** (feature `gameplay-effect` 세부 TODO 5b) — 데이터 모델·에디터(5a)는 완료됐으나 `GameplayModifier.GetMagnitude`가 여전히 **고정값만 반환**한다. source/target ASC의 어트리뷰트를 읽어 `(값 + preAdd) * coefficient + postAdd`를 실제 계산하고, `GameplayModifierSpec`/`GameplayEffectSpec` 생성자에 target ASC를 전달하는 배선이 남음. 아래 캡처 도입과 함께 갈지는 미정. _(2026-07-19 보드 등록 — 진행 추적은 progress.md 5b)_
- [ ] **(code)** GE **AttributeCapture 계층** 도입 — **방향 확정(유저).** 반대 결정이던 D6은 폐기·결번 처리됨. Modifier(AttributeBased)·Execution 양쪽이 소비하며, Execution엔 UE `RelevantAttributesToCapture`에 해당하는 캡처 선언 멤버가 필요. 계획 초안(파일 9개 단위)은 2026-07-19 세션 대화에 있음. **유저 지시로 일시 중단.** _(2026-07-19 등록)_
- [ ] **(code)** GE **Calculation Modifiers** 구현 (UE: `FGameplayEffectExecutionScopedModifierInfo`) — Execution 실행 스코프 동안만 캡처 값을 보정하는 GE 에셋별 데이터(실제 어트리뷰트 불변). 같은 계산 클래스를 쓰면서 GE마다 "방어력 50% 무시"·"공격력 1.5배"를 코드 수정 없이 지정하는 용도. **선행 조건: AttributeCapture 계층**(→gameplay-effect decisions D6에서 미도입) — 현재는 Execution이 ASC를 직접 읽어 개입 지점이 없다. 코드 TODO는 `GameplayEffectExecutionParameters.cs`에 기록. _(2026-07-19 등록 — UE Execution 분석 중 도출)_
- [ ] GE 스택 구현
- [ ] Gameplay Cue 구현
- [ ] **(docs)** architecture 하류 문서 **반영 기준 마커 정비** — `camera.md`·`item-equipment.md`·`controller-character.md`·`architecture/overview.md`는 마커 자체가 없어 HARNESS §3.4의 낡음 판별(하류 날짜 < 상류 날짜)이 불가능하다. 실제 반영 날짜를 알 수 없어 **지어내지 않고 그대로 뒀다**(§3.4 "모르는 날짜는 지어내지 않는다"). 채우려면 **유저가 날짜를 알려주거나, 다음에 그 문서를 실제로 갱신할 때 그날 날짜로 stamp**한다. 더불어 `ability-system/overview.md`는 상류를 feature-id가 아닌 **그룹명**(`ability-system`)으로 적어, 어느 progress와 비교할지 모호하다(그룹 대표 규칙을 정하든 하위 3개 중 하나로 바꾸든 결정 필요). _(2026-07-19 등록 — 하네스 점검 중 발견)_


## 🔧 진행 중

- [ ] **(design)** ⭐ **기획안 확정 (선행 — 아이템 설계보다 먼저)** — 아이템 구조를 못 정하는 이유가 결국 **기획이 안 정해져서**임이 드러났다(2026-07-13). 전투가 콤보·탄약·재장전·패링을 실제로 요구하는지에 따라 구조 결론이 뒤집힌다(design-draft의 미결 D-1). `dev-docs/project/design.md`를 전투 중심으로 확실히 세운 뒤 아이템 설계로 돌아온다. _(2026-07-13 등록)_

- [ ] **(design)** 아이템 시스템 신규 설계 — **기획안 확정 대기로 보류.** 유저와 설계 논의 중, 결론 전 구현 금지. 아래 유저 의도가 목적:
  - 어떤 캐릭터든 아이템 장착에 따라 **알아서** 작동 (캐릭터 특화 없이)
  - 아이템에 종속되는 로직이 아이템 외부에 작성되지 않을 것
  - Gun·Melee 등 종류별 로직이 들어갈 자리가 있되, 아이템별 클래스 분리·상속을 강제하지 않을 것 (전략 패턴 방향은 유저 언급)
  - 아이템을 구현할 때 알아야 하는 것(컨텍스트, 외부 접근 방법, 시스템 전체 이해)이 복잡하지 않을 것 — 복잡도 대비 이득 없는 과설계 배제
  - 표현부(ItemBehaviour·GameplayCue 활용 포함)의 역할과 Combat 로직과의 연동 방식은 재검토 대상
  - **진행(2026-07-12):** 규약 백지화 완료(이전 INV·D·로드맵 → `feature/item-system/archive/`). 새 판단 기준 4축 채택(HARNESS §2, Nystrom Type Object vs Subclass Sandbox).
  - **진행(2026-07-13):** 설계도 백지에서 재작성 → [`feature/item-system/design-draft.md`](feature/item-system/design-draft.md)(확정 아님). `ItemModule`·`ItemInstance`·"모듈 조합"은 **권위 없음**으로 강등하고, 그렇게 서술하던 project 문서(architecture/item-equipment.md·overview.md, design.md)를 **전부 미정 처리**. 후보 3안(A: 무기=Runtime 서브클래스 / B: 현행 시스템이 알고리즘 보유 / C: 어빌리티) — GAS엔 GameplayAbility가 없음(코드 확인). **결론은 기획안(위 ⭐)이 서야 남** — 콤보/탄약/패링 채택 여부가 A vs B를 가름. _(2026-07-10 등록, 07-13 갱신)_

## ✅ Done

- [x] **(docs)** 하네스 정합성 점검·수정 (유저 요청으로 "이상하거나 잘못된 것" 전수 대조) — 발견 11건 중 유저가 지목한 8건 수정: ①§3.1 vs §7.2 **TODO-BOARD 충돌** 해소(미리 훑지 않는 목록에서 제외, §7.2 예외 명시) ②§4.2 전이도에 `DONE ⇄ IN-PROGRESS` 반영(아래 문장과 모순이었음) ③**§2.1 신설 — NOW.md 성격 정의**(위치 명시 + "지금 뭐 하던 중이었더라"용 임시 메모: 매 세션 덮어씀·이력 없음·진실의 출처 아님·짧게, 단 선행조건/미승인 산출물은 필수) ④§9에 "NOW.md 갱신 없이 종료" 금지 추가 ⑤§3.3 종료 절차에 **TODO-BOARD 갱신** 단계 신설(5번) ⑥`feature-progress-TEMPLATE.md` **삭제** — §5 템플릿과 두 벌이라 이미 드리프트, 파일 쪽 안내 문구를 §5로 흡수하고 §6은 "§5 복사"로 ⑦§7.1에 "3칸은 에이전트 기본 틀 — **보드는 유저도 직접 씀**, 형식 벗어난 항목 존중·임의 재편 금지" ⑧§3.4 시각 확인 명령을 **Claude Code 실행 환경 기준**으로(Bash 툴 `TZ=...`, PowerShell 대체식 병기). 이어서 회고에서 도출된 2건 추가 — ⑨**모르는 날짜는 지어내지 않고 그대로 둔다**(하류 `반영 기준`에 상류 progress 값을 가져다 쓴 것도 추정이었음. §3.4 + §9) ⑩**문서를 지우거나 옮기면 그것을 가리키는 곳을 같은 턴에 고친다**(§3.2 + §9 — TEMPLATE 삭제는 지켰고, 07-18 item 아카이브 이관은 안 지켜 feature-list 링크 4개가 죽어 있음). **타임스탬프 형식은 시:분을 빼고 `YYYY-MM-DD` 날짜만으로 변경**(유저 결정) — HARNESS §3.4·§5, wiki/HARNESS §5, 실제 문서 전부 일괄 적용. 미처리분은 위 TODO(반영 기준 마커) 참조. _(2026-07-19)_
- [x] **(docs)** 하네스에 **대화 규율(§3.5)** 신설 — Grice 협력 원리 4격률(양·질·관계·방식), Gordon 커뮤니케이션 로드블록 중 해당분(요청 없는 조언·훈계·과도한 되묻기·화제 돌리기·값싼 안심·판단), 질문에 질문으로 답하지 않기, shift↔support response(주제 뺏기 금지), 전송 전 자기 점검 4문항. §3.2엔 "대화 스코프 이탈 금지"(요청 없는 추천·주제 밖 시스템·끝난 결정 재론·요청 축 넘는 비교·과한 길이) + 2026-07-19 사례 4건. §9 금지 사항 2줄. _(2026-07-19)_
- [x] **(docs)** 하네스에 **세션 종료 회고(§3.3.1·§3.3.2)** 신설 — 종료 시 ①문제가 있었던 지점을 스스로 보고(규약 위반·범위 이탈·승인 없이 만든 산출물 목록화) ②하네스 자체 점검(트리거 미발동·규칙 충돌·실제와 안 맞는 절차·반복된 실수는 빠진 규약·형식 규칙 준수 대조). 첫 실행에서 개정 2건 도출 → 아래 항목. _(2026-07-19)_
- [x] **(docs)** 하네스 개정 2건 — ①**`D#`은 유저가 확정한 결정에만 부여**(에이전트 판단을 D로 박지 않음. 제안 단계는 worklog·progress에 "제안"으로) ②**타임스탬프는 로컬 셸(`TZ=Asia/Seoul date`)로 확인**해 사용(세션 컨텍스트 날짜 불신). 둘 다 이번 세션에서 실제로 터진 문제(D6 무단 작성·날짜 오기 7파일)에서 도출. _(2026-07-19)_
- [x] **(docs)** `CLAUDE.md` — HARNESS 읽기 트리거를 "구현 작업 착수 전" → **"게임 feature에 해당하는 내용을 다루기 시작하면(구현이 아니어도 — 질문 답변·설명·설계 논의·문서 작성 포함)"** 으로 확장. 이유: 대화가 *설명 → 논의 → 편집*으로 미끄러지면 "착수" 시점이 없어 규약이 끝까지 안 켜진다(2026-07-19 실제 발생). _(2026-07-19)_

- [x] **(editor)** `SubclassSelector` 중복 제외 **다른 스코프** 지원 — 코드 대조로 확인 완료: 범용 `SubclassSelectorDrawer`는 `excluded`를 받아 그리기만(`DrawSelector`), 수집은 호출측 전용 드로어가 트리거(`AttributeSetDefinitionDrawer`→`CollectSiblingValues`, 수집 코어 `CollectArrayValues` 공유). 다른 스코프는 전용 드로어가 자체 수집 로직으로 `excluded`를 만들어 넘기면 됨 — 범용 드로어 변경 불필요, 추가 수집기는 필요 전까지 미구현. 확장 가이드는 코드 주석·`CODE_CONVENTION.md`에 이미 기록돼 있어 보드에서 내림. _(2026-07-09 확인)_
- [x] **(docs)** SO→Asset 네이밍 문서 반영 — architecture 문서의 클래스명 언급을 문맥별 갱신: `ability-system/gameplay-effect.md`(SO 정의·MCP type_name·미구현 표 파일 참조), `ability-system/overview.md`(구조도·현황/UE 대비 표 + "UE 용어 따르되 SO는 `Asset` 접미어"로 원칙 서술 조정), `item-equipment.md`(계층도·규칙·코드 위치 `ItemDataAsset`·`EquipItemAsset.cs`·`ConsumeItemAsset.cs`), `controller-character.md`(`possessEffect` 타입). UE 개념(`FGameplayEffect` 등)·개념적 GE 언급·ASC 메서드명은 유지, `attribute.md`는 이미 갱신돼 있었음. _(2026-07-09)_
- [x] **(refactor)** Attribute·SO 네이밍 정리 — Attribute 초기화 3층 헷갈림 해소(`AttributeInitData`→`AttributeDefinitionAsset` / `AttributeSetInitData`→`AttributeSetDefinition` / `AttributeFieldInitData`→`AttributeFieldDefinition`, 런타임 `AttributeSet`/`AttributeData`와 구분) + 모든 SO에 `Asset` 접미어 통일(`GameplayEffect(Execution)`·`ItemData`·`EquipItem`·`ConsumeItem`→`...Asset`). 대응 드로어·참조 갱신, `.meta`/GUID 보존. 문서 문맥별 갱신은 위 (docs) TODO로 분리. _(2026-07-08)_
- [x] **(docs)** 코드 설계 규칙 문서화 — `CODE_CONVENTION.md`에 ① **설계 원칙**(필요할 때만 범용화·과설계 지양, 좁은 스코프/설계 갈림은 착수 전 확인, 범용 코드는 정책 모름) ② **에디터 확장(드로어) 가이드**(범용/전용 분리, `SubclassSelector` 예시, 런타임/에디터 경계) 섹션 신설. _(2026-07-08)_
- [x] **(editor)** `SubclassSelector` 필드를 오브젝트 참조 필드처럼 개선 — 선택된 타입의 소스 스크립트를 `ObjectField` 스타일로 표시(단일클릭 ping / 더블클릭 open), 타입 선택은 우측 버튼 → **검색 가능한 네이티브 팝업**(`AdvancedDropdown`, Add Component 창과 동일). `EditorTypeUtility.FindScript`(타입→MonoScript, 캐시) + `SubclassAdvancedDropdown` 추가. 범용이라 ItemData `runtimeClass` 등 모든 `[SubclassSelector]`에 적용. (New Class 생성은 안 함 — 유저 결정) 이후 `uniqueInList` 옵션 추가(리스트 내 이미 쓰인 타입 제외 — 형제 배열 훑는 로직 범용화)하고, `AttributeSetInitData.attributeSetTypeName`을 커스텀 `EditorGUI.Popup` → `[SubclassSelector(typeof(AttributeSet), uniqueInList:true)]`로 전환(드로어 죽은코드 `DrawAttributeSetPopup`·`GetUsedTypeNamesByOthers` 제거). _(2026-07-08)_
- [x] **(refactor)** 에디터 스크립트 배치를 `Editor/<feature>` 미러 구조 → **feature-local `Core/<feature>/.../Editor`** 로 통일. 입도는 **서브시스템 레벨**(기존 `ItemSystem/Inventory/Editor` 선례와 일치): 드로어를 대상 타입 옆에 둠(Attribute·Effect·ItemSystem 각각 Editor). feature 무관 공통 에디터 유틸(ConditionalShow·SubclassSelectorDrawer·EditorDrawUtility·EditorTypeUtility·TypeChoiceList)은 `Core/Common/Editor`. `.meta`(폴더 meta 포함) 그대로 이동해 GUID 보존, asmdef 없어 참조 영향 없음. _(2026-07-08)_
- [x] **(docs)** 기존 구현분 정리 — **Player Character 계층 설계**(Controller · Character 분리, Possession 패턴) 문서화. 기존 `controller-character.md`가 얇은 스텁+코드 불일치라 재작성: 설계 의도(왜 분리), Possession 생명주기(사망 시 자동 언포제스·재빙의), 시스템 연결점(possessEffect·Speed Attribute·EquipmentComponent 위임), MonsterCharacter까지 반영. _(2026-07-07)_
- [x] **(infra/docs)** "하던거 하자" 재개가 매번 어긋나는 문제 수정 — 재개 지점이 모호(IN-PROGRESS 3개 + TODO-BOARD 진행중 공백)·낡음(item 다음작업 stale)·선행조건 미기록(Unity MCP)이 원인. **단일 재개 파일 `dev-docs/agent/NOW.md` 도입**: 이어하기 시 이것 하나만 읽고 즉시 실행(전체 확인 안 함). SessionStart 훅을 NOW.md 기반으로 교체, `CLAUDE.md`·`HARNESS.md §3.1/3.3` 재배선(시작=NOW만 읽기 / 종료=NOW 최우선 갱신 + 선행조건 명시). _(2026-07-06)_
- [x] **(docs)** GAS를 feature로 등록 — `ability-system` 그룹 신설(attribute ✅ / gameplay-effect 🔧 / gameplay-ability 📋), PR#2·#3 기준 progress·decisions·worklog 작성, feature-list 동기화. _(2026-07-05)_
- [x] **(docs)** architecture `ability-system.md` → `ability-system/` 폴더 4분할(overview·attribute·gameplay-effect·gameplay-ability), `overview.md` 링크 갱신. _(2026-07-05)_
- [x] **(docs)** GAS 문서화 — `architecture/ability-system.md`를 구현됨/미구현(계획)으로 개편, Modifier 연산·CurrentValue 공식·Execution 프레임워크·SO 초기화 반영, 미구현 로드맵(GA·스택·Cue·Execution concrete·TakeDamage 배선) 통합. ⚠️ GE 타입 enum 불일치 1건은 후속 TODO로 분리. _(2026-07-05)_
- [x] **(docs)** wiki 편집 하네스 규약 작성 — `wiki/HARNESS.md` 신설, `CLAUDE.md` 링크, feature `✅ DONE` 시 wiki 갱신 리마인드(`dev-docs/agent/HARNESS.md` §6) 추가. _(2026-07-05)_
