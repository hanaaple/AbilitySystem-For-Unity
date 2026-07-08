# attribute — 결정 기록

> 트레이드오프가 있었던 설계 결정의 상세. 품질 기준은 일반 [HARNESS.md](../../../HARNESS.md) §8.
> **ID(`D#`)는 feature 내 고유·불변**이며 progress 인덱스·worklog가 이 코드로 이 표를 가리킨다.
> 이 시스템은 **UE GAS를 참고해 필요한 축만 직접 구현**한 것 — 각 행의 `(UE 대응)` 표기와 전체 채택/생략 범위는 [architecture overview](../../../../project/architecture/ability-system/overview.md) 참조.

| ID | 날짜 | 결정 | 대안 | 채택 사유 |
|---|---|---|---|---|
| D1 | 2026-05-26 | `AttributeHandle`을 **불변 struct로 두고 생성 시 `FieldInfo`를 캐싱**. `SetType + fieldName` 기반 `IEquatable`로 Dictionary 키 사용 | ① string 키 Dictionary(매 접근 문자열 조회) ② enum 키 + switch | 수치 읽기는 매 프레임 다수 발생 → 접근 경로에 string 탐색이 있으면 비용이 누적된다. Reflection을 **생성 1회로 격리**하면 이후 읽기/쓰기는 캐싱된 FieldInfo로 O(1). struct라 GC 부담도 없음. 트레이드오프: 핸들 선언이 `static readonly` 보일러플레이트를 요구하나, 접근 성능과 타입 안전(INV: untyped 저장 금지 정신)이 우선. **(UE 대응)** UE `FGameplayAttribute`는 `UProperty`로 필드를 가리키지만 Unity엔 그 기반이 없어, `FieldInfo`를 생성 1회 캐싱하는 struct로 같은 목적을 축소 재현 |
| D2 | 2026-05-26 | `AttributeSet`은 **빈 추상 마커**, 구체 Set(`Character`/`Combat`)이 `public AttributeData` 필드로 수치를 보유. ASC는 `Dictionary<Type, AttributeSet>`로 타입당 하나 등록 | 모든 수치를 한 Set/Dictionary<string,float>에 몰아넣기 | 수치를 도메인(캐릭터 vs 전투)별 Set으로 나누면 필요한 Set만 붙일 수 있고(적/상자엔 Combat만 등 재사용), 필드가 컴파일타임에 존재해 오타·타입 오류를 막는다. untyped 블랙보드(문자열 키) 회피. **(UE 대응)** UE `UAttributeSet`(수치를 클래스 컨테이너로) 개념 채택 |
| D3 | 2026-05-26 | 초기값은 **`AttributeInitData`(SO) → Reflection(`Activator.CreateInstance`+`FieldInfo.SetValue`)**으로 세팅. 에디터 드로어가 Set·필드·값을 배선 | 코드/인스펙터에 초기값 하드코딩 | 밸런싱 값을 코드 수정 없이 데이터로 조정 가능해야 함. SO로 빼면 디자이너가 에디터에서 만지고, 같은 ASC 초기화 코드가 임의 Set에 일반화된다. 트레이드오프: 초기화에 Reflection 1회 비용이 있으나 Awake 시점 1회라 무시 가능. **(UE 대응)** UE는 GE·CurveTable로 초기값을 주지만, 여기선 전용 SO+Reflection으로 축소 |
