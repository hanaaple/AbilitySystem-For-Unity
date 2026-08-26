# aggregator — 어트리뷰트 반응성 / Aggregator (라이브 재평가)

- 상태: 🔧 IN-PROGRESS
- 우선순위: P1
- 최종 갱신: 2026-08-26  (KST)

> `ability-system` 그룹의 하위 feature. 형제: [attribute](../attribute/progress.md) · [gameplay-effect](../gameplay-effect/progress.md) · [gameplay-ability](../gameplay-ability/progress.md).

> **⚠ 본문(목표·범위·설계 개요·세부 TODO)은 아직 폐기된 "경량 R3-only" 서술이 섞여 있어 재작성 대기.** 확정 방향은 UE `FAggregator`에 준하는 **어트리뷰트별·채널별 Aggregator**(→D5). **선결 BaseValue 소유 주체 = Aggregator 소유로 확정(→D7), mod 채널 소유도 Aggregator로 확정(→D8).** 2026-08-10 세션에 `AttributeAggregator`(값/채널 본체)·컨테이너 재배선을 **구현(미검증·미커밋)** — 상세 [worklog 2026-08-10](worklog.md). 남은 것: 유저 Play 검증 + 본문/architecture 재작성.

## 목표
non-snapshot 캡처를 쓰는 AttributeBased 모디파이어가, **캡처 대상 어트리뷰트가 바뀌면 그 즉시** magnitude를 재평가해 반영되게 한다(예: "방어력 = 힘의 10%"에서 힘이 바뀌면 방어력이 즉시 갱신). 현재는 magnitude가 apply 시점에 고정돼 non-snapshot이 무력한 상태를 해소한다.

## 수용 기준 (Definition of Done)
유저가 에디터에서 확인 가능한 조건으로:
- [x] non-snapshot AttributeBased 모디파이어: 캡처 대상 어트리뷰트를 다른 경로로 바꾸면, 그 모디파이어가 적용된 어트리뷰트가 **즉시** 재계산된다 (cross-attribute). — **PlayMode 검증(2026-08-26): Health 100→200 → Speed 50→100 즉시 추종.**
- [x] self-effect(같은 ASC) 케이스에서 동일하게 동작한다. — **PlayMode 검증: `ApplyGameplayEffectToSelf`(Source=Target=동일 ASC)로 전 케이스 통과.**
- [x] snapshot=true 모디파이어는 기존처럼 고정(재평가 안 됨) — 회귀 없음. — **PlayMode 검증: Health 변경에도 Speed 50 고정.**
- [ ] 자기참조/연쇄 config에서 **무한 루프가 발생하지 않는다**(재진입 억제). — 미검증(전용 케이스 없음). 코드상 worklist/visited/cap 가드 존재. 필요 시 자기참조 GE 케이스 추가.

## 범위
### 포함
- **R3 반응성**: 어트리뷰트 변경 통지 → 의존 모디파이어 magnitude 재평가(라이브 캡처 재조회) → 영향 어트리뷰트 재계산 → 유한 연쇄 + 재진입 가드.
- 대상: **AttributeBased 모디파이어**(persistent/CurrentValue 경로). cross-attribute·self-effect 라이브.
- 태그(R5)는 미도입이되 **얹을 seam만** 남긴다(자격 판정 함수 경계 + 일반화된 dirty 트리거).
### 제외 (명시적으로 하지 않을 것)
- **cross-actor 라이브**(Source가 다른 액터인 버프가 그 액터 값 변화를 실시간 추종 — cross-ASC 구독) → 필요해질 때 후속.
- **자기참조 fixed-point 수렴**(1회-패스로 정의; 진짜 수렴은 후속).
- **Execution의 라이브 재평가**(Execution은 instant/periodic 이벤트성이라 대상 아님).
- **UE per-attribute FAggregator 전면 이식**(R1 mod 채널 소유 / R2 current 계산 이관) — R1·R2는 ASC 유지.
- **태그 자격 판정(R5) 실제 구현** — seam만.

## 설계 개요
> **UE 참고 자료:** `FAggregator` 개념·구조·소유관계·평가흐름 정리는 [ue-reference.md](ue-reference.md) (유저 제공 분석, 2차 자료). D5 재설계의 참조 모델.
> **임시 리서치 노트:** 의존성 전파(OnDirty vs Dependents)·자가순환·포팅 전략(풀어쓸 것/조심할 것) Q&A 정리는 [porting-notes.md](porting-notes.md) (유저 작성, 미검증·미완 뭉텅이 — 검증되면 decisions로 승격).

- **UE 확인 메커니즘(원문 근거):** `FGameplayEffectAttributeCaptureSpec::RegisterLinkedAggregatorCallback`이 `bSnapshot==false`일 때만 `Agg->AddDependent(Handle)`로 소스 aggregator의 `Dependents`에 등록 → 소스 변경 시 `FAggregator`가 `OnDirty.Broadcast` + `ASC->OnMagnitudeDependencyChange`로 dependent GE를 재평가. snapshot=true는 `TakeSnapshotOf`로 값 복사(등록 안 함). 상세는 [architecture/ability-system/aggregator.md](../../../../project/architecture/ability-system/aggregator.md).
- **우리 구현 방향(경량):** UE의 per-attribute aggregator 객체 전면 대신, **반응성 책임만** 분리한다.
  1. 어트리뷰트 변경 이벤트(옵저버) — 기존 쓰기 지점(`SetBaseAttributeValue`/`UpdateAttributeCurrentValue`)에서 fire. (현재 그런 이벤트 없음 → 신설)
  2. 핸들러가 **magnitude 재평가**(`GameplayEffectSpec.CalculateModifierMagnitudes` 라이브 캡처 재조회) 후 영향 어트리뷰트 재계산. *(지금은 frozen `EvaluatedMagnitude`만 써서 값이 안 변함 — 이 재평가 훅이 핵심)*
  3. **재진입 억제**(재계산 패스 중 이벤트 잠금)로 연쇄를 1패스로, 자기참조 무한루프 차단.
- **코드 위치:** `Assets/Scripts/Core/AbilitySystem/Aggregator/`(신설). 앵커 스텁 `AttributeAggregator.cs`. 관련 훅: `AbilitySystemComponent`(이벤트 발화·구독), `GameplayEffectSpec.CalculateModifierMagnitudes`(대상 슬롯 재평가), `GameplayEffectAttributeCaptureSpec`(라이브 재조회 — 이미 있음).

## 세부 TODO (구현 체크리스트)
> ⚠ 아래 1~6은 폐기된 경량안(D1/D2/D4) 기준이라 **무효**다. 새 TODO는 BaseValue 결정(→D5) 후 재작성한다. 아래는 이력으로만 둔다.
- [x] 1. 어트리뷰트 변경 이벤트를 ASC 쓰기 지점에 신설(옵저버 채널). — `UpdateAttributeCurrentValue` 단일 choke point에서 `AttributeChanged(handle,old,new)` 발화(→D3, 2026-08-08).
- [~] 2. ~~의존 등록/해지~~ → **불필요(스킵)**: 캡처가 이미 조회 시점 라이브 재조회(`GameplayEffectAttributeCaptureSpec.TryGetCapturedValue`)라 재캡처·등록이 없어도 된다. D2의 registry-free 방침대로 변경 시 **활성 이펙트 스캔+의존 필터**로 대체.
- [x] 3. 변경 핸들러(2026-08-08): `AbilitySystemComponent.ReevaluateDependents(handle)` — `DependsOnNonSnapshot(this,handle)`인 persistent 스펙만 골라 `spec.ReevaluateMagnitudes()`(=`CalculateModifierMagnitudes`) → `RecalculateAffectedAttributes`.
- [x] 4. 재진입 억제(2026-08-08): `AttributeAggregator`가 worklist(`_dirtyQueue`)+visited(`_visited`)+`_isProcessing`로 연쇄 유한·자기참조 1패스 종료.
- [ ] 5. 태그 seam: 자격 판정 함수 경계 + dirty 트리거 일반화(구현은 안 함, 자리만). — 미착수(후속).
- [ ] 6. **유저 에디터 검증(수용 기준)** — 다음 차례. 컴파일·Play 실측은 유저 몫.

## 결정 기록
→ [decisions.md](decisions.md) (상세). progress엔 요지 인덱스만.

| ID | 날짜 | 결정 (요지) |
|---|---|---|
| D5 | 2026-08-09 | **D1 반전(유저 확정).** 진짜 **어트리뷰트별·채널별 Aggregator 신설 + 기존 ASC 인라인 계산 이관**, mod 저장은 aggregator 소유·컨테이너 레이어 신설. 선결 BaseValue 소유 주체 → **D7로 해소** |
| D6 | 2026-08-09 | **컨테이너 1차 슬라이스(D5 첫 단계):** 활성 GE 소유를 `ActiveGameplayEffectsContainer`(**struct**, ASC 필드 하나로 위임)로 분리. 근거=ASC 복사 시나리오 없음(유저). Tick·계산·aggregator 맵은 후속 |
| D7 | 2026-08-10 | ~~**BaseValue 소유 = Aggregator.** aggregator가 진실, AttributeData 미러~~ **반전(→D14, 2026-08-16)** |
| D14 | 2026-08-16 | **D7 반전(유저 확정) — base 진실 = AttributeData(UE 원문), aggregator base = Evaluate용 동기화 사본.** `SetAttributeBaseValue`: AttributeData 직접 씀 + aggregator **있을 때만** 동기화(Find, 생성 안 함) / 없으면 current=base 직접. Execute(W2)도 `SetAttributeBaseValue`로 합류, `SetAggregatorBaseValue` 헬퍼 제거, force-create 폐기. aggregator는 **capture + non-periodic effect**일 때만 지연 생성. 근거: UE verbatim(`GetAttributeBaseValue`가 AttributeData 읽음·`Set/ApplyMod`이 Find) + HARNESS '왜의 근거는 유저 의도 → UE 원본'(제약 없으면 UE 형태 안 따름). **미구현 — 다음 세션** |
| D8 | 2026-08-10 | **mod 소유 = Aggregator ModChannel(유저 확정, UE 근접).** apply=AddMod/remove=RemoveModsFrom, Evaluate는 채널만 순회(전량 스캔 폐기). 반응성 worklist를 aggregator→컨테이너로 이관 |
| ~~D9~~ | 2026-08-11 | ~~채널별 `AggregatorModChannel` 맵으로 mod 저장~~ **폐기(→D10)** |
| D10 | 2026-08-11 | **D9 반전(유저 지시) — 채널 축 제거, 단일 소유.** 채널 1개만 쓰기로 해 `AggregatorModChannel`·`GameplayModEvaluationChannel`·채널 맵을 없애고 `AttributeAggregator`가 mod를 flat `List<Mod>`로 직접 소유(`Evaluate` 인라인 집계). `RemoveAggregatorMod` 실제 구현은 유지 |
| D11 | 2026-08-11 | **반응성 재설계(유저 지시, UE 이식) — D8 반응성 배치 반전.** 컨테이너 worklist dirty(`_dirtyQueue`/`_visited`/`_isProcessing`/`OnAttributeChanged`)·`ReevaluateDependents` 폐기. `AttributeAggregator.OnDirty` 발화 → 컨테이너 `OnAttributeAggregatorDirty` 구독(handle 캡처) → `Evaluate`로 그 어트리뷰트만 재계산. 명시적 recalc 폐기. **미커버:** 어트리뷰트 간 라이브 재평가 제거(UE `OnDirtyRecursive`+의존 등록으로 후속 복원, 스펙 seam 유지) |
| D12 | 2026-08-11 | **cross-attr 라이브 재평가 복원 방식 = UE 원문식(유저 확정, 미구현).** 의존 등록(`Dependents`=의존 이펙트 핸들) + `OnDirtyRecursive` cascade + `MAX_BROADCAST_DIRTY` cap. 위상정렬 대안은 반려(유저: UE 따름). 재평가 = 의존 mod magnitude를 소스 새 current로 갱신 후 대상 `Evaluate` 재실행. 설계까지만 — 4조각 구현 대기(→NOW.md) |
| D13 | 2026-08-15 | **`GameplayModifierSpec` = UE `FModifierSpec`처럼 EvaluatedMagnitude 단일 캐시로 축소(유저 확정).** 슬롯의 `Handle`·`Operation` 제거, identity는 정의 `GameplayModifier`를 같은 인덱스로 재해석(apply/execute/inspector). 대가로 apply/execute마다 `ToAttributeHandle()`(Type.GetType) 리플렉션 부활 — 병목 시 정의 레벨 캐시로 승격 |
| ~~D1~~ | 2026-08-07 | ~~반응성(R3) 책임만 별도 분리 — full aggregator 이식 안 함~~ **폐기(→D5)** |
| ~~D2~~ | 2026-08-07 | ~~구현은 경량(옵저버+재평가+재진입 억제)으로 시작~~ **폐기(→D5)** |
| D3 | 2026-08-08 | 변경 이벤트는 **`UpdateAttributeCurrentValue` 단일 choke point**에서 발화 — **재검토(재설계 시 OnDirty가 aggregator로 이동)** |
| ~~D4~~ | 2026-08-08 | ~~registry 없이 의존 필터 스캔 + worklist/visited 가드, 반응성은 `AttributeAggregator`가 소유~~ **폐기(→D5)** |

## 작업 로그
→ [worklog.md](worklog.md) (시간순 이력). 재개 앵커는 아래 `## 다음 작업`.

## 블로커
- 없음.

## 다음 작업

**▶ 2026-08-26 PlayMode 검증 통과 (실제 Asset · Unity MCP):** Edit 모드 셀프체크에 더해, **실제 플레이 중 실제 직렬화 에셋**으로 검증. `Tools ▸ Ability System ▸ Setup PlayMode Test`가 GE `.asset` 5개(HealthBoost·SpeedFromHealth Live/Snapshot·InstantHeal + AttributeDefinitionAsset) 생성 + 씬에 ASC+테스트 컴포넌트 배선 → Play → **`[PlayMode Self-Check] passed 14 / failed 0`**, 예외/에러 0. 실제 ASC.Awake 초기화 → `ApplyGameplayEffectToSelf` 파이프라인으로 A 초기화 / B non-snapshot 라이브 재평가(Health 100→200 → Speed 50→100 추종·복귀) / C snapshot 고정(Health 변경에도 50) / D Instant Base 영구 130 전부 PASS. 산출물: `Character/Testing/AggregatorPlayModeTest.cs`(런타임 씬 컴포넌트, 직렬화 참조) + `Character/Testing/Editor/AggregatorPlayModeTestSetup.cs`(에셋 생성·씬 배선) + `Assets/_PlayModeTest/` 에셋 5개. **미커밋.** (구현자 주의: MonoBehaviour 헬퍼를 `Reset()`으로 지으면 AddComponent 시 Unity `Reset` 메시지와 충돌 → `ResetAttributes()`로 분리.)

**▶ 2026-08-26 검증 통과 (Unity MCP 실행):** `AggregatorSelfCheck` 메뉴 실행 → 콘솔 `[Aggregator Self-Check] passed 44 / failed 0`, 컴파일 에러 0, FAIL 0. 5층 전부 PASS:
- **A 단위** — AddBase/MultiplyAdditive/Compound/Override + 추가·제거 복귀.
- **B ASC 라우팅** — Current가 aggregator `Evaluate` 경유, Base 불변, `SetBase`.
- **C capture 스냅샷** — non-snapshot 추종 150 / snapshot 고정 100.
- **D 에셋 다수 케이스** — AddBase·MultiplyAdditive·Divide·Compound·AddFinal·Override 적용/복귀.
- **E 런타임 dirty 재평가(핵심)** — `Health 100→200 후 Speed 라이브 재평가 → 100` = D12 체인(dirty→dependents→cross-attr Evaluate) 동작 확정.
→ "aggregator로 CurrentValue를 내는 구조 변경"·스냅샷·에셋 적용·cross-attr 라이브 재평가 검증 완료. 다음 = 캡슐화 조이기(Phase 3, 유저 승인 대기 — NOW.md). D14(W1 raw write·base 진실 정리)는 아래 유지.

**▶ 2026-08-25 진행:** capture를 aggregator 기반으로 전환(snapshot=TakeSnapshotOf 복사 / non-snapshot=컨테이너 aggregator 참조), **D12 dependents 등록 배선 ①②④ 구현**(`_dependents` null NPE 해소·`AddDependent`/`RemoveDependent`·`BroadcastOnDirty` 복사 버그 수정·캡처/컨테이너 `RegisterLinkedAggregatorCallbacks`·Apply/Remove 호출부, persistent(period≤0) 한정). ③ 재계산은 유저의 in-place `UpdateAggregatorMod` 경로. `GameplayAttributeHandle ==`·`Modifiers` 배열 노출 부수 추가. **미해결이던** ① `OnAttributeAggregatorDirty` 조건 반전 ② `FindCaptureSpecByDefinition`의 `= null` → **유저가 해소(2026-08-26), 위 검증으로 회귀 없음 확인.**

**▶ D14 구현 (base 진실 = AttributeData, aggregator 지연 생성 복원).** 방향은 유저 확정(2026-08-16, →decisions D14). 설계·이해만 끝났고 **코드는 미착수.** 구체 대상(감사 결과 W#/R#):
- **W1 `SetAttributeBaseValue`** → UE 3단계: ① `AttributeData.BaseValue` 직접 씀 ② aggregator **Find**(있을 때만) `SetBaseValue` → dirty가 current 갱신 ③ 없으면 `UpdateAttributeCurrentValue`(current=base). `SetAggregatorBaseValue` 헬퍼 제거.
- **W2 `ApplyModToAttribute`(Execute)** → `GetAttributeBaseValue`로 읽고 `ExecModOnBaseValue`→`SetAttributeBaseValue`로 합류(force-create 제거).
- **R2 `GetAttributeBaseValue`** → AttributeData를 진실로 읽기(현재 aggregator 우선).
- **R6 인스펙터** → `data.BaseValue`/`CurrentValue` 직접 읽기를 API 경유로.
- **선행 의존:** W1의 ② aggregator 경로가 실제로 돌려면 `AttributeAggregator.BroadcastOnDirty`의 `_dependents` null NPE부터 정리돼야 함(현재 초기화 안 됨). cross-attr 전파(D12) 배선과 함께 볼 것.
- **W1 미러 write 계층(2026-08-24 확정):** 미러 raw write = 핸들 `GameplayAttributeHandle.SetBaseValueRaw`/`SetCurrentValueRaw`(read-modify-write, 전파 없음), 고수준 `AttributeSet.SetBaseValue`/`ASC.SetAttributeBaseValue`(파이프라인)와 분리. 핸들 `SetBaseValue` 내 `set.SetBaseValue(this,value)` 순환 호출 제거 필요.
- **`OnAttributeAggregatorDirty` 버그 2건(2026-08-24 발견, 미수정):** ① `:256` `!Mathf.Approximately` 조건 반전 ② `:261` 인자 없는 `attributeSet.SetCurrentValue()` 유령 호출. Current 반영은 `:262` `SetCurrentValueRaw` 한 줄로 충분(raw = 재진입·dependents 중복 방지).
- **감사 결과 요약(2026-08-16 세션):** 읽기 R1~R5는 이미 aggregator 경유 OK, R6만 우회. 쓰기 W1·W2가 force-create로 UE lazy 모델과 어긋남. W3(persistent mod 등록)·W4·W5는 OK.

> ⚠ **프로젝트 방향 전환(2026-08-09):** 메인 포트폴리오 = GAS 온전 작동 완성도 데모(→ [design.md 포트폴리오 방향](../../../../project/design.md)). aggregator(라이브 재평가)는 그 데모가 요구할 때 이어간다 — **다음 세션 첫 액션은 이 feature가 아니라 데모 씬 확정**([NOW.md](../../../NOW.md)).

**2026-08-10 세션 구현 — 미검증·미커밋:** BaseValue·mod 소유를 Aggregator로 확정(→D7·D8)하고 구현했다.
- `AttributeAggregator`를 **per-attribute 값/채널 aggregator**로 재작성: BaseValue+mod 채널 소유, `Evaluate()`가 유일 계산 경로, `ExecModOnBaseValue`(base 연산) 이관. 반응성 worklist는 이 객체에서 제거.
- **컨테이너 재배선:** 맵을 `Dictionary<AttributeHandle, AttributeAggregator>`(per-attribute)로, apply=`AddSpecMods`(AddMod)/remove=`RemoveSpecMods`(RemoveModsFrom), 재계산은 `aggregator.Evaluate`, 반응성 worklist를 aggregator→컨테이너로 이관. 인라인 `CalculateAttributeCurrentValue` 폐기. base는 aggregator 진실 + `AttributeData` 미러 동기화(`SetAggregatorBaseValue`). `_tempHandles` 재진입 취약점을 spec.Modifiers 직접 순회로 제거.

> ⚠ **2026-08-11 상태(유저 명시): dirty·aggregator 쪽 미완성 + 작성 코드 신뢰성 100% 아님.** cross-attr 라이브 재평가가 빠져 있고 Play 미검증 — 정답으로 두지 말 것.

> ⚠ **2026-08-15 방향(유저 명시): dirty/aggregator는 유저가 다음 세션에 UE 큰 흐름 + 본인 이해대로 직접 재구현한다 — 현재 코드는 왠만해선 무시.** 아래 "남은 것"의 4조각 계획은 *현재 코드 위에 얹는* 전제라 **유저 주도 재구현으로 대체될 수 있다.** 재구현의 기반·정답으로 두지 말 것. 참조 = [porting-notes.md](porting-notes.md)(유저 리서치)·[ue-reference.md](ue-reference.md)·UE 원문. 에이전트는 선설계·선구현 금지, 유저가 이끄는 대로 보조.
> **재구현 큰 틀(유저 확정, 2026-08-15):** ① Aggregator Dirty 패턴 ② Dependents 기반 Broadcast(값 쓰는 GE 핸들 리스트) ③ 순환 의존성 방지(cap 등). **호출 순서 등 세부는 UE를 왠만해선 따른다.**

남은 것(참고 — 유저 재구현 방향에 종속):
1. **cross-attr 라이브 재평가 복원(→D12, UE식 확정·일부 구현):** 4조각 — ① `AttributeAggregator.Dependents`+`AddDependent` ② 등록 배선(attribute-based non-snapshot mod apply 시 소스 aggregator에 의존 등록, 캡처 컨테이너에 non-snapshot 소스 열거 API 신설) ③ ~~컨테이너 `OnMagnitudeDependencyChange`~~ **구현(2026-08-15, →worklog):** ASC 진입점 래퍼 + 컨테이너 재평가·재동기화. ④ `OnDirtyRecursive`(Dependents 순회→ASC 호출)+`MAX_BROADCAST_DIRTY` 전파 연동. **①②④ 구현 완료(2026-08-25) — 등록 배선·복사 버그·NPE 해소, persistent 한정.** ③은 유저 in-place `UpdateAggregatorMod`. 남은 것 = `OnAttributeAggregatorDirty :256` 조건 반전 버그 수정 + Play 검증.
2. **유저 Play 검증(수용 기준):** GE 적용/해제/Tick·Instant base 변경·cross-attribute 라이브(힘→방어력, 복원 후)·self-effect·snapshot 회귀 없음·자기참조 무한루프 없음·ASC Inspector 회귀 없음. 컴파일·Play는 유저 몫.
2. **본문 재작성:** 이 progress의 목표/범위/설계 개요/세부 TODO에 남은 "경량 R3-only" 서술을 새 방향(per-attribute aggregator)으로 갱신.
3. **architecture 갱신:** `architecture/ability-system/aggregator.md`를 구현된 구조로 재작성(현재 옛 경량안 서술).
4. 태그 seam(R5)은 후속. 미커밋 — 커밋 정리는 [TODO-BOARD](../../../TODO-BOARD.md).
