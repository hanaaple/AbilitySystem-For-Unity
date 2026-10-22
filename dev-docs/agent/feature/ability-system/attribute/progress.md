# attribute — 어트리뷰트 (수치·Set·핸들·초기 스탯)

> 작성 규약 → [progress-harness.md](../../progress-harness.md)

- 상태: ✅ 임시완료
- 우선순위: P0
- 최종 갱신: 2026-10-23 (KST)

## 목표
캐릭터 수치를 타입드 `AttributeSet` + 런타임 핸들로 담아 string 탐색 없이 읽고 쓴다. 값 변경 채널(GE·집계)은 형제 feature가 얹는다.

## 수용 기준 (Definition of Done)
- [x] `AttributeSet`을 타입별로 ASC에 등록/해제
- [x] `GameplayAttributeHandle`로 Base/Current 읽기·쓰기 (FieldInfo 캐싱)
- [x] 소유 쪽이 Set을 만들어 등록하고 초기값은 Instant GE(Override)로 준다
- [x] `AttributeSet.SetBaseValue`가 owner(ASC)로 위임
- [x] 에디터 드로어로 어트리뷰트 참조 배선
- [x] Set이 훅으로 값 변경에 게임 규칙을 건다 — Base 클램프(`PreAttributeBaseChange`), GE 실행 수치 변경·버림·meta attribute(`Pre/PostGameplayEffectExecute`), Current 클램프(`PreAttributeChange`)가 조회·이벤트에 반영

## 범위
### 포함
- `AttributeData`(Base/Current struct) / `AttributeSet`(추상 + owner + 훅 6개: `Pre/PostAttributeBaseChange`·`Pre/PostGameplayEffectExecute`·`Pre/PostAttributeChange`, UE와 같은 이름)
- 참조 2분할: `GameplayAttribute`(직렬화, AQN+필드명) ↔ `GameplayAttributeHandle`(런타임, FieldInfo 캐싱)
- 구체 Set(Game 쪽): `CharacterAttributeSet`·`CombatAttributeSet`·`BoxAttributeSet`·`TestAtrributeSet`(훅 검증용)
- 초기화: 소유 컴포넌트 `Awake`에 `AddSpawnedAttribute(new Set())`, `Start`에 초기 스탯 Instant GE(Override)
- 에디터 드로어(`GameplayAttributeDrawer`)
### 제외
- CurrentValue 연산·Modifier 누산 → [gameplay-effect](../gameplay-effect/progress.md)
- 라이브 재평가 → [aggregator](../aggregator/progress.md)
- 런타임 값 저장/직렬화·네트워크 복제

## 설계 개요
- **값 저장(→D2 Set=마커+구체 Set)**: 구체 `AttributeSet`이 `AttributeData` 필드를 들고, ASC가 `Dictionary<Type, AttributeSet>`로 타입당 하나 등록.
- **참조 2분할(→D1 Handle struct·FieldInfo 캐싱)**: 직렬화는 `GameplayAttribute`(문자열 쌍), 런타임 해석·값 접근은 `GameplayAttributeHandle`(FieldInfo 캐싱 불변 struct, `SetType`+`Name`으로 같음 판정 → Dictionary 키).
- **owner 위임(→D6 Set owner back-ref)**: 등록 때 `SetOwner`로 ASC를 받고 `SetBaseValue`를 ASC로 위임.
- **훅(UE 구조 그대로)**: 호출은 gameplay-effect 컨테이너가 한다. `Pre/PostAttributeBaseChange` = `SetAttributeBaseValue` 앞뒤(Pre는 `ref`). `Pre/PostGameplayEffectExecute` = GE 실행(Instant·Periodic·Execution 출력)마다 `InternalExecuteMod`에서, 데이터는 `GameplayEffectModCallbackData`. `Pre/PostAttributeChange` = Current 쓰기 앞뒤(`InternalUpdateNumericalAttribute`). Current 조회는 저장값이라 Pre 클램프가 보인다.
- **초기화(→D7 소유 쪽 Set 생성 + 초기값 GE)**: 소유 쪽이 Set을 만들어 ASC에 등록하고, 초기값은 Instant GE Override로 자기 적용 — GE 경로라 Set 훅·값 변경 이벤트를 거친다.
- **조회**: `AttributeSet.GetAttributesFromSetClass(type, list)` — Set 타입의 어트리뷰트 핸들을 선언 순으로(디버그 표시용, 결정은 [gameplay-effect D30](../gameplay-effect/decisions.md)).
- 코드: `Assets/Scripts/Core/AbilitySystem/Attribute/`, 구체 Set은 `Assets/Scripts/Game/Character/`·`Game/BoxAttributeSet.cs`
- 아키텍처: [architecture/ability-system/attribute.md](../../../../project/architecture/ability-system/attribute.md)

## 세부 TODO (구현 체크리스트)
- [x] 1. `AttributeData`/`AttributeSet` + 참조 2분할 자료구조
- [x] 2. ~~SO 초기화 파이프라인 + ASC `AddSet`~~ — 7로 대체(코드 삭제)
- [x] 3. 에디터 드로어 배선
- [x] 4. owner 주입(`SetOwner`) + 세터 위임 (→D6 Set owner back-ref)
- [x] 5. `PreAttributeBaseChange`를 `ref`로 — 값으로 받아 클램프가 무시되던 버그
- [x] 6. 훅 4개(`Pre/PostGameplayEffectExecute`·`Pre/PostAttributeChange`) + `TestAtrributeSet` 검증
- [x] 7. 초기값 에셋 경로 삭제 → 소유 쪽 Set 생성 + 초기 스탯 GE (→D7)

## 결정 기록
→ [decisions.md](decisions.md). progress엔 인덱스만. 치운 결정(D3·D4·D5)은 decisions 끝에 한 줄씩.

| ID | 제목 | 날짜 | 결정 (요지) |
|---|---|---|---|
| D1 | Handle struct·FieldInfo 캐싱 | 2026-05-26 | 런타임 핸들 = FieldInfo 캐싱 불변 struct (string 탐색 제거) |
| D2 | Set=마커+구체 Set | 2026-05-26 | `AttributeSet` 추상 베이스, 구체 Set이 `AttributeData` 필드 보유 |
| D6 | Set owner back-ref | 2026-08-22 | `AttributeSet` owner back-ref + 세터 ASC 위임, 주입은 `SetOwner` |
| D7 | 소유 쪽 Set 생성 + 초기값 GE | 2026-10-21 | Set은 소유 컴포넌트가 생성·등록, 초기값은 Instant GE Override (D3 대체) |

- 다른 feature 작업으로 이 feature 코드 수정: 2026-10-23 `AttributeSet.GetAttributesFromSetClass` 추가(디버그 조회 API — [gameplay-effect D30](../gameplay-effect/decisions.md)).

## 작업 로그
→ [worklog.md](worklog.md). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
- 이 feature의 남은 구현 없음. 후속은 형제 feature([gameplay-effect](../gameplay-effect/progress.md)·[aggregator](../aggregator/progress.md)·[gameplay-ability](../gameplay-ability/progress.md)).
- 남은 정리: 타입 참조를 rename-safe 하게(AQN 문자열 저장) — [TODO-BOARD](../../../TODO-BOARD.md) '정리 작업'.
