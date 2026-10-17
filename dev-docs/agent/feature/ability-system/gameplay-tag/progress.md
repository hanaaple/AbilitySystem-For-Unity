# gameplay-tag — 게임플레이 태그 (Tag 코어 + GE 태그 연동)

- 상태: 🚧 IN PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-10-16 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md) · [aggregator](../aggregator/progress.md).
> 유저가 에디터에서 확인할 항목·절차: [tests.md](tests.md).
> **현재 설계 합의 단계** — 아래 `범위`·`설계 개요`·`세부 TODO`는 **에이전트 제안**이다. 유저 확정 전이라 `결정 기록`에 `D#`을 부여하지 않았다. 착수 전 핵심 결정(태그 표현·연동 범위)을 확정한다.

## 목표
캐릭터/이펙트에 상태를 나타내는 GameplayTag를 부여하고, GameplayEffect가 그 태그와 연동되게 한다 — GE가 대상에 태그를 부여/회수하고, 대상이 가진 태그에 따라 GE 적용·유지가 조건부로 걸린다. (유저 확정 목표: "Tag + GE 태그 연동")

## 수용 기준 (Definition of Done)
> 제안 — 연동 범위 확정 후 확정한다.
- [ ] 계층 태그를 정의하고 부모 매칭이 동작한다 (`Status.Debuff.Stun`이 `Status.Debuff` 질의에 매칭)
- [ ] Duration/Infinite GE 적용 시 대상 ASC의 owned-tag에 GE의 부여 태그가 더해지고, 해제 시 제거된다 (에디터 인스펙터에서 확인 가능)
- [ ] 같은 태그를 두 GE가 부여하면 참조 카운트로 누적되고, 하나만 제거해도 태그가 남는다
- [ ] 대상이 특정 태그를 가지면(또는 없으면) GE 적용이 차단/허용된다 (Application 요구)

## 범위
### 포함 (제안)
- **GameplayTag 코어:** 계층 태그 식별자 + 소유 태그 컨테이너(참조 카운트) + 매칭 질의(정확/부모 포함)
- **GE 태그 필드:** `GameplayEffectAsset`에 태그 필드 추가 (아래 '연동 범위' 결정 대기)
- **ASC owned-tag 추적:** GE 적용/해제 시 태그 부여/회수, 요구 검사 훅
### 제외 (명시적으로 하지 않을 것)
- GameplayCue(태그 기반 연출) · GE Stack — 별도 feature(로드맵)
- GameplayAbility의 태그 배선(Activation/Block/Cancel 태그) — [gameplay-ability](../gameplay-ability/progress.md) 범위. 이번엔 **GE 연동만**. 단, Tag 코어(식별자·컨테이너·질의)는 **GA도 재사용할 공용 인프라로 설계**한다(유저 확정 방향) — 소비처 중립.
- 네트워크 복제 (프로젝트 전체 미채택)

## 설계 개요
> 제안. 아래는 코드에서 확인한 현재 상태와 연동 지점이다.
- **현재 상태(확인):** `GameplayEffectAsset`에 태그 필드 없음(`Effect/GameplayEffectAsset.cs` — stack/cue만 TODO 주석). `AbilitySystemComponent`에 owned-tag 컨테이너 없음(`AbilitySystemComponent.cs`). 적용/해제·수명은 `ActiveGameplayEffectsContainer`가 소유.
- **연동 지점(제안):**
  - Tag 코어: `AbilitySystem/Tag/` 신설(태그 식별자·컨테이너·질의).
  - ASC: owned-tag 컨테이너 보유 + 조회 API(`HasTag`/`HasAll`/`HasAny`) 위임.
  - GE 적용 경로(`ActiveGameplayEffectsContainer.ApplyGameplayEffectSpec`): 적용 요구 검사 → 통과 시 Duration/Infinite면 부여 태그를 owned-tag에 추가.
  - GE 해제 경로(`RemoveActiveGameplayEffect`): 부여했던 태그를 owned-tag에서 회수.
- 코드 예정 위치: `Assets/Scripts/Core/AbilitySystem/Tag/`, GE 연동은 `Effect/GameplayEffectAsset.cs` + `Effect/ActiveGameplayEffectsContainer.cs` + `AbilitySystemComponent.cs`.
- 아키텍처: [architecture/ability-system/overview.md](../../../../project/architecture/ability-system/overview.md) UE 대비 표에 GameplayTag는 🔜로 등재됨.

### UE GAS 원본 클래스 (참조 · 우리 설계의 미러링 대상)
> 원문 확인(2026-09-30, **UE 5.6.0** 로컬 소스 `Runtime/GameplayTags/Classes/{GameplayTagContainer.h, GameplayTagsManager.h}`). **상속이 아니라 소유·사용 관계**다. 우리 구현 상태는 위 `설계 개요`·아래 `세부 TODO` 참조.

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
| `FGameplayTagQuery` (+`Expression`) | `GameplayTagQuery` (빈 골격) |

> 주변 클래스(이번 정리 범위 밖·미검증): `UGameplayTagsSettings`(config), `FGameplayTagTableRow`(정의 DataTable), `IGameplayTagAssetInterface`, `FNativeGameplayTag`/`FGameplayTagRedirectors` 등. 네트워크(`NetIndex`·replication)는 프로젝트 전체 미채택.

## 세부 TODO (구현 체크리스트)
> 제안 — 결정 확정 후 확정한다.
- [x] 1. 태그 표현 타입 확정·구현 — 인터닝 래퍼(생성·역직렬화 경로 intern 보장). 2026-09-29: `InternedString`→`InternedName`으로 리네임 + `Core.Common`으로 이동(Unity InputSystem `InternedString`과 이름 충돌 회피, 범용 유틸로 격상). → worklog. **단, 중첩 struct 직렬화 콜백 실측 미완(아래 '다음 작업'). 또한 D3(대소문자=무시비교+표기보존)로 canonical/display 이중화가 필요해져 현재 단일 필드 구현은 D3 미충족 — '다음 작업' 참조.**
- [ ] 2. GameplayTag 식별자 + 계층 매칭 — 식별자 struct·**노드 트리 빌드(`GameplayTagNode`/`GameplayTagsManager`) 완료**(2026-10-01, → worklog). **2026-10-12: 매칭 API 구현**(태그 `MatchesTag*`·컨테이너 `HasTag/HasAny/HasAll(+Exact)`·Filter·매니저 Parents/Children/DirectParent/MatchDepth, → worklog). **미검증**(Unity 컴파일·실행 전) — 확인 후 ☑.
- [ ] 3. owned-tag 컨테이너(참조 카운트) + ASC 조회 API — 2026-10-16: 조회 함수 `GetOwnedGameplayTags`(두 형태)만 생김, 늘 빈 값(→D18 판정 태그 출처=ASC 조회 함수만·빈 값). 착수 시 함께 할 것: 소유 태그가 바뀌면 Target 태그 조건을 가진 지속형 mod의 CurrentValue를 다시 계산(→D21 지속형 태그 조건을 CurrentValue에도 적용).
- [ ] 4. `GameplayEffectAsset` 태그 필드 + Spec 전달
- [ ] 5. GE 적용/해제 시 태그 부여/회수 배선
- [ ] 6. GE 적용 요구(Application requirement) 검사
- [ ] 7. 에디터 확인 경로(인스펙터에 owned-tag 표시 등) — 2026-10-15 태그 필드 드로어·피커·태그 관리 창 구현(→D10 태그 드로어=UE 동작 최대 미러, →D12 태그 관리 포함=레지스트리 편집), 미검증. owned-tag 표시는 3 이후. 2026-10-16 Query 편집기 추가(→D17 Query 편집기=UE처럼 별도 창·8종·Categories 전달), 미검증.
- [ ] 8. Modifier Source/Target Tags(UE `FGameplayModifierInfo::SourceTags/TargetTags`) — 2026-10-16 필드·`GameplayTagRequirements`·Query 직렬화·인스펙터 표시 구현(→D13 Modifier Source/Target Tags=UE Requirements 전체·판정 미연결, →D15 Query=직렬화+판정·편집 UI 나중), 미검증. 판정 연결(→D14 지속형 판정=UE 그대로)은 3 이후. 2026-10-16: 판정을 UE 호출 위치대로 연결(→D18~D21, 아래 다음 작업 11), 컴파일만 확인. 소유 태그가 비어 있어 실제 동작 확인은 3 이후.

> 아래 9~13은 2026-10-16 "지금 코드에서 태그 달 만한 곳" 조사로 나온 **후보**다(유저: "필요한거 TODO로 해둬"). 순서·포함 여부·UE 그대로 따를지는 유저 미확정이다. 착수할 때 묻는다. UE 위치는 5.6 헤더에서 확인했다.
- [ ] 9. `AttributeBasedMagnitude` 태그 필터 — UE `FAttributeBasedFloat::SourceTagFilter/TargetTagFilter`(`GameplayEffect.h:182,186`). 속성을 읽을 때 태그 조건에 맞는 modifier만 집계한다. 8의 판정 연결(집계에 태그 전달)과 같은 경로라 함께 하기 좋다.
- [ ] 10. SetByCaller 크기 — UE `FSetByCallerFloat::DataTag`(`:267`) + `FGameplayEffectSpec::SetByCallerTagMagnitudes`(`:1244`). `MagnitudeCalculationType`에 새 유형이 생기고, Spec에 "태그 → 값" 저장이 생긴다. 쓸 곳 후보: `MeleeAttackModule`의 실행 중 데미지(에이전트 추론).
- [ ] 11. Spec 동적 태그 — UE `FGameplayEffectSpec::DynamicGrantedTags/DynamicAssetTags`(`:1209,1214`). 에셋을 고치지 않고 이번 적용에만 태그를 덧붙인다. 4(GE 태그 필드 + Spec 전달) 이후.
- [ ] 12. Execution 태그 — UE `FGameplayEffectExecutionDefinition::PassedInTags`(`:525`)·`FGameplayEffectCustomExecutionParameters::PassedInTags`(`GameplayEffectExecutionCalculation.h:192`), scoped modifier `SourceTags/TargetTags`(`GameplayEffect.h:470,474`). 대상: `GameplayEffectExecution`·`GameplayEffectExecutionParameters`.
- [ ] 13. 활성 GE 태그 질의·제거 — UE `FGameplayEffectQuery`(`OwningTagQuery`·`EffectTagQuery`·`SourceTagQuery` 등, `:1462-1474`). `ActiveGameplayEffectsContainer`에 태그로 찾기·제거 API를 둔다(예: "Debuff 전부 해제"). 4·5 이후.

## 결정 기록
상세는 → [decisions.md](decisions.md) (착수 시 생성). progress엔 요지 인덱스만.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | 인터페이스=계층 문자열 | 2026-09-26 | 개발자 인터페이스는 UE와 동일한 계층 문자열(`Status.Debuff.Stun`). |
| D3 | 대소문자=무시비교+표기보존(FName식) | 2026-09-29 | 비교는 대소문자 무시, 표시는 원본 표기 보존(canonical/display 이중화). |
| D4 | 질의 식=class+자기 반환 체이닝 | 2026-10-11 | `GameplayTagQueryExpression`은 class(잠정), 수정 메서드는 자기 자신 반환(체이닝). 조립 시 GC 할당 감수. |
| D5 | 생성 차단=Request 입구+internal 생성 함수 | 2026-10-12 | `GameplayTag` 생성자 private. 외부는 `RequestGameplayTag`·native로만 획득, 매니저는 `internal static Create` 하나로 생성(asmdef 분리 전까진 실효 없음 감수). 막는 이유는 근거 미기록. |
| D6 | native=코드 정의+저장 안 함 | 2026-10-12 | `[NativeTag]` static 필드로 정의, 트리 구성마다 탐색해 레지스트리보다 먼저 추가, 에셋에 저장 안 함. 매니저는 에디터에서도 지연 생성. |
| D7 | Container=class | 2026-10-12 | struct+public List 필드 → class. 사본 간 List 공유로 노드 캐시가 오염되는 함정 회피. 컨테이너당 GC 할당 감수. |
| D8 | 기존 API=UE 이름·시그니처 | 2026-10-14 | 구현된 함수의 이름·인자를 UE 5.6에 맞춤(`Get()`·`GetTagName()`·`GetTag()`·`MakeQuery_*` 등). UE 전용 함수는 추가 안 함, 미지원 인자는 `NotImplementedException`. 맞추는 목적은 근거 미기록. |
| D9 | 쓸모 있는 UE 함수=빈 시그니처 | 2026-10-15 | D8 범위 확장 — 쓸모 있는 UE 전용 함수는 시그니처만 두고 본문은 `NotImplementedException`. UE 5.6 이후 버전 함수(`*ExactMatch`)도 유지. 미리 두는 목적은 근거 미기록. |
| D10 | 태그 드로어=UE 동작 최대 미러 | 2026-10-15 | 단일·컨테이너 드로어를 UE `SGameplayTagCombo`·`SGameplayTagContainerCombo`·`SGameplayTagPicker` 동작대로. Categories 필터 속성·다중 오브젝트 편집 포함. 선택 이유는 "최대한 UE와 유사하게" 외 근거 미기록. |
| D11 | 피커 루트 입구=UE `GetFilteredGameplayRootTags` | 2026-10-15 | 매니저에 에디터 전용으로 추가(리플렉션 대신). 선택 이유는 근거 미기록. |
| D12 | 태그 관리 포함=레지스트리 편집 | 2026-10-15 | 피커·관리 창에서 추가·서브태그·복제·이름 변경·삭제. 리다이렉터·참조 검사·코멘트 없음, 편집은 Undo 밖. 선택 이유는 근거 미기록. |
| D13 | Modifier Source/Target Tags=UE Requirements 전체·판정 미연결 | 2026-10-16 | `GameplayModifier`에 `GameplayTagRequirements`(Require·Ignore·TagQuery + UE 함수 전부) 필드 2개. 이번엔 필드·인스펙터까지만, 적용 경로 판정은 다음 단계. 소유 태그 출처는 "UE에 따를" 방향만. 선택 이유는 근거 미기록. |
| D14 | 지속형 판정=UE 그대로 | 2026-10-16 | (다음 단계 설계) Instant/Periodic은 실행 시 건너뛰기, 지속형은 aggregator 평가 파라미터로 자격 판정 — CurrentValue는 빈 태그, 캡처 평가는 Spec 태그. 선택 이유는 근거 미기록. |
| D15 | Query=직렬화+판정·편집 UI 나중 | 2026-10-16 | `GameplayTagQuery` `[Serializable]`(기존 세 필드), `RequirementsMet`에 Query 판정. 인스펙터는 읽기 전용 설명. `TokenStreamVersion`·`AutoDescription` 없음. 선택 이유는 근거 미기록. (편집 UI·자동 설명은 D16·D17로 후속) |
| D16 | Query 자동 설명=UE처럼 저장·Build 때 생성 | 2026-10-16 | `_autoDescription` 직렬화 필드, `Build`가 UE 형식(`" ANY( A, B )"`)으로 생성, `GetDescription()`은 사용자 설명 없으면 자동 설명. UE와 달리 런타임 `Build`에서도 만든다. 선택 이유는 근거 미기록. |
| D17 | Query 편집기=UE처럼 별도 창·8종·Categories 전달 | 2026-10-16 | 인스펙터 줄(설명·Edit·Clear) + "Tag Query Editor" 창(즉시 반영·OK·Cancel 복원). 식 종류는 Exact 포함 8종, 필드 Categories를 창 안 피커로. 선택 이유는 근거 미기록. |
| D18 | 판정 태그 출처=ASC 조회 함수만·빈 값 | 2026-10-16 | UE `GetOwnedGameplayTags` 대응 함수를 ASC에 두고 지금은 빈 컨테이너. 실제 소유 태그는 세부 TODO 3. 선택 이유는 근거 미기록. |
| D19 | Execution 캡처 조회=UE처럼 평가 파라미터 인자 | 2026-10-16 | `GameplayEffectExecutionParameters.AttemptCalculateCapturedAttributeMagnitude`에 평가 파라미터 인자(BaseValue는 UE처럼 없음). gameplay-effect 코드·`SpeedBoostExecution` 수정. 선택 이유는 근거 미기록. |
| D20 | Spec 캡처 태그=UE `FTagContainerAggregator` 구조 | 2026-10-16 | `CapturedSourceTags`·`CapturedTargetTags` = ActorTags + SpecTags → 합친 태그. 실행 판정은 ActorTags, 캡처 평가는 합친 태그. 선택 이유는 근거 미기록. |
| D21 | 지속형 태그 조건을 CurrentValue에도 적용(UE와 다름) | 2026-10-16 | CurrentValue는 Target=소유 태그, Source=mod를 건 GE의 Source Actor 태그로 판정. 캡처 평가는 UE 그대로. 유저: "ue에서도 의도에 맞지 않는다고 해". D14의 CurrentValue 부분 대체. |

### 결정 대기 (유저 확정 필요)
1. **경량 경계** — 레지스트리 유무·형태(없음/정의 에셋/런타임 캐시), 등록 시점(지연 인터닝 vs 정의 에셋 등록). 내부 표현(인터닝 핸들)은 `InternedName`으로 구현돼 있으나 형식 결정은 미확정. **D3(대소문자=무시비교+표기보존)가 canonical/display 이중화(≈name-table)를 전제하므로 이 경계 결정에 제약을 건다 — 함께 확정.**
   - ~~내부 표현(interned string vs int 핸들)~~ **→ 해소(2026-09-28):** `InternedName`(구 `InternedString`) 래퍼 struct로 interned string 채택. 정수 핸들 전환은 `InternedName.Intern` 한 곳으로 국소화해 유보. → worklog.
2. **GE 연동 범위** — 최소(부여+적용요구) ~ 전체(+유지요구+자산+태그기반 제거). **단, Tag 코어는 GA에서도 재사용될 공용 인프라**(유저 확정 방향)이므로 코어는 소비처 중립으로 설계. 2026-10-15: 유저가 "ue에 맞춰서" 착수를 요청, GE 구조(평면 필드 vs GE Component)까지 묶어 질문 중 → 다음 작업 8.
3. **태그 조회 입력 ergonomics** (checkpoint, 2026-10-01) — `GameplayTagsManager.RequestGameplayTag`(현재 본문 미구현 — 맵 조회 + 미등록 처리 예정)가 문자열을 어떻게 받을지 미정. 후보: ① `implicit operator string→InternedName` ② `RequestGameplayTag(string)` 오버로드 ③ `new InternedName(...)` 명시 요구. **확인한 사실:** 지금 `InternedName`·`GameplayTag`엔 implicit operator가 없고 명시 생성자만 있음 → `RequestGameplayTag("...")`는 현재 컴파일 불가. intern 비용은 문자열 조회 시 ①②③ 모두 동일(맵 키가 `ReferenceEquals` 기반이라 인터닝 필수), 차이는 그 비용의 **가시성**뿐. 결정은 **주 call-site 형태**(코드 문자열 리터럴 빈도 vs 이미 핸들 보유)에 달림 — UE는 FName 암묵 생성에 의존. D1(계층 문자열 인터페이스)과 연결. **제안 기울기:** blanket implicit보다 명시(① 지양). 유저 확정 전. 2026-10-12: D5(생성 차단=Request 입구+internal 생성 함수) 구현 중 `GameplayTag.RequestGameplayTag(string)` 오버로드(②)가 생김 — 이 결정의 확정으로 보진 않음.

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
없음. (2026-10-12 코드 확인: 기존 '매니저 비-void 빈 스텁 4개' 블로커는 스텁이 이미 주석 처리돼 해소된 상태였고, `AddNativeGameplayTag` 스텁은 native 경로로 대체되어 제거됨.)

## 다음 작업
> **순서(2026-10-16 세션 종료 정리):** ① 유저 에디터 확인 — [tests.md](tests.md) §0→§5(아래 1·2·6·9·10·11·12의 확인 절차를 모은 것) ② 유저 결정 6개 — [TODO-BOARD](../../../TODO-BOARD.md) '당장 목표'의 (decision) 항목 ③ 소유 태그 입구 + CurrentValue 재계산(아래 3) ④ GE·ASC 태그 연동(아래 8) ⑤ 태그 후보(세부 TODO 9~13). 아래 번호는 항목 ID이고 순서가 아니다.

1. **[검증] Tag 네임스페이스 컴파일·동작 확인** (→D5 생성 차단=Request 입구+internal 생성 함수, →D6 native=코드 정의+저장 안 함, →D7 Container=class) — Unity 컴파일 후(상세 절차: [tests.md](tests.md) §5):
   - native: `[NativeTag] public static readonly NativeGameplayTag X = new("Test.Native.A");` → 에디터·플레이에서 `GameplayTag.RequestGameplayTag("Test.Native.A")`·`X.GetTag()`가 빈 태그 아님. 레지스트리에 `Test.Native.A.B` → native 아래로 합쳐짐.
   - 매칭: `{"A.B.C"}.HasTag("A.B")` true / `HasTagExact("A.B")` false, `"A.B.C".MatchesTagDepth("A.B.D")` 2, `RequestGameplayTagChildren("A")`에 `A.B`·`A.B.C`, 질의 `MakeQuery_MatchAnyTags`가 부모 매칭. 미등록 이름 `RequestGameplayTag`는 에러 로그가 이름당 1회(→D8 기존 API=UE 이름·시그니처). 인스펙터에 저장한 컨테이너를 로드한 뒤 부모 매칭이 되는지(역직렬화 시 이름 파싱 복원). (유저, 에디터)
   - 빈 시그니처(→D9 쓸모 있는 UE 함수=빈 시그니처): `GameplayTag.operator <`, 매니저 `DoneAddingNativeTags`·`CallOrRegister_OnDoneAddingNativeTagsDelegate`·`SplitGameplayTagFName`·`RequestAllGameplayTags` — 본문 미구현(호출 시 예외).
   - 후속(미룸): IL2CPP 코드 제거 시 마커 필드 유실 대응(`[Preserve]` 등), 빌드 탐색 비용 측정, 레지스트리 편집 시 에디터 매니저 재구성, 노드 출처·명시/암시 기록(편집 UI 착수 시). D5의 막는 이유 유저 확인.
2. **[에셋 생성·검증] 레지스트리 인스턴스** — 에디터 로드 시 `GameplayTagRegistryInitializer`가 `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`을 자동 생성하는지 확인(수동 생성 메뉴는 2026-10-01 제거). 인스펙터의 목록 직접 편집은 막혀 있고(읽기 전용, `Tag/Editor/GameplayTagRegistryAssetEditor`), 2026-10-15부터 편집은 관리 창·피커로 한다(→D12 태그 관리 포함=레지스트리 편집, 검증은 아래 9). (유저, 에디터)
3. **owned-tag 참조 카운트 컨테이너** — 계층 매칭은 2026-10-12 구현(위 1에서 검증). 남은 것은 세부 TODO 3(ASC owned-tag 참조 카운트, UE `FGameplayTagCountContainer` 대응 — Tag 네임스페이스 밖 ASC 쪽).
   - **2026-10-16 유저 지시: "문서로 자세히 써놔 다음에 하게".** 소유 태그를 바꾸는 입구와, 태그가 바뀌면 CurrentValue를 다시 계산하는 일(유저: "태그가 바뀌면 CurrentValue 다시 계산하도록 하는게 맞고")을 다음에 한다. 아래는 착수용 계획이다(에이전트 정리, 유저 미확정 — 착수 때 범위를 다시 묻는다).
   - **지금 상태(코드 사실):** `AbilitySystemComponent._ownedGameplayTags`는 늘 빈 `GameplayTagContainer`다. 조회는 `GetOwnedGameplayTags(채울 컨테이너)`·`GetOwnedGameplayTags()` 두 형태(→D18 판정 태그 출처=ASC 조회 함수만·빈 값). 태그를 넣는 API는 없다.
   - **UE 구조(5.6, 확인한 위치):**
     - 저장: `FGameplayTagCountContainer`(`GameplayEffectTypes.h:1043`). 태그별 개수(`GameplayTagCountMap`, 부모 태그 개수 포함), 명시 태그 개수(`ExplicitTagCountMap`), 명시 태그 컨테이너(`ExplicitTags`, `GetExplicitGameplayTags()` `:1272`).
     - 갱신: `UpdateTagCount(태그, 증감)`(`:1150`) → `UpdateTagMap_Internal`(`GameplayEffectTypes.cpp:617`) → `UpdateExplicitTags` + `GatherTagChangeDelegates`. 어떤 태그의 개수가 0↔양수로 바뀌면 "의미 있는 변화"로 true를 돌려준다.
     - ASC 입구: `AddLooseGameplayTag(s)`·`RemoveLooseGameplayTag(s)`·`SetLooseGameplayTagCount`(`AbilitySystemComponent.h:654-674`) → `UpdateTagMap`(`:621-631`). 의미 있는 변화면 `OnTagUpdated(태그, 존재 여부)`(`:1923`, 가상 훅)를 부른다. 조회: `HasMatchingGameplayTag`(`:576`)·`GetTagCount`(`:609`)·`GetOwnedGameplayTags`(`:591-600`). 이벤트: `RegisterGameplayTagEvent`(`:744`).
     - GE 부여 태그(세부 TODO 5)도 같은 `UpdateTagMap`으로 개수를 올리고 내린다.
   - **우리 쪽 할 일(제안 순서):**
     1. 새 `GameplayTagCountContainer`(위치 제안: `Core/AbilitySystem/`, ASC 쪽). 최소 기능: 개수 맵(부모 포함), 명시 태그 컨테이너, `UpdateTagCount`가 의미 있는 변화 여부를 돌려줌. 이벤트 등록은 범위를 물어서.
     2. ASC: `_ownedGameplayTags`를 카운트 컨테이너의 명시 태그로 교체. `AddLooseGameplayTag(s)`·`RemoveLooseGameplayTag(s)`(·`SetLooseGameplayTagCount`)·`HasMatchingGameplayTag`·`GetTagCount` 추가. 기존 `GetOwnedGameplayTags` 두 형태는 시그니처 유지.
     3. **CurrentValue 재계산(→D21 지속형 태그 조건을 CurrentValue에도 적용):** 의미 있는 변화가 생기면 ASC가 `ActiveGameplayEffectsContainer`에 알린다(UE `OnTagUpdated` 자리). 컨테이너는 Target 태그 조건을 가진 mod를 담은 aggregator만 골라 dirty를 울린다. 그러면 `OnAttributeAggregatorDirty`가 `EvaluateCurrentValue(소유 태그)`로 다시 계산하고, 값이 바뀌었을 때만 변경 이벤트를 낸다. 고를 방법: aggregator에 "Target 조건 mod가 있는가" 조회를 두거나, 단순하게 모든 aggregator를 dirty(비용 확인 후 결정).
     4. Source 조건은 GE 생성 때 캡처한 Source 태그로 판정하므로(UE와 같은 캡처 시점) 태그가 바뀌어도 다시 계산할 필요가 없다. 다시 캡처하려면 UE `RecaptureSourceActorTags`(이미 있음)를 쓴다.
     5. 검증: `ModifierTagSelfCheck`에 "Loose 태그 추가 → Target Require mod가 CurrentValue에 들어옴 → 제거 → 빠짐", "같은 태그 2회 추가 1회 제거 → 유지", "부모 태그 Require가 자식 태그 추가로 충족", "Instant Target Require가 태그 보유 시 적용" 케이스를 추가하고 [tests.md](tests.md) 갱신.
4. **[결정대기 3] 태그 조회 입력 ergonomics** 유저 확정 후 `RequestGameplayTag` 입력 경로 확정. → 결정 대기 3.
5. **[D3 구현] 대소문자 무시비교 + 표기 보존** — `InternedName`(단일 필드·case-sensitive)을 canonical/display 이중화로. **경량 경계**(결정 대기)와 묶임. → decisions.md D3.
6. **[검증] 중첩 struct `ISerializationCallbackReceiver` 콜백 실측** — 인스펙터에 `GameplayTag` 필드 저장→로드 후 `Equals` true 확인. 실패 시 fallback(컨테이너 재-intern / `Ordinal`). (에디터) → worklog 2026-09-28.
7. **[Query] `GameplayTagQuery`** — 경량 식 트리 방향으로 기움(복제 없음→바이트코드 불요). 현 DoD는 평면 All/Any/None 수준이라 **우선순위 낮음**. → progress 'UE GAS 원본 클래스'.
   - 2026-10-11: UE 5.6 기준 Query·Expression **시그니처(빈 본문)·필드** 작성, Expression은 class(→D4 질의 식=class+자기 반환 체이닝). ⚠ Query 필드는 UE대로 **토큰 스트림**(`_tagDictionary`·`_queryTokenStream`)으로 채움 — 위 '경량 식 트리' 기울기와 상충, **내부 표현 미확정**(유저 확인 필요). 직렬화 여부·`TokenStreamVersion`도 미정. → worklog 2026-10-11.
   - 2026-10-11(이후): 유저 요청으로 **토큰 스트림 방식으로 대략 구현**(평가·기록·복원·`MakeQuery*`). 미검증(빌드·실행 안 함). 내부 표현 결정은 여전히 열림. 컨테이너 매칭은 Query 안 임시 헬퍼 → 세부 TODO 2 때 Container로 이동.
   - 2026-10-16: 직렬화 결정(→D15 Query=직렬화+판정·편집 UI 나중) — 토큰 스트림 그대로 저장, `TokenStreamVersion`은 여전히 없음. 내부 표현은 열림(바꾸면 저장된 질의를 옮겨야 함).
   - 2026-10-16: 자동 설명(→D16 Query 자동 설명=UE처럼 저장·Build 때 생성)·편집 창(→D17 Query 편집기=UE처럼 별도 창·8종·Categories 전달) 구현, 미검증 → 아래 12. D15 트리거(편집 UI 착수 시 `TokenStreamVersion` 결정)에 걸렸으나 유저에게 아직 안 물음.
8. **GE 연동 범위**(최소~전체) 유저 확정. 2026-10-16: Modifier 태그 질문 중 소유 태그 출처는 "UE에 따를건데"라는 방향만 받음(형태 미정, →D13 Modifier Source/Target Tags=UE Requirements 전체·판정 미연결). 2026-10-15 유저 요청("GE, AbilitySystemCopmponent에 Tag 넣어봐 ue에 맞춰서")으로 착수 직전. 물은 네 가지(답 대기): ① GE 구조(평면 필드 vs UE 5.3+ GE Component) ② GE 범위(Asset·Granted / 적용·제거 요구 / 유지 요구 / 다른 GE 제거·면역) ③ ASC 범위(카운트 컨테이너+조회 / Loose / 태그 변경 이벤트 / 태그로 GE 조회·제거) ④ 요구 조건 TagQuery 포함 여부. UE 원문 근거 → worklog 2026-10-15 GE·ASC 항목. 답을 받으면 결정 기록 후 세부 TODO 3~6 착수.
9. **[검증] `GameplayTag`·`GameplayTagContainer` 드로어 + 피커 + 태그 관리** (→D10 태그 드로어=UE 동작 최대 미러, →D11 피커 루트 입구=UE `GetFilteredGameplayRootTags`, →D12 태그 관리 포함=레지스트리 편집) — 2026-10-15 구현, **미검증**(→ worklog). 세부 TODO 7(에디터 확인 경로)의 일부. 확인 순서(유저, 에디터 — 상세 절차: [tests.md](tests.md) §4):
   1. 컴파일 에러 확인.
   2. 레지스트리 인스펙터 "Manage Gameplay Tags..." → 관리 창 `+`로 `Test.A`·`Test.A.B`·`Test.C` 추가 → 레지스트리 에셋에 저장되는지. 행 메뉴 Rename·Delete, native 태그는 Rename·Delete가 회색인지.
   3. `Assets/_Test/GameplayTagTest.asset`에서 단일·List 요소·컨테이너 필드마다 피커가 열리고, 체크·해제(형제 없으면 부모가 들어감)·칩 ×·우클릭 Copy/Paste/Clear가 되는지. 저장→에디터 재시작 후 값 유지(위 6과 겹침). 중첩 struct/class·`[GameplayTagCategories]` 확인용 필드는 유저 요청으로 지웠다(2026-10-15) — 이 둘은 확인할 필드가 따로 있어야 한다.
   4. 테스트 에셋 두 개 다중 선택 → Multiple Values·흐린 칩·미정 체크박스.
   5. 위험 지점: 팝업 안 GenericMenu(행 ▾·우클릭·설정)에서 팝업이 닫히는지, 미등록 태그 경고 대화창 뒤 팝업이 뜨는지, 행 체크박스 클릭.
10. **[검증] Modifier Source/Target Tags 인스펙터** (→D13 Modifier Source/Target Tags=UE Requirements 전체·판정 미연결, →D15 Query=직렬화+판정·편집 UI 나중) — 2026-10-16 구현, **미검증**. 확인(유저, 에디터 — 상세 절차: [tests.md](tests.md) §3): ① 컴파일 에러 ② GE 에셋 modifier를 펼치면 Magnitude 아래 Source Tags·Target Tags foldout, 그 안에 Require Tags·Ignore Tags(컨테이너 피커)·Tag Query(회색 "Empty" + Edit·×, 편집은 12) ③ 태그를 넣고 저장 → 재시작 후 유지 ④ modifier가 여럿일 때 요소 높이·구분선이 겹치지 않는지 ⑤ foldout 화살표가 드래그 핸들과 겹치지 않고 값 열이 Modifier Op 줄과 맞는지.
11. **[판정 연결] Modifier 태그 판정** — 2026-10-16 **구현, 컴파일만 확인**(→D14 지속형 판정=UE 그대로, →D18 판정 태그 출처=ASC 조회 함수만·빈 값, →D19 Execution 캡처 조회=UE처럼 평가 파라미터 인자, →D20 Spec 캡처 태그=UE `FTagContainerAggregator` 구조, →D21 지속형 태그 조건을 CurrentValue에도 적용). 경위 → worklog 2026-10-16 판정 연결 항목.
   - 남은 일: ① 세부 TODO 3(소유 태그 + 태그 변경 시 CurrentValue 재계산) 뒤 동작 확인 ② 유저 확인 — CurrentValue와 캡처 평가의 태그 문맥 차이(D21 결과·트레이드오프), `UpdateAggregatorMod` 인덱스 문제(aggregator progress), 주기 실행 magnitude 재계산(gameplay-effect progress).
   - 확인 절차: [tests.md](tests.md) §1 — 셀프체크 `Tools ▸ Ability System ▸ Run Modifier Tag Self-Check`(2026-10-16 추가, 38항목) + 회귀 확인 + 결정 요청 4개.
   - 2026-10-17: Unity 컴파일 에러 0, Modifier Tag 셀프체크 38/0, Aggregator 셀프체크 44/0 통과(MCP로 확인). Play 모드 회귀(tests.md §1-3-2~5)는 아직.
12. **[검증] Query 편집기** (→D16 Query 자동 설명=UE처럼 저장·Build 때 생성, →D17 Query 편집기=UE처럼 별도 창·8종·Categories 전달) — 2026-10-16 구현, **동작 미검증**(컴파일은 Unity 컴파일러로 확인 — worklog 2026-10-16 Query 편집기). 확인 위치: GE 에셋 modifier → Source/Target Tags → Tag Query 줄. 확인(유저, 에디터 — 상세 절차: [tests.md](tests.md) §2):
   1. Unity 콘솔에 컴파일 에러 없는지(에이전트 컴파일은 통과).
   2. 줄이 회색 "Empty" + Edit + × 로 보이고, 설명·Edit를 누르면 "Tag Query Editor: Tag Query {에셋}" 창이 줄 왼쪽에 뜨는지.
   3. Root Expression 팝업 → Any Tags Match → Tags 피커로 태그 추가 → 인스펙터 줄이 ` ANY( 태그 )`로 바뀌는지(즉시 반영). All Expressions Match → `+`로 하위 식 둘 → 각각 종류·태그 → ` ALL(  ANY( A ),  NONE( B ) )`꼴.
   4. User Description을 넣으면 줄이 그 문구로 바뀌는지. Cancel → 연 시점 값으로 돌아가는지. OK → 닫히고 값 유지. 저장 → 재시작 후 유지.
   5. Ctrl+Z가 창이 열린 채로 되돌려지고 창 내용도 따라가는지. 인스펙터 ×(Clear) 시 창이 빈 질의로 바뀌는지.
   6. 위험 지점(에이전트 추측): 변경 감지를 `EditorUtility.GetDirtyCount`로 한다 — 사본 SO(`HideFlags.DontSave`)의 Apply가 횟수를 올리지 않으면 즉시 반영이 안 된다. 하위 식 `−` 삭제가 칸만 비우는지. 창 안 피커 팝업이 열리고 창이 다시 그려지는지.
   7. (2026-10-16 추가) 창 가독성 — worklog 2026-10-16 접기 항목·가독성 개선 항목. 식 줄 화살표로 본문이 접히는지(None 줄은 화살표 없이 글자 위치만 맞는지). 본문이 옅은 칠 + 왼쪽 색 안내선으로 묶여 보이고 안내선이 화살표 아래에 오는지. 접은 식 줄 끝에 회색 요약(` ANY( A )` 꼴)이 뜨는지. 태그 상자가 위 줄 팝업과 같은 열에서 시작하는지. "+ Expression"이 목록 끝에 있는지. 접은 칸 앞 칸을 `−`로 지워도 접힌 칸이 그대로인지. 접을 때 콘솔에 GUI 레이아웃 에러가 없는지. 밝은 스킨에서도 칠·안내선이 보이는지.
