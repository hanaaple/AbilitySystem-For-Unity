# TODO-BOARD — 작업 현황 보드

> feature 단위 추적은 `feature-list.md`가 담당한다. **이 보드는 그보다 잘게 쪼개진 잡다한 작업·개인 TODO·이슈**를 모은다 — 문서 정비, 인프라(GitHub 설정 등), 실험, 리팩터, 아이디어 등 feature로 묶기 애매한 것들.
> 관리 규약은 [TODO-BOARD.harness.md](TODO-BOARD.harness.md). 항목은 상태가 바뀌면 아래 칸 사이를 이동한다.

- 최종 갱신: 2026-10-23 (KST)

---

## 📋 TODO (데모 마무리 → 포트폴리오 — 2026-10-22)

> 이 칸으로 데모 목표는 끝(유저 2026-10-22). 위에서부터 순서대로. 테스트는 작업마다 돌리지 않고 4단계에서 몰아서 한다(test-harness 원칙 1).

- [ ] **(user)** **5. 포트폴리오 스크린샷·영상** _(2026-10-22 등록)_
- [ ] **(docs)** **6. 포트폴리오 작성** _(2026-10-22 등록)_

## 📋 TODO (GE·Tag 남은 것 — 2026-10-20 정리)

> 2026-10-20 유저 "그것들 TODO 문서로 해두고 기존 문서에 있던건 해당으로 옮기면서 하고" — GE·Tag 전체 회귀·코드 점검 뒤 남은 것을 한곳에 모았다. 이미 보드에 있던 항목은 본문 그대로 옮겼고(원래 칸: '뒤로 미룸'·'이전 당장 목표'·'이전 우선순위 낮음'), 새 항목은 2026-10-20 등록. 근거: gameplay-tag worklog 2026-10-20 '전체 회귀 + 코드상 빈 곳 점검', gameplay-effect worklog 2026-10-20.

### GE 기능 (미구현)

- [ ] **(code)** GE **SetByCaller** 구현 — 유저 도입 확정(→gameplay-effect D20(spec API+clone 유지)). 런타임 계산값(차징·콤보·RNG 등 **어트리뷰트가 아닌 값**)을 spec에 태그 키로 꽂아 적용. spec 직접생성 API(`MakeOutgoingSpec`)+`Clone()`의 실사용 소비처 — 이 기능 때문에 asset-only 축소를 반려하고 spec-API·clone을 유지했다(D20(spec API+clone 유지)). AttributeBased(어트리뷰트 유도)로 표현 안 되는 magnitude를 다룬다. 2026-10-19 `MagnitudeCalculationType.SetByCaller` 자리만 생김(인스펙터 "구현 예정", 런타임 계산 실패 → 0 + 경고). _(2026-08-04 등록 — class/struct·clone 설계 논의 중 확정)_
- [ ] **(code)** GE **Custom Calculation Class** magnitude 구현 — UE `UGameplayModMagnitudeCalculation`(`EGameplayEffectMagnitudeCalculation::CustomCalculationClass`). 2026-10-19 enum 자리만 생김(인스펙터 "구현 예정", 런타임 계산 실패 → 0 + 경고). _(2026-10-19 등록)_
- [ ] **(code)** GE Execution **Transient(임시 변수) 집계기** — 캡처 어트리뷰트 쪽 런타임 적용(①)은 2026-10-20 완료(gameplay-effect worklog), 남은 건 ②. 아래는 등록 당시 본문. 편집기·데이터는 2026-10-19(→gameplay-effect D23). ① Execution 파라미터를 만들 때 Calculation Modifier로 집계기 두 묶음: 캡처 어트리뷰트 → 그 aggregator 사본 + mod(실제 값 불변), Transient 태그 → 기본값 0 새 aggregator + mod(UE `GameplayEffectExecutionCalculation.cpp:26-57`). ② Transient: Execution이 `Defs()`처럼 virtual 함수로 태그 선언 → 편집기 Backing Data 선택지에 추가, Calculation Modifier에 `aggregatorType`·`transientAggregatorIdentifier` 필드, Execute는 `AttemptCalculateTransientAggregatorMagnitude`로 읽음(mod 없으면 false → 기본값은 Execute가). 쓰임: 같은 Execution을 여러 GE가 쓰면서 GE별 보정값(+20·×1.5)을 줄 때. 코드 TODO 주석: `GameplayEffectExecution`·`GameplayEffectExecutionScopedModifierInfo`·`GameplayEffectExecutionParameters`. 구현하면 `CalculationModifierSelfCheck` E-2(지금은 "런타임이 아직 안 씀"을 고정) 기대값을 바꾼다. 착수 전 확인: `AttributeAggregator` 단독 생성 가능 여부, UE가 mod 없는 캡처도 사본에 넣는지(`:63` 생성자). _(2026-10-19 등록 — 유저 "TODO로 두자")_
- [ ] **(code)** GE Execution **파라미터 조회 API 나머지** — UE `FGameplayEffectCustomExecutionParameters` 중 없는 것. 빈 시그니처(`NotImplementedException`)+TODO: `AttemptCalculateCapturedAttributeMagnitudeWithBase`(aggregator `EvaluateWithBase(base, 평가 파라미터)` 선행)·`AttemptCalculateCapturedAttributeBonusMagnitude`(UE `EvaluateBonus` 선행)·`AttemptGetCapturedAttributeAggregatorSnapshot`. TODO 주석만: `AttemptGatherAttributeMods`·`ForEachQualifiedAttributeMod` — aggregator `Mod`가 private이라 시그니처 불가, 공개 방식 결정 필요. 모두 Calculation Modifier 사본을 먼저 읽어야 한다. 위치: `GameplayEffectExecutionParameters.cs`. _(2026-10-20 등록 — 유저 "빠진것들 TODO로 빈 시그니처로만 만들어둬")_
- [ ] GE 스택 수 2 이상 — `StackLimitCount`, 스택 수만큼 modifier 세기, 만료 정책(`StackExpirationPolicy`), overflow. 스택 수 1(합치기·지속 시간·주기 갱신)은 2026-10-23 완료(gameplay-effect D31).
- [ ] **(code)** **ScalableFloat 레벨 커브** — magnitude가 지금은 고정 float. Level 기반 커브 테이블로(UE `FScalableFloat`). 코드 TODO `GameplayEffectModifierMagnitude.cs:14`. _(2026-10-20 등록)_
- [ ] **(code)** **Conditional GE** (신규) — GE 적용 시 Source 태그 조건이 맞으면 다른 GE를 따라 적용한다. UE `FConditionalGameplayEffect::RequiredSourceTags`(5.6 `GameplayEffect.h:495`). 우리 쪽엔 기능 자체가 없다. 선행: gameplay-tag의 GE 태그 연동. _(2026-10-16 등록 — 태그 후보 조사)_
- [ ] **(code)** **GE 태그 부가 기능** — UE GE Component 중 Immunity·Chance to Apply·Additional Effects·Custom Can Apply, Spec 동적 태그(`DynamicGrantedTags`·`DynamicAssetTags`, 세부 TODO 11), AttributeBased 태그 필터(세부 TODO 9), Execution `PassedInTags`·scoped modifier 태그(세부 TODO 12). Block Ability Tags·Abilities Component는 gameplay-ability 때. _(2026-10-18 등록 — 목표 범위 정리 중 제외)_
- [ ] **(code)** **GE 데이터 검증(UE `IsDataValid`)** — 에디터에서 잘못된 GE 조합을 오류로 알리는 체계가 없다. 알려진 경우: Instant GE에 Granted Tags(UE `TargetTagsGameplayEffectComponent`), Instant GE에 Ongoing 태그 조건(UE `TargetTagRequirementsGameplayEffectComponent`). 할지 유저 결정 대기(gameplay-tag progress 세부 TODO 6). _(2026-10-20 등록)_

### GE 버그

> Tick 순서가 결과에 영향을 주는 곳 — 구현·보완할 것은 [tick-order-improvements.md](feature/ability-system/gameplay-effect/tick-order-improvements.md), 사용자가 알아야 할 것은 [tick-order-user-notes.md](feature/ability-system/gameplay-effect/tick-order-user-notes.md). 아래는 improvements의 할 일. ('적용 프레임 deltaTime 차감' 버그는 2026-10-21 해결 → TODO-DONE)

- [ ] **(bug)** **Tick 도중 같은 ASC에 GE를 걸거나 지우면 예외 가능(미검증)** — 주기 GE 실행 → AttributeSet 훅·값 변경 이벤트 구독자가 같은 ASC에 Duration/Infinite GE를 걸거나 활성 GE를 지우면, `Tick`의 `foreach (… _activeGameplayEffects.Values)` 도중 사전이 바뀌어 `InvalidOperationException`이 날 수 있다(코드에서 추론, 실행 안 함). UE는 스코프 락 동안의 추가를 보류 목록에(`GameplayEffect.h:1978`). 먼저 재현 테스트 → 사본 순회 또는 보류 목록. tick-order-improvements.md 항목 2. _(2026-10-21 등록)_
- [ ] **(code)** **남은 시간 조회를 조회 시점 시각으로** — `GetTimeRemaining`이 Tick 때 갱신한 값이라 같은 프레임에 ASC.Update보다 먼저 읽으면 지난 프레임 값. UE처럼 `Duration - (GetWorldTime() - StartWorldTime)`으로(`GameplayEffect.h:1336`). tick-order-improvements.md 항목 3. _(2026-10-21 등록)_

### Tag

- [ ] **(code)** **gameplay-tag D3 대소문자 무시 비교 + 표기 보존** — 결정은 됐고 구현이 없다(`InternedName`은 대소문자 구분). progress 다음 작업 5. _(2026-10-18 보드 등록)_
- [ ] **(code)** **Gameplay Event** (신규) — `HandleGameplayEvent`/`SendGameplayEventToActor`, 이벤트 기반 능력 발동(예: 피격→반격). 선행: Tag. _(2026-08-09 등록)_
- [ ] **(code)** **호출하면 예외가 나는 미구현 API** — `GameplayTag` `<`/`>`(`GameplayTag.cs:41`), `GameplayTagsManager`의 `DoneAddingNativeTags`·`CallOrRegister_OnDoneAddingNativeTagsDelegate`·`SplitGameplayTagFName`·`RequestAllGameplayTags`(`:265-287`)가 `NotImplementedException`. `AddChildrenTags`는 `onlyIncludeDictionaryTags: true`면 예외(`:415` — 노드에 명시/암시 기록이 없어서). 지금 호출하는 곳은 없다. _(2026-10-20 등록)_
- [ ] **(editor)** **ASC 인스펙터에 소유 태그 표시** — gameplay-tag 수용 기준 "인스펙터에서 확인 가능"이 아직 안 됨(progress 세부 TODO 7 남은 것). _(2026-10-20 등록)_
- [ ] **(code)** **Tag 코어 후속(미룸)** — IL2CPP 코드 제거 시 마커 필드 유실 대응(`[Preserve]` 등), 빌드 탐색 비용 측정, 노드 출처·명시/암시 기록. D5의 막는 이유 유저 확인. 출처: gameplay-tag progress 다음 작업 1. _(2026-10-20 보드 등록)_
  - **2026-10-20 확인:** 원래 있던 "레지스트리 편집 시 에디터 매니저 재구성"은 이미 됨 — 관리 창의 추가·이름 변경·삭제가 `GameplayTagRegistryEditing`에서 `EditorRefreshGameplayTagTree`를 부른다(tests.md §4-1·4-2에서 바로 반영 확인).

### 검증
- [ ] **(bug)** **Modifier Tag 셀프체크 E-12가 Play 모드 뒤에 실패** (2026-10-21 발견, 미수정) — 에디터 `Time.time`이 0이 아니면(직전 Play 모드로 10.50373) 주기 1초 GE의 경계 Tick(시계를 딱 +1초)에서 주기 실행이 빠져 64/1. 시계를 0으로 맞추면 65/0 — 부동소수 경계로 보임. 테스트 경계를 넉넉히 할지(GA 테스트는 D27 때 그렇게 함), GE 주기 판정 쪽을 볼지 유저 판단. GE 쪽이 원인인지는 확인 안 함. _(2026-10-21 등록)_

- [ ] **(verify)** GE 캡처 **end-to-end 유저 검증** — 5b evaluate로 소비처가 생겼다(`GameplayModifier.EvaluateMagnitude` → `AttributeBasedMagnitude.Evaluate`가 캡처값 조회). 이제 **유저가 에디터에서 Source·snapshot·evaluate 경로가 실제로 맞게 캡처·계산하는지 처음 확인**한다 — `GE_EquipSpeedDown`이 Source `CombatAttributeSet.damage` 기반으로 speed를 깎는지. ⚠ 선행: `Player_AttributeInitData`에 `CombatAttributeSet` 존재(없으면 캡처 무효→경고+0). (`SpeedBoostExecution.Execute`는 여전히 +10 고정 — Execute 경로의 캡처 소비는 별도.) **검증 절차·항목 = [gameplay-effect/tests.md](feature/ability-system/gameplay-effect/tests.md).** _(2026-08-04 갱신 — 5b로 소비처 배선됨, 검증만 남음)_
- [ ] **(verify/setup)** **BoxRoom 검증 세팅 완성** — 디버그 도구는 만들어짐(2026-08-05, `BoxRoom.cs`+`.prefab`, ASC Inspector 창). 남은 세팅: ① 프리팹의 BoxRoom이 **Missing Script면** `m_Script` guid를 실제 값으로 교체(에이전트가 `.cs.meta` GUID `3a9f2b7c…` 발급). ② 3모드 GE 슬롯 꽂기 — 특히 **Buff에 `GE_EquipSpeedDown`**(Source damage 기반 speed 감소) → 5b evaluate end-to-end 검증과 직결. ③ **Room ASC `attributeInitData`**(Source 캡처 테스트 시) + **Player ASC에 `CombatAttributeSet`**(없으면 캡처 무효→경고+0). ④ 씬 배치·시각화(선택). 검증 항목 = [gameplay-effect/tests.md](feature/ability-system/gameplay-effect/tests.md). _(2026-08-05 등록)_
  - **2026-10-20 확인:** ① 해소 — 프리팹의 `m_Script` guid가 `BoxRoom.cs.meta`(`3a9f2b7c…`)와 같다. ②③은 프리팹 구조가 바뀌어(`otherEffect`·`selfEffect`·`sourceAttributes`) 판단 못 함.

### 결정 대기

- [ ] **(decision)** **Execution 파라미터 생성자 순서** — 지금 `(target, spec, scopedMods)`, UE는 `(Spec, ScopedMods, Target, PassedInTags, PredictionKey)`. UE 순서로 바꿀지(호출처 1곳). _(2026-10-20 등록)_
- [ ] **(decision)** **gameplay-tag 결정 대기 3개** — ① 경량 경계(레지스트리 유무·형태, 등록 시점 — D3 대소문자와 묶임) ② GE 연동 범위 남은 ②④(GE 범위 / 요구 조건 TagQuery 포함 여부) ③ `RequestGameplayTag` 입력 ergonomics. 상세: gameplay-tag progress '결정 대기'. _(2026-10-20 보드 등록)_
- [ ] **(decision)** **Query 내부 표현** — UE대로 토큰 스트림(D26). 식 트리로 바꿀지 유저 미확정(바꾸면 저장된 질의를 옮겨야 함). 출처: gameplay-tag progress 다음 작업 7. _(2026-10-20 보드 등록)_
- [ ] **(design)** GE 캡처 **사양 확정 2건 (소비처 배선 전에 정해야 함)** — 2026-08-04 설계 감사에서 도출. ① **"spec 생성 시점 = 시전 시점"이 이 프로젝트 의도인가** — Source를 `GameplayEffectSpec` ctor에서 캡처(`CaptureDataFromSource`)하는 전제다. `MakeOutgoingSpec`을 시전보다 훨씬 먼저 만들어 들고 있다가 적용하는 사용을 허용할 거면 ctor 캡처가 어긋난다(snapshot이 시전이 아닌 생성 시점에 얼어붙음). 코드·UE가 아니라 **유저 사양**으로 확정 필요. ② 확정되면 그 근거를 gameplay-effect `decisions.md`에 D로 남긴다(HARNESS '왜는 지어내지 않는다' — 지어내지 말고 유저 답을 적음). _(2026-08-04 등록)_
- [ ] **(chore)** **기존 GE 에셋 coefficient 0 점검** — `OnAddModifier` 버그(add 시 coefficient 0)로 이 수정 **이전에** 만든 AttributeBased modifier는 0으로 저장돼 있을 수 있음(식 전체가 0이 됨). 새 수정은 신규 추가분만 1로. 기존 에셋(`GE_EquipSpeedDown` 등)은 인스펙터에서 수동 확인·1로. _(2026-08-05 등록)_
  - **2026-10-20 확인:** `Assets/Data/GE_MeleeDamage.asset`이 AttributeBased(`magnitudeCalculationType: 1`, Source `stamina`)인데 `coefficient: 0` → magnitude가 늘 0. 의도인지 유저 확인 필요(고치지 않음). `GE_EquipSpeedDown`의 `coefficient: 0`(`:78`)은 ScalableFloat modifier라 쓰이지 않는다.

### 정리 작업

- [ ] **(docs)** **architecture 문서를 PR #8에 맞춤** — `architecture/ability-system/attribute.md`(`AttributeSet` "빈 추상 마커"(19행)·"Current는 aggregator가 있으면 `Evaluate()`로만"(114행) — 지금은 저장값 읽기·훅 표에 Base 훅 2개뿐(143·152행)), `aggregator.md`(CurrentValue 산출 서술), `gameplay-effect.md`(쓰기 경로 `ApplyModToAttribute`(122행) → 지금은 `InternalExecuteMod`를 거침, 값 변경 이벤트 없음). feature progress는 PR #9로 맞췄고 architecture는 유저 "feature 쪽만"으로 미룸. _(2026-10-20 등록)_
- [ ] **(code)** GE 캡처 **params 조회 함정 3종 (5b·Execute 작성 시 주의)** — 캡처를 실제로 읽는 코드를 쓸 때 걸리기 쉬운 지점. ① **false가 조용하다:** `AttemptCalculateCapturedAttribute*`(`GameplayEffectExecutionParameters.cs:47·54`)는 실패 사유(미등록/캡처 무효/Target 미캡처)를 전부 `false`+`0`으로 뭉개고 **경고 로그 없음** — 호출측이 bool을 안 보면 0이 계산식에 샌다. ② **값-동등성 key 취약:** 조회는 `BackingDefinition.Equals`(captureSource+attribute AQN+snapshot)라 **셋 중 하나만 달라도 매치 실패→false**. 조회 정의는 선언 정의와 완전히 같아야 함(SpeedBoost처럼 같은 static 필드 재사용이 안전). ③ **captureValueType 라우팅:** params 메서드가 Magnitude=Current / BaseValue=Base로 고정 분기인데 authoring의 `AttributeBasedMagnitude.captureValueType`(`:33`)가 Base/Current를 데이터로 지정 — 5b/Execute가 이 값에 맞춰 올바른 메서드를 호출해야 authoring 의도와 맞는다. _(2026-08-04 등록 — 캡처 조회 경로 분석 중 발견)_
- [ ] **(docs)** **attribute 런타임 타입 이름 정렬 + D5(직렬화/런타임 참조 분리) 재작성** — 코드는 `GameplayAttributeHandle`인데 2026-08-21 세션에 내가 적은 문서·주석 일부가 아직 `ResolvedGameplayAttribute`다(그 이름으로 확정했다가 유저가 되돌림). 정정 대상: attribute `decisions.md`(D5(직렬화/런타임 참조 분리))·`progress.md`, architecture `overview`·`attribute`·`gameplay-effect`, 잔재 2건(`AttributeAggregator.cs` 주석 `Dictionary<ResolvedGameplayAttribute,…>`·`GameplayEffectSpec.cs` 경고 문자열). **D5(직렬화/런타임 참조 분리)는 근거 자체 재작성 필요** — "Handle이 부정확"이라 `Resolved-`를 택한 논리가 최종 `GameplayAttributeHandle`(Handle 유지)과 모순. 유저에게 "왜 Handle로 되돌렸나" 받아 그 근거로 다시 쓴다(HARNESS '왜는 지어내지 않는다'). _(2026-08-21 등록)_
  - **2026-10-18:** decisions 정리로 attribute D5는 "치운 결정"으로 내려갔다(이름은 `GameplayAttributeHandle`로 되돌아갔고 분리는 D1·gameplay-effect D15). D5 재작성은 더 필요 없고, 남은 일은 문서·주석의 `ResolvedGameplayAttribute` 잔재 정리뿐.
- [ ] **(editor)** **GE Executions 리스트에 New Script 지원** — Attribute Sets 리스트엔 New Script가 있으나 Executions엔 없음(2026-08-07 Add만 붙임). 이유: `GameplayEffectExecution.Execute`가 abstract라 빈 서브클래스 템플릿(`: GameplayEffectExecution {}`)이 컴파일 안 됨. 붙이려면 New Script 템플릿(`SubclassScriptTemplate.BuildSource`)이 **abstract 멤버의 override 스텁 생성**을 해야 함(예: `Execute(...) => throw new NotImplementedException();`). 그 스텁 생성만 추가하면 GE Executions·다른 abstract-base 셀렉터에도 켤 수 있음. _(2026-08-07 등록)_
- [ ] **(code)** **타입 참조를 rename-safe 하게 (AttributeSet·GE Execution)** — 지금 둘 다 타입을 **AQN 문자열**로 저장(`attributeSetTypeName`·`executionTypeNames`)해, 클래스명·네임스페이스·asmdef 이동 중 뭐든 하면 `Type.GetType` 실패→"Missing"으로 링크가 끊긴다. **지금은 미룸** — 규모 작고(서브클래스 몇 개), 깨져도 인스펙터에 보이고 검색 드롭다운에서 다시 고르면 됨. 고치는 값이 큼(포맷·리졸버·드로어·런타임·New Script 자동배정·기존 에셋 마이그레이션 전부)이라 과설계에 가깝다. **재평가 트리거:** rename이 잦아져 여러 에셋을 반복해 다시 잇게 되면. **하게 되면:** 안정 GUID id(`[StableTypeId(guid)]` + id→Type 리졸버, AQN 폴백 가능) — 둘 다 문자열 저장이라 통일 적용됨. (`[MovedFrom]`은 커스텀 `Type.GetType` 방식엔 안 먹혀 탈락, SerializeReference는 GE만 가능.) _(2026-08-07 등록 — New Script 툴 논의 중 도출)_

## 📋 TODO (뒤로 미룸)

> 2026-10-18 목표 전환으로 미뤘다. 항목 본문은 옮기기 전 그대로다.
> 2026-10-20 유저 "TODO 쪽에 이전에 GAS 제외한 TODO인 부분들 다 치우자. 아카이빙은 해두고", "_parked로 옮겨" — 게임 쪽(아이템·캐릭터·아트) 항목은 [`_parked/agent/TODO-BOARD.md`](../_parked/agent/TODO-BOARD.md)로 보관. 그 때문에 '2026-10-18 목표에서 미룬 것'(GE·Tag는 이미 'GE·Tag 남은 것'으로 옮김)·'이전 당장 목표·진행 중이던 것' 소제목은 비어 지웠다. 남은 건 GAS·문서 인프라 항목.

> **이전 우선순위 낮음** (아래 GAS 대분류·기술은 포트폴리오 방향 전환(2026-08-09, [design.md 포트폴리오 방향](../project/design.md))의 목표 기능이다. 데모 씬 확정 후 필요분만 착수·feature 등록한다. 각 기능은 UE-complete가 아니라 "최소한 진짜 도는 버전".)

- [ ] **(art)** **데모 아트** — 2026-10-21 유저 "아트 관련은 다 미뤄둬". 데모 '개발 순서'에서 옮김.
  - ~~캐릭터 모델·애니메이션 + Animator 이동 연동~~ — 2026-10-21 3D-Action 프로젝트 것 사용(`Art/Character`), `Demo_Player`(단독 프리팹)에 연결, `PlayerCharacter`가 `MoveDirection X/Z`·`Speed`를 넣음.
  - 2026-10-21 Movement 레이어 정리 — 뒤 대각선(`StaticBlend`) 제거·뒤쪽 Speed 1D, 옆 걸음 2/6 구간, 빈 Crouch 제거, 이동 클립 이벤트(`OnFootstep`·`OnLand`·`OnRoll`) 제거. 발소리는 위 사운드와 함께.
  - 공격 클립 이벤트(`SetCombo*`·`SetRotation*`)는 받는 쪽 없음 — 공격 작업 때.
  - `Humanoid.controller` 상태에 붙은 3D-Action StateMachineBehaviour 10개가 이 프로젝트에 없음.
  - GA 발동 시 애니메이션 — 몽타주는 GA 범위 밖이라 게임 쪽에서 `OnAbilityActivated` 구독 → Animator 트리거. 반복 공격 허수아비의 공격 애니메이션 포함.
  - 방어 마법 피해 감소 피드백 — 감소가 적용된 피격마다 소리 + 이펙트(방어 마법이 스킬 목록에 남으면).
  - 용암·독가스 바닥 표시(머티리얼·파티클).
  - **DOTween — Cue juice** — Cue 레이어 전용(피격 `DOPunch`·스턴 `DOShake`·버프 pulse·데미지 숫자·카메라 shake). GAS 코어엔 넣지 않고 Cue 이음새 뒤에. `SetLink`로 오브젝트 파괴 시 자동 kill. _(2026-08-09 등록)_ — Cue를 미뤄 "Cue 이음새 뒤" 전제가 깨짐, 이벤트 구독 쪽에 붙인다(에이전트 제안).
  - 스킬·피격 이펙트, 사운드 에셋.
- [ ] **(code)** **GA — UE 대비 미구현(데모 뒤로 미룸)** — 2026-10-21 유저 "GameplayAbility 남은거 있어? UE 기능과 비교해서 확실하게 확인해" → "싹 정리하고 그 중에서 현재 데모를 목표로 하는데 필수로 필요한 건 현재 TODO로 넣어두고 나머지는 미루는 TODO로 해". UE 5.6 `GameplayAbility.h`·`AbilitySystemComponent.h` 선언을 우리 GA·ASC·Spec과 대조(헤더 grep). 데모 필수는 '데모 전 GAS 구현' 2-9로, 나머지가 아래. 필수/미룸 구분은 에이전트 판단(유저 미확정). D# = gameplay-ability decisions에 뺀 이유가 있는 것. _(2026-10-21 등록)_
  - **입력** — GA `InputPressed`/`InputReleased`, ASC `AbilityLocalInputPressed/Released`·`PressInputID/ReleaseInputID`·`FindAbilitySpecFromInputID`·`ClearAllAbilitiesWithInputID`·`Block/UnBlockAbilityByInputID`·`IsAbilityInputBlocked`(`AbilitySystemComponent.h:1117-1410`). 우리 Spec엔 `InputID` 값만 있다. 데모 입력은 게임 쪽에서 `TryActivateAbility(handle)`로('데모 제작 > 입력 바인딩').
  - **발동 방식** — `TryActivateAbilityByClass`(우리 쪽이면 에셋으로)·`TryActivateAbilitiesByTag`·`GetActivatableGameplayAbilitySpecsByAllMatchingTags`(`:1044-1060`). 태그 쪽은 아래 어빌리티 태그가 먼저.
  - **커밋 세부** (D7) — `CommitAbilityCooldown`·`CommitAbilityCost`(+`ForceCooldown`). 실패 태그 out 인자는 2-9에서.
  - **생명주기 세부** (D4) — `CanBeCanceled`/`SetCanBeCanceled`(+ ASC `HandleChangeAbilityCanBeCanceled` — 2-10 때도 D4대로 뺌, 2026-10-21 유저 "그것들 TODO로 해두고"), `bIsAbilityEnding`, GA 자체 스코프 락, 재발동 `bRetriggerInstancedAbility`. 기록 없이 빠진 것: `SetRemoveAbilityOnEnd`, `GiveAbilityAndActivateOnce`(progress엔 뺀 것으로 있음), `Get/SetUserAbilityActivationInhibited`, `ClearActorInfo`·`RefreshAbilityActorInfo`, `NotifyAvatarDestroyed`.
  - **이벤트 발동** — `AbilityTriggers`, `ShouldAbilityRespondToEvent`, `TriggerAbilityFromGameplayEvent`, `HandleGameplayEvent`, `SendGameplayEvent`, `IsTriggered`. 선행: 'GE·Tag 남은 것 > Tag > Gameplay Event'.
  - **GE 연동 세부** (D5) — 컨텍스트 `SetAbility`·SourceObject 타입(보류), `ApplyAbilityTagsToGameplayEffectSpec`. 기록 없이 빠진 것: `GetCurrentSourceObject`/`GetSourceObject`, GE가 부여한 어빌리티(`RemoveGrantedByEffect`·`GetGrantedByEffectContext`·ASC `FindAbilitySpecFromGEHandle` — progress엔 뺀 것으로 있음), GA의 GE 제거 헬퍼(`BP_RemoveGameplayEffectFromOwnerWith*` — ASC엔 같은 기능 있음).
  - **Source/Target 태그 조건** — `SourceRequiredTags`·`SourceBlockedTags`·`TargetRequiredTags`·`TargetBlockedTags`(`GameplayAbility.h:779-791`)와 `DoesAbilitySatisfyTagRequirements`의 sourceTags/targetTags 인자. UE에선 이벤트 발동 데이터(TriggerEventData)의 Instigator/Target 태그로만 검사돼 이벤트 발동과 같이 들어온다. 2026-10-21 2-10 착수 때 유저 "빼기" → "해당 나중에 TODO로 해둬".
  - **취소 Ignore 인자** — ASC `CancelAllAbilities(Ignore)`(`AbilitySystemComponent.h:1089`). D8 때 쓰는 곳이 없어 뺐고 2-10에서도 `CancelAbilitySpec`·`CancelAbilities`에만 넣음. 2026-10-21 유저 "그것들 TODO로 해두고".
  - **인스턴싱** (D6) — `NonInstanced`.
  - **편의** — `GetCurrentAbilitySpec`, 인자 없는 `GetCooldownTimeRemaining()`, 데이터 검증 `IsDataValid`(GE 쪽 'GE 데이터 검증'과 같은 종류).
  - **큰 기능(GA 범위 밖으로 둔 것)** — AbilityTask·GameplayTask(`ActiveTasks`·`*TaskByInstanceName`·`EndAbilityState`), 몽타주(`CurrentMontage`·`Montage*`·ASC `PlayMontage`·`GetAnimatingAbility`), 타겟팅(TargetData·`MakeTargetLocationInfo*`), GA에서 Cue 호출(`K2_*GameplayCue*` — Cue는 아래 'Gameplay Cue 구현'과 함께).
  - **안 함(UE 고유 제약이라 해당 없음)** — 네트워크·복제·예측(Replication/NetExecution/NetSecurity 정책, `HasAuthority`·`IsPredictingClient`·`ConfirmActivateSucceed`·`MarkAbilitySpecDirty` 등), 블루프린트 래퍼(`K2_*`·`BP_*`), 스켈레탈 메시 조회.

- [ ] **(bug)** **GA 타입 선택 목록에 테스트 전용 클래스 노출** (2026-10-21 발견, 미수정) — `EditorTypeUtility.GetConcreteSubclasses(GameplayAbility)` 후보에 `Test.AbilitySystemLifecyclePlayModeTest+LingeringAbility`(이번 세션 PlayMode 테스트, 런타임 어셈블리)·`Test.Editor.GameplayAbilitySelfCheck+RecordingAbility`(기존 셀프체크)가 `TestGameplayAbility`와 함께 뜬다. 중첩 클래스라 스크립트 파일이 없어 고르면 아이콘이 비고 ping/open도 안 된다(유저가 `GA_New`에서 겪음). 고칠지·어떻게(선택 목록에서 숨김 표시 / 테스트 클래스를 따로 파일로) 유저 판단. 2026-10-21 유저 "GA 타입 선택 목록에 테스트 전용 클래스 노출 이거 뒤로 미루고" → '데모 전 GAS 구현 > 2. GameplayAbility'에서 이리로 옮김. _(2026-10-21 등록)_
- [ ] **(code)** **GA 명시적 해제(인스턴스 무효 표시)** — UE `MarkAsGarbage` 대응(무효 표시·참조 끊기·정리 훅/Dispose). 해제된 GA 인스턴스를 밖에서 들고 있으면 계속 부를 수 있다(ASC를 거치면 Spec이 없어 못 씀). 2026-10-21 유저 "GA를 당장 여기서 Dispose를 두기에는 다른 곳들도 사실 할만한 게 많아서 다음으로 미루자"(제안했던 안: A 무효 표시 / B A+정리 훅 / C 약한 핸들), 같은 날 "GA 명시적 해제는 따로 빼둬" → '데모 전 GAS 구현 > 2. GameplayAbility'에서 이리로 옮김. 원래 'ASC 파괴 시 어빌리티 정리 없음' 항목에 묶여 있었고, 파괴 쪽은 2026-10-21 해결(→ TODO-DONE). _(2026-10-21 등록)_
- [ ] **(code)** **GA 실패 사유를 태그 + 설정 에셋 대신 실패 정보(종류 enum + 원인 태그)로 바꿀지** — 2026-10-21 유저 "굳이굳이 Tag가 필요해? 그냥 실패했다고 정보에 각각 뭐가 실패했는지 보내면 되는거 아냐?" → 비교(enum: 설정 에셋·레지스트리 불필요·누락 없음 / 하위 어빌리티 자기 사유 추가가 어렵고 UE와 시그니처 다름) 뒤 "일단 내비둬 탐탁친 않네". 지금은 D9(태그 + `GameplayAbilitiesSettings`) 유지. _(2026-10-21 등록)_
- [ ] **(idea)** **실패 사유 컨테이너를 구독자 사이에서 공유** (2026-10-21 GA 셀프체크 Q-15에서 관찰) — ASC가 시도마다 비워 쓰는 멤버 하나를 `OnAbilityFailed`로 넘겨, 실패 구독자 안에서 다른 발동을 시도하면 바깥 이벤트의 사유가 덮어써진다. UE도 같은 구조(`AbilitySystemComponent_Abilities.cpp:1687`). 구독자가 복사하면 안전(이벤트 설명에 적어 둠). 이벤트마다 복사본을 넘길지 유저 판단. _(2026-10-21 등록)_

- [ ] **(code)** **Gameplay Cue 구현** — 2026-10-21 유저 "데모 전 범위에서 뺌" → 같은 날 "이 부분 아예 밀어둬" — '데모 전 GAS 구현 > 3. GameplayCue (최소)' 칸째 이리로 옮김(본문 그대로). _(2026-10-20 등록)_
  - **2026-10-20 범위(에이전트 제안, 유저 미확정):** ① `GameplayCueEvent`(`OnActive`·`WhileActive`·`Executed`·`Removed`, UE `GameplayEffectTypes.h:958`) ② GE 에셋 Cue 태그 목록(UE `GameplayEffect.h:2299`, 레벨 범위·MagnitudeAttribute 뺌) ③ 발생 지점: Executed=`ExecuteActiveEffectsFrom` 끝(UE `GameplayEffect.cpp:3059`), OnActive·WhileActive=`AddActiveGameplayEffectGrantedTagsAndModifiers`(`:4434-4435`), Removed=`RemoveActiveGameplayEffectGrantedTagsAndModifiers`(`:4734`) ④ Cue 매니저 + 태그→프리팹 표(SO) ⑤ 프리팹 쪽 기본 컴포넌트(DOTween 연출 자리) ⑥ GE 인스펙터 Cue 칸. 빼는 것: 노티파이 클래스 계층·태그 계층 상속 조회·풀링·네트워크. 시간이 모자라면 태그 이벤트로 연출을 직접 붙이는 쪽으로 내려간다.

- [ ] **(infra)** **Ability System 플러그인화(asmdef/UPM 패키지)** — 유저 의도: "애초에 Ability System은 별개의 플러그인처럼". 2026-10-12 논의 후 **유저가 미룸**. 에이전트 제안(미확정): `Packages/com.<scope>.abilitysystem` embedded package(개발·설치 경로 동일). 정할 것: 패키지 이름(scope), feature-local `Editor/` 폴더 처리(asmdef 아래선 Editor 폴더 규칙이 안 먹힘 → 표준 Runtime/Editor 구조 vs `.asmref`), `Core.Common` 처리(패키지 안으로 vs 별도 common 패키지), 브랜치. 확인한 사실: 에셋에 `Core.AbilitySystem…, Assembly-CSharp` AQN 저장 0건, `Common`→AbilitySystem 코드 의존 없음. 착수 시 gameplay-tag D5(생성 차단=Request 입구+internal 생성 함수)의 `GameplayTag.Create`가 실제로 막히는지 확인. _(2026-10-12 등록)_

- [ ] **(editor, bonus)** **에셋 그래프(노드) 에디터** — 시각적 payoff 크나 GraphView 직렬화 rabbit hole. **맨 마지막 선택적 보너스.** 1~2일 스파이크로 리스크 확인 후, 가장 좁은 조각(GE 하나 저작→SO)으로만. 데모 성공을 여기 걸지 않음. _(2026-08-09 등록)_

- [ ] **(docs)** **UE Execution 실행조건 원문 확정분 반영 여부** — 2026-08-05 ylyking UE 미러 원문으로 "period 0 Duration/Infinite에선 Execution 안 돎"을 확정(근거: `ExecuteGameplayEffect` asc.cpp:820 `check(...)` assert + Execute 경로 라인). 이걸 `architecture/ability-system/gameplay-effect.md`나 gameplay-effect `decisions.md`에 근거(파일·라인)와 함께 남길지 **유저 판단 대기**. 위 19번(GameplayEffectType 실전 상태 확정)과 인접하나 별개(enum 확정 ≠ Execution 실행조건). _(2026-08-05 등록)_
- [ ] **(wiki)** 설계 문서 상호 연결 — 각 섹션 요약에서 `dev-docs/project/architecture/*` 심화 문서로 링크. 로컬/Pages에선 `.md`가 raw로 뜨므로 **GitHub blob URL** 사용. 저장소 public 전제. `SECTION_META.sources`도 같은 방식으로 클릭 가능하게. _(2026-07-05 등록)_
- [ ] **(docs/refactor)** **에이전트 생성 "왜"의 유저 소유화 — 이번 세션(2026-08-04) 산출물 정리** — 유저 지적: 코드 주석·decisions(특히 D21(ModifierSpec UE 정렬))에 에이전트가 만든 근거가 많고 본인 생각이 반영 안 됨(HARNESS '왜는 지어내지 않는다'/'왜의 근거는 유저 의도 → UE 원본'). 방침: **코드 주석은 사실(무엇/어떻게+함정)만 lean하게, "왜"는 decisions에만·유저가 소유한 것만.** 접근 미정 — ① `GameplayModifierSpec` 시범 후 세션 파일(`AttributeBasedMagnitude`·`GameplayModifier`·`GameplayModifierSpec`·`GameplayEffectSpec`) 톤 일괄 적용(추천) ② D21(ModifierSpec UE 정렬)부터 같이 훑으며 근거 재작성/삭제. 하네스 규칙화(주석 lean·왜는 유저 소유)도 유저 승인 시 docs-harness '각색은 쓰기 전에 묻는다'/HARNESS '왜는 지어내지 않는다'에 추가. _(2026-08-04 등록)_
- [ ] **(docs)** architecture 하류 문서 **반영 기준 마커 정비** — `camera.md`·`item-equipment.md`·`controller-character.md`·`architecture/overview.md`는 마커 자체가 없어 architecture/HARNESS '최종 갱신' 마커의 낡음 판별(하류 날짜 < 상류 날짜)이 불가능하다. 실제 반영 날짜를 알 수 없어 **지어내지 않고 그대로 뒀다**(그 절 "모르는 날짜는 지어내지 않는다"). 채우려면 **유저가 날짜를 알려주거나, 다음에 그 문서를 실제로 갱신할 때 그날 날짜로 stamp**한다. 더불어 `ability-system/overview.md`는 상류를 feature-id가 아닌 **그룹명**(`ability-system`)으로 적어, 어느 progress와 비교할지 모호하다(그룹 대표 규칙을 정하든 하위 3개 중 하나로 바꾸든 결정 필요). _(2026-07-19 등록 — 하네스 점검 중 발견)_
- [ ] **(docs)** architecture 문서 **UE 구조 미러링 분류 검토** — UE 모듈/타입 계층(`GameplayAbilities` → `U*` → `F*`)을 참고한 폴더·네이밍 분류. 억지로 서두르지 않음. _(상위/하위 계층 정리·Capture 전용 문서(`capture.md`) 분리는 2026-09-10 완료.)_ 상세 `architecture/HARNESS.md` '문서 구조'. _(2026-09-10 등록 — 유저 발의)_
- [ ] **(docs)** **§번호 제거 잔여분** — 이번(2026-08-22) 스윕에서 **의도적으로 남긴 것**: ① ✅ Done 로그의 과거 § 기록(동결 이력 — 당시 섹션번호를 서술한 기록이라 §9로 보존) ② `ue-reference.md` 자체 섹션 번호(§2·§9·§11 — 자기완결 UE 참고 자료, decisions/worklog가 "참고문서 §N"으로 참조) ③ `wiki/HARNESS.md` 자체 번호(§2·§2.1). ①은 이력이라 그대로 두는 게 맞고, ②③은 해당 문서 자체를 이름 기반으로 개편할지 유저 판단 필요. _(2026-08-22 등록)_

## 🔧 진행 중

(없음 — 2026-10-21 지시 없이 구현한 데모 0~4 일부는 '개발 순서' 칸 ⚠ 참고, 유저 판단 대기)

## ✅ Done

완료 항목은 [TODO-DONE.md](TODO-DONE.md)로 옮긴다(2026-10-20부터 별도 파일).
