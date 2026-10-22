# gameplay-tag — 결정 기록 (decisions)

> 무엇을·어떻게 적나 → [feature/HARNESS.md](../../HARNESS.md) '결정 기록 기준'. **유저가 확정한 결정만** 적고, 제안·미결은 progress '결정 대기'에 둔다.
> `D#`는 feature 내 고유·불변 코드다. 경위·UE 원문 인용은 [worklog](worklog.md), 2026-10-18 이전 본문 전체는 git `c98c21a`.

---

## D1 — 태그 인터페이스는 UE와 같은 계층 문자열

- 날짜: 2026-09-26
- 결정: 개발자가 태그를 쓰고 참조하는 형태는 UE `FGameplayTag`처럼 계층 문자열(`Status.Debuff.Stun`)이다.
- 왜: 유저 — "UE와 같이 개발자가 사용하는 데 있어서 계층 문자열로 동일하게 갈 것".
- 대안: SO 참조(오타 없음 / 계층 관리 부담·UE와 이질적), enum(가장 단순 / 계층·확장 불가).
- 대가: 오타·미등록 위험과 컴파일 타임 타입 안전 없음 → 드로어·레지스트리로 완화.

## D3 — 대소문자: 비교는 무시, 표기는 보존 (UE `FName`식)

- 날짜: 2026-09-29
- 결정: 태그는 대소문자를 무시하고 비교하되, 쓴 표기를 그대로 보여 준다.
- 왜: 유저 — 에디터는 드롭다운으로 고르니 문제없지만 **코드에서 대소문자를 헷갈릴 수 있어** 흡수하되 표기는 남기는 FName 방식.
- 대안: 대소문자 구분 + 레지스트리 검증(단순 / 코드에서 정확히 써야 함), 심볼 참조(자동완성·안전 / 정적 접근자 장치 필요).
- 대가: 정규화 형태와 표시 형태를 따로 들어야 한다. 잘못 쓴 표기도 조용히 받아들인다.
- 재검토: 코드가 태그를 대부분 심볼로 참조하게 되면. (구현은 미뤄 둠 — TODO-BOARD)

## D4 — 질의 식(`GameplayTagQueryExpression`)은 class + 자기 반환 체이닝 (class는 잠정)

- 날짜: 2026-10-11
- 결정: 질의 식은 class로 두고, 수정 메서드가 자기 자신을 돌려줘 UE처럼 이어서 조립한다.
- 왜: 유저 — "일단 class로 두자", "return도 다 자기 자신 해서 chainning 하게 해두고".
- 대안: struct(할당 1개 절약 / `List` 공유 사본·mutable struct 함정), `ref struct` writer(조립 할당 0 / 재귀 트리 불가라 UE와 멀어짐).
- 대가: 조립할 때 노드마다 GC 할당. `AddExpr`가 UE(복사)와 달리 참조를 담는다.
- 재검토: 런타임 질의 조립이 프로파일러에 GC로 잡히면.

## D5 — `GameplayTag` 생성 차단: Request 입구만 + 매니저 전용 `internal` 생성 함수 하나

- 날짜: 2026-10-12
- 결정: public 생성자를 없앤다. 외부는 `RequestGameplayTag`와 native 정의로만 태그를 얻고, 매니저 내부 생성은 `internal static Create` 하나로 연다.
- 왜: 유저 — "생성자 막고 Request 경로만 내비두고", 팩토리 전달안은 "괜히 복잡해"로 함수 하나 + 나중 교체. 막는 목적은 `근거 미기록`.
- 대안: asmdef 분리 + `internal` 생성자(컴파일러 강제 / 미룬 패키지화를 당겨야 함), 팩토리 전달(지금도 막힘 / static 생성자 실행에 기대는 우회라 복잡).
- 대가: asmdef 분리 전까지 `Create`는 같은 어셈블리에서 실제로 막히지 않는다(규약에만 기댐).
- 재검토: AbilitySystem asmdef 분리 때 `internal`이 실효를 갖는지 확인.

## D6 — native 태그: 코드 필드 마커로 정의, 저장하지 않고 트리 구성마다 탐색, 에디터에도 매니저

- 날짜: 2026-10-12
- 결정: `[NativeTag] static readonly NativeGameplayTag` 필드로 정의한다. 매니저가 트리를 만들 때 마커 필드를 찾아 native → 레지스트리 순으로 넣고, native는 에셋에 저장하지 않는다. 매니저는 에디터에서도 존재한다.
- 왜: 유저 — 저장은 "UE에서 하는 방향으로 가자", 에디터 매니저는 "해봐 일단", 마커는 "필드 마커가 좋아보이긴 하네". C#은 static 필드가 첫 접근 때 초기화돼 UE처럼 "로드되면 자동 등록"이 안 돼 명시적 탐색이 필요하다.
- 대안: 에디터에서 탐색해 에셋에 굽기(런타임 리플렉션 없음 / 사본이 코드와 어긋날 수 있음).
- 대가: 시작 시 리플렉션 비용(빌드 미측정), IL2CPP 코드 제거로 마커 필드가 빠질 위험(미대응).
- 재검토: 빌드에서 탐색 비용이 잡히거나 코드 제거로 태그가 빠지면 굽기·`[Preserve]`.

## D7 — `GameplayTagContainer`는 class (UE는 값 타입)

- 날짜: 2026-10-12
- 결정: 컨테이너를 struct에서 class로 바꾼다. 노드 캐시는 읽기 전용 약속으로 그대로 돌려주고, `GameplayTag.GetSingleTagContainer()`는 사본을 준다.
- 왜: 유저가 에이전트 권장안(class)을 골랐다. 고른 이유는 `근거 미기록`. (배경: C# struct에 `List`를 담으면 사본끼리 리스트를 공유해 노드 캐시가 조용히 오염된다.)
- 대가: UE의 "대입 = 복사"가 없다 — 복사가 필요하면 복사 생성자를 쓴다. 컨테이너마다 GC 할당.
- 재검토: 컨테이너 할당이 프로파일러에 잡히면 struct + 불변/풀링.

## D8 — 이미 만든 Tag API는 UE 이름·시그니처에 맞춘다 (없는 함수는 추가하지 않음)

- 날짜: 2026-10-14
- 결정: 구현된 함수의 이름·인자를 UE 5.6에 맞추고, 지금 지원 못 하는 인자는 `NotImplementedException`으로 둔다. UE에만 있는 함수는 추가하지 않는다(쓸모 있는 것만 빈 시그니처로 — 치운 D9).
- 왜: 유저 — "지금 구현해둔거 UE의 함수에 맞춰서 해줘. 안되면 그 시점에서 빈 껍데기로 냅두고", 범위 "기존 있는 함수들만 해둬". 맞추는 목적은 `근거 미기록`.
- 대안: UE public 전부(완전 대응 / 80개 이상, Unity 대응 타입이 없어 시그니처를 지어내야 함).
- 대가: C# 관용(프로퍼티 대신 `Get()`, `MakeQuery_*` 밑줄)을 포기. 미지원 인자는 런타임 예외라 컴파일에서 안 걸린다.

## D12 — 태그 관리 기능 포함: 피커·관리 창에서 레지스트리 에셋을 편집

- 날짜: 2026-10-15
- 결정: UE 피커의 태그 추가·하위 추가·복제와 관리 창의 이름 변경·삭제를 넣는다. 저장은 레지스트리 에셋(UE ini 대응), 편집 뒤 에디터 매니저 트리를 다시 만든다.
- 왜: 유저 — "포함" 선택(에이전트는 "이번엔 제외"를 권장). 이유는 "최대한 UE와 유사하게" 외 `근거 미기록`.
- 대가: **리다이렉터가 없어** 이름을 바꾸면 기존 에셋의 옛 이름은 미등록 태그가 된다. **삭제 전 참조 검사가 없다.** native·암시 태그는 바꿀 수 없다(UE도 같음).
- 재검토: 이름 변경·삭제로 참조가 깨지는 일이 잦으면 리다이렉터·참조 검사.

## D14 — modifier Source/Target 태그 판정은 UE 경로 그대로

- 날짜: 2026-10-16
- 결정: Instant·Periodic은 실행마다 소유 태그를 다시 읽고 조건을 못 맞춘 modifier를 건너뛴다. 지속형은 mod를 조건과 함께 aggregator에 넣고 평가할 때 평가 문맥의 태그로 판정한다(CurrentValue는 빈 문맥 — D28).
- 왜: 유저 — "UE 그대로". 이유는 `근거 미기록`.
- 대안: 적용 때 1회 판정(단순·직관적 / 이후 태그 변화 무시, "언데드 상대로만 +50%" 같은 문맥 조건부 버프 불가).
- 대가: Require가 있는 지속형 modifier는 스탯(CurrentValue)에 안 보이고, 다른 GE가 캡처해 평가할 때만 켜진다.

## D22 — 소유 태그는 UE `FGameplayTagCountContainer` 구조로 센다 + ASC Loose 태그 입구·태그 이벤트

- 날짜: 2026-10-17
- 결정: 부모 포함 개수 맵·명시 개수 맵·명시 태그 컨테이너를 두고, 태그별 `NewOrRemoved`·`AnyCountChange` 이벤트(자식 변화가 부모 이벤트도 부름)를 낸다. ASC에 Loose 태그 추가·제거·개수 설정과 조회·이벤트 등록 입구를 둔다.
- 왜: 유저 — 구조 "UE와 같은 구조", 이벤트 "UE 두 종류 모두". 이유는 `근거 미기록`.
- 대안: 명시 태그 개수만(코드가 적음 / 부모 태그 개수 조회 불가, 부모를 구독하면 자식 변화를 못 받음).
- 대가: 태그를 바꿀 때마다 부모 체인 컨테이너를 만든다.
- 에이전트 판단(유저 미확정): C#은 이벤트 참조를 돌려줄 수 없어 `Register(태그, 콜백, 종류)`·`Unregister(콜백, 태그, 종류)` 형태.

## D28 — CurrentValue는 UE처럼 빈 문맥, 소유 태그에 따른 on/off는 Ongoing Tag Requirements 담당 (D21·D23 되돌림)

- 날짜: 2026-10-18
- 결정: CurrentValue는 Source/Target 태그 없는 평가 문맥으로 계산한다(`Evaluate(default)`). 대상 소유 태그로 지속형 GE를 켜고 끄는 일은 GE 단위 `TargetTagRequirementsGameplayEffectComponent`(D29)가 맡는다. D21(CurrentValue에도 modifier 조건)·D23(modifier 조건 태그 구독)은 코드째 걷어냈다.
- 왜: 유저 — "GameplayTag CurrentValue에 적용시 UE에 따라 Source/Target 검사하는거 Ongoing Tag Requirements 담당으로 넘기고 가보자. 기존에 선택 치우고". UE: `GameplayEffect.cpp:3311-3317`.
- 대안: CurrentValue에도 modifier 조건 적용(D21, 되돌림 — 버프가 스탯에 보임 / UE와 다르고 modifier 단위 구독·전용 평가가 따로 필요).
- 대가: Ongoing 판정이 들어오기 전까지 소유 태그로 지속형 GE를 켜고 끄는 길이 없다.

## D29 — GE Component 도입 (UE 5.3+ 구조)

- 날짜: 2026-10-18
- 결정: GE 에셋에 `[SerializeReference]` `GameplayEffectComponent` 목록을 두고, 베이스에 UE와 같은 가상 함수 다섯 개를 둔다. 첫 Component는 `TargetTagRequirementsGameplayEffectComponent`(Application·Ongoing·Removal). 처음엔 빈 껍데기(필드·호출 지점만)로 넣었다.
- 왜: 유저 — "UE 처럼 GE Component 도입할거야", 범위 "셋 다", "빈 거로 넣어둬". 이유는 `근거 미기록`.
- 대안: 평면 필드(GE 에셋에 조건 필드 + 컨테이너 함수 / 작지만 Component로 옮길 때 다시 짜야 함).
- 대가: 에셋 안 인스턴스를 모든 적용이 함께 써 Component에 적용별 상태를 둘 수 없다(UE와 같음). 소유 GE 참조 `GetOwner()`는 직렬화하지 않고 GE가 `OnGameplayEffectChanged` 때 채운다(2026-10-19 유저 "UE에 따라 당연히 추가해야지").
- 이후: 판정·inhibit·인스펙터 목록·구체 Component(Asset Tags·Granted Tags·Remove Other)는 2026-10-19~20에 채웠다.

## D30 — Asset Tags는 UE `FInheritedTagContainer` 구조 그대로, 부모 GE는 없음

- 날짜: 2026-10-19
- 결정: `InheritedTagContainer`(Added·Removed·CombinedTags, `UpdateInheritedTagProperties`·`ApplyTo`·`AddTag`·`RemoveTag`)를 UE대로 옮기고, `AssetTagsGameplayEffectComponent`는 부모를 늘 null로 넘긴다.
- 왜: 유저 — "부모 없이 가자 일단". UE 부모는 블루프린트 클래스 계층(CDO 아키타입, `GameplayEffectComponent.h:94-99`)인데 ScriptableObject 에셋엔 그런 계층이 없다. 이 구조를 고른 이유는 `근거 미기록`.
- 대안: 평면 태그 목록(단순 / UE와 필드 모양이 달라짐). 부모 GE 참조 필드 추가(상속 흉내 / UE에 없는 새 개념, 범위 큼).
- 대가: 부모 태그를 물려받아 쌓는 동작이 없고, Removed는 지금 뺄 대상이 없다.
- 재검토: GE끼리 태그를 물려받을 필요가 생길 때.

## 치운 결정

중요하지 않거나, UE를 그대로 옮겨 고를 것이 없었거나, 단계 기록이거나, 뒤집힌 결정이라 본문을 치웠다(유저 2026-10-18). 코드는 재사용하지 않는다.

- **D2** — (제거됨) 유저 확정 없이 올라간 에이전트 제안이었다.
- **D9** (2026-10-15) — 쓸모 있는 UE 전용 함수는 빈 시그니처로 추가, UE 5.6 이후 함수(`*ExactMatch`)도 둔다.
- **D10** (2026-10-15) — 태그 드로어는 UE 에디터 동작을 따른다(Categories 필터·다중 오브젝트 편집 포함).
- **D11** (2026-10-15) — 피커 루트 입구로 매니저에 UE `GetFilteredGameplayRootTags`(에디터 전용).
- **D13** (2026-10-16) — Modifier Source/Target Tags = UE `FGameplayTagRequirements` 전체를 필드로.
- **D15** (2026-10-16) — `GameplayTagQuery` 직렬화 + `RequirementsMet`에 Query 판정.
- **D16** (2026-10-16) — Query 자동 설명을 UE처럼 Query에 저장하고 `Build` 때 만든다.
- **D17** (2026-10-16) — Query 편집기 = UE처럼 별도 창 + Exact 포함 8종 + Categories 전달.
- **D18** (2026-10-16) — 소유 태그 출처 = ASC `GetOwnedGameplayTags`(당시 빈 값, D22로 채움).
- **D19** (2026-10-16) — Execution 캡처 조회가 UE처럼 평가 파라미터 인자를 받는다.
- **D20** (2026-10-16) — Spec 캡처 태그 = UE `FTagContainerAggregator`(Actor + Spec 태그).
- **D21** (2026-10-16) — 지속형 modifier 조건을 CurrentValue에도 적용. D28로 되돌림.
- **D23** (2026-10-17) — 소유 태그가 바뀌면 modifier Target 조건 태그 구독으로 CurrentValue 재계산. D28로 되돌림.
- **D24** (2026-10-17) — Query 편집기 접힘 상태 키는 배열 경로 그대로.
- **D25** (2026-10-17) — None 하위 식은 UE처럼 `undefined`로 저장, 막지 않는다.
- **D26** (2026-10-17) — `GameplayTagQuery`에 `_tokenStreamVersion` 필드 저장.
- **D27** (2026-10-18) — mod 자격 판정을 UE처럼 `UpdateQualifies`·`Qualifies()`로 나눈다.
