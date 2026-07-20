# AbilitySystem — GameplayEffect

수치 변경을 통과시키는 단일 채널. 상위 개요는 [overview](overview.md), 대상 수치는 [attribute](attribute.md).

> UE GAS의 `FGameplayEffect(Spec)`·`FActiveGameplayEffect`·`FGameplayModifierInfo`를 참고해 **필요한 축만 직접 구현** — UE 대응·축소 지점은 [overview §UE GAS 대비](overview.md#ue-gas-대비--채택생략-범위) 표 참조.
> **반영 기준:** feature `ability-system/gameplay-effect` @ 2026-07-19 (KST) — HARNESS §3.4.

## GameplayEffect 계층

- **`GameplayEffectAsset`(SO, 불변 정의):** `type`, `duration`, `period`, `executePeriodicEffectOnApplication`, `modifiers[]`, `executionTypeNames[]`.
- **`GameplayEffectExecution`(순수 클래스):** 복합 계산 훅. **SO가 아니다** — 공유할 '데이터'가 아니라 '로직'이라 에셋으로 존재할 이유가 없다(→decisions D7). GE 에셋은 AQN(타입 이름)만 저장하고 Spec 생성 시 인스턴스화한다. 여러 GE가 같은 타입을 참조하므로 **런타임 가변 상태를 가지면 안 된다**(D2와 같은 원리).
- **`GameplayModifier`(struct 정의):** `attributeSetTypeName`(AssemblyQualifiedName) + `fieldName` → `AttributeHandle`, `operation`, `magnitudeCalculationType`, `magnitude`.
- **런타임 분리 — Spec:** `GameplayEffectSpec`은 생성 시점에 각 `GameplayModifier`를 `GameplayModifierSpec`으로 변환하며 **AttributeHandle resolve와 Magnitude 계산을 1회 완료해 캐싱**한다(이후 런타임 재해석 없음). resolve 실패 modifier는 경고 후 skip.
- **활성 상태 — Active:** `ActiveGameplayEffect`가 `Handle`·`Spec`·`RemainingDuration`(Duration용)·`PeriodTimer`(Period용)를 들고 ASC의 활성 목록에 등록된다.

## CurrentValue 계산 (Modifier 연산)

`GameplayModifierOperation` 6종과 최종 공식:

```
CurrentValue = ((Base + ΣAddBase) * MultiplyAdditive / DivideAdditive * ΠMultiplyCompound) + ΣAddFinal
(Override가 있으면 그 값이 최종값을 덮어씀)
```

| 연산 | 누산 방식 |
|---|---|
| `AddBase` | 곱셈 전 합산 |
| `MultiplyAdditive` | 배율 **누산**: `1 + Σ(mag-1)` (1.5x + 1.5x = 2.0x) |
| `DivideAdditive` | 제수 누산(분모에 동일 방식) |
| `MultiplyCompound` | 배율 **곱산**: `Π mag` (1.5x * 1.5x = 2.25x) |
| `AddFinal` | 곱셈 후 합산 |
| `Override` | 마지막 Override가 최종값 대체 |

- **읽기-쓰기 경로 분리:** persistent(period==0) GE의 CurrentValue는 **적용/해제/`SetBaseAttributeValue` 시점에만 재계산**되어 캐시된다. `GetAttributeCurrentValue`는 캐시를 반환하므로 **읽기 비용 O(1)**.
- **BaseValue vs CurrentValue 두 경로:**
  - Instant / Periodic(period>0) → `ExecuteGameplayEffect`로 **BaseValue를 순차 영구 변경**(Modifier 배열 순서가 결과에 영향). 상세는 아래 §ASC — GE 실행 파이프라인.
  - Duration/Infinite persistent(period==0) → aggregator가 **CurrentValue만** 계산(BaseValue 불변). 해제 시 회수.

## ASC — GameplayEffect API

| 메서드 | 용도 |
|---|---|
| `MakeEffectContext()` | 자신을 Instigator로 하는 컨텍스트 생성 (UE: MakeEffectContext) |
| `MakeOutgoingSpec(GE, ctx, level)` | 에셋 → 적용 대기 Spec (UE: MakeOutgoingSpec) |
| `ApplyGameplayEffectToSelf(GE, ctx, level)` | GE 적용. Instant는 즉시 실행 후 Invalid Handle 반환 |
| `ApplyGameplayEffectSpecToSelf(spec)` | 이미 만든 Spec을 자신에게 적용 |
| `ApplyGameplayEffectToTarget(GE, target, ctx, level)` | 자신을 Instigator로 대상에 적용 |
| `ApplyGameplayEffectSpecToTarget(spec, target)` | `target.ApplyGameplayEffectSpecToSelf`로 위임 |
| `RemoveActiveGameplayEffect(handle)` | 핸들로 Duration/Infinite GE 해제 |

관련 타입: `GameplayEffectContext` / `GameplayEffectContextHandle`(발동 주체·출처 오브젝트), `ActiveGameplayEffectHandle`(활성 GE 식별자).
`SpecHandle`은 **의도적 생략** — Spec이 이미 class라 래퍼가 주는 이득이 없다(→decisions D5).

---

## ASC — GE 적용 분기

`ApplyGameplayEffectSpecToSelf`가 **타입과 `period` 두 축**으로 경로를 가른다.

```
ApplyGameplayEffectSpecToSelf(spec)
 ├ spec == null → Invalid
 ├ Modifier 0개 && Execution 0개 → Invalid       ← Execution만 있는 GE도 유효
 │
 ├ type == Instant
 │   └ ExecuteGameplayEffect(spec) → Invalid Handle 반환 (활성 목록에 안 남음)
 │
 └ Duration / Infinite
     ├ _activeEffects에 등록 → Handle 발급
     ├ period > 0 && ExecutePeriodicEffectOnApplication → ExecuteGameplayEffect(spec)
     └ period == 0 → RecalculateAffectedAttributes(spec)      ← persistent 경로
```

| 타입 | `period` | 경로 | 무엇이 바뀌나 | 해제 시 |
|---|---|---|---|---|
| Instant | — | Execute | **BaseValue** (영구) | 활성 목록에 없음 |
| Duration | `> 0` | Execute (틱마다) | **BaseValue** (틱마다 누적, 영구) | 만료해도 이미 반영된 값은 남음 |
| Duration | `0` | persistent | **CurrentValue만** | 만료 시 자동 원복 |
| Infinite | `> 0` | Execute (틱마다) | **BaseValue** (영구) | 수동 해제 시 값은 남음 |
| Infinite | `0` | persistent | **CurrentValue만** | 해제 시 원복 |

**근거 — 왜 두 경로인가 (→D4):** 영구 소모(데미지·힐)와 일시 보정(장비·버프)은 성격이 근본적으로 다르다. 하나의 경로로는 "장비 벗으면 정확히 원복"과 "데미지는 영구 반영"이 동시에 성립하지 않는다. 갈림의 기준이 GE **타입이 아니라 `period`** 인 점이 헷갈리기 쉬운데, 주기 실행은 매 틱이 작은 Instant이므로 Execute 쪽에 묶인다.

---

## ASC — GE 실행 파이프라인 (Execute 경로)

**진입 조건:** Instant이거나 `period > 0`일 때만. Duration/Infinite에 `period == 0`이면 이 경로를 타지 않고 aggregator(persistent)로 간다.

```
ExecuteGameplayEffect(spec)                    (UE: ExecuteActiveEffectsFrom)
 ├ foreach modifier
 │   └ ApplyEvaluatedModifier(EvaluatedData)   ← 하나씩 즉시
 └ RunExecutions(spec)
     └ foreach execution
         ├ ExecutionParameters / ExecutionOutput 생성 (execution 스코프 지역 변수)
         ├ execution.Execute(params, output)   ← 출력만 수집, 어트리뷰트 안 건드림
         └ foreach output
             └ ApplyEvaluatedModifier(...)     ← 그 execution이 끝나는 즉시 반영

ApplyEvaluatedModifier(evaluated)              (UE: InternalExecuteMod)
 ├ ExecuteModOnBaseValue(base, op, mag)        (UE: FAggregator::StaticExecModOnBaseValue)
 ├ BaseValue 쓰기
 └ RecalculateAttributeCurrentValue            ← 같은 자리에서 CurrentValue까지
```

### 설계 근거

**① 쓰기 경로를 하나로 (`ApplyEvaluatedModifier`)**
Modifier와 Execution 출력이 **같은 함수**를 통과한다. 이전에는 두 곳에 각각 switch가 있어 지원 연산이 어긋나 있었다(Modifier 6종 / Execution 출력 `AddBase`·`Override` 2종). 같은 "어트리뷰트에 연산 적용"이 경로에 따라 능력이 다른 건 결함이므로 통합했다. UE도 둘 다 `InternalExecuteMod` 하나로 보낸다.
→ 부수 효과: 나중에 `PreGameplayEffectExecute`/`PostGameplayEffectExecute` 훅(클램프·사망 판정)을 걸 자리가 한 곳으로 확정된다.

**② BaseValue와 CurrentValue를 쌍으로, 즉시**
modifier 하나를 쓸 때마다 그 자리에서 CurrentValue를 재계산한다. 마지막에 몰아서 갱신하면 **뒤따르는 Modifier·Execution이 stale CurrentValue를 읽는다** — 실제로 그 버그가 있었다(Execution이 같은 GE의 modifier가 바꾼 값을 못 봄). UE는 `SetAttributeBaseValue`가 어그리게이터를 `MarkDirty`해 같은 효과를 낸다.
→ 트레이드오프: 같은 어트리뷰트를 여러 modifier가 건드리면 재계산이 그만큼 반복된다. 정합성을 우선했다.

**③ magnitude "평가"와 "적용"은 다른 시점**
magnitude는 Spec 생성 시 `EvaluatedMagnitude`로 **미리 확정**된다(→D2). 순차 적용으로 바뀌는 건 *무엇에 더하는가*(Base)지 *얼마를 더하는가*(magnitude)가 아니다. UE도 `CalculateModifierMagnitudes()`로 일괄 평가 후 `GetModifierMagnitude`로 캐시를 꺼내 쓴다.

**④ Execution은 단위별 즉시 반영**
`ExecutionParameters`·`ExecutionOutput`을 **execution 스코프 지역 변수**로 둔다. 따라서 Execution[1]은 Execution[0]이 적용한 결과를 본다. UE도 두 객체가 executions 루프 내부 지역 변수라 **출력을 execution 간에 누적하는 것 자체가 불가능**하다.
→ 한 execution 안에서는 `Execute()` 동안 출력만 모았다가 반환 직후 하나씩 반영된다. 즉 "모아서"(execution 내부)와 "즉시"(execution 사이)가 층위별로 다르다.

**⑤ 입력/출력 타입 분리, 둘 다 struct**
`ExecutionParameters`(입력: Target/Source ASC·Spec)와 `ExecutionOutput`(출력: `GameplayModifierEvaluatedData` 목록)을 나눴다 — UE의 `FGameplayEffectCustomExecutionParameters` / `FGameplayEffectCustomExecutionOutput` 대응.
- params는 참조 몇 개뿐이라 struct로 두면 **할당 0** → execution마다 만들어도 공짜라 UE와 같은 스코프를 유지할 수 있다.
- output도 타입 성격 통일을 위해 struct지만 **내부 버퍼는 힙 List**다. 원소 `GameplayModifierEvaluatedData`가 `AttributeHandle`을 통해 관리 참조(`Type`·`string`·`FieldInfo`)를 물고 있어 `stackalloc`이 불가능하다. 값으로 넘겨도 같은 List를 가리키므로 추가 내용은 호출측에 반영된다.
- struct는 파라미터 없는 생성자를 정의할 수 없어 `default`면 버퍼가 null이다 → `GameplayEffectExecutionOutput.Create()` 팩토리로 만들고, null 상태는 경고 후 무시한다.

**⑥ Execution 출력은 `AddBase`/`Override` 외 연산도 지원**
①로 경로가 합쳐지면서 6종 전부 동작한다. 다만 Execute 경로에서는 가산 계열(`AddBase`/`AddFinal`)이 같은 덧셈으로, 배율 계열(`MultiplyAdditive`/`MultiplyCompound`)이 같은 곱셈으로 수렴한다 — aggregator 누산이 아니라 Base에 직접 적용하기 때문이며, UE `StaticExecModOnBaseValue`도 4종(Override/Additive/Multiplicitive/Division)만 구분한다.

### 알려진 차이·한계

| 항목 | 상태 |
|---|---|
| `Pre`/`PostGameplayEffectExecute` 훅 | ❌ 없음. `AttributeSet`이 빈 추상 클래스라 **체력 클램프·사망 판정을 걸 자리가 없다**(health가 maxHealth 초과·음수 가능) |
| AttributeCapture 계층 | ❌ 미구현(**도입 예정**). Execution이 `SourceAsc`/`TargetAsc`를 **직접** 읽어, 스냅샷·Calculation Modifiers 같은 읽기 개입이 불가능 |
| persistent 경로의 `Override` | 활성 GE를 `Dictionary` 순회로 모아서 **어느 Override가 이길지 비결정적** |
| 재진입 | Execution이 다른 GE를 적용하면 `_activeEffects` 순회 중 수정으로 예외. 현재 그런 Execution 없음 |
| Spec 공유 | `ApplyGameplayEffectSpecToTarget`이 같은 Spec 인스턴스를 넘긴다. target 기반 magnitude 평가가 들어가면 대상 간 오염 → UE처럼 apply 시 Spec 복사 필요 |

---

## ASC — persistent 경로 (Duration/Infinite, `period == 0`)

Execute 경로와 달리 **아무것도 쓰지 않는다.** BaseValue는 불변이고, CurrentValue는 그때그때 활성 GE를 모아 산출한 결과다.

```
RecalculateAffectedAttributes(spec)        ← 적용·해제 시점에만 호출
 └ spec의 modifier가 가리키는 어트리뷰트만 추려서
     └ RecalculateAttributeCurrentValue(handle)
         └ CalculateAttributeCurrentValue(handle, BaseValue)
             ├ _activeEffects 순회 (period > 0 인 것은 제외)
             ├ 각 modifier를 **연산 종류별로 누산**
             └ 공식 적용 → CurrentValue 쓰기
```

**Execute 경로와의 결정적 차이 — 순서가 결과에 영향을 주지 않는다.**
Execute는 modifier를 배열 순서대로 Base에 순차 적용하지만, persistent는 `AddBase`끼리 합·`MultiplyCompound`끼리 곱… 식으로 **종류별로 모은 뒤 한 번에 공식에 넣는다.** 그래서 Duration GE에선 modifier 배열 순서를 바꿔도 결과가 같다(`Override` 제외 — 마지막으로 순회된 것이 이김).

### 재계산이 도는 시점 (전부)

| # | 시점 | 경로 |
|---|---|---|
| 1 | 초기화(Awake) | `InitAttributeSets` — 에셋 `baseValue`로 Base·Current 동시 세팅 |
| 2 | Base 직접 변경 | `SetBaseAttributeValue` |
| 3 | Execute 경로의 **modifier마다** | `ExecuteGameplayEffect` → `ApplyEvaluatedModifier` |
| 4 | Execute 경로의 **Execution 출력마다** | `RunExecutions` → `ApplyEvaluatedModifier` |
| 5 | persistent GE **적용** | `ApplyGameplayEffectSpecToSelf` → `RecalculateAffectedAttributes` |
| 6 | persistent GE **해제·만료** | `RemoveActiveGameplayEffect` → `RecalculateAffectedAttributes` |

**근거 (→D3):** CurrentValue 읽기는 매 프레임 다수(UI·이동·전투 판정)인데 변경은 드물다. 쓰기 시점에만 계산해 캐시하면 읽기가 O(1)이 된다. 대안이던 "읽을 때마다 활성 GE 순회"는 비용을 빈도 높은 쪽에 물리는 구조다.
**트레이드오프:** 위 6개 트리거 중 하나라도 빠뜨리면 stale 값이 나온다. 그래서 변경 API를 `SetBase`/`Apply`/`Remove`로 좁게 유지한다. (실제로 ③④가 "맨 끝에 일괄"이던 시절 Execution이 stale 값을 읽는 버그가 있었다.)

### 순서 규약 — 적용은 등록 후, 해제는 제거 후

```csharp
_activeEffects.Add(handle, active);      // 먼저 등록
RecalculateAffectedAttributes(spec);     // → 새 GE가 계산에 포함됨

_activeEffects.Remove(handle, out active);   // 먼저 제거
RecalculateAffectedAttributes(active.Spec);  // → 제거된 GE가 빠진 값으로 원복
```
둘 다 "목록을 먼저 확정하고 계산"이라 별도 보정 로직이 필요 없다.

## ASC — Tick (`TickActiveEffects`)

활성 GE가 0개면 `Update`에서 즉시 반환한다.

```
foreach active in _activeEffects
 ├ period > 0 → PeriodTimer 누적, while(타이머 ≥ period) { 차감; ExecuteGameplayEffect }
 └ type == Duration → RemainingDuration 차감, ≤ 0 이면 _expiredEffects에 수집

루프 종료 후 → 수집된 것들 RemoveActiveGameplayEffect
```

- **`while` 루프인 이유:** 프레임 지연으로 `deltaTime`이 `period`보다 커지면 그 사이 누락된 틱을 모두 소화한다(예: `period` 0.1초에 프레임이 0.35초 걸리면 3회 실행).
- **만료를 루프 밖에서 처리하는 이유:** `RemoveActiveGameplayEffect`가 `_activeEffects`를 수정하므로 순회 중 제거하면 예외가 난다. `_expiredEffects`에 모았다가 루프 종료 후 제거한다.
- **Infinite는 시간 차감 대상이 아니다** — `type == Duration`일 때만 `RemainingDuration`을 깎으므로 수동 해제 전까지 유지된다.

---

## 에셋 생성·세팅 (Authoring)

GameplayEffect 에셋을 만들고 값을 채우는 실전 절차. (MCP 도구/YAML 편집 일반 규칙은 → [dev-tools.md](../../dev-tools.md) "에셋 배선".)

### 필드와 값
| 필드 | 의미 | 값 |
|---|---|---|
| `type` | GE 타입 | `0`=Instant(BaseValue 일회 변경) · `1`=Infinite(CurrentValue 지속, 장비) · `2`=Duration(시간 제한) |
| `duration` | 지속 시간(초) | Duration일 때만 의미. Instant/Infinite는 `0` |
| `period` | 주기(초) | `0`=지속형(CurrentValue aggregator). `>0`=주기마다 BaseValue 실행 |
| `executePeriodicEffectOnApplication` | 주기형이 적용 즉시 1회 실행할지 | period>0일 때만. 아니면 무관 |
| `modifiers[]` | 수치 변경 목록 | 아래 |
| `executionTypeNames[]` | 복합 계산 클래스의 **AQN 문자열** 목록 | 보통 `[]`. 인스펙터에선 `SubclassSelector` 드롭다운 |

**Modifier 필드** (`GameplayModifier` struct):
- `attributeSetTypeName`: 대상 AttributeSet의 **AssemblyQualifiedName**. 예: `Character.CharacterAttributeSet, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`
- `fieldName`: 필드명(핸들 아님). 예: `health`, `speed`
- `operation`: `0`=AddBase · `1`=MultiplyAdditive · `2`=DivideAdditive · `3`=MultiplyCompound · `4`=AddFinal · `5`=Override (공식은 위 표)
- `magnitudeCalculationType`: `0`=ScalableFloat(고정 float) · `1`=AttributeBased(캡처 어트리뷰트 기반 — ⚠️ 에디터 authoring만, 런타임 미배선)
- `magnitude`: 고정값(음수 가능). ScalableFloat 전용
- `attributeBased`: AttributeBased 전용. `captureSource`(Source/Target) + 캡처 Attribute(Set+Field) + `captureValueType`(Current/Base) + `coefficient`/`preMultiplyAdditive`/`postMultiplyAdditive`. 계산식 `(value + PreAdd) * Coefficient + PostAdd`

### 만드는 법
1. **생성(MCP):** `manage_scriptable_object create`, `type_name: Core.AbilitySystem.Effect.GameplayEffectAsset`. patches로 `type`/`duration`/`period` 설정.
2. **Modifier(MCP):** 같은 create/modify patches에서 `modifiers.Array.data[0].<field>`를 쓰면 **배열이 자동 확장**되어 요소가 생긴다(예: `.attributeSetTypeName`, `.fieldName`, `.operation`, `.magnitude`). ⚠ `modifiers.Array.size`(ArraySize) 직접 설정은 **미지원** — data[0] 방식 또는 YAML.
3. **여러 Modifier/안 될 때:** `.asset` YAML을 직접 편집(형식은 아래 예시).

### 예시 (실제 에셋)
**근접 데미지** `Assets/Data/GE_MeleeDamage.asset` — 명중 시 대상 Health 감소(Instant):
```yaml
  type: 0            # Instant
  duration: 0
  period: 0
  modifiers:
  - attributeSetTypeName: Character.CharacterAttributeSet, Assembly-CSharp, Version=0.0.0.0,
      Culture=neutral, PublicKeyToken=null
    fieldName: health
    operation: 0     # AddBase
    magnitudeCalculationType: 0
    magnitude: -10
  executionTypeNames: []
```
**장비 스탯(이동속도 −5)** `Assets/Data/GE_EquipSpeedDown.asset` — 장착 중 Speed 감소(Infinite, 해제 시 회수):
```yaml
  type: 1            # Infinite
  duration: 0
  period: 0
  modifiers:
  - attributeSetTypeName: Character.CharacterAttributeSet, ...
    fieldName: speed
    operation: 0     # AddBase → CurrentValue = (Base + (-5)) ...
    magnitude: -5
```
> 적용법: Instant 데미지는 `대상 ASC.ApplyGameplayEffectToSelf(GE)`(EquipmentComponent.TriggerAttack). Infinite 장비 효과는 `StatModifierModule`이 장착 시 소유자 ASC에 적용하고 핸들로 해제 시 회수.

---

## GE 타입별 실전 상태

`GameplayEffectType` enum 주석("Instant/Duration 미구현")은 **낡았다** — ASC엔 세 타입 실행 경로가 다 있고, 아래는 실제 게임 흐름 검증 여부다.

- **Instant** — ✅ 검증됨(2026-07-06 play): 근접 평타 `GE_MeleeDamage`가 대상 Health를 깎음(item S1). `ExecuteModifiers`로 BaseValue 변경.
- **Infinite** — ✅ 사용중: 장비 스탯(`possessEffect`, `GE_EquipSpeedDown`). CurrentValue aggregator.
- **Duration** — ⚠ 경로만 있고 미검증. 실사용 시 확인.
- (남은 정리: enum 주석 문구 갱신 → TODO-BOARD. 이 절·overview 요약 표 동기화.)

## ❌ 미구현 (GameplayEffect 계열)

| 항목 | 내용 | 근거 위치 | wiki todo id |
|---|---|---|---|
| **GameplayEffectExecution 실사용** | 프레임워크(추상 클래스·Params/Output·ASC 파이프라인·에디터 배선)는 **구현 완료**. 남은 것은 concrete 구현체와 플레이 검증 — 현재 `SpeedBoostExecution`(검증용) 1개뿐이고 실제 전투 공식 미작성 | `GameplayEffectExecution.cs`, ASC `RunExecutions` | `ability-ge-execution` |
| **Calculation Modifiers** | Execution 스코프 동안만 캡처 값을 보정하는 GE 에셋별 데이터(UE `FGameplayEffectExecutionScopedModifierInfo`). **선행 조건: AttributeCapture 계층**(미구현, 도입 예정) — 지금은 Execution이 ASC를 직접 읽어 개입 지점이 없다 | `GameplayEffectExecutionParameters.cs` TODO | - |
| **AttributeSet 훅** | `PreGameplayEffectExecute`/`PostGameplayEffectExecute`. 체력 클램프·사망 판정을 거는 자리. `AttributeSet`이 빈 추상 클래스라 **현재 클램프가 전혀 없음**(health가 maxHealth 초과·음수 가능) | `AttributeSet.cs` | - |
| GE 스택 | 같은 GE 중복 적용 시 스택 수 관리·Overflow 정책 | `GameplayEffectAsset.cs:20` TODO | `ability-ge-stack` |
| Gameplay Cue | GE 적용·해제 시 VFX·SFX 트리거 | `GameplayEffectAsset.cs:22` TODO | `ability-gameplay-cue` |
| ScalableFloat 커브 | Magnitude를 Level 기반 커브 테이블로 (현재 고정 float) | `GameplayModifier.cs:17` TODO | - |
| Magnitude 계산 타입 확장 | `AttributeBased` ⚠️ 진행 중(데이터·에디터 완료, 런타임 미배선) · `SetByCaller` / `CustomCalculationClass` ❌ | `AttributeBasedMagnitude.cs`, `MagnitudeCalculationType.cs` | - |
