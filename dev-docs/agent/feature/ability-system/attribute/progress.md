# attribute — 어트리뷰트 (수치·Set·핸들·SO 초기화)

> 작성 규약 → [progress-harness.md](../../progress-harness.md)

- 상태: ✅ 임시완료
- 우선순위: P0
- 최종 갱신: 2026-10-20 (KST)

## 목표
캐릭터 수치를 타입드 `AttributeSet` + 런타임 핸들로 담아 string 탐색 없이 읽고 쓴다. 값 변경 채널(GE·집계)은 형제 feature가 얹는다.

## 수용 기준 (Definition of Done)
- [x] `AttributeSet`을 타입별로 ASC에 등록/해제
- [x] `GameplayAttributeHandle`로 Base/Current 읽기·쓰기 (FieldInfo 캐싱)
- [x] `AttributeDefinitionAsset`(SO)로 초기값을 Reflection 세팅
- [x] `AttributeSet.SetBaseValue`가 owner(ASC)로 위임
- [x] 에디터 드로어로 Set·필드·초기값 배선
- [x] Set이 훅으로 값 변경에 게임 규칙을 건다 — Base 클램프(`PreAttributeBaseChange`), GE 실행 수치 변경·버림·meta attribute(`Pre/PostGameplayEffectExecute`), Current 클램프(`PreAttributeChange`)가 조회·이벤트에 반영 (Aggregator 셀프체크 J·K, 2026-10-20)

## 범위
### 포함
- `AttributeData`(base/current struct) / `AttributeSet`(추상 + owner + 훅 6개: `Pre/PostAttributeBaseChange`·`Pre/PostGameplayEffectExecute`·`Pre/PostAttributeChange`, UE 같은 이름)
- 참조 2분할: `GameplayAttribute`(직렬화, AQN+필드명) ↔ `GameplayAttributeHandle`(런타임, FieldInfo 캐싱)
- 구체 Set: `CharacterAttributeSet`·`CombatAttributeSet`·`BoxAttributeSet`·`TestAtrributeSet`(훅 검증용 — meta Damage·Health 클램프, 셀프체크 K가 씀)
- SO 초기화: `AttributeDefinitionAsset`→`AttributeSetDefinition`→`AttributeFieldDefinition` + ASC `AddSet`
- 에디터 드로어(`AttributeDefinitionAssetDrawer`·`AttributeSetDefinitionDrawer`·`GameplayAttributeDrawer`)
### 제외
- CurrentValue 연산·Modifier 누산 → [gameplay-effect](../gameplay-effect/progress.md)
- 라이브 재평가 → [aggregator](../aggregator/progress.md)
- 런타임 값 저장/직렬화·네트워크 복제

## 설계 개요
- **값 저장**: 구체 `AttributeSet`이 `public AttributeData` 필드 보유, ASC가 `Dictionary<Type, AttributeSet>`로 타입당 하나 등록(→D2(Set=마커+구체 Set)).
- **참조 2분할(→D5(직렬화/런타임 참조 분리))**: 직렬화는 `GameplayAttribute`(문자열 쌍), 런타임 해석·값 접근은 `GameplayAttributeHandle`(FieldInfo 캐싱 불변 struct, `SetType`+`Name` IEquatable → Dictionary 키).
- **owner 위임(→D6(Set owner back-ref))**: `AttributeSet`이 ASC back-ref를 `SetOwner`(등록 시)로 받고 `SetBaseValue`를 ASC로 위임.
- **훅(2026-10-20, UE 구조 그대로)**: 호출은 gameplay-effect 컨테이너가 한다. `PreAttributeBaseChange(ref)`·`PostAttributeBaseChange` = `SetAttributeBaseValue` 앞뒤. `PreGameplayEffectExecute`(수치 변경·false로 버림)·`PostGameplayEffectExecute` = GE 실행(Instant·Periodic·Execution 출력)마다 `InternalExecuteMod`에서, 받는 데이터는 `GameplayEffectModCallbackData`. `PreAttributeChange(ref)`·`PostAttributeChange` = Current 쓰기 앞뒤(`InternalUpdateNumericalAttribute`) — 이유를 가리지 않음. Current 조회(`GetAttributeCurrentValue`)는 저장값이라 Pre 클램프가 보인다. 경위 → worklog 2026-10-20.
- **초기화(→D3(초기값 SO+Reflection))**: SO → Reflection(`Activator.CreateInstance`+`FieldInfo.SetValue`)으로 초기값 세팅.
- 코드: `Assets/Scripts/Core/AbilitySystem/Attribute/`, 구체 Set은 `Assets/Scripts/Game/Character/`·`Game/BoxAttributeSet.cs`

## 세부 TODO (구현 체크리스트)
- [x] 1. `AttributeData`/`AttributeSet` + 참조 2분할 자료구조
- [x] 2. SO 초기화 파이프라인 + ASC `AddSet`
- [x] 3. 에디터 드로어 배선 (+ New Script 생성 →D4(New Script 생성))
- [x] 4. owner 주입(`SetOwner`) + 세터 위임 (→D6(Set owner back-ref))
- [x] 5. `PreAttributeBaseChange`를 `ref`로 — 값으로 받아 클램프가 무시되던 버그 (2026-10-20)
- [x] 6. 훅 4개(`Pre/PostGameplayEffectExecute`·`Pre/PostAttributeChange`) + `TestAtrributeSet` 검증 (2026-10-20, PR #8)

## 결정 기록
→ [decisions.md](decisions.md). progress엔 인덱스만. 치운 결정(D4·D5)은 decisions 끝에 한 줄씩.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | Handle struct·FieldInfo 캐싱 | 2026-05-26 | 런타임 핸들 = FieldInfo 캐싱 불변 struct (string 탐색 제거) |
| D2 | Set=마커+구체 Set | 2026-05-26 | `AttributeSet` 빈 추상 마커, 구체 Set이 `AttributeData` 필드 보유 |
| D3 | 초기값 SO+Reflection | 2026-05-26 | 초기값은 SO + Reflection 세팅 |
| D6 | Set owner back-ref | 2026-08-22 | `AttributeSet` owner back-ref + 세터 ASC 위임, 주입은 `SetOwner` |

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
- 코어·훅 완료. 후속은 형제 feature([gameplay-effect](../gameplay-effect/progress.md)·[aggregator](../aggregator/progress.md)·[gameplay-ability](../gameplay-ability/progress.md)). 훅을 쓰는 게임 규칙(사망 처리·방어 감소 등)은 데모 쪽 — TODO-BOARD '📋 TODO (데모 제작)'.
- 2026-10-20 이 feature 작업(훅)으로 [gameplay-effect](../gameplay-effect/progress.md)·[aggregator](../aggregator/progress.md) 코드를 고쳤다 — 사유·내용은 두 progress `## 다음 작업` 끝에 적었다.
