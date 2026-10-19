# gameplay-tag — 게임플레이 태그 (Tag 코어 + GE 태그 연동)

- 상태: 🚧 IN PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-10-19 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md) · [aggregator](../aggregator/progress.md).
> 유저가 에디터에서 확인할 항목·절차: [tests.md](tests.md).
> 현재 목표·순서는 [TODO-BOARD](../../../TODO-BOARD.md) '현재 목표'(2026-10-18 유저)가 기준이다. 아래 `수용 기준`·`범위`는 에이전트 제안(유저 미확정).

## 목표
캐릭터/이펙트에 상태를 나타내는 GameplayTag를 부여하고, GameplayEffect가 그 태그와 연동되게 한다 — GE가 대상에 태그를 부여/회수하고, 대상이 가진 태그에 따라 GE 적용·유지가 조건부로 걸린다. (유저 확정 목표: "Tag + GE 태그 연동")

## 수용 기준 (Definition of Done)
> 제안 — 연동 범위 확정 후 확정한다.
- [ ] 계층 태그를 정의하고 부모 매칭이 동작한다 (`Status.Debuff.Stun`이 `Status.Debuff` 질의에 매칭)
- [ ] Duration/Infinite GE 적용 시 대상 ASC의 owned-tag에 GE의 부여 태그가 더해지고, 해제 시 제거된다 (에디터 인스펙터에서 확인 가능)
- [ ] 같은 태그를 두 GE가 부여하면 참조 카운트로 누적되고, 하나만 제거해도 태그가 남는다 — 2026-10-19 GE Component 셀프체크 F-5로 동작 확인(수용 기준 자체는 아직 제안)
- [ ] 대상이 특정 태그를 가지면(또는 없으면) GE 적용이 차단/허용된다 (Application 요구) — 2026-10-19 GE Component 셀프체크 G-1·G-2로 동작 확인(수용 기준 자체는 아직 제안)

## 범위
### 포함 (제안)
- **GameplayTag 코어:** 계층 태그 식별자 + 소유 태그 컨테이너(참조 카운트) + 매칭 질의(정확/부모 포함)
- **GE 태그:** GE Component(→D29 GE Component 도입, 첫 단계는 빈 껍데기)로 Asset·Granted·Target Tag Requirements
- **ASC owned-tag 추적:** GE 적용/해제 시 태그 부여/회수, 요구 검사 훅
### 제외 (명시적으로 하지 않을 것)
- GameplayCue(태그 기반 연출) · GE Stack — 별도 feature(로드맵)
- GameplayAbility의 태그 배선(Activation/Block/Cancel 태그) — [gameplay-ability](../gameplay-ability/progress.md) 범위. 이번엔 **GE 연동만**. 단, Tag 코어(식별자·컨테이너·질의)는 **GA도 재사용할 공용 인프라로 설계**한다(유저 확정 방향) — 소비처 중립.
- 네트워크 복제 (프로젝트 전체 미채택)

## 설계 개요
- **Tag 코어:** `Assets/Scripts/Core/AbilitySystem/Tag/` — 식별자·노드 트리·매니저·컨테이너·Query(토큰 스트림)·native 태그·레지스트리 에셋, 에디터 드로어·피커·관리 창·Query 편집기(`Tag/Editor/`).
- **ASC 소유 태그:** `GameplayTagCountContainer`(→D22 소유 태그=UE `FGameplayTagCountContainer` 구조+Loose·이벤트) + ASC Loose 태그·조회·이벤트 API, GE 부여 태그는 internal `UpdateTagMap`으로 더하고 뺀다.
- **GE:** `GameplayEffectAsset`의 `[SerializeReference]` Component 목록과 `GetAssetTags()`·`GetGrantedTags()` 캐시(지금은 채우는 Component가 없어 늘 빈 값). 적용·해제 때 `ActiveGameplayEffectsContainer.Add/RemoveActiveGameplayEffectGrantedTagsAndModifiers`가 부여 태그를 ±1.
- **Modifier 태그 판정:** Instant/Periodic은 실행 때 소유 태그로 건너뛰기, 지속형 CurrentValue는 UE처럼 빈 문맥(→D14 modifier 태그 판정=UE 경로, →D28 CurrentValue=빈 문맥).
- 아키텍처: [architecture/ability-system/overview.md](../../../../project/architecture/ability-system/overview.md).

### UE GAS 원본 클래스 (참조 · 우리 설계의 미러링 대상)
> 원문 확인(2026-09-30, **UE 5.6.0** 로컬 소스 `Runtime/GameplayTags/Classes/{GameplayTagContainer.h, GameplayTagsManager.h}`). **상속이 아니라 소유·사용 관계**다.

```
UGameplayTagsManager  : UObject   싱글턴. 태그 트리 소유·등록·조회 (Get())
├─ GameplayRootTag    : TSharedPtr<FGameplayTagNode>                 빈 센티널 루트(하위 태그를 한 트리로)
│   └─ FGameplayTagNode (USTRUCT)   계층 트리 노드 · 구성 시 1회 빌드
│       ├─ Tag                    : FName                  단순(짧은) 이름
│       ├─ CompleteTagWithParents : FGameplayTagContainer  완전태그[0]+부모들 (매칭 가속 캐시)
│       ├─ ChildTags              : TArray<TSharedPtr<Node>>
│       ├─ ParentNode             : TSharedPtr<Node>
│       └─ NetIndex               : FGameplayTagNetIndex    네트워크 복제 인덱스(우리 미채택)
└─ GameplayTagNodeMap : TMap<FGameplayTag, TSharedPtr<FGameplayTagNode>>   태그→노드 역인덱스

FGameplayTag (USTRUCT)   단일 태그 식별자(핸들)
└─ TagName : FName        유일 멤버 — ==·GetTypeHash·직렬화 모두 TagName 기준

FGameplayTagContainer (USTRUCT)   태그 집합·매칭 단위
├─ GameplayTags : TArray<FGameplayTag>   명시 목록 (SaveGame 직렬화됨)
└─ ParentTags   : TArray<FGameplayTag>   펼친 부모들 (Transient · 검색 가속, 직렬화 안 됨)

FGameplayTagQuery (USTRUCT)   컨테이너 대상 논리 질의(컴파일된 형태)
├─ TagDictionary      : TArray<FGameplayTag>   질의가 참조하는 태그들 (토큰이 인덱스로 참조)
├─ QueryTokenStream   : TArray<uint8>          계층 질의의 바이트코드 스트림
└─ TokenStreamVersion : int32
     (빌더: FGameplayTagQueryExpression · 평가: FQueryEvaluator)
```

확인한 핵심(원문 근거):
- **핸들 = FName 하나.** `FGameplayTag`는 `TagName`뿐이고 비교·해시·직렬화가 전부 FName 기준 → 값(이름)으로 안정적, 비교는 FName 내부 인덱스. (우리 `InternedName`이 이 역할)
- **부모 체인 캐시.** 계층 매칭은 노드 트리를 매번 타지 않고 `CompleteTagWithParents`/`ParentTags`(펼친 부모)를 읽는다. `ParentTags`는 `Transient`라 저장 안 하고 재구성.
- **직렬화.** 컨테이너는 `GameplayTags`(명시 목록)만 SaveGame 저장, `ParentTags`는 로드 후 재구성. `FGameplayTag`는 `TagName`(FName)로.
- **질의.** `FGameplayTagQuery`는 실행 시 트리가 아니라 **바이트코드 스트림 + TagDictionary(인덱스 참조)**로 평가.

우리 대응(미러링):

| UE 원본 | 우리 (`Core/AbilitySystem/Tag`) |
|---|---|
| `UGameplayTagsManager` | `GameplayTagsManager` |
| `FGameplayTagNode` | `GameplayTagNode` |
| `FGameplayTag` (FName) | `GameplayTag` (`InternedName` 래핑) |
| `FGameplayTagContainer` | `GameplayTagContainer` |
| `FGameplayTagQuery` (+`Expression`) | `GameplayTagQuery` (+`GameplayTagQueryExpression`, 토큰 스트림) |

> 주변 클래스(이번 정리 범위 밖·미검증): `UGameplayTagsSettings`(config), `FGameplayTagTableRow`(정의 DataTable), `IGameplayTagAssetInterface`, `FNativeGameplayTag`/`FGameplayTagRedirectors` 등. 네트워크(`NetIndex`·replication)는 프로젝트 전체 미채택.

## 세부 TODO (구현 체크리스트)
- [x] 1. 태그 표현 타입 — `InternedName`(`Core.Common`) 인터닝 래퍼. D3(대소문자=무시비교+표기보존)는 아직 미충족(단일 필드·대소문자 구분) → 다음 작업 5.
- [x] 2. GameplayTag 식별자 + 계층 매칭 — 노드 트리·매칭 API(태그 `MatchesTag*`·컨테이너 `HasTag/HasAny/HasAll(+Exact)`·매니저 Parents/Children/DirectParent/MatchDepth). 2026-10-17 확인(tests.md §5-2).
- [x] 3. owned-tag 컨테이너(참조 카운트) + ASC 조회 API — `GameplayTagCountContainer` + ASC Loose 태그·조회·이벤트(→D22). 셀프체크 D.
- [ ] 4. GE 태그 필드 + Spec 전달 — UE 5.6 캐시 구조(`_cachedAssetTags`·`_cachedGrantedTags`, UE `GameplayEffect.h:2412,2415`) + `GetAssetTags()`·`GetGrantedTags()` + `OnGameplayEffectChanged()`(`OnEnable`·에디터 `OnValidate`에서 호출, Component에 넘김 →D29). 2026-10-19 Asset Tags 완료(→D30 Asset Tags=UE `FInheritedTagContainer` 구조, 부모 없음): `InheritedTagContainer`(`Effect/`), `AssetTagsGameplayEffectComponent`(새 폴더 `GameplayEffectComponents/` — 유저 지시, `TargetTagRequirements…`도 옮김), Component `GetOwner()`, GE `CachedAssetTags`(internal), Spec 생성 때 Source Spec 태그에 Asset Tags(UE `GameplayEffect.cpp:1627`), `Spec.GetAllAssetTags`. 셀프체크 E(tests.md §6). Granted Tags(UE `UTargetTagsGameplayEffectComponent` → `TargetTagsGameplayEffectComponent`)·인스펙터 Component 목록도 2026-10-19 완료(worklog).
- [ ] 5. GE 적용/해제 시 태그 부여/회수 배선 — `ActiveGameplayEffectsContainer.Add/RemoveActiveGameplayEffectGrantedTagsAndModifiers`(UE `GameplayEffect.cpp:4312`, `:4652`): period 0이면 modifier 등록·해제, period와 상관없이 `ASC.UpdateTagMap(GetGrantedTags(), ±1)`. 적용 시 주기 실행은 부여 뒤(UE `:4247-4249`). 태그만 주는 GE 허용(셀프체크 D-16). 2026-10-19 억제(inhibit) 경로가 생겨 Add/Remove는 `SetActiveGameplayEffectInhibit`을 거친다(아래 6). 남은 것: Spec `DynamicGrantedTags`(11).
- [x] 6. GE 적용 요구(Application requirement) 검사 — `TargetTagRequirementsGameplayEffectComponent`에 Application·Ongoing·Removal 필드만(→D29). 2026-10-19 GE Component 호출 골격 완료: GE `CanApply`·`OnAddedToActiveContainer`·`OnExecuted`·`OnApplied`(UE `GameplayEffect.cpp:881-935`)를 컨테이너가 UE 위치에서 부르고, `ActiveGameplayEffect.IsInhibited` + `SetActiveGameplayEffectInhibit`(UE `AbilitySystemComponent.cpp:288`)로 GE를 끄고 켠다(꺼지면 modifier·부여 태그를 거두고 주기 실행·magnitude 반영을 건너뜀). 셀프체크 `Run GE Component Self-Check` 27/0(tests.md §6). 2026-10-19 판정 완료: Application·Removal 적용 거부, Ongoing·Removal 조건 태그 구독 → 켜고 끄기·제거, GE 제거 때 구독 해제(UE `TargetTagRequirementsGameplayEffectComponent.cpp` 그대로, 네트워크 권한 분기는 없음). 새 `ActiveGameplayEffectEvents`(`OnEffectRemoved`)·컨테이너 `Owner`·ASC `GetActiveGameplayEffect`·`GetActiveEffectEventSet`. 셀프체크 G. 남은 것: UE `IsDataValid`(Instant인데 Ongoing 조건) 에디터 오류 — 검증 체계 없음, 유저 결정 대기.
- [ ] 7. 에디터 확인 경로 — 태그 드로어·피커·관리 창·Query 편집기 구현(2026-10-15~16), 확인은 tests.md §2~§4(남은 것 §4-1·4-2·4-6). 남은 것: ASC 인스펙터에 owned-tag 표시.
- [x] 8. Modifier Source/Target Tags(UE `FGameplayModifierInfo::SourceTags/TargetTags`) — 필드·`GameplayTagRequirements`·Query 직렬화·인스펙터·판정(→D14). 셀프체크 A~E, tests.md §1·§3.

> 아래 9~13은 2026-10-16 "지금 코드에서 태그 달 만한 곳" 조사로 나온 **후보**다(유저: "필요한거 TODO로 해둬"). 9·11·12는 TODO-BOARD '뒤로 미룸'(GE 태그 부가 기능), 10은 SetByCaller(미룸), 13은 현재 목표 6. UE 위치는 5.6 헤더에서 확인했다.
- [ ] 9. `AttributeBasedMagnitude` 태그 필터 — UE `FAttributeBasedFloat::SourceTagFilter/TargetTagFilter`(`GameplayEffect.h:182,186`). 속성을 읽을 때 태그 조건에 맞는 modifier만 집계한다.
- [ ] 10. SetByCaller 크기 — UE `FSetByCallerFloat::DataTag`(`:267`) + `FGameplayEffectSpec::SetByCallerTagMagnitudes`(`:1244`). `MagnitudeCalculationType`에 새 유형, Spec에 "태그 → 값" 저장. 쓸 곳 후보: `MeleeAttackModule`의 실행 중 데미지(에이전트 추론).
- [ ] 11. Spec 동적 태그 — UE `FGameplayEffectSpec::DynamicGrantedTags/DynamicAssetTags`(`:1209,1214`). 에셋을 고치지 않고 이번 적용에만 태그를 덧붙인다. 4 이후.
- [ ] 12. Execution 태그 — UE `FGameplayEffectExecutionDefinition::PassedInTags`(`:525`)·`FGameplayEffectCustomExecutionParameters::PassedInTags`(`GameplayEffectExecutionCalculation.h:192`), scoped modifier `SourceTags/TargetTags`(`GameplayEffect.h:470,474`).
- [ ] 13. 활성 GE 태그 질의·제거 — UE `FGameplayEffectQuery`(`OwningTagQuery`·`EffectTagQuery`·`SourceTagQuery` 등, `:1462-1474`). `ActiveGameplayEffectsContainer`에 태그로 찾기·제거 API(예: "Debuff 전부 해제"). 4·5 이후.

## 결정 기록
상세는 → [decisions.md](decisions.md). progress엔 요지 인덱스만.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | 인터페이스=계층 문자열 | 2026-09-26 | 개발자 인터페이스는 UE와 동일한 계층 문자열(`Status.Debuff.Stun`). |
| D3 | 대소문자=무시비교+표기보존(FName식) | 2026-09-29 | 비교는 대소문자 무시, 표시는 원본 표기 보존(canonical/display 이중화). |
| D4 | 질의 식=class+자기 반환 체이닝 | 2026-10-11 | `GameplayTagQueryExpression`은 class(잠정), 수정 메서드는 자기 자신 반환(체이닝). 조립 시 GC 할당 감수. |
| D5 | 생성 차단=Request 입구+internal 생성 함수 | 2026-10-12 | `GameplayTag` 생성자 private. 외부는 `RequestGameplayTag`·native로만 획득, 매니저는 `internal static Create` 하나로 생성(asmdef 분리 전까진 실효 없음 감수). 막는 이유는 근거 미기록. |
| D6 | native=코드 정의+저장 안 함 | 2026-10-12 | `[NativeTag]` static 필드로 정의, 트리 구성마다 탐색해 레지스트리보다 먼저 추가, 에셋에 저장 안 함. 매니저는 에디터에서도 지연 생성. |
| D7 | Container=class | 2026-10-12 | struct+public List 필드 → class. 사본 간 List 공유로 노드 캐시가 오염되는 함정 회피. 컨테이너당 GC 할당 감수. |
| D8 | 기존 API=UE 이름·시그니처 | 2026-10-14 | 구현된 함수의 이름·인자를 UE 5.6에 맞춤(`Get()`·`GetTagName()`·`GetTag()`·`MakeQuery_*` 등). UE 전용 함수는 추가 안 함, 미지원 인자는 `NotImplementedException`. 맞추는 목적은 근거 미기록. |
| D12 | 태그 관리 포함=레지스트리 편집 | 2026-10-15 | 피커·관리 창에서 추가·서브태그·복제·이름 변경·삭제. 리다이렉터·참조 검사·코멘트 없음, 편집은 Undo 밖. 선택 이유는 근거 미기록. |
| D14 | modifier 태그 판정=UE 경로 | 2026-10-16 | Instant/Periodic은 실행 시 건너뛰기, 지속형은 aggregator 평가 파라미터로 자격 판정 — CurrentValue는 빈 태그, 캡처 평가는 Spec 태그. 선택 이유는 근거 미기록. |
| D22 | 소유 태그=UE `FGameplayTagCountContainer` 구조+Loose·이벤트 | 2026-10-17 | 부모 포함 개수·명시 개수·명시 태그, 이벤트 `NewOrRemoved`·`AnyCountChange`. ASC에 Loose 태그 Add/Remove/SetCount·`HasMatchingGameplayTag`·`GetTagCount`·이벤트 등록/해제. 선택 이유는 근거 미기록. |
| D28 | CurrentValue=UE처럼 빈 문맥, 소유 태그 on/off=Ongoing Tag Requirements(D21·D23 되돌림) | 2026-10-18 | CurrentValue는 `Evaluate(default)`. modifier 조건은 캡처 평가에서만. GE 단위 켜고 끄기는 `TargetTagRequirementsGameplayEffectComponent` 담당. |
| D29 | GE Component 도입(UE 5.3+), 첫 단계는 빈 껍데기 | 2026-10-18 | GE 에셋에 `[SerializeReference]` Component 목록 + 베이스 가상 함수 5개 + `TargetTagRequirementsGameplayEffectComponent`(Application·Ongoing·Removal 필드만). 판정·inhibit·인스펙터 없음. |
| D30 | Asset Tags=UE `FInheritedTagContainer` 구조, 부모 없음 | 2026-10-19 | Added·Removed·CombinedTags를 UE대로 옮기고 부모는 늘 null. 부모 태그 쌓기 없음, Removed는 지금 역할 없음. |

치운 결정(D9·D10·D11·D13·D15~D21·D23~D27)은 decisions.md 끝 "치운 결정"에 한 줄씩만 있다.

### 결정 대기 (유저 확정 필요)
1. **경량 경계** — 레지스트리 유무·형태(없음/정의 에셋/런타임 캐시), 등록 시점(지연 인터닝 vs 정의 에셋 등록). 내부 표현은 `InternedName`(interned string, 정수 핸들 전환은 `InternedName.Intern` 한 곳으로 국소화해 유보). **D3(대소문자=무시비교+표기보존)가 canonical/display 이중화(≈name-table)를 전제하므로 함께 확정.**
2. **GE 연동 범위** — 2026-10-15 물은 네 가지 중 ① GE 구조는 D29(GE Component), ③ ASC 범위는 D22(소유 태그)로 답이 됐다. 남은 것: ② GE 범위(Asset·Granted / 적용·제거 요구 / 유지 요구 / 다른 GE 제거·면역) ④ 요구 조건 TagQuery 포함 여부. UE 원문 근거 → worklog 2026-10-15 GE·ASC 항목.
3. **태그 조회 입력 ergonomics** — `RequestGameplayTag`가 문자열을 어떻게 받을지. 후보: ① `implicit operator string→InternedName` ② `RequestGameplayTag(string)` 오버로드 ③ `new InternedName(...)` 명시 요구. intern 비용은 셋 다 같고 차이는 비용의 **가시성**뿐, 결정은 주 call-site 형태에 달림. D1(계층 문자열 인터페이스)과 연결. **제안 기울기:** blanket implicit보다 명시(① 지양). 2026-10-12 D5 구현 중 ② 오버로드가 생겼지만 확정으로 보진 않음.

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
없음.

## 다음 작업
> 구현 순서는 TODO-BOARD '현재 목표'(2 Asset Tags → 3 Granted Tags → 4 인스펙터 Component 목록 → 5 Target Tag Requirements 판정 → 6 태그로 활성 GE 찾기·제거), 착수는 유저가 말할 때만. 아래 번호는 항목 ID라 끝난 항목(3·10·11·12)을 지워도 다시 매기지 않는다.

- **1. [검증·후속] Tag 코어** (→D5 생성 차단=Request 입구+internal 생성 함수, →D6 native=코드 정의+저장 안 함) — 동작 확인은 tests.md §5에서 끝났고 §5-1 피커 화면(레지스트리 하위 태그가 native 노드 아래로 합쳐 보이는지)만 남음.
  - 후속(미룸): IL2CPP 코드 제거 시 마커 필드 유실 대응(`[Preserve]` 등), 빌드 탐색 비용 측정, 레지스트리 편집 시 에디터 매니저 재구성, 노드 출처·명시/암시 기록. D5의 막는 이유 유저 확인.
- **2. [검증] 레지스트리 자동 생성** — 레지스트리를 지우고 다시 열면 `GameplayTagRegistryInitializer`가 `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`을 만드는지. tests.md §5-4(유저가 하거나 삭제 허락 필요).
- **4. [결정 대기 3] 태그 조회 입력 ergonomics** 유저 확정 후 `RequestGameplayTag` 입력 경로 확정.
- **5. [D3 구현] 대소문자 무시비교 + 표기 보존** — `InternedName`을 canonical/display 이중화로. 경량 경계(결정 대기 1)와 묶임. TODO-BOARD '뒤로 미룸'.
- **6. [검증] 중첩 struct `ISerializationCallbackReceiver` 콜백 실측** — 인스펙터에 `GameplayTag` 필드 저장→로드 후 `Equals` true 확인. 실패 시 fallback(컨테이너 재-intern / `Ordinal`). 컨테이너 저장 후 매칭은 tests.md §5-3에서 확인됨. → worklog 2026-09-28.
- **7. [Query] 내부 표현** — UE대로 토큰 스트림(`_tagDictionary`·`_queryTokenStream`)으로 구현·직렬화, 형식 버전 저장(D26, 치운 결정). 식 트리로 바꿀지는 유저 미확정(바꾸면 저장된 질의를 옮겨야 함).
- **8. [결정 대기 2] GE 연동 범위 ②④** 유저 확정 → 결정 기록 후 세부 TODO 4~6.
- **9. [검증] 태그 관리 창·대화창** — tests.md §4-1(관리 창 태그 추가)·§4-2(행 메뉴 Rename·Delete, native 행 비활성)·§4-6(미등록 태그 경고 대화창과 피커 앞뒤, 유저 몫). Unity를 앞으로 띄워야 한다.
