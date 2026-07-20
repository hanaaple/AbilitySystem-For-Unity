# gameplay-effect — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-07-19 — Execution 프레임워크 완성 (SO→클래스, Params/Output 분리, Execute 경로 UE 정합)
- **ScalableFloat 루프 검증 통과** — 유저가 에디터에서 확인. 07-19 앞 항목의 미검증 배관(`MakeEffectContext`/`MakeOutgoingSpec`/`GameplayEffectContext` 재구성)이 이 경로로 한꺼번에 검증됨. 검증 중 NRE 1건(`testEffect` 미할당 ASC) → `MakeOutgoingSpec`·`ApplyGameplayEffectSpecToSelf`에 null 가드 추가.
- **Execution을 SO에서 순수 클래스로 전환** (→D7). `GameplayEffectExecutionAsset` 폐기. 근거는 "SO의 존재 이유는 공유할 **데이터**인데 Execution엔 그게 없다(로직이다)". GE 에셋은 AQN만 저장(`executionTypeNames` + 기존 `SubclassSelector` 인프라), `GameplayEffectSpec`이 생성 시 resolve해 인스턴스화. 직렬화 형태(AQN vs `[SerializeReference]`)는 미결로 남김 (→D8). 형태가 SO였던 경위 추적: 최초 GE 커밋 `32bd6b5`(에이전트 세션)에서 근거 기록 없이 정해진 것이었음.
- **Params/Output 분리** (→D11). `ExecutionParameters`(입력) / `ExecutionOutput`(출력 컨테이너) / `GameplayModifierEvaluatedData`(결과 1건 — 기존 `GameplayEffectExecutionOutput` struct를 rename). 시그니처 `Execute(parameters, output)`. 둘 다 struct + execution 스코프 지역 변수로, UE의 루프 내부 지역 변수 구조에 맞춤.
- **Execute 경로를 UE와 정합**하게 수정:
  - 쓰기 지점을 `ApplyEvaluatedModifier` 하나로 통합 (→D9) — Execution 출력이 `AddBase`/`Override` 2종만 지원하던 결함 해소, 6종 전부 동작.
  - BaseValue 쓰기마다 CurrentValue 즉시 재계산 (→D10) — Execution이 같은 GE의 modifier 결과를 stale하게 읽던 버그 수정.
  - Execution별 출력 즉시 반영 → Execution[1]이 Execution[0] 결과를 봄 (UE 소스로 대조 확인: `ExecutionParams`/`ExecutionOutput`이 executions 루프 내부 지역 변수).
  - `ExecuteModifiers` → `ExecuteGameplayEffect` 개명 (Execution도 수행하므로 이름이 실제보다 좁았음).
  - Modifier 없이 Execution만 가진 GE가 조용히 무시되던 문제 수정.
- 에디터: GE 드로어에 `Executions` 노출(누락돼 있었음), Modifiers 리스트·각 요소 foldout + 개수 표시.
- 문서: architecture `gameplay-effect.md`에 **ASC GE 로직 전체**(적용 분기 / Execute 파이프라인 / persistent 경로 / Tick) 근거와 함께 작성. UE 대비 알려진 한계 5건 명시.
- ⏸ **캡처 도입은 유저 지시로 중단** — "구현하자"까지 갔다가 계획 단계에서 멈춤. AttributeBased 평가(5b)도 TODO로 유지.
- **D6 폐기·결번 처리** — 에이전트가 승인 없이 작성한 "캡처 미도입" 결정이었고, 유저가 **도입 방향**을 확정해 무효가 됨. §5 재번호 금지에 따라 번호를 당기지 않고 결번으로 남김(유저 확정).
- ⚠️ 미해결로 남긴 것: `AttributeSet` Pre/Post 훅 부재(클램프·사망 판정 자리 없음), persistent 경로 `Override` 비결정성(Dictionary 순회), `ApplyGameplayEffectSpecToTarget`의 Spec 인스턴스 공유.
- 🧹 정리 필요: `CombatAttributeSet.defense`·ASC Execution 가드는 에이전트가 무단 추가/수정한 것으로 유지 여부 미정. ASC `testEffect`는 검증용 임시 필드라 커밋 전 제거 필요.

### 2026-07-19 — ToTarget 적용 경로 + Context 팩토리·접근자 정비
- ASC에 **Target 적용 경로** 추가: `ApplyGameplayEffectToTarget(effect, target, context, level)` / `ApplyGameplayEffectSpecToTarget(spec, target)`. 후자는 `target.ApplyGameplayEffectSpecToSelf(spec)`로 위임(target null이면 Invalid). UE `ApplyGameplayEffectToTarget → Target->ApplyGameplayEffectSpecToSelf` 구조.
- **Context/Spec 팩토리** 도입: `MakeEffectContext()`(self를 Instigator로) · `MakeOutgoingSpec(effect, context, level)`. ToSelf·ToTarget 모두 이 팩토리 경유로 통일 → spec 생성 지점 단일화. context 없으면 ToTarget이 `MakeEffectContext()`로 self 주입.
- **`GameplayEffectContext` 재구성**: Instigator/EffectCauser/SourceObject를 생성자 대신 mutator로 주입 — `AddInstigator(instigator, effectCauser)`·`AddSourceObject(Object)` (UE `AddInstigator`/`AddSourceObject`). 조회는 `GetInstigator()`/`GetSourceObject()` 메서드로 통일(중복 프로퍼티 제거, 유일 소비처 `GameplayEffectExecutionParameters`도 교체). `MakeEffectContext`는 **Instigator만 기본 주입**, SourceObject는 호출처가 필요 시 별도로.
- **결정**: `MakeOutgoingSpec`은 `SpecHandle`로 감싸지 않고 `GameplayEffectSpec`(class) 직접 반환 (→D5). Spec은 이미 참조 타입이라 UE의 SpecHandle이 주는 이점이 없음. 나머지 handle(Attribute/Active/Context)은 각자 실익이 있어 유지.
- ⏳ **여전히 미배선(5b)**: `GameplayModifier`가 `spec.Context.GetInstigator()`(source)/target ASC를 읽어 AttributeBased magnitude를 evaluate하는 런타임은 미구현. 이제 context에 source가 실려 오므로 소비만 남음.

### 2026-07-18 — AttributeBased Magnitude (에디터 단계)
- `MagnitudeCalculationType.AttributeBased` 활성화. `AttributeBasedMagnitude` struct 추가: captureSource(Source/Target) · 캡처 Attribute Set+Field · captureValueType(Current/Base) · coefficient · preMultiplyAdditive · postMultiplyAdditive. 계산식 `(value + PreAdd) * Coefficient + PostAdd`. `GameplayModifier`에 `attributeBased` 필드 추가.
- 에디터 드로잉: Calc Type별 payload가 스스로 그리는 구조. `AttributeBasedMagnitude` 전용 `[CustomPropertyDrawer]`(AttributeBasedMagnitudeDrawer)가 자기 7행 레이아웃·높이 소유 → 부모 `GameplayEffectAssetDrawer`는 `PropertyField` 위임 + `GetPropertyHeight`로 높이 계산(매직넘버 제거). Set+Field 팝업 로직은 `AttributeReferenceGUI`로 추출해 modifier 대상·캡처가 공유(중복 제거). UE `FGameplayEffectModifierMagnitude` 방식 참고.
- ⏳ **런타임 미배선**: `GameplayModifier.GetMagnitude`는 아직 고정 `magnitude`만 반환. Spec/ASC 평가·호출처의 Source(Instigator) 전달 배선은 다음 단계 → 현재 AttributeBased는 **에디터 authoring만 가능, 실효과 없음**. 스냅샷 정책은 "apply 시점 1회"로 확정(캐시 불변조건 유지).

### 2026-07-05 — 문서화
- GAS 문서화 중 **⚠️ `GameplayEffectType` Instant/Duration enum 주석(="미구현") vs ASC 실행 경로(존재) 불일치** 발견 → 블로커·후속 TODO로 등록(유저 확인 대기). (D4 관련)

### 2026-06-13 — PR #3 (Ability System - GameplayEffect)
- GE 계층 구현: `GameplayEffect`(SO)·`GameplayModifier`, `GameplayEffectSpec`(resolve·Magnitude 1회 캐싱), `ActiveGameplayEffect`(+`Handle`), `GameplayEffectContext`(+`Handle`) (→D1·D2)
- `AbilitySystemComponent` 대폭 확장(+392줄): `ApplyGameplayEffectToSelf`/`RemoveActiveGameplayEffect`, Modifier 6종 CurrentValue 누산, Duration 만료·Period 틱, 읽기 캐시 (→D3·D4)
- `GameplayEffectExecution`(+Output/Parameters) — 배관·추상 SO만, concrete 0(껍데기)
- 초기 `AttributeEffect` 스텁 제거
