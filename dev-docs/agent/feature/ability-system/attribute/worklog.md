# attribute — 작업 로그

> 시간순 "무엇을 왜 했나" 기록. progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정은 `(→D#)`(decisions.md)로 링크한다. 2026-10-23 정리로 검증 수치·조사 경로를 걷어냈다 — 원문은 git 이력.

### 2026-10-21 — 초기값 에셋 경로 삭제, 소유 쪽 Set 생성 + 초기 스탯 GE (→D7)
- 유저가 기본 스탯 설정 경로를 묻고 "임시 코드니까 빼고 정식적으로 UE에서 하는거처럼 해", 이어 `attributeInitData` 체계 전부와 BoxRoom을 지우되 기존 것은 그대로 동작하게 하라고 했다.
- UE는 Owner 쪽 Set을 ASC가 모으고, ASC 초기값 테이블은 테스트용 카테고리다(`AbilitySystemComponent.h:196-197`). 그래서 소유 컴포넌트(`PlayerCharacter`·`TrainingDummy`)가 `Awake`에 Set을 등록하고 `Start`에 초기 스탯 GE를 건다(GE 컨테이너가 ASC `Awake`에서 생겨서).
- 막힌 곳: health 클램프가 maxHealth를 상한으로 써서 Override 모디파이어는 maxHealth를 먼저 둬야 한다. 옛 값이 전부 0인 적은 GE 없이 둔다(GE로 주면 health 하한 1이 걸림).

### 2026-10-20 — AttributeSet 훅 4개 + Current 조회를 저장값으로
- 유저 "이벤트 함수를 구현하라는거지?" → "구현해", 테스트는 기존 `TestAtrributeSet`으로.
- UE `InternalExecuteMod` 흐름대로 컨테이너에 `InternalExecuteMod`를 두고 Modifier 실행·Execution 출력이 이리로 모이게 했다(Pre → 적용 → Post). Current 쓰기 앞뒤엔 `Pre/PostAttributeChange`.
- 막힌 곳: Pre 클램프(150 → 100)가 저장·이벤트엔 반영되는데 조회는 aggregator 재계산값 150을 냈다. UE `GetNumericAttribute`는 저장값을 읽으므로 `GetAttributeCurrentValue`를 저장값 읽기로 바꿨다(유저 "그래 해"). 클램프 훅이 없으면 결과는 전과 같다.
- 같이 고침: `TestAtrributeSet`의 `Damage` 핸들이 다른 Set을 가리키던 버그.

### 2026-10-20 — `PreAttributeBaseChange`가 새 Base 값을 고칠 수 있게 (`ref`)
- `newValue`를 값으로 받아 override에서 클램프해도 사본만 바뀌었다. UE는 `float&`이고 고친 값으로 Base·Current·Post까지 쓴다 → 선언·호출을 `ref`로.

### 2026-08-22 — AttributeSet owner back-ref (→D6)
- UE `ATTRIBUTE_ACCESSORS`의 `SetX`도 owning ASC로 위임하므로 "Set에 두기 vs ASC로만"은 양자택일이 아니라 얼굴은 Set, 쓰기는 ASC — 그래서 `AttributeSet`이 owner를 안다.
- 생성자 주입으로 바꾸던 중 유저가 범위 초과로 중단("멋대로 구현하지 말라"). 이후 `SetOwner`(등록 시 주입)로 확정.

### 2026-08-07 — Attribute Set 선택기 "New Script..."
- Set 목록 "+"에서 이름만 적으면 `AttributeSet` 상속 템플릿을 만들고 리로드 뒤 자동 배정. 리로드로 팝업·SerializedProperty가 무효화돼 배정 정보를 `SessionState`에 넘긴다.
- 처음엔 필드 선택기에만 넣었는데 유저가 새 Set을 만드는 곳은 목록 헤더 "+"였다 → 진입점 이동. 배선 반복은 파사드(`NewSubclassScript`)로 흡수하고 UI·저작·배정을 파일별로 나눴다.

### 2026-07-08 — 초기값 드로어 필드 간헐 미렌더 수정
- 배열 인덱스 경로를 키로 "이미 sync함"을 캐시해, 요소를 추가·삭제하면 같은 경로가 다른 요소를 가리키는데도 sync를 건너뛰었다. 캐시를 없애고 매번 sync(이미 맞으면 no-op)로 — 성급한 최적화가 낳은 버그라 단순한 쪽으로 되돌림.

### 2026-05-26 — PR #2 (Ability System - Attribute)
- Attribute 계층: `AttributeSet`·`AttributeData`·FieldInfo 캐싱 핸들, `CharacterAttributeSet`·`CombatAttributeSet` (→D1·D2).
- SO 초기화 파이프라인 + ASC `Awake` 리플렉션 세팅(치운 D3 — 2026-10-21 D7로 대체), 에디터 드로어, ASC 최소 API.
