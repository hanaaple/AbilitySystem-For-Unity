# 연습 모드 데모

> **최종 갱신:** 2026-10-23 (KST)

어빌리티 시스템(GAS 재구현)의 기능이 **한 씬 안에서 서로 맞물려 돌아가는 것**을 보여 주는 데모다. 허수아비를 상대로 공격·스킬·아이템·구역을 쓰면서, 그때마다 GA·GE·Tag·Attribute가 어떻게 적용되는지 게임 화면(F1 디버그 오버레이)에서 바로 볼 수 있다.

![연습 모드 데모 시작 화면](media/00_Opening.png)

- 씬: `Assets/Scenes/Demo_PracticeMode.unity`
- 어빌리티 시스템 구조 → [architecture/overview.md](architecture/overview.md)

---

## 1. 조작

| 입력 | 동작 |
|---|---|
| WASD · 마우스 | 이동 · 시점 |
| 마우스 왼쪽 (누르고 있으면 연사) | 일반 공격 |
| 마우스 오른쪽 (누르는 동안) | 조준 |
| Left Shift (누르는 동안) | 달리기 |
| Space | 점프 |
| E | 관통 파동 스킬 (마나 30) |
| F | 구르기 |
| F1 | GAS 디버그 오버레이 (끔 → 간단 → 상세) |
| Esc | 설정 메뉴 (열린 동안 게임 정지) |
| F11 | 전체 화면 켜고 끄기 |

---

## 2. 씬 구성

시작 지점에서 정면을 보면 허수아비 7개가 부채꼴로 서 있다(뒷줄 5개, 앞줄 2개). 관통 파동 5개가 이 부채꼴을 따라 나간다. 왼쪽에 아이템 4개, 오른쪽에 화염 구역이 있다.

| 오브젝트 | 역할 |
|---|---|
| 실드 허수아비 | 실드 250이 체력보다 먼저 피해를 받는다 |
| 일반 허수아비 5개 | 뒷줄 3개, 앞줄 2개 |
| 공격 허수아비 | 일정 간격으로 정면(시작 지점 쪽)에 투사체를 쏜다. 앞에 서면 플레이어가 맞는다 |
| 아이템 4개 | 회복 · 실드 · 강화 · 마나 충전. 먹으면 숨었다가 10초 뒤 다시 나타난다 |
| 화염 구역 | 안에 있는 동안 큰 화상, 나오면 3초 동안 약한 화상 |

- 연습 모드라 죽지 않는다 — 체력은 1 아래로 내려가지 않는다.
- 허수아비는 3초 동안 맞지 않으면 체력이 최대로 돌아간다.

---

## 3. 기능별 GAS 적용

데모의 모든 수치 변화는 GE를 거친다. 행동은 GA로 만들고, 비용·쿨다운·효과는 GA가 GE로 건다. 상태는 태그로 나타나고, 애니메이션·연출·HUD는 그 태그와 어트리뷰트를 읽어 표시한다.

GE 지속 방식: **Instant**(한 번 적용하고 끝) · **Duration**(정해진 시간 동안) · **Infinite**(직접 지울 때까지).

### 3-1. 플레이어 행동

| 행동 | GA | GE | Tag | Attribute |
|---|---|---|---|---|
| 달리기 (Shift) | `GA_Sprint` — 누르는 동안 활성 | `GE_Sprint_Speed` (Infinite, 활성 동안만) speed ×2 | `State.Sprinting` | speed 3.5 → 7 |
| 조준 (우클릭) | `GA_Aim` — 누르는 동안 활성, 달리기를 끊는다 | `GE_Aim_Speed` (Infinite, 활성 동안만) speed ×0.6 | `State.Aiming` | speed 3.5 → 2.1 |
| 점프 (Space) | `GA_Jump` — 착지할 때까지 활성, 조준을 끊는다 | — | `State.Jumping` | jumpHeight 1.2 |
| 구르기 (F) | `GA_Dash` — 구르는 동안 활성, 조준·달리기를 끊는다. 공중에선 못 쓴다 | `GE_Cooldown_Dash` (Duration 3초) | `State.Dashing`, `Cooldown.Skill.Dash` | dashDistance 5 |
| 일반 공격 (좌클릭) | `GA_NormalAttack` — 십자선 쪽으로 투사체 발사. 조준 중이 아니면 탄이 약간 퍼진다 | `GE_Damage_Attack` (Instant, 피해 계산), `GE_CombatStance` (Duration 1초, 겹치지 않고 시간만 갱신), `GE_Cooldown_NormalAttack` (Duration 0.25초 ÷ 공격속도) | `State.Combat`, `Cooldown.NormalAttack` | 대상 health·shield |

- 행동끼리 막고 끊는 관계는 GA의 어빌리티 태그·차단·취소 설정으로 정한다(예: `GA_Aim`은 `Ability.Sprint` 태그를 가진 어빌리티를 취소하고 막는다).
- `GE_CombatStance`는 쏠 때마다 다시 걸리지만, 스택 설정(대상 기준 1개)이라 여러 개로 쌓이지 않고 남은 시간만 갱신된다.

| 이동 — 조준이 달리기를 끊고, speed가 `base 3.5`에서 바뀐다 | 일반 공격 — 머리에 맞으면 ×2 (십자선 빨강) |
|---|---|
| ![이동](media/01_Move.gif) | ![일반 공격](media/02_Attack.gif) |

### 3-2. 관통 파동 (E)

| GA | GE | Tag | Attribute |
|---|---|---|---|
| `GA_Skill_Wave` — 수평으로 나가는 파동 5개를 60° 부채꼴로 날려, 지나가는 허수아비를 모두 맞힌다 | `GE_Cost_Wave` (Instant, mana −30) · `GE_Cooldown_Wave` (Duration 4초) · `GE_Wave_Damage` (Instant, 공격력 95~105% 피해) · `GE_Wave_Mark` (Duration 4초, 끝날 때 공격력 ×2.5 피해로 폭발) | `Cooldown.Skill.Wave` (스킬 바가 이 태그로 쿨다운을 그린다), 대상에 `Status.Debuff.Marked` (머리 위 검붉은 구) | mana 100 → 70 후 재생, 대상 health가 두 번 깎인다 |

- 마나가 30보다 적으면 발동이 막힌다(실패 사유 `ActivateFail.Cost`). 쿨다운 중엔 `ActivateFail.Cooldown`. 실패 사유는 디버그 오버레이 이벤트 로그에 나온다.
- 마나는 패시브 `GE_Mana_Regen`(Infinite, 1초마다 +5)으로 찬다.

![관통 파동 — 파동 5개가 허수아비를 관통하고, 4초 뒤 낙인이 터진다](media/03_Wave.gif)

### 3-3. 아이템

아이템을 먹으면 픽업이 GA를 발동하고, 효과는 그 GA가 GE로 건다. 픽업 자신에게는 재생성 쿨다운 GE가 걸린다.

| 아이템 | GA | GE | Tag | Attribute |
|---|---|---|---|---|
| 회복 | `GA_Item_Heal` | `GE_Item_Heal` (Instant, health +300) · `GE_Item_Heal_Fx` (Duration 1.2초, 연출용) | `State.Healing` | health 회복 (최대 체력에서 잘림) |
| 마나 | `GA_Item_Mana` | `GE_Item_Mana` (Instant, mana +100) | — | mana 충전 (최대 마나에서 잘림) |
| 강화 | `GA_Item_Empower` | `GE_Item_Empower` (Duration 8초) attackSpeed +1 · attack ×1.3 | `Status.Buff.Empowered` | attackSpeed 1 → 2 (공격 간격 절반), attack 50 → 65 |
| 실드 | `GA_Shield` — 유지 GE가 있는 동안 활성 | `GE_Shield_Amount` (Instant, shield 100) · `GE_Shield_Duration` (Duration 10초) · 시간이 다 되면 `GE_Shield_Clear` (Instant, shield 0) | `Status.Buff.Shielded` | shield 100. 피해로 0이 되면 바로 깨진다 |
| (픽업 자신) | — | `GE_Pickup_RespawnCooldown` (Duration 10초) | `Cooldown.Pickup.Respawn` (붙어 있는 동안 숨는다) | — |

![아이템 — 강화 GE가 붙은 동안 attack 65(base 50)·attackSpeed 2(base 1)](media/04_Items.gif)

### 3-4. 화염 존

구역에 들어오면 구역이 캐릭터에게 GA를 발동하고, 나가면 그 GA를 취소한다. 안팎에서 무엇을 걸지는 GA가 정한다.

| GA | GE | Tag | Attribute |
|---|---|---|---|
| `GA_FireZone` — 안에 있는 동안 활성, 나가면 취소 | `GE_FireZone_Inside` (Infinite, 1초마다 health −30, 나가면 지움) → `GE_FireZone_Afterburn` (Duration 3초, 1초마다 health −5) | `Status.Debuff.Burning` | health 주기 감소, 1에서 멈춤 |

![화염 구역 — 화상 태그로 화면 가장자리 연출이 켜지고, 체력은 1에서 멈춘다](media/05_Fire.gif)

### 3-5. 허수아비

| 허수아비 | GA | GE | Tag | Attribute |
|---|---|---|---|---|
| 공격 (`Dummy_Attacker`) | `GA_Dummy_FireProjectile` | `GE_Damage_Attack` · `GE_Cooldown_DummyAttack` (Duration 2초 ÷ 공격속도 = 1초) | `Cooldown.Dummy.Attack` | 플레이어 shield·health |
| 실드 (`Dummy_Shield`) | `GA_DummyShield` — 실드가 깨질 때까지 활성 | `GE_DummyShield_Duration` (Infinite) · `GE_DummyShield_Regen` (Duration 약 5초, 0.1초마다 shield +5) | — | shield 0 → 250. 깨지면 체력이 다 찬 뒤 다시 찬다 |

- 공격 허수아비의 공격 간격은 자체 타이머가 아니라 쿨다운 GE를 공격속도로 나눈 값이다(플레이어 일반 공격과 같은 방식).
- 공격 허수아비는 플레이어를 겨누지 않고 정면(`castPoint.forward`)으로만 쏜다.

![허수아비 — 실드가 먼저 깎이고, 공격 허수아비 정면에 서면 맞는다](media/06_Dummies.gif)

---

## 4. 어트리뷰트

어트리뷰트는 두 세트로 나뉜다. 모든 캐릭터가 붙이는 `CharacterAttributeSet`과 플레이어만 붙이는 `PlayerAttributeSet`(점프·구르기·마나)이다(`Assets/Scripts/Game/Character/`). 허수아비는 공용 세트만 붙인다. 초기값은 시작할 때 자신에게 거는 초기 스탯 GE(Instant, Override)로 넣는다.

| 세트 | 어트리뷰트 | 뜻 | 플레이어 (`GE_Demo_Player_DefaultAttributes`) | 허수아비 (`GE_Demo_Dummy_DefaultAttributes`) |
|---|---|---|---|---|
| Character | health / maxHealth | 체력 | 300 / 300 | 1000 / 1000 |
| Character | shield | 체력보다 먼저 피해를 받는 양 | 0 | 0 (실드 허수아비는 GA로 250) |
| Character | attack | 공격력 (피해 계산이 캡처) | 50 | 30 |
| Character | attackSpeed | 공격 속도 배율. 공격속도를 따르는 쿨다운을 이 값으로 나눈다 | 1 | 2 |
| Character | speed | 이동 속도 (m/s) | 3.5 | 0 |
| Character | stamina / maxStamina | (데모에서는 쓰지 않음) | 100 / 100 | 100 / 100 |
| Player | mana / maxMana | 스킬 코스트 자원 | 100 / 100 | 없음 |
| Player | jumpHeight | 점프 높이 (m) | 1.2 | 없음 |
| Player | dashDistance | 구르기 거리 (m) | 5 | 없음 |

**값 제한** — `PreAttributeBaseChange`(즉시·주기 피해가 바꾸는 Base 값)와 `PreAttributeChange`(버프가 바꾸는 Current 값) 두 곳에서 같은 제한을 건다.

| 어트리뷰트 | 제한 |
|---|---|
| health | 1 ~ maxHealth (연습 모드라 죽지 않는다) |
| mana | 0 ~ maxMana |
| shield, speed, jumpHeight, dashDistance | 0 이상 |
| attackSpeed | 0.1 이상 (0에 가까우면 쿨다운이 끝없이 길어진다) |

---

## 5. 피해 계산

일반 공격·관통 파동·허수아비 공격의 피해는 모두 GE Execution `DamageExecution`(`Assets/Scripts/Game/Character/DamageExecution.cs`)이 계산한다.

1. 시전자의 attack을 캡처해 무작위 비율 0.95~1.05를 곱한다.
2. 투사체가 머리 히트박스(`Gameplay.Zone.Head` 태그)에 맞았으면 ×2.
3. 대상의 shield를 캡처해, 피해만큼 shield에서 먼저 깎고 남은 피해를 health에서 깎는다.

관통 파동의 낙인 폭발은 같은 Execution을 쓰되, GE의 계산 Modifier가 캡처한 attack에 ×2.5를 걸어 넘긴다.

---

## 6. 화면 표시

| 표시 | 내용 | 코드 |
|---|---|---|
| 체력·마나 HUD | 왼쪽 아래. 얼굴 아이콘 옆에 `HP`·`MP` 줄(바 + 현재/최대 숫자), 실드 칸, 버프·디버프 아이콘과 남은 시간 | `UI/PlayerHealthHud` |
| 스킬 바 | 아래 가운데. 기본 동작(공격·조준·달리기·점프)은 왼쪽 작은 칸, 스킬(E·F)은 오른쪽 큰 칸에 쿨다운 숫자와 코스트 배지. 코스트 값은 어빌리티의 코스트 GE에서 읽고, 마나가 모자라면 칸을 덮는다 | `UI/SkillBarHud` |
| 허수아비 정보 | 체력 바(실드 칸 포함), 피해량·초당 피해(3초 평균)·누적 피해, 데미지 숫자(헤드샷은 노란 큰 숫자), 피격 흔들림 | `UI/TrainingDummyHealthBar` · `UI/DamageNumbers` · `Character/TrainingDummyHitShake` |
| 십자선 | 머리를 겨누면 빨갛게 바뀐다 | `UI/Crosshair` |
| 디버그 오버레이 (F1) | 아래 따로 | `UI/DebugOverlay` |

### 태그로 켜지는 연출

애니메이션과 연출은 GA·GE를 직접 알지 않고 **캐릭터가 가진 태그만 본다.**

| 컴포넌트 | 하는 일 | 데모에서 쓰는 곳 |
|---|---|---|
| `GameplayTagAnimatorParameterMap` | 태그 개수를 Animator 파라미터로 옮긴다 | `State.*` 태그 → 구르기·점프 등 애니메이션 |
| `TagScreenEffect` · `TagScreenOverlay` | 태그가 있는 동안 화면 후처리·화면 가장자리 색 | 조준·회복·강화·화상 |
| `TagParticleEffect` · `TagVfxEffect` | 태그가 있는 동안 파티클을 켜거나, 태그를 얻고 잃을 때 VFX 이벤트를 보낸다 | 낙인이 있는 동안 머리 위 검붉은 구 → 빠질 때 폭발 |
| `ShieldBubble` | 실드 태그가 있는 동안 몸을 감싸는 막. 진하기가 남은 실드를 따라간다 | 플레이어·실드 허수아비 |

### 디버그 오버레이 (F1)

UE의 `showdebug abilitysystem`에 해당하는 게임 화면 안의 패널이다. F1을 누를 때마다 끔 → 간단 → 상세로 바뀐다.

- **왼쪽 = 플레이어, 오른쪽 = 십자선으로 겨눈 대상**(허수아비·아이템·구역). 다른 대상을 겨눌 때까지 유지된다.
- **간단:** Attributes · Active Effects(남은 시간 / 전체 시간) · Owned Tags.
- **상세:** 간단 + Abilities · 이벤트 로그(GE 적용·주기 실행·제거, GA 발동·실패 사유·종료) · 대상 정보.
- 값이 방금 바뀐 어트리뷰트는 잠깐 색으로 강조되고, Current가 Base와 다르면(버프가 걸려 있으면) `base X`가 같이 붙는다.

---

## 7. 코드 구성

데모 게임 코드는 `Assets/Scripts/Game/`, GA·GE 데이터는 `Assets/Data/Demo/`에 있다. 어빌리티 시스템 코어(`Assets/Scripts/Core/AbilitySystem/`)는 데모 코드를 알지 않는다.

| 위치 | 내용 |
|---|---|
| `Game/Abilities/` | 데모 GA 클래스 — `CharacterGameplayAbility`(쿨다운을 공격속도로 나눔), `HoldInputAbility`(달리기·조준), `JumpAbility`, `DashAbility`, `FireProjectileAbility`(+ `Projectile`), `ApplyEffectsAbility`(아이템), `ShieldAbility`, `ZoneAbility` |
| `Game/Character/` | `CharacterAttributeSet`·`PlayerAttributeSet`, `DamageExecution`, 허수아비(`TrainingDummy`·`HitZone`), 태그 연출 컴포넌트 |
| `Game/UI/` | HUD·스킬 바·허수아비 정보·디버그 오버레이(`DebugOverlay`·`AbilitySystemDebugLog`) |
| `Game/Settings/` | 설정 메뉴(Esc)·전체 화면(F11) |
| `Game/` | `PlayerCharacter`·`PlayerController`, 픽업(`GameplayEffectPickup`), 구역(`AbilityZone`) |
| `Data/Demo/` | `Character/`·`Skill/`·`Item/`·`Shield/`·`Zone/`·`Dummy/`·`Common/` — 위 3장의 GA·GE 에셋. 에셋마다 `description`에 하는 일이 적혀 있다 |

---

## 8. 아트·에셋

- **맵:** Sci Fi Arena 3 팩의 바닥·벽 조각으로 단순한 연습장을 깔았다. GAS 동작이 잘 보이도록 주변 환경은 눈에 띄지 않게 했다.
- **캐릭터:** Y Bot + Mixamo 애니메이션(플레이어), 허수아비 모델.
- **VFX:** UNI VFX(파동·낙인 폭발), Vefects 등. 낙인 표시(검붉은 구)는 기본 구 메시 파티클. 실드 막은 자체 셰이더 `Game/ShieldFresnel`.
- 스토어 에셋(Sci Fi Arena 3 · Opsive · UNI VFX · Vefects · Small Props Pack)은 재배포할 수 없어 저장소에 없다(`Assets/Art/Third Party/`, `.gitignore`). 클론하면 그 참조가 비어 보이지만 어빌리티 시스템과 데모 로직은 그대로 동작한다.
