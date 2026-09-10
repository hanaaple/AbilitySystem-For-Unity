# AbilitySystem — GameplayEffect

> **최종 갱신:** 2026-09-10 (KST)

수치 변경을 통과시키는 단일 채널. 상위 개요는 [overview](overview.md), 대상 수치는 [attribute](attribute.md), 라이브 재평가는 [aggregator](aggregator.md).

> UE GAS의 GameplayEffect를 참고해 **필요한 축만 직접 구현** — UE 대비 채택/생략은 [overview §UE GAS 대비](overview.md#ue-gas-대비--채택생략).
> 이 문서는 **개념·방향** 중심이다. 필드·에셋 작성 절차 등 세부는 코드와 각 feature progress를 본다.

## 정의 ↔ 런타임 (Asset ↔ Spec)

- **`GameplayEffectAsset`(SO):** "무엇을·얼마나·어떻게" 바꿀지의 **불변 정의** — 타입, 지속/주기, Modifier 목록, Execution 목록.
- **`GameplayEffectSpec`(런타임):** 적용 시 Asset으로부터 만들어지는 인스턴스. 적용 경계에서 **대상별로 복제(Clone)**돼 대상 캡처값이 서로 섞이지 않는다.
- **`ActiveGameplayEffect`:** 지속형 GE의 활성 상태(남은 시간·주기 타이머)를 들고 컨테이너에 등록. 핸들로 식별·해제.

**왜 나누나:** 하나의 Asset을 여러 대상·인스턴스가 공유하므로, 런타임 가변 상태(캡처값·타이머)는 Spec 쪽에 둬야 서로 간섭하지 않는다. Execution도 같은 이유로 '데이터(SO)'가 아니라 '로직(순수 클래스)'이며 무상태다.

## 값 변경의 두 경로 — BaseValue vs CurrentValue

GAS의 핵심 구분. 성격이 다른 두 종류의 수치 변경을 갈라 다룬다.

- **영구 변경(BaseValue):** 데미지·힐 같은 소모. Base에 직접 적용돼 남는다.
- **일시 보정(CurrentValue):** 장비·버프 같은 지속 효과. Base는 두고 활성 mod를 모아 CurrentValue를 산출 → 해제하면 정확히 원복.

갈림의 축은 **타입이 아니라 `period`** 다 — 주기 실행은 매 틱이 작은 영구 변경이라 영구 경로에 묶인다.

| 타입 | `period` | 무엇이 바뀌나 | 해제 시 |
|---|---|---|---|
| Instant | — | BaseValue (영구) | 활성 목록에 없음 |
| Duration / Infinite | `> 0` | BaseValue (틱마다 영구) | 반영된 값은 남음 |
| Duration / Infinite | `0` | CurrentValue만 | 자동/수동 원복 |

적용은 타입과 `period`로 경로를 가른다:

```
ApplyGameplayEffectSpecToSelf(spec)
 ├ spec == null → Invalid
 ├ Modifier 0 && Execution 0 → Invalid        ← Execution만 있는 GE도 유효
 ├ spec.Clone() + Target 캡처                  ← 대상별 복제(캡처 오염 방지)
 │
 ├ type == Instant
 │   └ ExecuteGameplayEffect → Invalid Handle (활성 목록에 안 남음)
 │
 └ Duration / Infinite → 활성 등록 · Handle 발급
     ├ period > 0 && ExecutePeriodicEffectOnApplication → ExecuteGameplayEffect
     └ period == 0 → aggregator에 mod 등록          ← persistent 경로
```

### CurrentValue 집계

persistent(period 0) mod는 대상 어트리뷰트별 **Aggregator**에 등록되고, CurrentValue는 읽을 때 공식으로 누산된다(집계·라이브 재평가 상세는 [aggregator](aggregator.md)).

```
CurrentValue = ((Base + ΣAddBase) * MultiplyAdditive / DivideAdditive * ΠMultiplyCompound) + ΣAddFinal
(Override가 있으면 그 값이 최종값을 덮어씀)
```

`GameplayModifierOperation` 6종: `AddBase`(곱셈 전 합) · `MultiplyAdditive`(배율 누산 `1+Σ(m-1)`) · `DivideAdditive`(제수 누산) · `MultiplyCompound`(배율 곱산 `Πm`) · `AddFinal`(곱셈 후 합) · `Override`(대체). persistent는 연산 종류별로 모아 한 번에 공식에 넣으므로 **modifier 순서가 결과에 영향을 주지 않는다**(Override 제외). 영구 경로는 Base에 순차 적용이라 순서가 영향을 준다.

### CurrentValue가 다시 계산되는 시점

CurrentValue는 캐시가 아니라 base·mod로부터 `Evaluate`로 파생된다. 다음에서 재계산(aggregator dirty)이 돈다:

| 시점 | 계기 |
|---|---|
| Base 직접 변경 | `SetAttributeBaseValue` |
| Execute 경로가 Base를 바꿀 때 | Instant/주기형 modifier·Execution 출력 |
| persistent mod 등록/해제 | 지속형 GE 적용 · 해제 · 만료 |
| 캡처 소스 변경 | non-snapshot 의존 이펙트 라이브 재평가 → [aggregator](aggregator.md) |

초기화(Awake)는 base·current를 직접 세팅한다(dirty 없음). dirty 전파·연쇄 상세는 [aggregator](aggregator.md).

## Execute 경로 (영구 변경)

Instant/주기형이 타는 경로. Modifier와 Execution 출력을 BaseValue에 적용한다.

```
ExecuteGameplayEffect(spec)                      (UE: ExecuteActiveEffectsFrom)
 ├ foreach modifier → ApplyModToAttribute(...)   ← 하나씩 즉시
 └ RunExecutions(spec)
     └ foreach execution
         ├ params/output 생성 (execution 스코프 지역 변수)
         ├ execution.Execute(params, output)     ← 출력만 수집, 어트리뷰트 안 건드림
         └ foreach output → ApplyModToAttribute(...)  ← execution 끝나는 즉시 반영

ApplyModToAttribute(handle, op, mag)             (UE: InternalExecuteMod)
 ├ ExecModOnBaseValue(base, op, mag)             (UE: FAggregator::StaticExecModOnBaseValue)
 └ SetAttributeBaseValue(...)                    ← Base 쓰기 + aggregator 동기화 → CurrentValue 재계산
```

설계상 세 가지가 핵심:

- **쓰기 경로 단일화(→D9):** Modifier와 Execution 출력이 같은 함수로 적용된다 — 경로별 지원 연산이 어긋나지 않고, 클램프·사망 판정 훅 자리도 한 곳으로 모인다.
- **magnitude는 미리 평가, 적용은 나중(→D2):** magnitude는 Spec 생성 시 확정된다. 순차 적용으로 바뀌는 건 *무엇에 더하는가*(Base)지 *얼마*가 아니다.
- **Execution은 단위별 즉시 반영(→D11):** 뒤 Execution이 앞 Execution의 결과를 본다.

## Execution — 복합 계산 훅

단순 사칙연산으로 부족한 계산(분기·클램프·다중 어트리뷰트)을 담는 순수 로직 클래스. Asset은 타입 이름만 저장하고 Spec 생성 시 인스턴스화한다. **Instant/주기형에서만 실행** — 지속형(period 0)의 상시 계산은 아래 AttributeBased가 맡는다.

## AttributeBased Magnitude — 캡처 기반 계산

Modifier의 magnitude를 고정값 대신 다른 어트리뷰트에서 끌어온다("방어력 = 힘의 10%"). GAS의 캡처 개념을 축소 재현한다.

- **캡처 시점:** Source는 Spec 생성 시, Target은 적용 시.
- **snapshot vs non-snapshot:** snapshot은 캡처 시점 값으로 고정, non-snapshot은 **원본 어트리뷰트가 바뀌면 추종**(라이브 재평가는 [aggregator](aggregator.md)가 담당).

## ASC — GameplayEffect API

| 메서드 | 용도 |
|---|---|
| `MakeEffectContext()` / `MakeOutgoingSpec(GE, ctx, level)` | 컨텍스트·Spec 생성 |
| `ApplyGameplayEffectToSelf` / `ToTarget` | GE 적용(자신/대상, 자신이 Instigator) |
| `RemoveActiveGameplayEffect(handle)` | 지속형 GE 해제 |

`SpecHandle`은 **의도적 생략** — Spec이 이미 class라 래퍼 이득이 없다(→D5). 실제 소유·수명·집계는 ASC가 아니라 `ActiveGameplayEffectsContainer`가 맡고 ASC는 얇은 위임 래퍼다.

## 수명 (Tick)

주기형은 타이머로 매 주기 실행하고(프레임 지연 시 누락 틱을 몰아 소화), Duration은 남은 시간을 차감해 만료 시 해제한다. Infinite는 수동 해제까지 유지된다.

## 미구현 / 알려진 한계

| 항목 | 내용 |
|---|---|
| AttributeSet 클램프 훅 | 값 범위 강제(체력이 max 초과·음수)가 없다 — Pre/Post 훅 자리는 있으나 로직 미탑재 |
| GE 스택 | 같은 GE 중복 적용 시 스택 수·Overflow 정책 |
| Gameplay Cue | GE 적용/해제 시 VFX·SFX 트리거 |
| GameplayTag | 태그 기반 자격·면역·조건 |
| ScalableFloat 커브 | Magnitude를 Level 기반 커브로 (현재 고정 float) |
| Magnitude 확장 | `SetByCaller`(도입 예정) · `CustomCalculationClass` |
