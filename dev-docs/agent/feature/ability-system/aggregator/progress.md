# aggregator — 어트리뷰트 반응성 / Aggregator (라이브 재평가)

- 상태: ✅ 임시완료
- 우선순위: P1
- 최종 갱신: 2026-09-09 (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md).
> UE `FAggregator`에 준하는 **어트리뷰트별 Aggregator**로 구현됨(→D5). base 진실 = `AttributeData`, aggregator는 지연 생성 동기화 사본(→D14). cross-attribute 라이브 재평가는 dependents+OnDirty 전파로 구현(→D12) — 2026-08-26 PlayMode 검증 통과.

## 목표
non-snapshot 캡처를 쓰는 AttributeBased 모디파이어가, **캡처 대상 어트리뷰트가 바뀌면 그 즉시** magnitude를 재평가해 반영되게 한다(예: "방어력 = 힘의 10%"에서 힘이 바뀌면 방어력이 즉시 갱신). apply 시점에 magnitude가 고정돼 non-snapshot이 무력하던 상태를 해소한다.

## 수용 기준 (Definition of Done)
유저가 에디터/PlayMode에서 확인 가능한 조건으로:
- [x] non-snapshot AttributeBased 모디파이어: 캡처 대상 어트리뷰트를 다른 경로로 바꾸면 그 모디파이어가 적용된 어트리뷰트가 **즉시** 재계산된다 (cross-attribute). — PlayMode: Health 100→200 → Speed 50→100 즉시 추종.
- [x] self-effect(같은 ASC) 케이스에서 동일 동작. — PlayMode: `ApplyGameplayEffectToSelf`(Source=Target=동일 ASC) 통과.
- [x] snapshot=true 모디파이어는 고정(재평가 안 됨) — 회귀 없음. — PlayMode: Health 변경에도 Speed 50 고정.
- [~] 자기참조/연쇄 config에서 무한 루프가 발생하지 않는다. — 코드상 `MaxBroadcastDirty` 깊이 상한으로 폭주 차단. 전용 자기참조 케이스 검증은 미실시(폭주 방지지 수렴 보장 아님).

## 범위
### 포함
- **어트리뷰트별 `AttributeAggregator`**: base + flat mod 리스트(채널 없음 →D10) 소유, `Evaluate()`가 CurrentValue 산출 유일 경로(6종 연산 공식). Instant/Periodic의 영구 base 변경은 `ExecModOnBaseValue`.
- **소유·수명**: 컨테이너(`ActiveGameplayEffectsContainer`)가 어트리뷰트별로 하나씩 **지연 생성**(`FindOrCreateAttributeAggregator`, `AttributeData.BaseValue`로 시드). persistent(period 0) GE의 mod를 apply=`AddAggregatorMod`/remove=`RemoveAggregatorMod`로 등록/해제.
- **base 동기화(→D14)**: base 진실은 `AttributeData`. `SetAttributeBaseValue`가 AttributeData raw write 후 aggregator가 **있을 때만** Find해 동기화(생성 안 함). aggregator는 캡처·persistent mod가 필요할 때만 생성.
- **반응성(→D11)**: base/mod 변경 → `AttributeAggregator.OnDirty` → 컨테이너 `OnAttributeAggregatorDirty`(handle 캡처)가 `Evaluate`로 그 어트리뷰트만 CurrentValue 재계산·변경 델리게이트 발화.
- **cross-attribute 라이브(→D12)**: non-snapshot 캡처가 소스 aggregator에 dependent 등록(`RegisterLinkedAggregatorCallbacks`) → 소스 dirty 시 `BroadcastOnDirty`가 dependents 순회 → `OnMagnitudeDependencyChange`가 의존 GE의 modifier magnitude 재평가(`AttemptRecalculateMagnitudeFromDependentAggregatorChange`) → 대상 aggregator에 제자리 갱신 → 대상 dirty로 연쇄. `MaxBroadcastDirty`로 순환 차단.
- 태그(R5)는 미도입이되 얹을 seam(자격 판정 경계 + 일반화된 dirty 트리거)만 남긴다.
### 제외 (현 시점 — 이후 확장 가능)
- **cross-actor 라이브**(Source가 다른 액터인 버프가 그 액터 값 변화를 실시간 추종 — cross-ASC 구독) → 필요 시 후속.
- **자기참조 fixed-point 수렴**(현재 유한 패스+깊이 상한; 진짜 수렴은 후속).
- **Execution의 라이브 재평가**(Execution은 instant/periodic 이벤트성이라 대상 아님).
- **태그 자격 판정(R5) 실제 구현** — seam만.

## 설계 개요
> UE 참고: `FAggregator` 개념·구조·평가흐름은 [ue-reference.md](ue-reference.md)(유저 제공 분석). 포팅 Q&A는 [porting-notes.md](porting-notes.md).

- **소유 구도**: ASC는 얇은 위임 래퍼, 활성 GE 소유·수명·집계·반응성은 전부 `ActiveGameplayEffectsContainer`가 맡는다(→ [gameplay-effect](../gameplay-effect/progress.md) D18). 어트리뷰트별 aggregator 맵을 컨테이너가 지연 생성으로 관리.
- **값 경로**: `GetAttributeCurrentValue`는 aggregator가 있으면 `Evaluate()`로만 계산, 없으면(=mod 없어 current==base) `AttributeData` 미러를 읽는다. `GetAttributeBaseValue`도 aggregator 있으면 그 base, 없으면 미러.
- **왜 지연 생성(→D14)**: base 진실을 `AttributeData`에 두고 aggregator를 캡처/persistent가 필요할 때만 만들어, 모든 어트리뷰트에 aggregator를 강제 생성하던 이전(force-create) 대비 UE lazy 모델에 맞춘다. base write는 AttributeData가 항상 진실, aggregator는 있을 때만 동기화.
- **dirty 전파 = UE 원문식(→D12)**: 위상정렬 대안 대신 UE처럼 dependent 핸들 리스트 + `OnDirty` cascade + 깊이 상한(`MaxBroadcastDirty`). 재평가는 의존 mod magnitude를 소스 새 값으로 갱신 후 대상 `Evaluate` 재실행.
- **코드 위치**: `Assets/Scripts/Core/AbilitySystem/Effect/AttributeAggregator.cs`(값/mod/전파 본체) · `ActiveGameplayEffectsContainer.cs`(소유·재계산·dirty 진입) · `AbilitySystemComponent.cs`(dependency change 진입 래퍼). _(폴더는 `Aggregator/`→`Effect/`로 이동됨.)_
- 아키텍처: [architecture/ability-system/aggregator.md](../../../../project/architecture/ability-system/aggregator.md) — **옛 경량안 서술이라 코드와 어긋남, 재작성 대상**(아래 다음 작업).

## 세부 TODO (구현 체크리스트)
- [x] 1. 어트리뷰트별 `AttributeAggregator`(base+flat mod, `Evaluate` 단일 계산, `ExecModOnBaseValue`)
- [x] 2. 컨테이너 소유·지연 생성 + persistent mod add/remove
- [x] 3. base 동기화 — AttributeData 진실 + aggregator 지연 동기화 사본(→D14)
- [x] 4. 반응성 — `OnDirty` → 컨테이너 재계산(그 어트리뷰트만) (→D11)
- [x] 5. cross-attribute 라이브 재평가 — dependents 등록/전파 + 재계산 + 순환 상한(→D12)
- [ ] 6. 태그 seam(R5): 자격 판정 함수 경계 + dirty 트리거 일반화 (자리만, 구현 안 함)
- [ ] 7. 자기참조 전용 케이스 검증 + architecture 문서 재작성

## 결정 기록
상세(맥락·대안·근거·트레이드오프)는 → [decisions.md](decisions.md). **해당 영역을 건들 때 그 결정을 연다.**

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| ~~D1~~ | 2026-08-07 | ~~반응성(R3) 책임만 분리 — full aggregator 이식 안 함~~ **폐기(→D5)** |
| ~~D2~~ | 2026-08-07 | ~~경량(옵저버+재평가+재진입 억제)으로 시작~~ **폐기(→D5)** |
| D3 | 2026-08-08 | 변경 이벤트를 단일 choke point에서 발화 — 재설계로 OnDirty가 aggregator로 이동 |
| ~~D4~~ | 2026-08-08 | ~~registry 없이 의존 필터 스캔 + worklist 가드~~ **폐기(→D5)** |
| D5 | 2026-08-09 | **D1 반전** — 진짜 어트리뷰트별 Aggregator 신설 + ASC 인라인 계산 이관, 컨테이너 레이어 신설 |
| D6 | 2026-08-09 | 활성 GE 소유를 `ActiveGameplayEffectsContainer`로 분리 (D5 첫 단계) |
| ~~D7~~ | 2026-08-10 | ~~BaseValue 진실 = Aggregator~~ **반전(→D14)** |
| D8 | 2026-08-10 | mod 소유 = Aggregator (apply=AddMod/remove=RemoveMods, Evaluate가 순회 — 전량 스캔 폐기) |
| ~~D9~~ | 2026-08-11 | ~~채널별 맵으로 mod 저장~~ **폐기(→D10)** |
| D10 | 2026-08-11 | **채널 축 제거** — 채널 1개라 flat `List<Mod>`로 직접 소유, `Evaluate` 인라인 집계 |
| D11 | 2026-08-11 | **반응성 = aggregator `OnDirty` → 컨테이너 구독**(그 어트리뷰트만 재계산). 명시적 recalc·worklist 폐기 |
| D12 | 2026-08-11 | **cross-attr 라이브 = UE 원문식** — 의존 등록(`_dependents`) + `BroadcastOnDirty` cascade + `MaxBroadcastDirty` cap. **구현·검증 완료(2026-08-26)** |
| D13 | 2026-08-15 | `GameplayModifierSpec`을 EvaluatedMagnitude 단일 캐시로 축소, identity는 정의를 같은 인덱스로 재해석 |
| D14 | 2026-08-16 | **D7 반전** — base 진실 = `AttributeData`(UE 원문), aggregator base = 동기화 사본. aggregator는 캡처+non-periodic일 때만 지연 생성. **구현 완료** |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업
1. **자기참조 전용 케이스 검증** — A→B→A 류 config를 만들어 `MaxBroadcastDirty` 상한이 폭주를 끊는지 PlayMode 확인(수용 기준 4번). 필요 시 자기참조 GE 케이스 추가.
2. **architecture 갱신** — `architecture/ability-system/aggregator.md`가 옛 경량안 서술이라 현재 per-attribute aggregator 구조로 재작성(유저 요청 시).
3. (확장 시) 태그 seam(R5) 실제 구현·cross-actor 라이브는 데모 요구가 생기면.

> 프로젝트 방향(2026-08-09): 메인 포트폴리오 = GAS 온전 작동 데모(→ [design.md](../../../../project/design.md)). aggregator 확장은 데모가 요구할 때 이어간다 — 다음 액션 우선순위는 [NOW.md](../../../NOW.md) 참조.
