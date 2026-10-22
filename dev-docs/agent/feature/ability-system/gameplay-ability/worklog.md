# gameplay-ability — 작업 로그

> 시간순 "무엇을 왜 했나" 기록. progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정은 `(→D#)`(decisions.md)로 링크한다. 2026-10-23 정리로 검증 수치·UE 줄 번호 나열을 걷어냈다 — 원문은 git 이력.

### 2026-10-22 — GA 에셋 설명(description) 필드
- GE 설명 요청에 이어 "GA쪽도". 런타임 원형이 아니라 에셋에 둔다(동작에 안 쓰임).

### 2026-10-22 — 입력 통로 (→D11)
- 데모에서 유저 "달리기도 Tag 기반 GA로", "점프도 GA 기반으로" — 누르는 동안 유지하는 어빌리티를 뗄 때 끝낼 통로가 없었다. UE 방식(Spec 눌림 상태 + ASC 전달 + GA `InputReleased`)과 게임 쪽 취소를 비교해 설명한 뒤 유저 "A로 하자"(코어에 UE식 통로).
- 인스턴스 전달은 목록 복사본을 돈다 — 받은 어빌리티가 그 자리에서 끝나면 Per Execution 인스턴스가 목록에서 빠져서.
- 게임 쪽: 막혀서 못 켜진 '누르는 동안' 어빌리티는 `PlayerCharacter`가 매 프레임 다시 시도 — Lyra `ProcessAbilityInput`의 `WhileInputActive`와 같은 방식, 코어가 아니라 게임 쪽.

### 2026-10-21 — 이름 변경: Actor → GameObject
- 유저 "Actor라는 이름을 바꾸자 안맞아" → 범위 "GA 쪽만", "GameObject로". `GameplayAbilityActorInfo` → `GameplayAbilityGameObjectInfo` 등. UE 이름을 적은 `<remarks>`와 GE 쪽 이름은 그대로. 인스턴싱 정책은 enum 정수로 저장돼 에셋은 안 바뀜.

### 2026-10-21 — GA 마무리 테스트: 셀프체크 Q
- 유저 "다양한 경우에 따라 Test Case 해서 해". 기존 A~P는 기능 하나씩이라 기능끼리 엮이는 경우·재진입·경계를 Q로 더했다(케이스 선정은 에이전트 판단).
- 발견: 실패 이벤트로 넘기는 사유 컨테이너가 ASC 멤버 하나라, 실패 구독자 안에서 다른 발동을 시도하면 바깥 이벤트의 사유가 덮어써진다. UE도 같은 구조 — 고치지 않고 TODO-BOARD.

### 2026-10-21 — 어빌리티 태그·차단·취소 (→D10)
- 유저 "Tag 기반으로 막기만 하면 금방이지?". UE 원문대로 발동 때 막기·취소(요청한 인스턴스 제외, Spec 활성 횟수 증가 전), 종료 때 풀기, 발동 조건에 ASC 막힌 태그 검사. Source/Target 태그는 유저 "빼기" → TODO.
- 회귀에서 Modifier Tag E-12가 실패 — 진단해 보니 직전 Play 모드 때문에 에디터 시계가 0이 아니면 1초 경계 Tick이 빗나가는 테스트 경계 문제. 이번 변경과 무관해 TODO-BOARD.

### 2026-10-21 — 어빌리티 이벤트·실패 사유 (→D9)
- 사유 태그 위치는 "설정 에셋으로 지정 (UE 방식)", 종료 이벤트는 "UE처럼 둘". 차단·필요 조건을 바로 끝내지 않고 다 따져 사유를 모두 모으게 바꿨다(UE와 같음, 결과 bool은 그대로).
- 레지스트리를 유저가 고치는 중이라 처음엔 사유 태그 칸을 비웠다가 유저 "이것도 알아서" → `Ability.ActivateFail.*` 5개 등록(이름은 UE 설정 변수 이름).

### 2026-10-21 — GA 인스펙터: 선언 클래스별 상자 + 코스트·쿨다운 GE 적합 표시
- 유저 "GA 상속 field Base Field 분리해서 UI 가독성 좋게" → 원형 필드를 선언 클래스별 상자로. 나누는 정책은 GA 전용이라 범용 드로어엔 넣지 않음.
- 유저 "Cost Cooldown GE에 대해 적합한지 표시 … 초록 빨강 … Tooltip으로" → 속성 하나 + 드로어 하나로 두 용도. 기준(에이전트 판단): UE는 타입을 강제하지 않으므로 "우리 확인·적용 경로에서 의도대로 동작하는가" — 코스트는 Instant·AddBase·음수, 쿨다운은 Duration·지속 > 0·부여 태그 있음. 유저 "좋아 적절하네".
- 발견: 타입 선택 목록에 테스트 전용 중첩 클래스가 노출되고 스크립트 아이콘이 비는 것 → TODO-BOARD.

### 2026-10-21 — GE 수명 버그 수정 (gameplay-effect D27)
- PlayMode에서 쿨다운이 적용된 프레임의 deltaTime 전체만큼 덜 남는 걸 찾았다. 유저 "단순 버그면 고쳐" → 단순하지 않아 두 안(실행 순서 앞당김 / 적용 시각 기록)을 냄 → "B로 해", "Tick의 순서가 영향을 미칠 부분을 정리해서 문서로" → gameplay-effect `tick-order-*.md` 두 문서(유저 "보완/사용자 두 문서로").
- 막힌 곳: 처음엔 남은 시간도 경과를 빼 쌓았더니 정확히 지속시간을 흘린 Tick 뒤 쿨다운이 남았다(float 경계) → 남은 시간을 적용 시각에서 다시 계산.

### 2026-10-21 — ASC 복제·파괴 (→D8)
- 유저 "UE의 결과와 동일하게". UE 원문: 어빌리티·AttributeSet 목록은 Transient, 설정만 복사. 파괴는 활성 어빌리티 취소 → 해제. 우리 구조는 복제가 이미 그래서 테스트로 고정만, 파괴는 `OnDestroy` → `DestroyActiveState`. UE의 재진입 플래그는 Unity `OnDestroy`가 한 번만 불려 뺐다.

### 2026-10-21 — 검증: TestGameplayAbility 셀프체크 M·N + PlayMode
- 유저 "검증 — TestGameplayAbility 해", 방식 "둘 다". `TestGameplayAbility`가 커밋 → GE 적용 → 종료(데모 즉발형과 같은 흐름).
- PlayMode 첫 실행은 에디터가 포커스를 잃어 프레임이 길어진 탓에 실패 → 쿨다운을 늘렸더니 진짜 버그(위 GE 수명)가 드러났다.

### 2026-10-21 — `SerializedDeepCopy` 콜백 누락 수정
- 셀프체크에서 활성 중 태그(ActivationOwnedTags) 2개가 실패. 원인: 깊은 복사가 `OnAfterDeserialize`를 부르지 않아 복제된 태그의 해시(`[NonSerialized]`)가 0으로 남고, 개수 사전에서 원래 태그로 못 찾음. 앞 항목에서 "영향 없음"이라 했던 건 부모 캐시만 보고 해시를 안 본 탓.
- 두 안 중 유저 "그래" → 복사본마다 `OnAfterDeserialize` 호출(부모 캐시 문제도 같이 풀림). 해시를 복사하지 않고 다시 계산하므로 `[NonSerialized]`로 둔 유저 의도("HashCode가 엔진 키고 끌 때마다 다르니까")와 맞다.

### 2026-10-21 — 코스트·쿨다운·커밋·태그 조건 (→D7)
- 유저 "Cost, Cooldown, Tag 조건 구현해". UE에선 코스트·쿨다운이 커밋 때만 적용돼 `CommitAbility`를 함께 넣었다(에이전트 판단). 코스트 확인은 GE 컨테이너 질의라 gameplay-effect에 `CanApplyAttributeModifiers`를 더함.

### 2026-10-21 — 인스턴싱 정책 (→D6)
- 유저 "Per Instance, Per Actor만 구현할거야". Per Actor 인스턴스 생성을 UE 위치(`OnGiveAbility`)로 옮기고, Per Execution은 발동마다 만들고 끝나면 버린다. UE `IsActive`는 Per Execution이면 CDO도 true지만 우리는 끝난 인스턴스를 버리므로 원형은 false.

### 2026-10-20 — GA에서 GE 적용 (→D5)
- 유저 "일단 UE에 맞춰서 구현하고 있어봐 따로 이해하고 있을게". UE 헬퍼를 옮기고 네트워크·Cue·스택 수·TargetData는 뺐다.
- 막힌 곳: UE는 컨텍스트에 어빌리티를 넣는데(`SetAbility`) 그 파일은 유저가 고치는 중이라 보류.

### 2026-10-20 — 생명주기 (→D4)
- UE 원문 흐름을 옮김. `bIsAbilityEnding`은 UE에서 BP 훅 재진입을 막는 용도인데 여기선 그 사이에 외부 코드가 돌지 않아 뺐다.

### 2026-10-20 — ASC 부여·해제
- 유저 "TODO에 GameplayAbility 구현 이거 세분화해" → 단계 나눔. "단순히 구현하지 말고 UE에 맞춰서", "GameplayAbilitySpecContainer 단순하게 구현하지 말고 UE에 맞춰서" → 스코프 락·보류 부여/해제·ActorInfo까지 UE 구조로.

### 2026-10-20 — 정의/런타임 분리(→D1) + SerializeReference 원형·깊은 복사(→D3)
- 유저 "GA는 이벤트 함수, field를 직접 사용하는게 중요" → SO 상속을 시도했다가, SO를 객체마다 복제하는 방식의 웹 조사 뒤 "Runtime Class 별도로 두는거로 가고, SO Asset은 단일로". 처음엔 타입만 저장(D2) → "SerializeReference로 하자 하위 field draw하고 저장할 필요가 있겠어"(D3).
- 예전 GA 골격(2026-09-13 stash 복원본)은 커밋 없이 유실돼 같은 날 다시 만들고 그 위에서 진행.
