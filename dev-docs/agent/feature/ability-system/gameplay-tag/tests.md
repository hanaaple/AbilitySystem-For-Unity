# gameplay-tag — 유저 확인 목록 (검증)

> 이 feature에서 **유저가 Unity 에디터에서 직접 확인해야 하는 것**을 순서대로 모은 문서다. 위에서부터 그대로 따라 하면 된다.
> Unity MCP가 연결돼 있으면 에이전트가 콘솔 읽기·메뉴 실행·코드 실행까지는 한다(2026-10-17 §0-1·§1-1·§1-3-1·§5-5를 MCP로 확인). 창·인스펙터 클릭이나 Play 중 눈으로 보는 확인은 여전히 유저 몫이다.
> 결과 기록: 항목 앞 `- [ ]`를 `- [x]`로 바꾸고 날짜를 적는다. 실패하면 콘솔 로그(빨간 줄 전체)를 복사해 에이전트에게 주면 된다.
> 진행 문서: [progress.md](progress.md). 최종 갱신: 2026-10-17 (KST)

---

## 0. 공통 준비

- [x] _(2026-10-17, MCP 콘솔 조회 errorCount 0)_ **0-1. Unity를 열고 컴파일이 끝날 때까지 기다린다.** 하단 오른쪽 스피너가 멈추고 Console에 빨간 에러(`error CS…`)가 **0개**여야 한다.
  - 에러가 있으면 여기서 멈추고 에러 줄을 전부 복사해 에이전트에게 준다. 아래 모든 확인은 컴파일이 돼야 의미가 있다.
- [ ] **0-2. Console 창 준비.** `Window ▸ General ▸ Console`. 오른쪽 위 검색창으로 로그를 거를 수 있다(아래 항목마다 거를 문자열을 적어 둠). `Clear on Play`는 꺼도 켜도 상관없다.
- **0-3. 이번에 테스트용으로 추가한 태그**(피커 트리에 보이는 게 정상):
  - native(코드 정의, 에디터에서만): `Test.ModifierTag.Native`, `Test.ModifierTag.Native.Enraged` — `Assets/Scripts/Test/Editor/ModifierTagSelfCheck.cs`.
  - 레지스트리(에셋에 저장, 빌드에도 포함): `Test.ModifierTag.Registry.Stunned` — `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset` 마지막 줄.

---

## 1. Modifier Source/Target 태그 판정 (2026-10-16 구현) ← 가장 먼저

관련 결정: D14(지속형 판정=UE 그대로), D18(판정 태그 출처=ASC 조회 함수만·빈 값), D19(Execution 캡처 조회=UE처럼 평가 파라미터 인자), D20(Spec 캡처 태그=UE `FTagContainerAggregator` 구조), D21(지속형 태그 조건을 CurrentValue에도 적용). 경위: [worklog.md](worklog.md) 2026-10-16 판정 연결 항목.

**알아 둘 전제:** ASC 소유 태그를 넣는 입구가 아직 없다(D18). 그래서 실제 캐릭터에서는 **Target 조건의 Require는 늘 실패, Ignore는 늘 통과**한다. 셀프체크는 이 제약을 우회해, Target 조건은 aggregator에 태그를 직접 넘겨서(B), Source 조건은 Spec에 태그를 직접 넣어서(C) 확인한다.

### 1-1. 셀프체크 실행

- [x] _(2026-10-17)_ **1-1-1.** 메뉴 `Tools ▸ Ability System ▸ Run Modifier Tag Self-Check`를 누른다. Play 모드는 필요 없다.
- [x] _(2026-10-17, 에이전트가 MCP로 메뉴 실행 — PASS 38줄·FAIL 0줄·에러 0·경고 2(아래 `[GESpec]`))_ **1-1-2.** Console 검색창에 `ModifierTag`를 넣는다. 마지막 줄이 **`[ModifierTag Self-Check] passed 38 / failed 0`** 이면 통과.
  - `[ModifierTag PASS] …`가 38줄, `[ModifierTag FAIL] …`(빨간 줄)이 0줄이어야 한다.
  - `테스트 태그 준비 실패로 중단`이 나오면 1-2의 0번 항목부터 본다.
  - 실행 중 노란 경고 2줄 `[GESpec] ''의 0번 modifier magnitude Calculation Failed - falling back to 0.`(C-12·C-13의 Speed GE Spec 생성 시)은 **실패가 아니다**. Target 캡처를 쓰는 AttributeBased GE는 Spec을 만들 때 Target이 아직 없어 계산이 실패하고, 적용 때 다시 계산된다(C-12·C-13 값이 맞음). 원인은 `GameplayEffectSpec.Initialize`가 생성 시점에도 `CalculateModifierMagnitudes`를 부르는 기존 코드(HEAD에도 있음, 이번 작업과 무관). UE 5.6 `FGameplayEffectSpec::Initialize`·`CaptureDataFromSource`(`GameplayEffect.cpp:1609-1630`·`:1754-1774`)는 생성 시 magnitude를 계산하지 않으므로 이 경고가 나지 않는다. gameplay-effect 소관.
- [x] _(2026-10-17, 실패 없음 — 해당 없음)_ **1-1-3.** 실패한 줄이 있으면 그 줄의 ID(예: `B-9`)를 아래 표에서 찾아 "실패 시 의심"을 확인하고, 빨간 줄 전체를 에이전트에게 준다.

### 1-2. 셀프체크 항목별 의미

값은 모두 Health 기준값(Base) 100에서 시작한다. "문맥"은 aggregator를 평가할 때 넘기는 Source·Target 태그다.

**0. 테스트 태그 준비**

| ID | 확인 내용 | 기대 | 실패 시 의심 |
|---|---|---|---|
| 0-1 | native `Test.ModifierTag.Native.Enraged` 유효 | PASS | native 태그 수집(`NativeGameplayTag.GetRegisteredNativeTags`)이 에디터 어셈블리 필드를 못 찾음. 스크립트 재컴파일 후 다시 실행. |
| 0-2 | native 부모 `Test.ModifierTag.Native` 유효 | PASS | 위와 같음. |
| 0-3 | 레지스트리 `Test.ModifierTag.Registry.Stunned` 유효 | PASS | 레지스트리 에셋에 줄이 없음 / 매니저가 에셋 변경 전에 만들어짐 → Unity 재시작 또는 스크립트 재컴파일 후 재실행. |
| 0-4 | ASC 소유 태그가 비어 있음 | PASS | 누가 소유 태그 입구를 이미 만들었다면 이 항목과 C 기대값을 다시 봐야 한다. |

**A. `GameplayTagRequirements` 판정 (조건 객체 단독)**

| ID | 확인 내용 | 기대 |
|---|---|---|
| A-1 | Require Enraged, Enraged 보유 | 충족 |
| A-2 | Require Enraged, 빈 태그 | 불충족 |
| A-3 | Require 부모 `Test.ModifierTag.Native`, 자식 Enraged 보유 | 충족(계층 매칭) |
| A-4 | Ignore Stunned(레지스트리 태그), Stunned 보유 | 불충족 |
| A-5 | Ignore Stunned, 빈 태그 | 충족 |
| A-6 | TagQuery "Stunned 중 하나", Stunned 보유 | 충족 |
| A-7 | 같은 TagQuery, Enraged만 보유 | 불충족 |
| A-8 | 아무 조건도 없는 객체 | `IsEmpty` true |

실패 시 의심: A는 이번 작업 전에 만든 `GameplayTagRequirements`·`GameplayTagContainer`·`GameplayTagQuery` 자체의 문제다(D13·D15).

**B. `AttributeAggregator` 자격 판정 (aggregator 단독, 태그를 직접 넘김)**

| ID | 확인 내용 | 기대 | 의미 |
|---|---|---|---|
| B-1 | mod +50, Target Require Enraged / 문맥 Target=Enraged | 150 | 캡처 평가 경로(`Evaluate`)가 Target 조건을 본다 |
| B-2 | 같은 mod / 문맥 비어 있음 | 100 | 조건 못 맞추면 빠진다 |
| B-3 | 같은 mod / Enraged가 문맥 Source에만 | 100 | Target 조건은 Target 태그로만 판정 |
| B-4 | mod +50, Source Ignore Stunned / 문맥 Source=Stunned | 100 | Source 조건 |
| B-5 | 같은 mod / 문맥 비어 있음 | 150 | Ignore만 있으면 빈 태그에서 통과 |
| B-6 | 조건 없는 mod(기존 호출 형태) / 아무 문맥 | 150 | **기존 GE 회귀 없음** |
| B-7 | B-1의 mod / CurrentValue, 소유 태그=Enraged | 150 | CurrentValue 경로도 Target 조건 판정(D21) |
| B-8 | 같은 mod / CurrentValue, 소유 태그 빈 | 100 | |
| B-9 | Source Require Enraged mod 3개(+10: 건 GE의 Source가 Enraged, +20: 빈 Source, +40: Source 정보 없음) / CurrentValue | 110 | CurrentValue의 Source 조건은 **각 mod를 건 GE의 Source 태그**로 판정(D21, 에이전트 판단) |
| B-10 | 같은 3개 / 캡처 평가, 문맥 Source=Enraged | 170 | 캡처 경로는 GE별 Source가 아니라 **평가 문맥의 Source**로 판정(UE 그대로) |
| B-11 | B-1 aggregator의 snapshot / 문맥 Enraged·빈 | 150·100 | snapshot이 조건까지 복사 |
| B-12 | Override 7, Target Require Enraged / 문맥 빈·Enraged | 100·7 | Override도 조건 못 맞추면 무시 |

실패 시 의심: `Assets/Scripts/Core/AbilitySystem/Effect/AttributeAggregator.cs`의 `Mod.Qualifies`·`EvaluateWithBase`·`EvaluateCurrentValue`, `TakeSnapshotOf`.

**C. 실제 ASC에 GE 적용 (메모리에서 만든 GE, 에셋 파일은 안 생김)**

| ID | 확인 내용 | 기대 | 의미 |
|---|---|---|---|
| C-1 | Instant +30, Target Require Enraged | Base 100 | 실행 시 조건 못 맞춘 modifier 건너뛰기(UE `ExecuteActiveEffectsFrom`) |
| C-2 | Instant +30, Target Ignore Stunned | Base 130 | Ignore만 있으면 적용 |
| C-3 | Instant +30, Source Require Enraged, Source 태그 빈 | Base 100 | Source 조건 |
| C-4 | 같은 GE, Spec의 Source 태그에 Enraged를 직접 넣고 적용 | Base 130 | Source 조건 충족 시 적용 |
| C-5 | Infinite +50, Target Require Enraged | 핸들 유효, Current 100 | 등록은 되고 CurrentValue에선 빠짐 |
| C-6 | Infinite +50, Target Ignore Stunned | Current 150 | |
| C-7 | C-6 제거 | Current 100 | |
| C-8 | Infinite +50, Source Require Enraged, Source 빈 | Current 100 | |
| C-9 | 같은 GE, Source에 Enraged 넣고 적용 | Current 150 | **UE 5.6이면 100**(빈 태그로 계산). D21로 달라진 지점 |
| C-10 | C-9 제거 | Current 100 | |
| C-11 | Health 버프(Infinite +100, Source Require Enraged, Source에 Enraged) | Health Current 200 | |
| C-12 | **[현재 설계]** Speed GE(Speed += Health.current × 1, Target 캡처·non-snapshot, Source 태그 빈) 적용 | Speed 100 | Health CurrentValue는 200인데 캡처 평가는 Speed GE의 Source 태그로 판정해 버프가 빠진다. 아래 1-5의 결정 요청 ①. |
| C-13 | **[현재 설계]** 같은 Speed GE, Source에 Enraged | Speed 200 | |
| C-14 | 정리 후 | Health 100, Speed 0 | |

실패 시 의심: `ActiveGameplayEffectsContainer`(`ExecuteGameplayEffect`·`CaptureTargetActorTags`·`AddSpecMods`·`OnAttributeAggregatorDirty`), `GameplayEffectSpec`(Source 태그 캡처·복제), `AttributeBasedMagnitude.Evaluate`.

### 1-3. 기존 기능 회귀 확인

이번 작업으로 aggregator 평가 함수 시그니처가 바뀌었다. 기존 동작이 그대로인지 본다.

- [x] _(2026-10-17, MCP로 메뉴 실행 → `passed 44 / failed 0`, 에러·경고 0. 이전 N은 기록이 없었지만 `AggregatorSelfCheck.cs`의 미커밋 diff가 호출 시그니처 17줄뿐이고 `check(...)` 추가·삭제가 없어 같은 수로 판단)_ **1-3-1. Aggregator 셀프체크.** 메뉴 `Tools ▸ Ability System ▸ Run Aggregator Self-Check`. Console 검색 `Aggregator`. 마지막 줄 `[Aggregator Self-Check] passed N / failed 0`이면 통과(N은 이전과 같아야 함 — 이번에 케이스를 더하지 않았다). **N = 44.**
- [ ] **1-3-2. PlayMode 셀프체크(선택).** 이미 셋업돼 있으면 해당 씬에서 Play. Console `[PlayMode Self-Check] passed N / failed 0`. 셋업이 없으면 `Tools ▸ Ability System ▸ Setup PlayMode Test`부터(기존 aggregator 도구).
- [ ] **1-3-3. 실제 게임 GE.** Play 모드에서 평소 쓰던 GE(예: `GameplayEffectPickup`에 연결된 GE, `GE_EquipSpeedDown` 등)를 적용해 이동 속도·체력 변화가 전과 같은지. 기존 GE는 태그 조건이 비어 있어 결과가 같아야 한다.
- [ ] **1-3-4. ASC 인스펙터의 캡처 값 표시.** Play 모드에서 AttributeBased 또는 Execution을 쓰는 GE를 적용한 캐릭터를 선택 → ASC 인스펙터의 활성 GE 아래 캡처 줄(`Source/Target  어트리뷰트  «by …»  →  Base: … Current: …`)이 에러 없이 나오는지. (이번에 이 줄이 Spec의 태그 문맥으로 값을 계산하도록 바뀌었다.)
- [ ] **1-3-5. `SpeedBoostExecution`.** 이 Execution을 쓰는 GE가 있으면 적용해서 Console의 `{health} + {stamina} = {result}` 로그가 전과 같은 값인지. (호출 시그니처가 바뀌었다.)

### 1-4. 인스펙터에서 조건을 넣어 보는 수동 확인 (선택)

셀프체크로 충분하지만, 실제 에셋으로 보고 싶으면:

- [ ] **1-4-1.** 테스트용 GE 에셋 하나를 복제해 Modifier를 펼친다 → Magnitude 아래 `Source Tags`·`Target Tags` 접기 → 안에 `Require Tags`·`Ignore Tags`·`Tag Query`.
- [ ] **1-4-2.** `Target Tags ▸ Ignore Tags`에 `Test.ModifierTag.Registry.Stunned`를 넣고 Play에서 적용 → 평소처럼 적용되는지(소유 태그가 비어 있으므로).
- [ ] **1-4-3.** `Target Tags ▸ Require Tags`에 `Test.ModifierTag.Native.Enraged`를 넣고 적용 → **적용되지 않는지**(Instant면 Base 그대로, Infinite면 Current 그대로). 소유 태그 입구가 생기기 전엔 이게 정상이다.
- [ ] **1-4-4.** 확인 후 복제한 에셋은 지운다.

### 1-5. 결정이 필요한 것 (보고 답해 주면 됨)

- [ ] **① CurrentValue와 캡처 평가의 태그 문맥 차이(D21 트레이드오프).** 셀프체크 C-11~C-13이 지금 동작을 보여 준다: Health 버프는 스탯(CurrentValue)에 보이지만(200), 그 Health를 캡처하는 다른 GE는 자기 Source 태그로 판정해 버프를 뺄 수 있다(100).
  - 선택지 A — 그대로(캡처는 UE 방식: "이 계산 문맥에서만 붙는 보너스"를 표현 가능).
  - 선택지 B — 캡처 평가도 CurrentValue처럼 "각 mod를 건 GE의 Source + 그 어트리뷰트 주인의 소유 태그"로 판정(스탯과 계산이 항상 일치, 대신 UE의 문맥 보너스 표현 불가).
  - 선택지 C — mod마다 어느 방식을 쓸지 고르게 함(설정 하나 추가).
- [ ] **② CurrentValue의 Source 조건 데이터(D21, 에이전트 판단).** 지금은 "그 mod를 건 GE를 만들 때 캡처한 Instigator 태그(액터 태그만)". GE 에셋 태그(Spec 태그, 세부 TODO 4)가 생긴 뒤 이것도 합칠지(UE 캡처 경로는 합친 태그를 씀).
- [ ] **③ 기존 버그 — `UpdateAggregatorMod`.** 상세: [TODO-BOARD](../../../TODO-BOARD.md) (bug) 항목. 언제 고칠지.
- [ ] **④ UE와 다른 기존 동작 — 주기 실행 magnitude 재계산.** 상세: [TODO-BOARD](../../../TODO-BOARD.md) (code) 항목. UE처럼 맞출지.

### 1-6. 지금은 확인할 수 없는 것 (소유 태그 입구가 생긴 뒤)

- 캐릭터가 태그를 **가졌을 때** Target Require 조건이 켜지는지(Instant·지속형 모두).
- 태그를 넣고 빼면 지속형 mod가 CurrentValue에 들어오고 빠지는지(재계산). 유저 확정: "태그가 바뀌면 CurrentValue 다시 계산하도록 하는게 맞고".
- 같은 태그를 두 번 넣고 한 번 빼면 남는지(참조 카운트).
- 계획: [progress.md](progress.md) 다음 작업 3.

### 1-7. 이번 작업에서 UE(5.6)와 다른 동작 — 상세

| # | 내용 | UE | 우리 | 왜 / 상태 |
|---|---|---|---|---|
| 1 | 지속형 mod의 CurrentValue 태그 판정 | `OnAttributeAggregatorDirty`가 빈 평가 파라미터로 `Evaluate`(`GameplayEffect.cpp:3317`·`:3358`) → Require 있는 mod는 CurrentValue에서 늘 빠짐 | `EvaluateCurrentValue(소유 태그)` — Target=소유 태그, Source=그 mod를 건 GE의 Source 액터 태그 | 유저 결정 D21("ue에서도 의도에 맞지 않는다고 해"). 원문에서 Epic 입장은 확인 못 함 — 5.4에서 Instant/Periodic의 같은 문제를 버그로 고친 cvar(`AbilitySystem.Fix.UseModTagReqsOnAllGE`, `:77-79`)만 확인. |
| 2 | 소유 태그가 바뀔 때 | 지속형 mod 태그 조건은 다시 계산 안 함(빈 태그라 의미 없음). GE 전체 on/off는 `TargetTagRequirements` GE Component가 태그 이벤트로 처리 | 아직 다시 계산 안 함 — 소유 태그 입구가 없어 바뀔 일이 없음 | 다음에(progress 다음 작업 3). |
| 3 | 평가 파라미터 | `FAggregatorEvaluateParameters`에 `AppliedSourceTagFilter`·`AppliedTargetTagFilter`·`IgnoreHandles`·`IncludePredictiveMods` | Source·Target 태그만 | 필터는 세부 TODO 9(AttributeBased 태그 필터)에서. 나머지는 예측·네트워크용이라 해당 없음. |
| 4 | Instigator 태그를 얻는 법 | `FGameplayEffectContext::GetOwnedGameplayTags`가 Instigator 액터의 `IGameplayTagAssetInterface`를 먼저 보고, 없으면 ASC(`GameplayEffectTypes.cpp:368-379`) | ASC만 | 액터 태그 인터페이스가 없다. `specTagContainer` 인자는 자리만(UE 기본 구현도 안 채움). |
| 5 | `FTagContainerAggregator` | `ScopedTags`(5.3부터 deprecated) 포함 | 없음 | 쓰지 않는 필드. |
| 6 | 주기 실행 magnitude | 실행마다 `CalculateModifierMagnitudes`(`:3084`) | 적용 때 값을 계속 씀(기존 동작) | [TODO-BOARD](../../../TODO-BOARD.md) (code). |
| 7 | 자격 판정 시점 | `EvaluateQualificationForAllMods`가 mod에 `IsQualified`를 캐시 | 평가 때마다 계산 | 결과 같음. 캐시는 성능 문제가 보이면. |

- 함께 발견한 것: `AttributeAggregator.Evaluate`의 요약 주석은 "마지막 Override 우선"인데, 코드는 목록에서 **먼저 만난** Override를 돌려준다. UE `FAggregatorModChannel::EvaluateWithBase`(`GameplayEffectAggregator.cpp:76-84`)도 먼저 만난(조건 맞는) Override를 돌려주므로 코드가 UE와 같고 주석이 틀렸다. aggregator feature 소관이라 고치지 않았다.

---

## 2. Query 편집기 (2026-10-16 구현) — progress 다음 작업 12

관련 결정: D16(Query 자동 설명=UE처럼 저장·Build 때 생성), D17(Query 편집기=UE처럼 별도 창·8종·Categories 전달). 확인 위치: GE 에셋 → Modifier 펼치기 → `Source Tags` 또는 `Target Tags` 펼치기 → `Tag Query` 줄.

- [ ] **2-1. 인스펙터 줄 모양.** 회색 `Empty` + 오른쪽 `Edit` 버튼 + `×` 버튼이 한 줄에 보인다.
- [ ] **2-2. 창 열기.** `Empty`(설명 글자) 또는 `Edit`를 누르면 제목이 `Tag Query Editor: Tag Query {에셋 이름}`인 작은 창이 인스펙터 줄의 **왼쪽, 조금 위**에 뜬다. 다시 누르면 기존 창이 닫히고 새로 열린다(창은 하나만).
- [ ] **2-3. 태그 식.** 창의 `Root Expression` 팝업 → `Any Tags Match` → 아래 `Tags` 상자에서 피커로 `Test.ModifierTag.Native.Enraged` 추가 → **창을 닫지 않아도** 인스펙터 줄이 ` ANY( Test.ModifierTag.Native.Enraged )`로 바뀐다.
- [ ] **2-4. 하위 식.** `Root Expression`을 `All Expressions Match`로 → 본문 끝 `+ Expression` 두 번 → `[0]`을 `Any Tags Match`+태그 A, `[1]`을 `No Tags Match`+태그 B → 인스펙터 줄이 ` ALL(  ANY( A ),  NONE( B ) )` 꼴.
- [ ] **2-5. 사용자 설명.** `User Description`에 글자를 넣으면 인스펙터 줄이 그 글자로 바뀐다. 지우면 자동 설명으로 돌아간다.
- [ ] **2-6. Cancel.** 몇 가지 고친 뒤 `Cancel` → 창이 닫히고 인스펙터 줄이 **창을 열었을 때 값**으로 돌아간다.
- [ ] **2-7. OK·저장.** 고친 뒤 `OK` → 닫히고 값 유지 → `Ctrl+S` → Unity 재시작 → 값 유지.
- [ ] **2-8. Undo.** 창을 연 채로 고친 뒤 `Ctrl+Z` → 인스펙터 줄과 **창 내용**이 함께 되돌아간다.
- [ ] **2-9. Clear.** 창을 연 채로 인스펙터 줄의 `×` → 창 내용도 빈 질의로 바뀐다.
- [ ] **2-10. 삭제.** 하위 식의 `−` → 그 칸이 **목록에서 사라진다**(빈 칸만 남으면 실패).
- [ ] **2-11. 창 안 피커.** `Tags` 상자에서 피커가 열리고, 태그를 고르면 창이 바로 다시 그려진다.
- [ ] **2-12. 가독성(접기·구분선·묶음).**
  - 종류를 고른 식 줄에 접기 화살표가 있고, 누르면 그 아래가 접힌다. `None` 줄은 화살표가 없고 글자 위치만 다른 줄과 맞다.
  - 본문이 옅은 회색 칠 + 왼쪽 색 안내선(깊이마다 파랑·초록·주황·보라)으로 묶여 보이고, 안내선이 위 줄의 화살표 아래에 온다.
  - 접은 식 줄 끝에 회색 요약(` ANY( A )` 꼴)이 뜨고, 마우스를 올리면 전체 문구 툴팁.
  - 태그 상자가 위 줄의 종류 팝업과 같은 세로 열에서 시작한다.
  - 같은 목록의 칸 사이에 가는 가로줄.
  - 접은 칸 **앞** 칸을 `−`로 지워도 접혀 있던 칸이 그대로 접혀 있다.
  - 접을 때 Console에 GUI 레이아웃 에러(`Getting control … position`)가 없다.
  - `Edit ▸ Preferences ▸ General ▸ Editor Theme`를 Light로 바꿔도 칠·안내선이 보인다.
- **위험 지점(에이전트 추측):** 변경 감지를 `EditorUtility.GetDirtyCount`로 한다. 편집용 사본(`HideFlags.DontSave`)에서 이 값이 안 오르면 2-3의 "즉시 반영"이 안 된다 — 그러면 2-3이 실패로 나타난다.

---

## 3. Modifier Source/Target Tags 인스펙터 (2026-10-16 구현) — progress 다음 작업 10

관련 결정: D13(Modifier Source/Target Tags=UE Requirements 전체), D15(Query=직렬화+판정).

- [ ] **3-1.** GE 에셋의 Modifier를 펼치면 Magnitude 아래에 `Source Tags`·`Target Tags` 접기 두 개.
- [ ] **3-2.** 각각 펼치면 `Require Tags`·`Ignore Tags`(태그 상자 + 피커)·`Tag Query`(2번 항목의 줄). 라벨은 UE 표시 이름(Must Have Tags 등)이 아니라 필드 이름이다(에이전트 판단, 바꾸고 싶으면 말해 줌).
- [ ] **3-3.** 태그를 넣고 `Ctrl+S` → Unity 재시작 → 그대로 있는지.
- [ ] **3-4.** Modifier가 여러 개일 때 요소 높이가 맞고 구분선·다음 요소와 겹치지 않는지.
- [ ] **3-5.** 접기 화살표가 리스트 드래그 핸들과 겹치지 않고, 값 열이 `Modifier Op` 줄의 값 열과 같은 세로선에 있는지.

---

## 4. 태그 드로어·피커·태그 관리 창 (2026-10-15 구현) — progress 다음 작업 9

관련 결정: D10(태그 드로어=UE 동작 최대 미러), D11(피커 루트 입구=UE `GetFilteredGameplayRootTags`), D12(태그 관리 포함=레지스트리 편집). 확인용 에셋: `Assets/_Test/GameplayTagTest.asset`(단일·List·Container 필드).

- [ ] **4-1. 관리 창.** `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset` 선택 → 인스펙터 `Manage Gameplay Tags...` → 창의 `+`로 `Test.A`·`Test.A.B`·`Test.C` 추가 → 레지스트리 에셋 목록에 저장되는지.
- [ ] **4-2. 행 메뉴.** 관리 창 행의 메뉴에서 Rename·Delete가 동작하는지. native 태그(`Test.ModifierTag.Native…`) 행은 Rename·Delete가 회색(비활성)인지.
- [ ] **4-3. 필드별 피커.** `GameplayTagTest.asset`의 단일·List 요소·컨테이너 필드마다 피커가 열리는지. 체크·해제(형제가 없으면 부모가 들어감), 칩의 `×`, 칩 우클릭 Copy/Paste/Clear.
- [ ] **4-4. 저장.** 값을 넣고 `Ctrl+S` → Unity 재시작 → 값 유지(아래 5-3과 겹침).
- [ ] **4-5. 다중 선택.** 테스트 에셋을 두 개 만들어 값을 다르게 하고 둘 다 선택 → `Multiple Values` 표시, 일부에만 있는 태그는 흐린 칩, 피커 체크박스는 미정 상태.
- [ ] **4-6. 위험 지점.** 피커 팝업 안의 메뉴(행 ▾·우클릭·설정)를 열 때 팝업이 닫혀 버리지 않는지. 미등록 태그 경고 대화창 뒤에 팝업이 뜨는지. 행 체크박스가 눌리는지.
- 참고: 중첩 struct/class 필드·`[GameplayTagCategories]` 확인용 필드는 유저 요청으로 지웠다(2026-10-15). 이 둘을 보려면 확인용 필드가 따로 있어야 한다.

---

## 5. Tag 코어 (2026-10-12~15 구현) — progress 다음 작업 1·2·6

- [ ] **5-1. native 태그.** 1번 셀프체크의 0-1·0-2가 통과하면 native 태그 수집·트리 구성은 확인된 것이다. 추가로 레지스트리에 native와 같은 경로의 하위 태그(예: `Test.ModifierTag.Native.Calm`)를 넣으면 피커에서 native 노드 아래로 합쳐 보이는지.
- [ ] **5-2. 계층 매칭.** 1번 셀프체크 A-3이 통과하면 "부모 Require가 자식 태그로 충족"은 확인된 것이다. 나머지(progress 다음 작업 1의 `MatchesTagDepth`·`RequestGameplayTagChildren` 등)는 셀프체크가 없다 — 필요하면 에이전트에게 셀프체크 추가를 요청.
- [ ] **5-3. 저장 후 매칭.** 인스펙터에서 컨테이너에 `A.B.C`를 넣고 저장 → 재시작 → 그 에셋을 코드로 읽어 `HasTag("A.B")`가 true인지(역직렬화 시 이름 파싱 복원, progress 다음 작업 6). 지금은 확인용 코드가 없다 — 필요하면 요청.
- [ ] **5-4. 레지스트리 자동 생성.** 레지스트리 에셋을 (백업 후) 지우고 Unity를 다시 열면 `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`이 자동으로 생기는지(progress 다음 작업 2). 끝나면 백업을 되돌린다.
- [x] _(2026-10-17, MCP 코드 실행 — `No.Such.Tag.McpCheckA` 2회 + `…B` 1회 요청 → 에러 2줄(이름당 1회), 세 결과 모두 `IsValid()` false)_ **5-5. 미등록 이름.** 코드에서 `GameplayTag.RequestGameplayTag("No.Such.Tag")`를 두 번 부르면 에러 로그가 **이름당 한 번**만 나오는지.

---

## 6. 테스트용으로 추가한 것 (정리 대상)

| 것 | 위치 | 지울 때 |
|---|---|---|
| 셀프체크 코드 + native 테스트 태그 2개 | `Assets/Scripts/Test/Editor/ModifierTagSelfCheck.cs` | 파일 삭제 → native 태그도 사라짐 |
| 레지스트리 테스트 태그 | `Assets/Resources/AbilitySystem/GameplayTagRegistry.asset`의 `Test.ModifierTag.Registry.Stunned` | 관리 창에서 Delete 또는 줄 삭제. 지우면 셀프체크 0-3이 실패한다 |
