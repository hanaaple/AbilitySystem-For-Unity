# gameplay-tag — 결정 기록 (decisions)

> 상세 기준(맥락·대안·근거·결과·재평가 트리거)은 [feature/HARNESS.md](../../HARNESS.md) '결정 기록 품질 기준'.
> `D#`는 feature 내 고유·불변 코드이며 progress 인덱스·worklog가 `코드(내용)`로 이 표를 가리킨다. **유저가 확정한 결정만** 기록하고, 제안·미결은 progress '결정 대기'에 둔다.

---

## D1 — 개발자 인터페이스는 UE와 동일한 계층 문자열

- **날짜:** 2026-09-26
- **결정:** 개발자가 태그를 authoring·참조하는 인터페이스는 UE `FGameplayTag`와 동일한 **계층 문자열**(`Status.Debuff.Stun`)로 간다.
- **맥락(forces):** GameplayTag 도입 시 개발자 노출 방식을 정해야 함. 후보 인터페이스는 계층 문자열 / SO 참조 / enum.
- **대안:**
  - 계층 문자열 — UE 패리티, 계층 매칭 자연스러움 / 오타·미등록 위험, 컴파일타임 타입 안전 없음.
  - SO 참조 — 오타 없음·드래그앤드롭 / 계층·에셋 관리 부담, UE 사용 경험과 이질적.
  - enum — 최단순 / 계층·동적 확장 불가.
- **근거:** 유저 확정 — "UE와 같이 개발자가 사용하는 데 있어서 계층 문자열로 동일하게 갈 것". UE 개발 경험 패리티가 이유. (그 외 근거는 유저 미언급.)
- **결과·트레이드오프:** 계층 매칭이 자연스럽고 UE 지식이 그대로 이전됨. **포기한 것:** 문자열 오타/미등록 리스크(→ 드로어·검증·레지스트리 등 완화책이 별도로 필요) + 컴파일타임 타입 안전.
- **재평가 트리거:** 내부 표현을 SO/enum 기반으로 확정할 때, 그 표현이 문자열 인터페이스 노출을 그대로 지원하는지 재검토.

## D2 — (제거됨)

유저 확정 없이(명시 근거 미기록) 에이전트 제안이 결정으로 올라가 있어 내렸다 — decisions는 **유저 확정만**(상단 규칙). 내부 표현(인터닝 핸들)은 코드로 구현돼 있고(`InternedName` → worklog), 남은 인프라 선택은 progress '결정 대기 — 경량 경계'에 있다. 코드 `D2`는 재사용하지 않는다(재번호 금지).

## D3 — 대소문자: 무시 비교 + 표기 보존 (UE FName식)

- **날짜:** 2026-09-29
- **결정:** 태그 대소문자는 UE `FName`처럼 **비교는 대소문자 무시, 표시는 원본 표기 보존**으로 간다. 즉 식별은 정규화(폴딩)된 형태로 하되 authoring 표기를 유지하는 **표현 이중화(canonical / display)**. 소문자로 접어 저장(표기 손실)하는 방식은 배제.
- **맥락(forces):** D1(문자열 인터페이스)과 인터닝 핸들 내부 표현(→ progress '결정 대기 — 경량 경계') 위에서, 같은 태그의 대소문자 차이를 같게 볼지 정해야 함. 특히 코드에서 태그 문자열을 손으로 칠 때의 혼동.
- **대안:**
  - case-sensitive + 레지스트리 검증 — 단순, InternedName 그대로, 오타는 "미등록"으로 명확히 걸림 / 코드에서 대소문자까지 정확히 써야 함.
  - 타입 있는 접근(UE native tag식, 심볼 참조) — 코드에 문자열이 없어 대소문자 무의미·자동완성·리팩터 안전 / 등록 태그를 정적 접근자로 노출하는 장치 필요.
  - **무시+보존(FName식) 채택** — 코드의 대소문자 혼동을 흡수 + 표기 보존 / name-table(canonical+display 이중화) 복잡도 선지불.
- **근거:** 유저 확정 — editor는 관리 중인 태그명을 드롭다운에서 선택(Add)하므로 문제없으나, **코드에서 대소문자 혼동 가능성**이 있어 이를 흡수하되 표기는 보존하는 FName 형식을 택함. (그 외 근거는 유저 미언급.)
- **결과·트레이드오프:** 코드에서 대소문자 오타가 조용히 흡수되고 표시는 authoring 표기가 유지됨(가독성). **포기한 것:** ① 표현 이중화(canonical 폴딩 + display) → 현재 단일 필드 인터닝(InternedName)으로는 부족, name-table/레지스트리를 선지불(과설계 리스크는 '경량'으로 억제) ② 잘못된 표기를 바로잡지 않고 조용히 수용 — 타입 있는 접근이 주는 컴파일타임 안전·자동완성은 얻지 못함.
- **재평가 트리거:** 코드가 태그를 대부분 심볼(타입 있는 접근)로 참조하게 되면 관대한 조회의 값이 낮아지므로 case-sensitive+검증으로 축소 재검토.
- **관련(coupled):** FName식은 canonical/display 이중화를 담을 name-table을 전제하므로 '경량 경계'(결정 대기) 확정 시 함께 설계. 등록 시점(지연 인터닝 vs 정의 에셋)은 여전히 미결 → progress '결정 대기'.

## D4 — 질의 식(`GameplayTagQueryExpression`)은 class + 자기 반환 체이닝 (class는 잠정)

- **날짜:** 2026-10-11
- **결정:** `GameplayTagQueryExpression`을 **class**로 둔다(유저 표현상 **잠정("일단")**). 수정 메서드(식 유형 설정·`AddTag`·`AddTags`·`AddExpr`)는 **자기 자신을 반환**해 UE식 체이닝을 지원한다. (같은 날 처음엔 "체이닝 신경쓰지 말고"로 `void`였다가, 이어서 유저가 체이닝으로 변경.)
- **맥락(forces):** Expression은 Query를 조립할 때만 쓰는 임시 식 트리(`ExprSet`·`TagSet` 두 `List` + 재귀). UE의 struct는 C++ 값 의미론(`TArray` 깊은 복사, `Expr&` 반환 체이닝)에 기대는데, C# struct로는 둘 다 재현되지 않는다. 동시에 C# 임시 객체는 RAII처럼 즉시 해제되지 않고 GC로 넘어간다는 불편(유저 제기)이 있었다.
- **대안:**
  - struct — 식 객체 할당 1개 절약 / `List` 참조만 복사돼 "절반 공유" 사본, 사본에 수정 메서드가 조용히 먹는 mutable struct 함정, 기본값 리스트 null(C# 9: 매개변수 없는 struct 생성자 불가).
  - **class 채택** — 사본 함정 없음, 필드 초기화로 리스트 항상 존재 / 노드마다 GC 할당(조립 시), `AddExpr`가 UE(복사)와 달리 참조를 담음, null 인자 가능.
  - `ref struct` writer(stackalloc 토큰 + `ArrayPool` 태그, `using`으로 반납) — 조립 시 garbage 0·결정적 반납(RAII 근사) / 재귀 트리 불가 → `BeginExpr`/`EndExpr` 평면 API로 UE에서 이탈, `GetQueryExpr` 불가, 버퍼 성장·반납 관리 직접 구현. `GameplayTag`는 managed(`InternedName`의 `string`)라 stackalloc 불가.
- **근거:** 유저 확정 — class: "class로 두면 … 크게 상관없는거 아는데 … 일단 class로 두자". 체이닝: "return도 다 자기 자신 해서 chainning 하게 해두고". (그 외 근거는 유저 미언급.)
- **결과·트레이드오프:** 단순한 참조형 트리로 시그니처·필드를 UE와 가깝게 유지하고 UE식 한 줄 조립이 가능. class라 `return this`가 원본을 가리켜 struct의 사본 반환 함정이 없음. **포기한 것:** 조립 시 GC 할당(노드 + 리스트), `AddExpr` 복사 의미론. code-guide "연쇄 호출"과의 충돌 우려는 규약 문구가 의도(객체를 건너가는 연쇄 금지)보다 넓게 쓰인 탓이었음 — 유저 설명대로 문구를 고쳐 해소(LINQ·빌더식 fluent 허용).
- **재평가 트리거:** (논의에서 도출 — 유저 확정 아님) 런타임 Query 조립이 GC 문제로 프로파일러에 잡히면 `ref struct` writer 재검토. Query 내부 표현(토큰 스트림 vs 경량 식 트리, → progress '다음 작업 7') 확정 시 함께 재검토.

## D5 — `GameplayTag` 생성 차단: Request 입구만 + 매니저 전용 생성 함수 하나(`internal`)

- **날짜:** 2026-10-12 (같은 날 개정 — 아래 '개정')
- **결정:** `GameplayTag`의 public 생성자를 없애고 **private 생성자 하나**로 닫는다. 외부가 태그를 얻는 길은 **이름 조회(`RequestGameplayTag`)**와 **native 정의(→D6 native 태그=코드 정의+저장 안 함)**뿐이다. 매니저 내부 생성은 **`internal static GameplayTag Create(InternedName)` 함수 하나**로 연다 — 지금은 단일 어셈블리라 실제로 막지 못하는 걸 감수하고, asmdef 분리 때 이 함수의 `internal`이 그대로 실효를 갖게 한다.
- **개정:** 처음엔 **팩토리 전달**(static 생성자가 `n => new GameplayTag(n)`을 `GameplayTagsManager.RegisterTagFactory`에 넘김 + 매니저가 `RuntimeHelpers.RunClassConstructor`로 실행 강제)로 구현했으나, 유저가 "이렇게 하지 말고 함수 하나만 뚫어놓고 나중에 바꾸는 거로 — 지금거 괜히 복잡해"로 변경. 팩토리 코드는 제거.
- **맥락(forces):** 기존엔 `new GameplayTag("...")`로 미등록 태그를 아무 데서나 만들 수 있었다(`Test.cs`). UE는 `FGameplayTag(const FName&)`을 protected로 막고 friend(매니저·리다이렉터·`FNativeGameplayTag`·컨테이너)만 생성하게 한다(`GameplayTagContainer.h:230-240`). C#엔 friend가 없고, asmdef가 없어 전 코드가 Assembly-CSharp 단일 어셈블리라 `internal`도 아무것도 막지 못한다. 플러그인화(asmdef)는 유저가 미룸.
- **대안:**
  - asmdef 분리 + `internal` 생성자 — 컴파일러 강제, 정석 / 미룬 패키지화 작업(Common asmdef, feature-local `Editor/` 폴더 처리)을 당겨와야 함.
  - Passkey(생성자가 매니저만 만들 수 있는 키 객체 요구) — 기존 구조 유지 / `null` 전달이 컴파일돼 런타임에서야 걸림.
  - 노드 지연 생성(트리는 이름만, 태그는 첫 접근 시 Request로) — 우회 장치 없음 / 노드 구조 변경(구성 시 1회 → 첫 접근 시 1회).
  - 팩토리 전달(처음 채택 → 개정으로 폐기) — 생성자 호출이 지금도 컴파일 단계에서 막힘 / static 생성자 실행 시점에 기대 `RunClassConstructor`로 강제해야 하는 우회 장치라 복잡, asmdef 분리 시 버릴 코드.
  - **`internal` 생성 함수 하나 채택(개정)** — 단순, asmdef 분리 시 코드 변경 없이 실효 / 그 전까지는 같은 어셈블리 어디서든 `Create`를 부를 수 있어 규약(주석)에만 기댐.
- **근거:** 유저 확정 — "생성자 막고 Request 경로만 내비두고". 내부 생성은 처음 "팩토리 전달" 선택 후 "괜히 복잡해"로 함수 하나 + 나중 교체로 변경. 생성을 막는 **이유는 유저 미언급**.
  > 근거 미기록(유저 확정 전) — 막는 목적(오타 방지 / UE 정책 일치 / 데이터 무결성 등) 유저에게 확인 필요.
- **결과·트레이드오프:** 외부 코드는 `new GameplayTag(...)`를 쓸 수 없고 Request·native로 얻는다(빈 태그 `default`는 예외 — "태그 없음"). **포기한 것:** asmdef 분리 전까지 `Create`가 실제로 막히지 않아 미등록 태그 생성이 규약 위반으로만 남는다. 로드 경로(Unity 역직렬화는 생성자를 우회)로 들어오는 미등록 이름도 막지 못한다 — UE도 이 경로는 보존+경고로 처리하며 우리는 미착수.
- **재평가 트리거:** AbilitySystem asmdef 분리(플러그인화) 시 `Create`의 `internal`이 실효를 가지는지 확인(테스트 어셈블리가 필요하면 `InternalsVisibleTo`).

## D6 — native 태그: 코드 정의(필드 마커) + 저장 안 함(구성마다 탐색) + 에디터에도 매니저

- **날짜:** 2026-10-12
- **결정:** 코드에서 `[NativeTag] public static readonly NativeGameplayTag X = new("A.B");`로 태그를 정의한다. 매니저는 트리 구성 시 마커 필드를 탐색해 **native 태그를 먼저, 레지스트리 에셋 태그를 나중에** 넣는다(같은 경로는 한 노드로 합쳐짐). native 태그는 **레지스트리 에셋에 저장하지 않는다**. 매니저는 `Instance` 최초 접근 시 생성돼 **에디터에서도 존재**한다(플레이 진입 시 `Bootstrap`이 재생성하는 동작은 유지).
- **맥락(forces):** 유저 요구 — "native code에서 등록, 엔진 에디터 타임에 매니저로 등록". UE(5.6 원문): `UE_DEFINE_GAMEPLAY_TAG`가 전역 `FNativeGameplayTag`를 만들고 생성자가 자기 등록(`NativeGameplayTags.cpp:71-75`) → 트리 구성 시 ini·DataTable보다 먼저 추가(`GameplayTagsManager.cpp:543-552`). native 출처 목록은 `RF_Transient`라 저장되지 않음(`:2103-2106`). 매니저는 모듈 시작 시 초기화되어(`GameplayTagsModule.cpp:22-26`) 에디터·게임이 같은 싱글턴을 쓰고, 피커는 매니저 트리를 그대로 보여 줘 native 태그도 드롭다운에 뜬다(이름 변경·삭제만 `.ini` 출처로 제한, `SGameplayTagPicker.cpp:1212-1222`). C#의 static 필드는 타입 첫 접근 시에야 초기화되므로 UE처럼 "로드되면 자동 등록"이 성립하지 않아 **명시적 탐색**이 필요하다.
- **대안:**
  - 저장: (a) **구성마다 탐색, 저장 안 함(채택)** — UE와 동일 / 시작 시 리플렉션 비용, IL2CPP 코드 제거 위험. (b) 에디터에서 탐색해 에셋에 별도 목록으로 굽기 — 런타임 리플렉션 없음 / 사본이 코드와 어긋날 수 있음, UE와 다른 구조.
  - 탐색 마커: 클래스 마커(범위 좁음) / **필드 마커(채택)** — 어느 클래스에든 둘 수 있음 / 필드마다 작성. 마커 없이 전체 스캔 — 작성 편함 / 탐색 비용 큼.
- **근거:** 유저 확정 — 저장은 "UE에서 하는 방향으로 가자", 에디터 시점은 UE 방식 확인 후 설계안(에디터에도 매니저)에 "해봐 일단", 마커는 "필드 마커가 좋아보이긴 하네". (각 선택의 그 외 근거는 유저 미언급.)
- **결과·트레이드오프:** 레지스트리 에셋이 없어도 native 태그로 트리가 구성되고, 에디터에서도 매니저가 있어 이후 드롭다운이 매니저 트리만 읽으면 된다(UE 구조). 마커 없이 만든 `NativeGameplayTag`는 트리에 없어 `Tag`가 빈 태그 → 생성 우회 불가(D5 유지). **포기한 것:** 시작 시 리플렉션 비용(에디터는 `TypeCache`로 완화, 빌드 비용 미측정), IL2CPP 코드 제거로 참조 없는 마커 필드가 빠질 위험(미대응), 에디터 매니저는 레지스트리 편집 후 재컴파일·플레이 전까지 낡음. native 정의 클래스의 static 초기화에서 태그를 조회하면 구성이 재귀하므로 이를 예외로 막는다.
- **재평가 트리거:** 빌드 시작 시 탐색 비용이 프로파일러에 잡히거나 코드 제거로 native 태그가 빠지면 (b) 굽기 또는 `[Preserve]` 대응 재검토. 노드 출처(native/레지스트리)·명시/암시 기록은 편집 UI 착수 시 추가.

## D7 — `GameplayTagContainer`는 class (struct에서 변경)

- **날짜:** 2026-10-12
- **결정:** `GameplayTagContainer`를 struct(public `List` 필드 2개)에서 **class**(`[Serializable]`, private 리스트 + 메서드 API)로 바꾼다. 노드의 `GetSingleTagContainer()`는 트리 캐시 인스턴스를 그대로 반환하되 읽기 전용 약속으로 두고, `GameplayTag.GetSingleTagContainer()`는 UE처럼 **사본**을 반환한다.
- **맥락(forces):** UE `FGameplayTagContainer`는 `TArray` 깊은 복사인 값 타입이다. C# struct에 `List`를 담으면 복사본끼리 리스트를 공유해, 노드 캐시 컨테이너의 사본에 `AddTag`하면 매니저 트리 캐시가 조용히 오염된다(D4 질의 식=class+자기 반환 체이닝과 같은 계열의 문제).
- **대안:**
  - struct 유지 — UE와 형태 동일 / 사본 공유 함정.
  - **class 채택** — 참조 의미가 드러나 함정 없음 / 컨테이너마다 GC 할당, null 가능.
- **근거:** 유저 확정 — 에이전트가 제시한 두 선택지 중 권장안(class) 선택. (선택 이유는 유저 미언급.)
- **결과·트레이드오프:** 컨테이너 공유가 코드상 명시적이 됨. **포기한 것:** UE 값 의미론(대입=복사) — 복사가 필요하면 복사 생성자를 명시적으로 쓴다. 노드 캐시는 호출측이 수정하면 여전히 오염되므로 "읽기 전용" 약속(주석)에 기댄다. `Equals`/`GetHashCode`는 순서 무관(UE `operator==`)이지만 내용이 바뀌면 해시도 바뀌어 해시 키로 쓰면 안 된다.
- **재평가 트리거:** 컨테이너 할당이 프로파일러에 잡히면 struct + 불변/풀링 재검토.

## D8 — 구현된 Tag API의 이름·시그니처를 UE 원본에 맞춘다 (기존 함수만, 미지원 인자는 `NotImplementedException`)

- **날짜:** 2026-10-14
- **결정:** 이미 구현한 Tag 네임스페이스 함수의 **이름·인자**를 UE 5.6 원본에 맞춘다. **UE에만 있는 함수는 추가하지 않는다.** 맞춘 시그니처 중 지금 지원할 수 없는 부분은 `NotImplementedException`을 던지는 껍데기로 둔다.
- **맥락(forces):** 유저가 "함수명이나 그런 거까지 따라가려는데 그게 되려 나를 붙잡는 거 같다"고 한 직후 내린 결정. 대조 결과(`GameplayTagContainer.h`·`GameplayTagsManager.h`·`NativeGameplayTags.h`) 대부분은 이미 일치했고, 어긋난 것은 `Instance`(UE `Get()`)·`GameplayTag.TagName`(`GetTagName()`)·`NativeGameplayTag.GameplayTag`(`GetTag()`)·`MakeQueryMatch*`(`MakeQuery_Match*`)·반환형 `GetQueryExpr()`(UE는 out 인자, `Read`가 식을 새로 채움 — `GameplayTagContainer.cpp:213-216`)·`RequestGameplayTag`의 `ErrorIfNotFound` 누락(`GameplayTagsManager.cpp:2207-2215`)·`Reset(Slack)`·`AddChildrenTags`의 `RecurseAll`/`OnlyIncludeDictionaryTags`·노드 생성자(UE는 전체 이름 `FName`을 받아 노드가 태그를 만듦)였다.
- **대안:**
  - UE public 전부 — 시그니처 완전 대응 / 80개 이상, 네트워크·쿡·에디터·ini처럼 Unity 대응 타입이 없어 시그니처를 지어내야 함.
  - Unity에 의미 있는 것만 추가(약 25개) — 기능 대응 확대 / 쓰일지 모르는 API 증가.
  - **기존 함수만(채택)** — 이름 대응 비용이 거의 없고 범위가 늘지 않음 / UE에만 있는 함수는 계속 비어 있음.
- **근거:** 유저 확정 — "지금 구현해둔거 UE의 함수에 맞춰서 해줘. 안되면 그 시점에서 빈 껍데기로 냅두고. 특히 기존 함수는 매칭되게", 범위는 "기존 있는 함수들만 해둬", 껍데기 형태는 에이전트 권장안(`NotImplementedException`) 선택.
  > 근거 미기록(유저 확정 전) — UE 이름에 맞추는 목적(UE 사용자 가독성 / 원문 대조 비용 절감 등)은 유저 미언급.
- **결과·트레이드오프:** UE 문서·코드를 보며 같은 이름으로 찾을 수 있다. `RequestGameplayTag`의 기본값이 UE처럼 "미등록이면 에러 로그(이름당 1회)"가 되어, 마커 없는 `NativeGameplayTag.GetTag()` 등 예전엔 조용하던 경로가 에러를 낸다. **포기한 것:** C# 관용 — 싱글턴 접근이 프로퍼티가 아닌 `Get()`, `MakeQuery_*`의 밑줄. 지원 못 하는 인자는 런타임 예외라 컴파일 단계에서 걸리지 않는다(`AddChildrenTags(onlyIncludeDictionaryTags: true)` — 노드가 명시/암시 등록을 기록하지 않음).
  - **맞추지 않은 것:** UE 대응이 없는 우리 헬퍼(`GameplayTag.Create`, 노드 `TryFindChild`·`AddChildNode`, 매니저 `AddTagToTree`·`GetSimpleNamesFromLeaf`·`CreateManager`·`Bootstrap`, `NativeGameplayTag.TagName`). 노드 생성자의 에디터 전용 플래그 3개(`bIsExplicitTag`·`bIsRestrictedTag`·`bAllowNonRestrictedChildren`)는 받아도 읽을 함수가 범위 밖이라 생략. `ConstructGameplayTagTree`·`DestroyGameplayTagTree`는 UE에선 public이지만 접근 수준은 private 유지. `GameplayTagContainer.ToString()`의 출력 형식은 UE(ImportText 형식)와 다르게 유지.
- **재평가 트리거:** (논의에서 도출 — 유저 확정 아님) GA·GE 구현 중 UE에만 있는 함수가 실제로 필요해지면 그때 추가.

## D9 — 쓸모 있는 UE 전용 함수는 빈 시그니처로 추가 (D8 범위 확장) + UE 5.6 이후 함수 허용

- **날짜:** 2026-10-15
- **결정:** D8의 "UE에만 있는 함수는 추가하지 않는다"를 고친다. 이미 미러링한 클래스(`GameplayTag`·`GameplayTagContainer`·`GameplayTagQuery`·`GameplayTagQueryExpression`·`GameplayTagNode`·`GameplayTagsManager`)의 UE 함수 중 **우리에게도 쓸모 있는 것은 시그니처만 만들고 본문은 `NotImplementedException`**으로 둔다. 또 UE 5.6엔 없고 **이후 버전에 있는 함수**(`GameplayTagQueryExpression.AnyTagsExactMatch`·`AllTagsExactMatch`)도 유지한다.
- **맥락(forces):** 에이전트가 UE 5.6 GameplayTags 모듈 헤더와 우리 public API 이름을 대조(정규식 추출·`WITH_EDITOR` 제외, 이름만 비교)해 "쓸모 있을 것 / 제외해도 될 것(네트워크·UE 직렬화·deprecated·JSON)"으로 나눠 제시한 뒤 내린 결정. 대조에서 `*ExactMatch` 두 함수가 5.6 헤더에 없어(`ExactMatch` grep 0건) D8과 어긋나 보였다.
- **대안:**
  - D8 유지(추가 안 함) — 범위가 안 늘어남 / 필요할 때마다 UE 원문을 다시 대조해야 함.
  - **쓸모 있는 것만 빈 시그니처(채택)** — 앞으로 채울 자리가 코드에 드러남 / 호출하면 런타임 예외라 컴파일에서 안 걸림, 쓰일지 모르는 API가 늘어남.
  - 바로 구현 — 동작까지 확보 / 당장 소비처가 없어 범위만 커짐.
- **근거:** 유저 확정 — "쓸모있는건 빈 함수로 시그니처만 만들어두고 Exact는 5.6에 없긴 해도 다음 버전에 있어서 넣긴 할거야".
  > 근거 미기록(유저 확정 전) — 빈 시그니처로 미리 두는 목적은 유저 미언급. `*ExactMatch`가 들어간 UE 버전은 유저 진술이며 원문 미확인(로컬 소스는 5.6.0뿐).
- **결과·트레이드오프:** 추가한 것 — `GameplayTag`: `operator <`(UE `CompareWithNumericSuffix`)·`IsValidGameplayTagString`. 매니저: `IsValidGameplayTagString`·`DoneAddingNativeTags`·`CallOrRegister_OnDoneAddingNativeTagsDelegate`·`SplitGameplayTagFName`·`RequestAllGameplayTags`. **UE와 다른 점:** `IsValidGameplayTagString`의 선택적 포인터 인자(`FText*`·`FString*`)는 C#에 선택적 `out`이 없어 "인자 없음 / `out` 둘" 두 오버로드로 나눔. `operator >`는 C#이 `<`와 쌍을 요구해 UE에 없는데도 둠. `CallOrRegister_*`는 `FDelegateHandle` 대신 `void` 반환(등록 해제 불가). **이번 범위 밖:** `RemoveTagByExplicitName`은 UE에서 protected이고 리다이렉터 전용(`GameplayTagContainer.h:684-690`)이라 에이전트 판단으로 제외. 모듈의 다른 클래스(태그 소스·리다이렉터·restricted·`IGameplayTagAssetInterface`)는 함수 단위가 아니라 범위 밖.
- **재평가 트리거:** (논의에서 도출 — 유저 확정 아님) 빈 시그니처가 계속 소비처 없이 남거나, 우리 구조(native를 트리 구성 시 한꺼번에 수집 — D6)와 개념이 안 맞는 함수(`DoneAddingNativeTags` 류)를 채울 때 정의가 막히면 삭제 검토.

## D10 — 태그 드로어는 UE 에디터 동작을 최대한 따른다 (Categories 필터·다중 오브젝트 편집 포함)

- **날짜:** 2026-10-15
- **결정:** `GameplayTag`·`GameplayTagContainer` PropertyDrawer는 UE 5.6 `SGameplayTagCombo`·`SGameplayTagContainerCombo`·`SGameplayTagPicker`(`Plugins/Editor/GameplayTagsEditor`)의 동작을 따른다. 따르는 것: 체크·해제 규칙(체크하면 명시 부모를 빼고, 해제하면 형제가 하나도 없을 때 부모를 넣고, 하위는 모두 해제 — `SGameplayTagPicker.cpp:784-879`), 칩 표시와 우클릭 메뉴(Copy·Paste·Clear), 피커 헤더(Clear Selection·Manage Gameplay Tags...), 검색, Expand/Collapse All, 펼침 상태 저장, 열 때 현재 태그까지 펼쳐 보여 주기, 열 때 미등록 태그 제거 경고(`VerifyAssetTagValidity`). 여기에 더해 UE `meta=(Categories="...")`에 대응하는 **필드 속성**(피커 루트를 제한)과 **다중 오브젝트 편집**(오브젝트별 값, Multiple Values 표시, 체크박스 미정 상태)을 넣는다.
- **맥락(forces):** 유저 요구 — "어느 클래스나 struct의 Field로 들어가도 작동 가능하게", "기능은 최대한 UE와 유사하게". 에이전트가 UE 원문을 확인한 뒤 범위가 갈리는 두 지점(Categories, 다중 편집)을 선택지로 물었다.
- **대안:**
  - Categories: **포함(채택)** — 필드별로 고를 수 있는 태그를 좁힘 / 런타임 속성 타입이 하나 늘어남. 제외 — 범위가 작음 / 모든 필드가 전체 트리를 봄.
  - 다중 편집: **UE처럼 지원(채택)** — 여러 에셋을 한 번에 고침 / 오브젝트마다 `SerializedObject`를 따로 만들어 읽고 써야 해 코드가 늘고, 다중 선택 중엔 그릴 때마다 그 비용이 든다. 표시만·편집 막음 — 단순함 / UE와 다름.
- **근거:** 유저 확정 — 위 요청 + 선택지에서 Categories "포함", 다중 편집 "UE처럼 지원" 선택.
  > 근거 미기록(유저 확정 전) — 두 선택의 이유는 "최대한 UE와 유사하게" 외에 유저 미언급.
- **결과·트레이드오프:** UE 사용자가 같은 조작으로 쓸 수 있다. **포기한 것:** UE 동작 중 직관적이지 않은 부분도 그대로 따른다 — 단일 태그 필드에서 `A.B.C`를 해제하면 형제가 없을 때 `A.B`가 된다(UE `OnTagUnchecked`). 미등록 태그는 피커를 여는 순간 데이터에서 지워진다.
  - **UE에서 가져오지 않은 것:** Search For References(레퍼런스 뷰어 없음), 태그 DevComment 툴팁·태그 소스 표시 대부분, restricted 태그 — 대응할 데이터가 없음. `ShowOnlyLeafTag`·`GameplayTagFilter` meta, `CategoryRemapping`, 검색어 하이라이트 — 에이전트 판단으로 뺌(유저 미확정).
- **재평가 트리거:** (에이전트 제안 — 유저 확정 아님) 다중 선택 편집이 눈에 띄게 느려지면 오브젝트별 읽기를 캐시한다.

## D11 — 피커가 루트 노드를 얻는 입구 = 매니저에 UE `GetFilteredGameplayRootTags` 추가

- **날짜:** 2026-10-15
- **결정:** `GameplayTagsManager`에 UE와 같은 이름의 `GetFilteredGameplayRootTags(필터 문자열, 결과 목록)`를 에디터 전용으로 추가한다(UE 5.6 `GameplayTagsManager.h:670-672`, `WITH_EDITOR`). 동작은 UE대로 — 쉼표로 나눈 필터마다 단순 이름 경로를 따라 내려가 일치 노드를 모으고, 하나도 일치하지 않으면 전체 루트를 돌려준다(`GameplayTagsManager.cpp:1567-1640`). D10(Categories 필터)의 값이 이 필터 인자로 들어간다.
- **맥락(forces):** 매니저의 루트 노드가 private이라 에디터가 트리를 훑을 입구가 없었다(`RequestAllGameplayTags`는 D9 빈 시그니처).
- **대안:**
  - **UE 함수 추가(채택)** — UE 피커와 같은 호출 / 런타임 클래스에 에디터 전용 API가 늘어남.
  - 에디터에서 리플렉션으로 private 필드 접근 — 런타임 코드 무변경 / 필드 이름이 바뀌면 컴파일 에러 없이 깨짐.
- **근거:** 유저 확정 — 에이전트 권장안 선택.
  > 근거 미기록(유저 확정 전) — 선택 이유는 유저 미언급.
- **결과·트레이드오프:** 에디터 전용(`#if UNITY_EDITOR`) 블록이라 빌드에는 들어가지 않는다. **포기한 것:** UE 설정의 `CategoryRemapping`(카테고리 이름 바꿔 치기)은 대응 설정이 없어 넣지 않는다.
- **재평가 트리거:** 런타임에서도 루트 목록이 필요해지면 에디터 전용 블록 밖으로 뺀다.

## D12 — 태그 관리 기능 포함: 피커·관리 창에서 레지스트리 에셋을 편집한다

- **날짜:** 2026-10-15
- **결정:** UE 피커의 태그 관리 기능을 넣는다 — `+` 새 태그 추가(선택된 행이 있으면 그 하위로), 행 메뉴의 Add Sub Tag·Duplicate Tag, 관리 창(UE Gameplay Tag Manager)의 Rename·Delete, 진입점 "Manage Gameplay Tags..."·"Open Tag in Manager...". 저장 위치는 레지스트리 에셋(UE의 ini 대응)이고, 편집 뒤 에디터의 매니저 트리를 다시 만든다(UE `EditorRefreshGameplayTagTree`). 태그 문자열 검증은 D9에서 빈 시그니처로 둔 `IsValidGameplayTagString`을 UE대로 구현해 쓴다.
- **맥락(forces):** 2026-10-15 레지스트리 인스펙터를 읽기 전용으로 막아 에디터에서 태그를 넣을 경로가 없었다(native만 가능). 에이전트는 "이번엔 제외(레지스트리 편집 도구는 드로어 다음 후보)"를 권장했으나 유저가 포함을 골랐다.
- **대안:**
  - 이번엔 제외 — 범위가 작고 읽기 전용 방침 유지 / 태그 정의는 계속 코드(native)로만.
  - **포함(채택)** — 에디터에서 태그를 정의할 수 있음 / 레지스트리 읽기 전용 방침이 바뀌고 범위가 커짐.
- **근거:** 유저 확정 — "포함" 선택.
  > 근거 미기록(유저 확정 전) — 선택 이유는 "최대한 UE와 유사하게" 외에 유저 미언급.
- **결과·트레이드오프:** 레지스트리 태그를 에디터에서 추가·이름 변경·삭제할 수 있다. **포기·한계:**
  - native 태그는 바꿀 수 없다(코드 정의). UE도 ini 출처 태그만 수정할 수 있다(`CanModifyTag`).
  - 암시 태그(중간 부모로만 생긴 노드)는 지울 수 없다 — UE와 같다("remove children manually").
  - **이름 변경에 리다이렉터가 없다.** UE는 옛 이름→새 이름 리다이렉터를 남겨 기존 참조가 따라가지만, 우리는 기존 에셋의 옛 이름이 미등록 태그가 된다(피커를 열면 제거 경고).
  - **삭제 전 참조 검사가 없다.** UE는 에셋 레지스트리로 참조 중인 태그의 삭제를 막는다.
  - 태그 코멘트·소스 선택이 없다 — 레지스트리가 이름 목록뿐이라 넣으려면 데이터 모델을 바꿔야 한다.
  - **에이전트 판단(유저 미확정):** ① 레지스트리 편집은 Undo에 남기지 않는다 — UE도 ini 기록은 트랜잭션 밖이고, Undo로 에셋만 되돌아가면 매니저 트리와 어긋나기 때문. ② 레지스트리 인스펙터의 목록 직접 편집은 계속 막고 "Manage Gameplay Tags..." 버튼만 둔다(UE Project Settings 대응). ③ `A..B`처럼 빈 구간이 있는 이름은 UE와 달리 거부한다 — 우리 트리 구성(`AddTagToTree`)이 빈 이름 노드를 만들기 때문.
- **재평가 트리거:** (에이전트 제안 — 유저 확정 아님) 이름 변경·삭제로 참조가 깨지는 일이 잦으면 리다이렉터·참조 검사를 넣는다(레지스트리 데이터 모델 변경).

## D13 — Modifier Source/Target Tags: UE `FGameplayTagRequirements` 전체를 필드로, 이번엔 판정 미연결

- **날짜:** 2026-10-16
- **결정:** `GameplayModifier`에 UE `FGameplayModifierInfo::SourceTags`·`TargetTags` 대응 필드를 넣는다. 타입은 UE `FGameplayTagRequirements` 전체를 옮긴 `GameplayTagRequirements`다(`RequireTags`·`IgnoreTags`·`TagQuery` + `RequirementsMet`·`IsEmpty`·`ToString`·`operator==`·`ConvertTagFieldsToTagQuery`). **이번 단계는 필드와 인스펙터 표시까지만** 하고, GE 적용 경로(Instant/Periodic 실행·aggregator 평가)에서 판정하는 연결은 다음 단계로 미룬다.
- **맥락(forces):** 유저 요청 "Modifier에 Source Tags와 Target Tags 구현해보자. 이번에 한거로". 판정하려면 Source/Target의 소유 태그가 필요한데 ASC에 소유 태그가 아직 없다(progress 다음 작업 8 질문 ③과 겹침). 소유 태그 출처를 물었더니 유저는 "UE에 따를건데 일단, Editor쪽 모양새 채우는 멤버 변수만 채워둬", 되물음에 "판정 없이 필드만"을 골랐다. 요구 구성은 "ue대로 다 구현해".
- **대안:**
  - ASC 소유 태그(카운트 컨테이너+Loose)까지 만들고 판정 연결 — 바로 동작 / 다음 작업 8의 ASC 범위 결정을 앞질러 범위가 커짐.
  - ASC에 임시 인스펙터 태그 필드 — 확인이 쉬움 / UE와 다르고 나중에 교체.
  - **필드만(채택)** — 변경이 작고 에디터 모양을 먼저 갖춤 / 지금은 조건을 적어도 아무 효과가 없다.
  - 요구 구성을 Require·Ignore만(에이전트 권장) — Query 직렬화·편집 UI가 없어서 / 유저는 UE 전체를 골랐다(TagQuery 범위는 D15).
- **근거:** 유저 확정. 소유 태그 출처는 "UE에 따를" 방향만 정하고 형태는 미정.
  > 근거 미기록(유저 확정 전) — "필드만"·"다 구현"을 고른 이유는 유저 미언급.
- **결과·트레이드오프:** GE 에셋 인스펙터에서 modifier마다 Source/Target 조건을 적을 수 있다. **포기·한계:** 판정이 연결되기 전까지 조건은 무시된다(코드에 TODO). UE의 `TokenStreamVersion`·`AutoDescription`이 없어 Query 저장 형식은 UE와 같지 않다(D15).
- **재평가 트리거:** ASC 소유 태그 형태(다음 작업 8)가 정해지면 판정 연결(D14 방식)에 착수한다.

## D14 — 지속형(Duration/Infinite) modifier 태그 판정은 UE 그대로 (다음 단계 설계)

- **날짜:** 2026-10-16
- **결정:** 판정을 연결할 때 UE 5.6 방식을 따른다.
  - Instant/Periodic: 실행할 때마다 대상 소유 태그를 다시 읽고, Source(Spec 생성 때 캡처)·Target 조건을 못 맞춘 modifier는 건너뛴다(`GameplayEffect.cpp:2950-2980`).
  - Duration/Infinite: mod를 조건과 함께 aggregator에 넣고, 평가할 때 평가 파라미터(Source/Target 태그)로 자격을 판정한다(`GameplayEffectAggregator.cpp:28-74` `FAggregatorMod::UpdateQualifies`). CurrentValue 계산은 **빈 태그**로 평가한다(`GameplayEffect.cpp:3311-3315`) — `RequireTags`가 있는 mod는 CurrentValue에 안 들어가고, 다른 GE가 그 어트리뷰트를 캡처해 평가할 때(AttributeBased·Execution) 그 Spec의 태그로만 켜진다(`:983-984`).
- **맥락(forces):** UE는 지속형 경로에서 CurrentValue와 캡처 평가의 태그 문맥이 다르다. 그래서 "버프가 스탯 창에 안 보이는" 동작이 직관적이지 않다. 에이전트가 이 점을 짚고 세 안을 물었다.
- **대안:**
  - **UE 그대로(채택, 에이전트 권장)** — "언데드 상대로만 공격력 +50%" 같은 문맥 조건부 버프를 표현할 수 있음 / CurrentValue에 안 보이는 mod가 생기고, aggregator 평가 시그니처가 바뀐다.
  - 적용 시 1회 판정 — 단순하고 직관적 / UE와 의미가 다르고(이후 태그 변화 무시) 문맥 조건부 버프를 표현할 수 없음.
  - Instant/Periodic만 — 변경 최소 / 지속형 조건은 무시.
- **근거:** 유저 확정 — "UE 그대로".
  > 근거 미기록(유저 확정 전) — 고른 이유는 유저 미언급.
- **결과·트레이드오프:** (예정) `AttributeAggregator.Evaluate`에 평가 파라미터가 생기고, 캡처 조회(`GameplayEffectAttributeCaptureSpec.TryGetCapturedValue`)가 Spec의 Source/Target 태그를 넘겨야 한다. Spec에 캡처 태그(UE `CapturedSourceTags`·`CapturedTargetTags`)가 필요하다. **포기:** 지속형 `RequireTags` mod는 어트리뷰트 CurrentValue로 확인할 수 없다.
- **재평가 트리거:** (에이전트 제안 — 유저 확정 아님) 데모에서 "적용된 버프가 스탯에 안 보인다"가 실제 혼란을 만들면 재검토.
- **후속(2026-10-16):** CurrentValue 부분은 D21(지속형 태그 조건을 CurrentValue에도 적용)로 바뀌었다. Instant/Periodic 건너뛰기와 캡처 평가 경로는 이 결정대로 유지한다.

## D15 — `GameplayTagQuery`: 직렬화 + 판정, 편집 UI는 나중 (인스펙터는 읽기 전용 설명)

- **날짜:** 2026-10-16
- **결정:** `GameplayTagQuery`를 `[Serializable]`로 바꿔 기존 세 필드(`_tagDictionary`·`_queryTokenStream`·`_userDescription`)를 저장한다. `GameplayTagRequirements.RequirementsMet`에 Query 판정을 넣는다. 인스펙터는 질의를 편집하지 못하고 설명 한 줄만 보인다(`GameplayTagQueryPropertyDrawer`, UE `SGameplayTagQueryEntryBox`의 표시 규칙: Empty / Multiple Selected / 설명).
- **맥락(forces):** D13으로 `TagQuery` 필드가 생겼는데 Query는 직렬화도 편집 UI도 없었다. progress 다음 작업 7에 "직렬화 여부·`TokenStreamVersion` 미정"이 열려 있었다.
- **대안:**
  - **필드+판정, 편집 UI 나중(채택, 에이전트 권장)** — 코드로 만든 질의를 에셋에 저장·판정할 수 있다 / 인스펙터에서 질의를 만들 수 없다.
  - 필드만(직렬화 없음) — 변경 최소 / 에셋에 저장 안 됨.
  - Query 편집 UI까지 — UE와 같은 편집 경험 / 별도 세션 규모.
- **근거:** 유저 확정.
  > 근거 미기록(유저 확정 전) — 고른 이유는 유저 미언급.
- **결과·트레이드오프:** 질의가 에셋에 저장된다. **포기·한계:** 인스펙터에서 질의를 만들 수 없어 지금은 코드로만 채울 수 있다. UE의 `TokenStreamVersion`·`AutoDescription`은 넣지 않았다 — 토큰 형식이 바뀌면 저장된 질의를 옮길 장치가 없고, 설명을 자동 생성하지 않아 설명 없는 질의는 "(No Description)"으로 보인다. Query 내부 표현(토큰 스트림 vs 식 트리, 다음 작업 7)은 여전히 열려 있다. 직렬화하면서 표현을 바꾸는 비용은 커졌다.
- **재평가 트리거:** Query 편집 UI에 착수할 때, 또는 Query 내부 표현을 바꿀 때 `TokenStreamVersion` 도입을 함께 정한다.
- **후속(2026-10-16):** D16(Query 자동 설명=UE처럼 저장·Build 때 생성)으로 `AutoDescription`이 생겼고, D17(Query 편집기=UE처럼 별도 창·8종·Categories 전달)로 편집 UI가 생겨 "인스펙터 읽기 전용"은 해소됐다. 위 트리거에 걸렸지만 `TokenStreamVersion`은 아직 묻지 않았다 — 유저 확인 대기.

## D16 — Query 자동 설명: UE처럼 Query에 저장, `Build` 때 생성

- **날짜:** 2026-10-16
- **결정:** `GameplayTagQuery`에 직렬화 필드 `_autoDescription`을 더한다. `Build`(=`BuildQuery`·`MakeQuery_*`)가 토큰을 만들면서 UE 형식(`" ANY( A, B )"`·`" ALL("`·`" NONE("`, 하위 식은 `,`로 잇고 빈 식은 `undefined`)의 설명을 만든다. `GetDescription()`은 UE처럼 사용자 설명이 비었으면 자동 설명을 돌려준다(`GameplayTagContainer.h:898`). `GameplayTagQueryExpression.EmitTokens`에 설명 출력 인자를 더한다.
- **맥락(forces):** Query 편집 창(D17)을 만들며 인스펙터 줄에 보일 설명이 필요했다. 지금은 사용자 설명만 있어 대부분 "(No Description)"으로 보였다. UE는 편집 경로(`BuildFromEditableQuery`, `GameplayTagContainer.cpp:1980-1990`)에서만 자동 설명을 만들어 Query에 저장한다.
- **대안:**
  - 에디터에서만 계산(에이전트 권장) — 런타임 코드 수정 없음 / 런타임 `GetDescription()`은 사용자 설명만.
  - **UE처럼 Query에 저장(채택)** — 런타임에서도 설명을 얻음, UE 필드 구성에 가까워짐 / `GameplayTagQuery`·`GameplayTagQueryExpression` 수정, 저장 필드 하나 증가.
  - 자동 설명 없음 — 수정 없음 / 설명 없는 질의가 대부분.
- **근거:** 유저 확정.
  > 근거 미기록(유저 확정 전) — 고른 이유는 유저 미언급.
- **결과·트레이드오프:** **UE와 다른 점:** UE는 런타임 `Build`에서 자동 설명을 만들지 않지만(`:1743-1757`), 우리는 `Build` 하나로 만든다. 편집 창도 `Build`를 쓰기 때문이다. 그래서 코드로 만든 질의(`MakeQuery_*`)도 설명을 갖는다. `ReplaceTagsFast`·`ReplaceTagFast`는 UE처럼 설명을 갱신하지 않는다(태그만 바뀌어 설명이 낡을 수 있음). Exact 식(`AnyTagsExactMatch`·`AllTagsExactMatch`)은 UE 5.6에 없어 설명 문구가 원문에 없다 — 에이전트가 `" ANY EXACT("`·`" ALL EXACT("`로 정했다(유저 미확정).
- **재평가 트리거:** 설명 문자열 생성 비용이 런타임 질의 생성에서 문제가 되면 에디터 전용 경로로 옮긴다.

## D17 — Query 편집기: UE처럼 별도 창 + Exact 포함 8종 + Categories 전달

- **날짜:** 2026-10-16
- **결정:** UE `SGameplayTagQueryEntryBox`·`SGameplayTagQueryWidget`을 따른다.
  - 인스펙터 줄: 설명(누르면 편집) + Edit + Clear.
  - 창: "Tag Query Editor: {필드} {에셋}", 사용자 설명 + 루트 식 트리. 식 종류 팝업, 태그는 기존 컨테이너 피커, 하위 식 추가·삭제.
  - 반영: 고치면 즉시 반영, OK 닫기, Cancel 원래 값 복원.
  - 식 종류: 우리 런타임이 지원하는 8종 전부(UE 5.6 에디터는 Exact 없이 6종).
  - Categories: Query 필드의 `[GameplayTagCategories]`를 창 안 피커에 넘긴다(UE `OnGetCategoriesMetaFromPropertyHandle`, `SGameplayTagQueryWidget.cpp:102-119`).
- **맥락(forces):** D15(Query=직렬화+판정·편집 UI 나중)에서 미뤄 둔 편집 UI. 유저 요청 "이제 TagQuery Editor 구현해봐".
- **대안:**
  - **별도 창(채택, 에이전트 권장)** — UE와 같은 흐름, 깊은 트리도 넓게 편집 / 창 관리(하나만 열기·대상 소실 시 닫기)가 필요.
  - 인스펙터 안에서 펼쳐 편집 — 창 없음 / UE와 다르고 인스펙터가 길어짐.
  - 식 종류 6종(UE 5.6 에디터) — UE와 같음 / 코드로 만든 exact 질의를 창에서 편집할 수 없음.
  - Categories 미적용 — 기존 파일 수정 없음 / UE와 다름.
- **근거:** 유저 확정.
  > 근거 미기록(유저 확정 전) — 고른 이유는 유저 미언급.
- **결과·트레이드오프:** 인스펙터에서 질의를 만들고 고칠 수 있다. Categories 전달을 위해 기존 `GameplayTagDrawerUtility.GetPropertyHandle`에 필터 덮어쓰기 입구를 더한다. **포기·한계:** UE는 편집용 UObject를 식 종류마다 클래스로 두지만, 우리는 식 종류 enum을 가진 클래스 하나로 둔다(런타임 `GameplayTagQueryExpression`과 같은 모양, 에이전트 판단). 읽기 전용 창(UE `bReadOnly`)은 넣지 않는다 — 인스펙터가 비활성이면 버튼도 비활성이라 열 경로가 없다(에이전트 판단).
- **재평가 트리거:** 질의가 깊어져 창 편집이 불편하면 트리 뷰 등 다른 편집 형태를 검토.

## D18 — Modifier 태그 판정의 소유 태그 출처: ASC 조회 함수만 두고 지금은 빈 값

- **날짜:** 2026-10-16
- **결정:** ASC에 UE `GetOwnedGameplayTags`에 대응하는 조회 함수를 둔다. 지금은 빈 컨테이너를 돌려준다(TODO). Spec 생성 때 Instigator(UE `RecaptureSourceActorTags`, `GameplayEffect.cpp:1776-1779`)와 실행·적용 때 Owner(`:2950-2951`·`:3081-3082`·`:4153-4154`)가 이 함수로 태그를 채운다. 실제 소유 태그(카운트 컨테이너·Loose 태그)는 세부 TODO 3에서 한다.
- **맥락(forces):** Modifier Source/Target Tags 판정 연결(유저: "modifier에 gameplaytagRequirements SourceTags, TargetTags 구현 들어가자. ue 호출 경로나 위치 참고하면서") 착수 시점에 ASC 소유 태그가 없었다.
- **대안:**
  - 카운트 컨테이너 같이(에이전트 권장) — 판정이 실제로 동작 / 세부 TODO 3까지 범위가 커짐.
  - **조회 함수만, 빈 값(채택)** — 판정 배선만 끝냄 / 소유 태그가 생기기 전까진 `RequireTags`는 늘 실패, `IgnoreTags`만 있는 조건은 늘 통과.
  - 단순 컨테이너(카운트 없음) — 작음 / UE와 다름.
- **근거:** 유저 확정.
  > 근거 미기록(유저 확정 전) — 고른 이유는 유저 미언급.
- **결과·트레이드오프:** 판정 경로는 UE 호출 위치대로 이어지지만, 세부 TODO 3 전까지 동작 확인은 태그가 빈 경우로 한정된다.
- **재평가 트리거:** 세부 TODO 3(ASC 소유 태그) 착수 시 조회 함수 본문을 채운다.

## D19 — Execution 캡처 조회: UE처럼 평가 파라미터 인자를 받는다

- **날짜:** 2026-10-16
- **결정:** `GameplayEffectExecutionParameters.AttemptCalculateCapturedAttributeMagnitude`에 평가 파라미터(UE `FAggregatorEvaluateParameters`) 인자를 더한다(UE `FGameplayEffectCustomExecutionParameters::AttemptCalculateCapturedAttributeMagnitude(정의, 평가 파라미터, out)`, `GameplayEffectExecutionCalculation.cpp:107`). Execution 작성자가 태그를 넘긴다. BaseValue 조회는 UE처럼 인자가 없다(`GameplayEffectExecutionCalculation.h:85`, mod 판정과 무관). (2026-10-16 정정: 처음엔 BaseValue에도 더한다고 잘못 적었다.)
- **맥락(forces):** 지속형 mod의 태그 자격은 평가 파라미터로 판정된다(→D14 지속형 판정=UE 그대로). Execution도 캡처 어트리뷰트를 평가하므로 태그 문맥을 받을 입구가 필요했다.
- **대안:**
  - **UE처럼 인자 추가(채택)** — UE와 같음, 작성자가 문맥을 정함 / gameplay-effect 코드(`GameplayEffectExecutionParameters`)와 호출부(`Character/SpeedBoostExecution`) 수정.
  - 시그니처 유지, Spec 태그 자동 — 수정 작음 / UE와 다르고 작성자가 태그를 못 바꿈.
  - 이번엔 제외 — 범위 작음 / Execution 캡처는 태그 없이 평가.
- **근거:** 유저 확정.
  > 근거 미기록(유저 확정 전) — 고른 이유는 유저 미언급.
- **결과·트레이드오프:** 다른 feature(gameplay-effect) 코드가 바뀐다 → 그 progress에 사유를 남긴다.
- **재평가 트리거:** 세부 TODO 12(Execution 태그: `PassedInTags`·scoped modifier) 착수 시 함께 본다.

## D20 — Spec의 캡처 태그: UE `FTagContainerAggregator` 구조 그대로

- **날짜:** 2026-10-16
- **결정:** `GameplayEffectSpec`에 `CapturedSourceTags`·`CapturedTargetTags`를 두고, 타입은 UE `FTagContainerAggregator`처럼 ActorTags + SpecTags → 합친 태그(`GetAggregatedTags`)로 한다. modifier 실행 판정은 ActorTags만(`GameplayEffect.cpp:2971-2976`), 캡처 평가는 합친 태그(`:983-984`)를 쓴다.
- **맥락(forces):** 판정 연결에 Spec이 Source/Target 태그를 들고 다녀야 했다.
- **대안:**
  - **UE 구조 그대로(채택)** — GE 에셋 태그(세부 TODO 4)·동적 태그(세부 TODO 11)가 들어갈 자리가 미리 생김 / 지금은 SpecTags가 비어 있는 타입 하나 추가.
  - Actor 태그만(컨테이너 두 개) — 작음 / 에셋 태그가 생길 때 구조를 바꿔야 함.
- **근거:** 유저 확정.
  > 근거 미기록(유저 확정 전) — 고른 이유는 유저 미언급.
- **결과·트레이드오프:** GE 에셋 태그가 생기기 전까지 SpecTags는 비어 있다.
- **재평가 트리거:** 세부 TODO 4(GE 태그 필드) 착수 시 SpecTags를 채운다.

## D21 — 지속형 modifier 태그 조건을 CurrentValue에도 적용 (UE와 다름, D14 일부 대체)

- **날짜:** 2026-10-16
- **결정:** 지속형(period 0) modifier의 Source/Target 조건을 어트리뷰트 CurrentValue 계산에도 적용한다.
  - Target 조건: 소유 ASC의 현재 소유 태그로 판정.
  - Source 조건: 그 mod를 건 GE Spec의 `CapturedSourceTags` Actor 태그로 판정.
  - 캡처 평가(AttributeBased·Execution)는 UE 그대로 캡처하는 Spec의 태그로 판정한다(D14의 이 부분은 유지).
- **맥락(forces):** D14 설명 뒤 유저: "Current Value에 대해 GameplayTagRequirements가 정상적으로 작동하지 않는 것은 ue에서도 의도에 맞지 않는다고 해. 적용되도록 해".
  - UE 5.6 원문 사실: CurrentValue는 빈 평가 파라미터로 계산한다(`GameplayEffect.cpp:3317`·`:3358`).
  - 5.4에서 Instant/Periodic 쪽은 "MustHave/MustNotHave tags did not apply"를 고친 cvar가 들어갔다(`AbilitySystem.Fix.UseModTagReqsOnAllGE`, `:77-79`).
  - 지속형 CurrentValue에 대한 Epic의 입장은 원문에서 확인하지 못했다(유저가 들은 출처는 미언급).
- **대안:**
  - UE 5.6 그대로(D14) — UE와 같음 / Require가 있는 지속형 mod는 CurrentValue에 안 보인다.
  - **CurrentValue도 태그로 판정(채택)** — 버프가 스탯에 보인다 / UE와 다르고, 소유 태그가 바뀌면 다시 계산해야 한다.
- **근거:** 유저 — "ue에서도 의도에 맞지 않는다고 해. 적용되도록 해".
- **에이전트 판단(유저 미확정):**
  - Source 조건에 쓰는 태그를 그 mod를 건 GE의 Source Actor 태그로 정했다. Instant/Periodic 경로(`:2971`)와 같은 데이터다.
  - 소유 태그가 바뀔 때 다시 계산하는 일은 세부 TODO 3(소유 태그 생성)으로 미뤘다. 지금은 소유 태그가 늘 비어 있어 바뀔 일이 없다(→D18 판정 태그 출처=ASC 조회 함수만·빈 값).
- **결과·트레이드오프:**
  - aggregator에 CurrentValue 전용 평가가 하나 더 생긴다(UE `Evaluate(평가 파라미터)` 경로는 그대로 둠).
  - **CurrentValue와 캡처 평가의 태그 문맥이 다르다.** 예: Target Require=`State.Enraged`인 공격력 버프는 Owner가 Enraged면 CurrentValue에 보인다. 하지만 그 공격력을 데미지 GE가 캡처하면 데미지 GE의 Target(맞는 쪽) 태그로 판정돼 빠질 수 있다.
- **재평가 트리거:** 캡처 평가도 같은 문맥으로 맞출지 유저 확인.
