# gameplay-tag — 작업 로그 (worklog)

> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다. 시간순 "판단의 이야기"로 쓴다.
> (주의: 아래 기존 두 항목은 오름차순이나, 규약은 '최신이 위로'. 이 항목부터 최신을 위에 둔다.)

## 2026-10-03 — 레지스트리 에셋: 에디터 로드 시 자동 생성 + 수동 Create 메뉴 제거

- **방향(유저).** "고정 경로에 레지스트리 에셋을 코드로 직접 만들고, 외부에서 Create 못 하게 막자"(`~해봐`). 처음엔 "매니저 생성 시"로 지시했으나, 이어서 **"플레이 말고 엔진 켜질 때"**로 트리거를 조정 → 생성 시점을 **에디터 로드/재컴파일**로 옮김. (그 외 근거는 유저 미언급.)
- **핵심 제약.** `AssetDatabase.CreateAsset`은 **에디터 전용**(빌드에 없음). 엔진 로드·재컴파일마다 도는 에디터 훅 = `[InitializeOnLoad]`. 여기서 에셋을 보장해 두면 런타임(플레이·빌드)은 `Load()`로 **읽기만**. UE식 "정의는 영속·빌드는 읽기만"(→worklog 2026-09-30)과 동형.
- **왜 플레이(RuntimeInitializeOnLoad)가 아니라 InitializeOnLoad인가.** 유저 요청 + 설계상 이점: 플레이 진입 시 생성은 "`CreateAsset`→같은 프레임 `Load`" 타이밍이 애매하고 플레이해야만 생김. 에디터 로드 훅은 켜두면 항상 보장되고 런타임 경로를 순수 읽기로 유지.
- **구현.**
  - `GameplayTagRegistryAsset`: `[CreateAssetMenu]` 제거(수동 생성 메뉴 차단), 에디터 전용 `LoadOrCreate()`(Load 실패 시 `EditorAssetPath="Assets/Resources/"+ResourcesPath+".asset"`에 생성·저장) + `EnsureParentFolders()`(CreateAsset은 부모 폴더가 AssetDatabase에 있어야 해 `Assets`부터 한 단계씩 `CreateFolder`).
  - **`Tag/Editor/GameplayTagRegistryInitializer.cs` 신설** — `[InitializeOnLoad]` static 클래스, static 생성자에서 `LoadOrCreate()` 호출. feature-local `Editor/` 폴더라 에디터 어셈블리로 자동 컴파일(asmdef 없음 → Assembly-CSharp-Editor). `#if` 불필요.
  - `GameplayTagsManager.ConstructGameplayTagTree`: 다시 `Load()`만(생성 책임 이관). 런타임은 읽기 전용.
  - 경로는 기존 `ResourcesPath`("AbilitySystem/GameplayTagRegistry") + 기존 `Assets/Resources/` 폴더로 **확정**. 최종 `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`.
  - **차단 범위(절제).** "막기"는 **메뉴 노출 제거**까지(`sealed` 유지). 코드의 `CreateInstance`는 여전히 가능하나 노출 안 됨 — 솔로 규모에 완전 봉인은 과설계라 미도입.
- **선행 이슈 해소 관측.** 2026-10-01 worklog의 '어셈블리 컴파일 불가'(비-void 빈 스텁 4개)는 **현재 코드에서 주석 처리돼 해소**된 상태(이번 세션 밖 변경).
- **⚠ 미검증(확신 낮음).** 에디터 실측 안 함 — Unity MCP 연결 막혀 에이전트가 트리거 못 함(유저가 Unity 포커스 시 재컴파일 → 생성됨). 확인할 것: 에셋이 `Assets/Resources/AbilitySystem/`에 실제 생성되는지, 폴더 자동 생성(`EnsureParentFolders`). **D# 정식화 대기:** 유저가 접근만 지정하고 근거 미언급 → decisions 미등록(원칙 8).

## 2026-10-01 — Tag 트리 코어: Node 생성자 + Manager 빌드·싱글톤 + 조회 API

- **설계 논의(UE 5.6.0 원문 대조).** 트리는 포인터 링크 노드 + 태그→노드 맵(**맵 값이 곧 노드**, 별개 두 트리 아님). 매칭은 트리를 안 타고 노드의 `CompleteTagWithParents` 캐시를 맵으로 읽음. 저장은 **평면 점표기 문자열 유지**(편집·diff·병합 때문, 효율 아님) + **리프만 적어도 중간 부모는 빌드가 파생**. 데이터지향 평면배열은 안 내는 비용(순회 지역성) 최적화라 기각.
- **`GameplayTagNode`.** 생성자 책임 분담을 UE대로 — `(짧은이름, 전체이름, 부모)`를 받고 노드는 분할·부모탐색 안 함(매니저가 함). `CompleteTagWithParents`(완전태그[0] + 펼친 부모 캐시) 구성. **루트=빈 센티널**(`_tag`=default ≈ NAME_None), 부모 체인 가드 `!parent._tag.Equals(default)`로 루트 제외(UE `GetSimpleTagName() != NAME_None` 대응). `TryFindChild`/`AddChildNode`. **버림(근거):** NetIndex(복제 미채택)·에디터 충돌추적·정렬삽입(대규모 네이티브 테이블 성능용)·`_parentNode` 역참조(조상은 캐시에 이미 펼침).
- **`GameplayTagsManager`.** 문자열 파싱 빌드(`split('.')` + 레벨 dedup으로 부모 파생). **싱글톤 = 플레인 C#**(MonoBehaviour 아님, UE `Get()` 대응) + `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`로 startup 결정적 빌드(도메인리로드 OFF여도 매 재생 새 인스턴스). 기존 `Start()`는 비-MonoBehaviour라 안 불려서 생성자로 이관. 레지스트리 배선: `GameplayTagRegistryAsset.Load()` + null 가드. 조회 API 채움: `FindTagNode`×2·`RequestGameplayTag`(등록된 것만, 미등록 default)·`DestroyGameplayTagTree`.
- **절제 판단.** `accumulatedPath` StringBuilder 최적화 검토 → 기각(빌드 1회성·인터닝은 string 필요·범용 유틸 오염). 루트 접근자 **bounds 가드(②)**: 노드에 완전태그 조회 접근자 추가 시 `Num()>0` 가드 필요(빈 루트 보호) — 아직 접근자 없어 미도입.
- **열린 질문(→progress 결정대기 3).** 태그 조회 입력 ergonomics: implicit `string→InternedName` vs `(string)` 오버로드 vs `new InternedName()` 명시. 명시 쪽 제안, 유저 확정 전.
- **⚠ 알려진 이슈.** 매니저 비-void 빈 스텁 4개(`AddNativeGameplayTag`·`RequestGameplayTagParents`·`RequestGameplayTagChildren`·`RequestGameplayTagDirectParent`)가 반환 누락으로 **어셈블리 컴파일 불가**. `RequestGameplayTagDirectParent()`는 파라미터 누락(시그니처 깨짐). 다음 세션 선행 해결.

## 2026-09-28 — 태그 표현 타입 확정: `InternedString` 래퍼

- **논의 흐름:** 내부 표현 후보 "FName처럼" vs "TagsManager index map"을 두고 봤더니 **결국 같은 것**(둘 다 struct가 핸들만 들고 뒤에서 string→인덱스 인터닝)이라는 데 합의. 이어 "int 핸들 안 하고 문자열 통째 들어도, intern만 보장되면 참조 비교·중복 제거가 성립 = FName 실질 대체"까지 정리.
- **`string.Intern` 확인:** 리터럴은 자동 인터닝, 런타임 문자열은 `string.Intern` 호출 + **반환값 대입** 필요. 전역 풀이라 앱 수명 내 GC 안 됨(태그는 유한이라 무해).
- **결정(유저):** 내부 문자열을 **`InternedString` struct 래퍼**로 대체. 래퍼가 인터닝 책임을 캡슐화. **근거(유저):** "string을 대체하되, intern string임을 타입으로 명시적으로." 동등성은 `ReferenceEquals`(유저 선택 — "둘 다 interned라 참조 비교로 충분").
  - **정수 핸들 유보(회색지대):** 밀집 int 인덱싱이 필요해지면 자체 레지스트리로 교체. **교체 지점은 `InternedString.Intern` 한 곳**으로 국소화(호출측·GameplayTag 무영향).
- **구현:**
  - `InternedString.cs` 신설 — struct, 생성자 `string.Intern` 정규화, `ReferenceEquals` 동등성, `ISerializationCallbackReceiver.OnAfterDeserialize`에서 재-intern(역직렬화가 생성자 우회하므로).
  - `GameplayTag.cs` — 임시 `string _tagName` → `[SerializeField] InternedString _name`. 동등성·해시 래퍼 위임.
- **⚠ 미검증(확신 낮음):** 참조 비교 정확성은 "역직렬화된 태그도 intern됨"에 의존. **중첩 struct의 `ISerializationCallbackReceiver` 콜백이 실제로 불리는지 미확인** — 안 불리면 인스펙터/에셋 로드 태그가 intern 안 돼 참조 비교가 조용히 깨짐. **에디터에서 실측 필요.** fallback: ① 컨테이너 레벨 재-intern, ② `Equals`를 `Ordinal` 문자열 비교로 강등(intern=성능 최적화로).

## 2026-09-30 — 태그 정의 소재: 단일 SO(고정 위치)

- **문제:** 에디터에서 추가/제거한 태그를 **빌드·에디터 동일하게 startup에 로드**할 소재가 필요. UE는 이걸 어떻게 하나 확인(로컬 5.6.0 원문) — 정의는 config `DefaultGameplayTags.ini`(+플러그인 ini·DataTable 여러 개)에 텍스트로 저장, 시작 시 매니저가 모아 트리 빌드. **정의는 영속(텍스트), 런타임 트리·인덱스는 휘발(재구성)**. 편집은 Project Settings 전용 UI, 빌드는 읽기만.
- **논의 흐름:** 발견 방식(다중 SO)·은닉(Addressable/codegen)까지 넓혔다가 좁힘.
  - 다중 SO 자동 발견은 `AssetDatabase`가 빌드에서 안 되는 함정 → 런타임 발견은 `Resources.LoadAll`/Addressables/루트참조만 가능. 하지만 **"단일 트리"만 성립하면 되지 단일 파일일 필요는 없음** — 지금 규모엔 단일 SO면 자명하게 충족(다중 병합 기계는 과설계).
  - "은닉·못 건드림"은 런타임 로드(빌드 포함 에셋=Project 창 노출)와 상충. 완전 은닉은 codegen(숨은 저장처→C# 상수 생성)이어야 하나 **오바로 판단**. custom 텍스트 포맷도 파서·검증·편집 UX를 SO 재구현이라 이득 없음(솔로·외부편집 불요).
- **결정(유저):** 정의 소재 = **단일 ScriptableObject(고정 위치, 단순 string 리스트)**, startup에 로드해 트리 빌드. 커스텀 Inspector·codegen·custom 포맷 **미채택**. **근거(유저):** "codegen·custom format은 오바, 단순함 우선." Inspector는 나중에 따로.
  - 포기한 것(의식적): 은닉/손댐 방지(SO는 Project 창 노출), 오타 방지·자동완성(문자열 기반). 필요해지면 매니저의 수집 지점 한 곳만 바꿔 드로어/codegen 확장.
- **구현:** `GameplayTagRegistryAsset.cs` 신설 — `sealed ScriptableObject`, 고정 경로 `ResourcesPath`("AbilitySystem/GameplayTagRegistry") 상수, `List<string> _tags`, `static Load()`(Resources 로드).
- **다음:** ① Resources 하위에 에셋 인스턴스 생성(에디터: Create ▸ Ability System ▸ Gameplay Tag Registry → `Resources/AbilitySystem/`로 이동). ② 매니저 startup 로드 + 트리 빌드 배선(단, `GameplayTagNode` 생성자 미완이 선행). ③ 결정을 `decisions.md`에 D#로 정식화할지 유저 확인.
