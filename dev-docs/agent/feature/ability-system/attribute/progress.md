# attribute — 어트리뷰트 (수치·Set·핸들·SO 초기화)

- 상태: ✅ 임시완료
- 우선순위: P0
- 최종 갱신: 2026-09-09 (KST)

## 목표
캐릭터 수치를 타입드 `AttributeSet` + 런타임 핸들로 담아 string 탐색 없이 읽고 쓴다. 값 변경 채널(GE·집계)은 형제 feature가 얹는다.

## 수용 기준 (Definition of Done)
- [x] `AttributeSet`을 타입별로 ASC에 등록/해제
- [x] `GameplayAttributeHandle`로 Base/Current 읽기·쓰기 (FieldInfo 캐싱)
- [x] `AttributeDefinitionAsset`(SO)로 초기값을 Reflection 세팅
- [x] `AttributeSet.SetBaseValue`가 owner(ASC)로 위임
- [x] 에디터 드로어로 Set·필드·초기값 배선

## 범위
### 포함
- `AttributeData`(base/current struct) / `AttributeSet`(추상 마커 + owner + Pre/PostAttributeBaseChange 훅)
- 참조 2분할: `GameplayAttribute`(직렬화, AQN+필드명) ↔ `GameplayAttributeHandle`(런타임, FieldInfo 캐싱)
- 구체 Set: `CharacterAttributeSet`·`CombatAttributeSet`·`BoxAttributeSet`·`TestAtrributeSet`
- SO 초기화: `AttributeDefinitionAsset`→`AttributeSetDefinition`→`AttributeFieldDefinition` + ASC `AddSet`
- 에디터 드로어(`AttributeDefinitionAssetDrawer`·`AttributeSetDefinitionDrawer`·`GameplayAttributeDrawer`)
### 제외
- CurrentValue 연산·Modifier 누산 → [gameplay-effect](../gameplay-effect/progress.md)
- 라이브 재평가 → [aggregator](../aggregator/progress.md)
- 런타임 값 저장/직렬화·네트워크 복제

## 설계 개요
- **값 저장**: 구체 `AttributeSet`이 `public AttributeData` 필드 보유, ASC가 `Dictionary<Type, AttributeSet>`로 타입당 하나 등록(→D2).
- **참조 2분할(→D5)**: 직렬화는 `GameplayAttribute`(문자열 쌍), 런타임 해석·값 접근은 `GameplayAttributeHandle`(FieldInfo 캐싱 불변 struct, `SetType`+`Name` IEquatable → Dictionary 키).
- **owner 위임(→D6)**: `AttributeSet`이 ASC back-ref를 `SetOwner`(등록 시)로 받고 `SetBaseValue`를 ASC로 위임. Pre/PostAttributeBaseChange 훅 제공.
- **초기화(→D3)**: SO → Reflection(`Activator.CreateInstance`+`FieldInfo.SetValue`)으로 초기값 세팅.
- 코드: `Assets/Scripts/Core/AbilitySystem/Attribute/`, 구체 Set은 `Assets/Scripts/Character/`·`BoxAttributeSet.cs`

## 세부 TODO (구현 체크리스트)
- [x] 1. `AttributeData`/`AttributeSet` + 참조 2분할 자료구조
- [x] 2. SO 초기화 파이프라인 + ASC `AddSet`
- [x] 3. 에디터 드로어 배선 (+ New Script 생성 →D4)
- [x] 4. owner 주입(`SetOwner`) + 세터 위임 (→D6)

## 결정 기록
→ [decisions.md](decisions.md). progress엔 인덱스만.

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-05-26 | 런타임 핸들 = FieldInfo 캐싱 불변 struct (string 탐색 제거) |
| D2 | 2026-05-26 | `AttributeSet` 빈 추상 마커, 구체 Set이 `AttributeData` 필드 보유 |
| D3 | 2026-05-26 | 초기값은 SO + Reflection 세팅 |
| D4 | 2026-08-07 | Set 셀렉터 "New Script..." — 템플릿 생성 후 자동 배정 |
| D5 | 2026-08-21 | 참조를 직렬화 `GameplayAttribute` + 런타임 `GameplayAttributeHandle`로 분리 |
| D6 | 2026-08-22 | `AttributeSet` owner back-ref + 세터 ASC 위임, 주입은 `SetOwner` |

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
- 코어 완료. 후속은 형제 feature([gameplay-effect](../gameplay-effect/progress.md)·[aggregator](../aggregator/progress.md)·[gameplay-ability](../gameplay-ability/progress.md)).
