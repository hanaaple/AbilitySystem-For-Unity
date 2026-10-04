# gameplay-tag — 게임플레이 태그 (Tag 코어 + GE 태그 연동)

- 상태: 🚧 IN PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-10-01 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md) · [aggregator](../aggregator/progress.md).
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
- [ ] 2. GameplayTag 식별자 + 계층 매칭 — 식별자 struct·**노드 트리 빌드(`GameplayTagNode`/`GameplayTagsManager`) 완료**(2026-10-01, → worklog). **매칭(컨테이너 대상 정확/부모 포함)은 미착수** — 노드 `CompleteTagWithParents` 캐시 위에 올림.
- [ ] 3. owned-tag 컨테이너(참조 카운트) + ASC 조회 API
- [ ] 4. `GameplayEffectAsset` 태그 필드 + Spec 전달
- [ ] 5. GE 적용/해제 시 태그 부여/회수 배선
- [ ] 6. GE 적용 요구(Application requirement) 검사
- [ ] 7. 에디터 확인 경로(인스펙터에 owned-tag 표시 등)

## 결정 기록
상세는 → [decisions.md](decisions.md) (착수 시 생성). progress엔 요지 인덱스만.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | 인터페이스=계층 문자열 | 2026-09-26 | 개발자 인터페이스는 UE와 동일한 계층 문자열(`Status.Debuff.Stun`). |
| D3 | 대소문자=무시비교+표기보존(FName식) | 2026-09-29 | 비교는 대소문자 무시, 표시는 원본 표기 보존(canonical/display 이중화). |

### 결정 대기 (유저 확정 필요)
1. **경량 경계** — 레지스트리 유무·형태(없음/정의 에셋/런타임 캐시), 등록 시점(지연 인터닝 vs 정의 에셋 등록). 내부 표현(인터닝 핸들)은 `InternedName`으로 구현돼 있으나 형식 결정은 미확정. **D3(대소문자=무시비교+표기보존)가 canonical/display 이중화(≈name-table)를 전제하므로 이 경계 결정에 제약을 건다 — 함께 확정.**
   - ~~내부 표현(interned string vs int 핸들)~~ **→ 해소(2026-09-28):** `InternedName`(구 `InternedString`) 래퍼 struct로 interned string 채택. 정수 핸들 전환은 `InternedName.Intern` 한 곳으로 국소화해 유보. → worklog.
2. **GE 연동 범위** — 최소(부여+적용요구) ~ 전체(+유지요구+자산+태그기반 제거). **단, Tag 코어는 GA에서도 재사용될 공용 인프라**(유저 확정 방향)이므로 코어는 소비처 중립으로 설계.
3. **태그 조회 입력 ergonomics** (checkpoint, 2026-10-01) — `GameplayTagsManager.RequestGameplayTag`(현재 본문 미구현 — 맵 조회 + 미등록 처리 예정)가 문자열을 어떻게 받을지 미정. 후보: ① `implicit operator string→InternedName` ② `RequestGameplayTag(string)` 오버로드 ③ `new InternedName(...)` 명시 요구. **확인한 사실:** 지금 `InternedName`·`GameplayTag`엔 implicit operator가 없고 명시 생성자만 있음 → `RequestGameplayTag("...")`는 현재 컴파일 불가. intern 비용은 문자열 조회 시 ①②③ 모두 동일(맵 키가 `ReferenceEquals` 기반이라 인터닝 필수), 차이는 그 비용의 **가시성**뿐. 결정은 **주 call-site 형태**(코드 문자열 리터럴 빈도 vs 이미 핸들 보유)에 달림 — UE는 FName 암묵 생성에 의존. D1(계층 문자열 인터페이스)과 연결. **제안 기울기:** blanket implicit보다 명시(① 지양). 유저 확정 전.

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- **매니저 비-void 빈 스텁 4개로 어셈블리 컴파일 불가** — `AddNativeGameplayTag`·`RequestGameplayTagParents`·`RequestGameplayTagChildren`·`RequestGameplayTagDirectParent` 반환 누락. 다음 작업 1에서 해결(self-resolvable).

## 다음 작업
1. **[컴파일 복구] 매니저 비-void 빈 스텁 해결** (블로커) — `AddNativeGameplayTag`·`RequestGameplayTagParents`·`RequestGameplayTagChildren`·`RequestGameplayTagDirectParent` 반환 누락으로 어셈블리 컴파일 불가. placeholder(`NotImplementedException`) 또는 구현. **선행:** `RequestGameplayTagDirectParent()`는 파라미터(`GameplayTag`) 추가. Parents/Children은 노드에 컨테이너·자식 접근자 + 소유권(복사본 반환?) + 빈노드 bounds 가드(②) 결정 필요.
2. **[에셋 생성·검증] 레지스트리 인스턴스** — 에디터에서 Create ▸ Ability System ▸ Gameplay Tag Registry → `Resources/AbilitySystem/GameplayTagRegistry`, `Tags`에 샘플 입력 후 startup 빌드 동작 확인. (유저, 에디터)
3. **계층 매칭** — 노드 `CompleteTagWithParents` 캐시로 컨테이너 정확/부모 포함 질의 + `GameplayTagContainer`(참조 카운트).
4. **[결정대기 3] 태그 조회 입력 ergonomics** 유저 확정 후 `RequestGameplayTag` 입력 경로 확정. → 결정 대기 3.
5. **[D3 구현] 대소문자 무시비교 + 표기 보존** — `InternedName`(단일 필드·case-sensitive)을 canonical/display 이중화로. **경량 경계**(결정 대기)와 묶임. → decisions.md D3.
6. **[검증] 중첩 struct `ISerializationCallbackReceiver` 콜백 실측** — 인스펙터에 `GameplayTag` 필드 저장→로드 후 `Equals` true 확인. 실패 시 fallback(컨테이너 재-intern / `Ordinal`). (에디터) → worklog 2026-09-28.
7. **[Query] `GameplayTagQuery`** — 경량 식 트리 방향으로 기움(복제 없음→바이트코드 불요). 현 DoD는 평면 All/Any/None 수준이라 **우선순위 낮음**. → progress 'UE GAS 원본 클래스'.
8. **GE 연동 범위**(최소~전체) 유저 확정.
