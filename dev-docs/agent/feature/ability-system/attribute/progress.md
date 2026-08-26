# attribute — 어트리뷰트 (수치·Set·Attribute 참조·SO 초기화)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P0
- 최종 갱신: 2026-08-22 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md).

## 목표
캐릭터 수치(체력·스태미나·데미지 등)를 타입드 `AttributeSet`/`ResolvedGameplayAttribute`(런타임 해석·접근)로 담고, string 탐색 없이 O(1)로 읽고 쓰는 GAS 수치 기반을 제공한다.

## 수용 기준 (Definition of Done)
- [x] `AttributeSet` 타입별 등록/해제 (같은 타입 중복 불가)
- [x] `ResolvedGameplayAttribute`로 BaseValue 읽기·쓰기 (FieldInfo 캐싱, string 탐색 없음)
- [x] `AttributeDefinitionAsset`(SO)로 초기값을 Reflection 세팅
- [x] 에디터 드로어로 Set·필드·초기값 배선

## 범위
### 포함
- `AttributeSet`(마커) / `AttributeData` / `GameplayAttribute`(직렬화)·`ResolvedGameplayAttribute`(런타임), `Character`·`CombatAttributeSet`
- SO 초기화(`AttributeDefinitionAsset`→`AttributeSetDefinition`→`AttributeFieldDefinition`) + ASC `AddAttributeSet`/`Get`/`SetBaseAttributeValue`
- Editor 드로어(`AttributeDefinitionAssetDrawer`/`AttributeSetDefinitionDrawer`/`AttributeReflectionUtility`)
### 제외 (명시적으로 하지 않을 것)
- CurrentValue 연산·Modifier(→ [gameplay-effect](../gameplay-effect/progress.md)) / 저장 / 네트워크

## 설계 개요
- 아키텍처: [architecture/ability-system/attribute.md](../../../../project/architecture/ability-system/attribute.md)
- 코드: `Assets/Scripts/Core/AbilitySystem/Attribute/`, `Assets/Scripts/Editor/AbilitySystem/`

## 세부 TODO (구현 체크리스트)
- [x] 1. AttributeSet/Data/Attribute 참조(`GameplayAttribute`·`ResolvedGameplayAttribute`) 자료구조
- [x] 2. SO 초기화 파이프라인 + ASC.Awake Reflection 세팅
- [x] 3. 에디터 드로어 배선

## 결정 기록
상세(맥락·대안·근거·트레이드오프)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D1 | 2026-05-26 | `ResolvedGameplayAttribute`(구 `AttributeHandle`)가 FieldInfo를 캐싱하는 불변 struct — 런타임 string 탐색 제거 |
| D2 | 2026-05-26 | `AttributeSet`은 빈 추상 마커, 구체 Set이 `public AttributeData` 필드 보유 |
| D3 | 2026-05-26 | 초기값은 SO + Reflection 세팅 (하드코딩 아님) |
| D4 | 2026-08-07 | Attribute Set 셀렉터에 "New Script..." — 템플릿 생성 후 리로드 넘겨 자동 배정(GlobalObjectId+SessionState) |
| D5 | 2026-08-21 | `AttributeHandle`→`ResolvedGameplayAttribute` 리네임 — UE 단일 `FGameplayAttribute`를 직렬화용 `GameplayAttribute` + 런타임용 `ResolvedGameplayAttribute` 둘로 나눈 구조 명확화 |
| D6 | 2026-08-22 | `AttributeSet`에 소유 ASC(`owner`) back-ref 추가 — 어트리뷰트별 편의 세터를 owner 통해 ASC로 위임(UE `ATTRIBUTE_ACCESSORS` SETTER처럼). **⚠ 주입 방식 미확정·미완(컴파일 불가)** |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력·아카이브). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- **`AttributeSet` owner 주입 미완 → 컴파일 불가.** 생성자만 있고 서브클래스·Activator 미배선(→D6).

## 다음 작업
1. **`AttributeSet` owner 주입 방식 확정·배선 완성 (→D6).** `SetOwner`(등록 시 주입) vs **생성자 주입** 중 확정 → 생성자면 서브클래스 4개(`Character`·`Combat`·`Box`·`Test`)에 `: base(owner)` 전달 생성자 + `AbilitySystemComponent.AddSet`의 `Activator.CreateInstance(type, this)` 배선. 현재 컴파일 불가.
2. (그 외 코어 완료) 후속은 형제 feature — 수치 변경 채널은 [gameplay-effect](../gameplay-effect/progress.md), 실행 계층은 [gameplay-ability](../gameplay-ability/progress.md).
