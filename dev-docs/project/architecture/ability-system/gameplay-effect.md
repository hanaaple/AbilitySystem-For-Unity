# AbilitySystem — GameplayEffect

수치 변경을 통과시키는 단일 채널. 상위 개요는 [overview](overview.md), 대상 수치는 [attribute](attribute.md).

> UE GAS의 `FGameplayEffect(Spec)`·`FActiveGameplayEffect`·`FGameplayModifierInfo`를 참고해 **필요한 축만 직접 구현** — UE 대응·축소 지점은 [overview §UE GAS 대비](overview.md#ue-gas-대비--채택생략-범위) 표 참조.
> **반영 기준:** feature `ability-system/gameplay-effect` @ 2026-07-05 20:18 (KST) — HARNESS §3.4.

## GameplayEffect 계층

- **`GameplayEffect`(SO, 불변 정의):** `type`, `duration`, `period`, `executePeriodicEffectOnApplication`, `modifiers[]`, `executions[]`.
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
  - Instant / Periodic(period>0) → `ExecuteModifiers`로 **BaseValue를 순차 영구 변경**(Modifier 배열 순서가 결과에 영향).
  - Duration/Infinite persistent(period==0) → aggregator가 **CurrentValue만** 계산(BaseValue 불변). 해제 시 회수.

## ASC — GameplayEffect API

| 메서드 | 용도 |
|---|---|
| `ApplyGameplayEffectToSelf(GE, ctx, level)` | GE 적용. Instant는 즉시 실행 후 Invalid Handle 반환 |
| `RemoveActiveGameplayEffect(handle)` | 핸들로 Duration/Infinite GE 해제 |

관련 타입: `GameplayEffectContext` / `GameplayEffectContextHandle`(적용 출처·타깃 스냅샷), `ActiveGameplayEffectHandle`(활성 GE 식별자).

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
| `executions[]` | 복합 계산(미구현·껍데기) | 보통 `[]` |

**Modifier 필드** (`GameplayModifier` struct):
- `attributeSetTypeName`: 대상 AttributeSet의 **AssemblyQualifiedName**. 예: `Character.CharacterAttributeSet, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null`
- `fieldName`: 필드명(핸들 아님). 예: `health`, `speed`
- `operation`: `0`=AddBase · `1`=MultiplyAdditive · `2`=DivideAdditive · `3`=MultiplyCompound · `4`=AddFinal · `5`=Override (공식은 위 표)
- `magnitudeCalculationType`: 현재 `0`(고정 float)만
- `magnitude`: 고정값(음수 가능)

### 만드는 법
1. **생성(MCP):** `manage_scriptable_object create`, `type_name: Core.AbilitySystem.Effect.GameplayEffect`. patches로 `type`/`duration`/`period` 설정.
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
  executions: []
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
| **GameplayEffectExecution 전체** | 복합 계산(예: Source.Damage − Target.Armor, 크리티컬) 처리 계층. 추상 SO + ASC `RunExecutions` 배관은 있으나 **에디터 미배선·미검증의 껍데기**(concrete 0) — 사실상 미구현 | `GameplayEffectExecution.cs`, ASC `RunExecutions` | `ability-ge-execution` |
| GE 스택 | 같은 GE 중복 적용 시 스택 수 관리·Overflow 정책 | `GameplayEffect.cs:20` TODO | `ability-ge-stack` |
| Gameplay Cue | GE 적용·해제 시 VFX·SFX 트리거 | `GameplayEffect.cs:22` TODO | `ability-gameplay-cue` |
| ScalableFloat 커브 | Magnitude를 Level 기반 커브 테이블로 (현재 고정 float) | `GameplayModifier.cs:17` TODO | - |
| Magnitude 계산 타입 확장 | `AttributeBased` / `SetByCaller` / `CustomCalculationClass` | `MagnitudeCalculationType.cs` 주석 | - |
