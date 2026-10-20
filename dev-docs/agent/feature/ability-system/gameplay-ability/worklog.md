# gameplay-ability — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-10-20 — ASC 부여·해제(TODO-BOARD 2-1) + 셀프체크

**요청.** 유저: "TODO에 GameplayAbility (GA) 구현 이거 세분화해" → 2-1~2-8(나눈 단위는 에이전트 제안). "ASC에 GA 부여·해제 구현하자" → "단순히 구현하지 말고 UE에 맞춰서"(답: ActorInfo 도입, 범위 "UE대로") → 정정 "GameplayAbilitySpecContainer 단순하게 구현하지 말고 UE에 맞춰서". "테스트 직접하고".

**구현.** UE `GiveAbility`·`ClearAbility`·`ClearAllAbilities`·스코프 락(보류 부여·해제·ClearAll)·`InitAbilityActorInfo` 구조를 따름. 새 타입 `GameplayAbilityActorInfo`·`ScopedAbilityListLock`·`ConsiderPending`. Spec 생성자가 핸들 발급, 핸들은 id만. 컨테이너는 UE처럼 `Items`·`Owner`·`RegisterWithOwner`, 해제는 인덱스 순회 + RemoveAtSwap. 뺀 것: 네트워크·AbilityTriggers·GE가 부여한 어빌리티·인스턴싱 정책 선택. 활성 중 해제 시 `EndAbility`는 2-2로(TODO 주석).

**확인.** 컴파일(Unity 없이) 통과. 셀프체크 `Tools/Ability System/Run Gameplay Ability Self-Check` 58/0(Unity MCP 메뉴 실행, 의도된 에러 로그 4줄). 회귀 Aggregator 83/0·Calculation Modifier 19/0·GE Component 89/0·Modifier Tag 65/0. 인스펙터 화면·Play 모드는 안 봄.

### 2026-10-20 — 정의/런타임 분리(→D1) + SerializeReference 원형·깊은 복사(→D3)

**요청.** 유저: "GA는 이벤트 함수, field를 직접 사용하는게 중요" → SO 상속 시도(생성 창 포함) → SO를 객체마다 Instantiate하는 방식 웹 조사 → "Runtime Class 별도로 두는거로 가고, SO Asset은 단일로 가자"(→D1, 처음엔 타입만 저장 →D2). 이후 "ability Class SubClassSelector 말고 SerializeReference로 하자 하위 field draw하고 저장할 필요가 있겠어"(→D3, D2 번복), 복제는 "직렬화 규칙 따라 깊은 복사" 선택.

**구현.** `GameplayAbilityAsset`(sealed SO) + `GameplayAbility`([Serializable] abstract C#). 에셋 `abilityTemplate`은 `[SerializeReference][SubclassSelector]`. `Core.Common.SerializedDeepCopy`(Unity 직렬화 규칙 깊은 복사 — UnityEngine.Object·엔진 클래스는 참조 공유, SerializeReference 공유 관계 유지). `SubclassSelectorDrawer`가 SerializeReference 단일 필드면 하위 필드까지 그림. SO 상속 시도분·생성 창(`NewGameplayAbilityWindow`)은 삭제. `Test.TestGameplayAbility`는 런타임 하위 클래스로.

**경위.** 예전 GA 골격(2026-09-13 stash 복원본)은 유실 확인 뒤 같은 날 "이전 골격만"으로 다시 만들고 그 위에서 진행.
