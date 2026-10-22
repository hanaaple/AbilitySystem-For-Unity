# gameplay-tag — 게임플레이 태그 (Tag 코어 + GE 태그 연동)

> 작성 규약 → [progress-harness.md](../../progress-harness.md)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-10-23 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md) · [aggregator](../aggregator/progress.md).
> 확인 항목·절차: [tests.md](tests.md). 2026-10-18 유저 목표(Tag + GE 태그 연동 일반 기능)는 2026-10-20에 끝났다. 남은 일은 [TODO-BOARD](../../../TODO-BOARD.md) 'GE·Tag 남은 것 > Tag'. 아래 `수용 기준`·`범위`는 에이전트 제안(유저 미확정).

## 목표
캐릭터/이펙트에 상태를 나타내는 GameplayTag를 부여하고, GameplayEffect가 그 태그와 연동되게 한다 — GE가 대상에 태그를 부여/회수하고, 대상이 가진 태그에 따라 GE 적용·유지가 조건부로 걸린다. (유저 확정 목표: "Tag + GE 태그 연동")

## 수용 기준 (Definition of Done)
> 제안 — 유저 확정 전. 동작은 아래 근거로 확인됨.
- [ ] 계층 태그를 정의하고 부모 매칭이 동작한다 (`Status.Debuff.Stun`이 `Status.Debuff` 질의에 매칭) — tests.md §5-2
- [ ] Duration/Infinite GE 적용 시 대상 ASC 소유 태그에 GE의 부여 태그가 더해지고, 해제 시 빠진다 (ASC 인스펙터 Owned Tags에서 확인) — GE Component 셀프체크 F
- [ ] 같은 태그를 두 GE가 부여하면 개수로 누적되고, 하나만 제거해도 태그가 남는다 — GE Component 셀프체크 F-5
- [ ] 대상이 특정 태그를 가지면(또는 없으면) GE 적용이 차단/허용된다 (Application 요구) — GE Component 셀프체크 G-1·G-2

## 범위
### 포함 (제안)
- **Tag 코어:** 계층 태그 식별자 + 노드 트리·매니저 + 컨테이너 + 질의(Query) + native 태그 + 레지스트리 에셋, 에디터 드로어·피커·관리 창·Query 편집기
- **ASC 소유 태그:** 개수 컨테이너(Loose 태그·조회·이벤트), GE 부여 태그 반영
- **GE 태그:** GE Component(Asset Tags·Granted Tags·Target Tag Requirements·Remove Other), Modifier Source/Target 태그 조건, 태그로 활성 GE 찾기·제거
### 제외 (명시적으로 하지 않을 것)
- GameplayCue · GE 스택 수 2+ — 로드맵
- GameplayAbility의 태그 배선 — [gameplay-ability](../gameplay-ability/progress.md) 범위. Tag 코어는 GA도 쓰는 공용 인프라로 둔다(유저 확정 방향).
- 네트워크 복제 (프로젝트 전체 미채택)

## 설계 개요
- **Tag 코어:** `Assets/Scripts/Core/AbilitySystem/Tag/` — 식별자(`GameplayTag` = `InternedName` 하나)·노드 트리(`GameplayTagNode`)·매니저(`GameplayTagsManager`)·컨테이너·Query(토큰 스트림)·native 태그(→D6)·레지스트리 에셋(`Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`, 에디터 로드 때 자동 생성). 에디터는 `Tag/Editor/`.
- **ASC 소유 태그(→D22):** `GameplayTagCountContainer` + ASC Loose 태그·조회·이벤트 API. GE 부여 태그는 internal `UpdateTagMap`으로 ±1.
- **GE Component(→D29·D30):** `GameplayEffectAsset`의 `[SerializeReference]` Component 목록이 GE 캐시(`CachedAssetTags`·`CachedGrantedTags`)를 채우고, 컨테이너가 UE 위치에서 Component 훅을 부른다. Target Tag Requirements는 소유 태그를 구독해 GE를 켜고 끄거나(inhibit) 제거한다.
- **Modifier 태그 판정(→D14·D28):** Instant/Periodic은 실행 때 소유 태그로 건너뛰기, 지속형은 aggregator 평가 문맥으로 — CurrentValue는 빈 문맥(UE와 같음).
- 아키텍처: [architecture/ability-system/gameplay-tag.md](../../../../project/architecture/ability-system/gameplay-tag.md).

### UE 원본 대응
> UE 5.6.0 로컬 소스 `Runtime/GameplayTags/Classes/{GameplayTagContainer.h, GameplayTagsManager.h}` 확인. 상속이 아니라 소유·사용 관계다.

| UE 원본 | 우리 (`Core/AbilitySystem/Tag`) |
|---|---|
| `UGameplayTagsManager` | `GameplayTagsManager` |
| `FGameplayTagNode` | `GameplayTagNode` |
| `FGameplayTag` (FName) | `GameplayTag` (`InternedName` 래핑) |
| `FGameplayTagContainer` | `GameplayTagContainer` |
| `FGameplayTagQuery` (+`Expression`) | `GameplayTagQuery` (+`GameplayTagQueryExpression`, 토큰 스트림) |
| `FGameplayTagCountContainer` | `GameplayTagCountContainer` |

- 핸들 = 이름 하나(비교·해시·직렬화 모두 이름 기준). 계층 매칭은 트리를 타지 않고 펼친 부모 캐시를 읽는다. 컨테이너는 명시 목록만 저장하고 부모는 로드 뒤 재구성. Query는 토큰 스트림 + 태그 사전으로 평가.

## 세부 TODO (구현 체크리스트)
- [x] 1. 태그 표현 타입 — `InternedName`(`Core.Common`) 인터닝 래퍼. D3(대소문자=무시비교+표기보존)는 미구현 → 다음 작업 5.
- [x] 2. GameplayTag 식별자 + 계층 매칭 (tests.md §5-2)
- [x] 3. 소유 태그 개수 컨테이너 + ASC 조회 API (→D22)
- [x] 4. GE 태그 필드 + Spec 전달 — Asset Tags(→D30)·Granted Tags Component, 인스펙터 Component 목록
- [x] 5. GE 적용/해제 시 태그 부여/회수 — 태그만 주는 GE 허용, inhibit 경로 포함. 남은 것: Spec 동적 태그(11)
- [x] 6. GE 적용 요구 — Target Tag Requirements(Application·Ongoing·Removal) 판정·구독. 남은 것: UE `IsDataValid`류 에디터 검증(TODO-BOARD 'GE 데이터 검증')
- [x] 7. 에디터 확인 경로 — 태그 드로어·피커·관리 창·Query 편집기(tests.md §2~§4), ASC 인스펙터 Owned Tags
- [x] 8. Modifier Source/Target Tags — 필드·`GameplayTagRequirements`·Query 직렬화·인스펙터·판정(→D14)

> 9~12는 "태그 달 만한 곳" 후보(2026-10-16). 9·11·12는 TODO-BOARD 'GE 태그 부가 기능', 10은 SetByCaller.
- [ ] 9. `AttributeBasedMagnitude` 태그 필터 — UE `FAttributeBasedFloat::SourceTagFilter/TargetTagFilter`
- [ ] 10. SetByCaller 크기 — UE `FSetByCallerFloat::DataTag` + Spec `SetByCallerTagMagnitudes`
- [ ] 11. Spec 동적 태그 — UE `DynamicGrantedTags/DynamicAssetTags`
- [ ] 12. Execution 태그 — UE `PassedInTags`
- [x] 13. 활성 GE 태그 질의·제거 — `GameplayEffectQuery`·ASC 질의 API·Remove Other(→gameplay-effect D24·D25)

## 결정 기록
상세는 → [decisions.md](decisions.md). progress엔 요지 인덱스만. 치운 결정(D2·D9~D11·D13·D15~D21·D23~D27)은 decisions 끝에 한 줄씩.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | 인터페이스=계층 문자열 | 2026-09-26 | 개발자 인터페이스는 UE와 같은 계층 문자열(`Status.Debuff.Stun`) |
| D3 | 대소문자=무시비교+표기보존(FName식) | 2026-09-29 | 비교는 대소문자 무시, 표시는 원본 표기 보존 (구현 미룸) |
| D4 | 질의 식=class+자기 반환 체이닝 | 2026-10-11 | `GameplayTagQueryExpression`은 class(잠정), 수정 메서드는 자기 자신 반환 |
| D5 | 생성 차단=Request 입구+internal 생성 함수 | 2026-10-12 | 생성자 private, 외부는 `RequestGameplayTag`·native로만. 막는 이유는 근거 미기록 |
| D6 | native=코드 정의+저장 안 함 | 2026-10-12 | `[NativeTag]` static 필드, 트리 구성마다 탐색해 레지스트리보다 먼저, 에셋에 저장 안 함 |
| D7 | Container=class | 2026-10-12 | struct+List → class. 사본 간 List 공유로 노드 캐시가 오염되는 함정 회피 |
| D8 | 기존 API=UE 이름·시그니처 | 2026-10-14 | 구현된 함수의 이름·인자를 UE 5.6에 맞춤, 미지원 인자는 `NotImplementedException` |
| D12 | 태그 관리 포함=레지스트리 편집 | 2026-10-15 | 피커·관리 창에서 추가·서브태그·복제·이름 변경·삭제. 리다이렉터·참조 검사 없음 |
| D14 | modifier 태그 판정=UE 경로 | 2026-10-16 | Instant/Periodic은 실행 시 건너뛰기, 지속형은 aggregator 평가 문맥으로 |
| D22 | 소유 태그=UE `FGameplayTagCountContainer` 구조+Loose·이벤트 | 2026-10-17 | 부모 포함 개수·명시 개수·명시 태그, 이벤트 `NewOrRemoved`·`AnyCountChange` |
| D28 | CurrentValue=빈 문맥, on/off=Ongoing Tag Requirements | 2026-10-18 | CurrentValue는 `Evaluate(default)`, GE 단위 켜고 끄기는 Component 담당 (D21·D23 되돌림) |
| D29 | GE Component 도입(UE 5.3+) | 2026-10-18 | GE 에셋에 `[SerializeReference]` Component 목록 + 베이스 가상 함수 5개 |
| D30 | Asset Tags=UE `FInheritedTagContainer` 구조, 부모 없음 | 2026-10-19 | Added·Removed·CombinedTags를 UE대로, 부모는 늘 null |

### 결정 대기 (유저 확정 필요)
1. **경량 경계** — 레지스트리 형태·등록 시점(지연 인터닝 vs 정의 에셋 등록). 내부 표현은 `InternedName`(정수 핸들 전환은 `InternedName.Intern` 한 곳으로 국소화해 유보). D3가 canonical/display 이중화를 전제하므로 함께 확정.
2. **태그 조회 입력 모양** — `RequestGameplayTag`가 문자열을 어떻게 받을지: ① `implicit string→InternedName` ② `RequestGameplayTag(string)` 오버로드 ③ `new InternedName(...)` 명시. 비용은 같고 차이는 비용의 가시성. 제안 기울기: 명시(① 지양). ②는 D5 구현 중 생겼지만 확정 아님.
- (해소) GE 연동 범위 — 2026-10-18 유저 목표(Asset·Granted·적용/제거/유지 요구·Query 포함·태그로 GE 제거)로 답이 됐다.

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
> 착수는 유저가 말할 때만. 번호는 항목 ID라 끝난 항목을 지워도 다시 매기지 않는다.

- **1. Tag 코어 후속(미룸)** (→D5, →D6) — IL2CPP 코드 제거 시 마커 필드 유실 대응(`[Preserve]` 등), 빌드 탐색 비용 측정, 노드 출처·명시/암시 기록, D5의 막는 이유 유저 확인. TODO-BOARD 'Tag'.
- **4. 결정 대기 2(조회 입력 모양)** 확정 후 `RequestGameplayTag` 입력 경로 정리.
- **5. D3 구현** — `InternedName`을 canonical/display 이중화로. 결정 대기 1과 묶임. TODO-BOARD 'Tag'.
- **7. Query 내부 표현** — 지금 UE대로 토큰 스트림 + 형식 버전 저장. 식 트리로 바꿀지 유저 미확정(바꾸면 저장된 질의를 옮겨야 함).
