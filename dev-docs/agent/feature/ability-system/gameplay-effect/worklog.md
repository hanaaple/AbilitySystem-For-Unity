# gameplay-effect — 작업 로그

> 시간순 "무엇을 왜 했나" 기록. progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정은 `(→D#)`(decisions.md)로 링크한다. 2026-10-23 정리로 검증 수치·조사 경로·회고를 걷어냈다 — 원문은 git 이력.

### 2026-10-23 — 대상별 Spec·컨텍스트 복제 (→D32)
- 데모 관통 파동은 발사 때 만든 Spec 하나를 여러 대상에 건다. `Clone()`은 컨텍스트를 공유해 대상마다 `AddHitResult`가 같은 객체를 덮어써, 2초 뒤 터지는 낙인이 마지막 대상의 부위로 헤드샷을 판정하게 된다.
- UE도 대상마다 Spec 복사 + `Context.Duplicate()` + `SetContext`를 한다 → 컨텍스트 `Duplicate`(얕은 복사), Spec `SetContext`.

### 2026-10-23 — GE 스택(스택 수 1, →D31)
- 데모에서 쏠 때마다 거는 `GE_CombatStance`가 여러 개 쌓였다. 유저 "Core에 GE 스택 구현", "상한 1개에".
- UE `FindStackableActiveGameplayEffect`(ByTarget은 정의만, BySource는 시전자까지)와 갱신 정책을 옮기고, 합칠 때 Spec은 처음 것 그대로 시간만 갱신.
- 막힌 곳: 셀프체크 S-4 실패는 테스트가 부여 태그를 Component로만 넣어 GE 캐시가 비었던 것 — 테스트 쪽 수정.

### 2026-10-23 — 디버그 조회 API (→D30)
- 디버그 오버레이용으로 UE와 같은 이름의 읽기 전용 조회 API. 에디터 ASC 인스펙터의 활성 GE 리플렉션은 필드 이름이 어긋나 표시가 안 되고 있었다 → 이 API로 교체. Awake 전(프리팹 에셋)엔 컨테이너가 없어 빈 목록을 돌려준다.

### 2026-10-22 — GE 에셋 설명(description) 필드
- 유저 "각 GE마다 Comment 같은? Description? 같은 걸 넣을 수 있게". UE GE엔 설명 필드가 없다. 동작에 안 쓰이는 `[TextArea]` 필드 + 인스펙터 맨 위, GE 에셋마다 설명을 채웠다(구성·쓰는 곳에서 확인한 사실만).

### 2026-10-22 — Context 피격 정보 (→D29)
- 유저 "GAS Tag 기반으로 헤드 판정을 넣어서 데미지 150%로"(이후 200%), 전달 방식 "컨텍스트에 피격 정보 (Lyra 방식)". Lyra는 Context HitResult → 물리 재질 태그 → 무기의 태그별 배율.
- 게임 쪽: 부위 콜라이더에 태그(`HitZone`), 투사체가 맞은 부위를 Context에 담고 `DamageExecution`이 부위 태그로 배율.

### 2026-10-22 — Spec 지속 시간 + `SetDuration` (→D28)
- 유저 "캐릭터 강화 되면서 버프동안 쿨다운 비율 감소로". 지속 시간이 에셋 고정값이라 걸 때 길이를 바꿀 통로가 없었다. 활성 GE·Tick·인스펙터가 정의 대신 Spec 값을 읽게 했다. 게임 쪽 `CharacterGameplayAbility.ApplyCooldown`이 감소율을 곱한다.

### 2026-10-20 — 어트리뷰트 값 변경 이벤트 (→D26)
- 유저 "구현 안되어있어? 간단하지 않아?" → "UE 구조대로". 맵·호출은 있었는데 맵에 넣는 코드가 없어 늘 비어 있었다.
- 구독 모양은 "클래스 getter 유지"(C# delegate가 불변이라 이벤트 객체를 돌려줌), 원인(`GameplayEffectModCallbackData`)은 "지금 넣기", 값이 같을 때는 "UE대로 항상 울림" — 결정 기록 없이 들어와 있던 `Mathf.Approximately` 건너뛰기를 지웠다. 0 데미지·클램프된 피격도 이벤트가 온다.

### 2026-10-20 — Calculation Modifier 런타임 적용 (→D23)
- 유저 "실제 계산 구현해둬 Transient만 미루고. 이미 있는거로 하면 금방 되는거 아냐?" — 확인해 보니 런타임 적용이 통째로 없었다.
- UE처럼 실행마다 캡처 aggregator 사본을 만들어 Calculation Modifier를 더하고, Execution 조회가 사본을 먼저 읽게 했다. 있던 부품(스냅샷 복사)을 이었다.
- UE와 다른 점(유저 질문 "호출 경로·파라미터 UE에 맞췄나"에 답한 것): Execution을 정의 대신 미리 만든 인스턴스로 돌아 평행 목록을 둠, 생성자 인자 순서, 사본 mod 핸들 기본값. 없는 조회 API는 유저 "빈 시그니처로만" → TODO-BOARD.

### 2026-10-20 — 태그로 활성 GE 찾기·제거 (→D24·D25)
- 유저 "태그로 활성 GE 찾기·제거 구현하자", 범위 "UE대로", `StacksToRemove` "자리만".
- 제거는 핸들을 먼저 모은 뒤 지운다 — 제거가 태그 변화로 다른 GE 제거를 불러도 안전.
- Remove Other: UE는 자기 핸들 목록을 첫 질의에 `MoveTemp`로 넘겨 둘째 질의부터 자기 제외가 빠진다. UE git 이력까지 보고 회귀로 판단해 모든 질의에 건다(유저 "UE의 의도대로").
- 유저 지적으로 새 코드를 규약에 맞춤(상수 PascalCase, private 필드 + 프로퍼티).

### 2026-10-19 — Execution Calculation Modifiers 편집기 + Magnitude 구조체 (→D22·D23)
- 유저 "Calculation Modifiers Editor 먼저 … Defs로 받아와서 읽어오면 어떻게 UE처럼 가능할거야". UE 편집기도 계산 클래스 CDO의 캡처 정의로 대상 콤보를 채운다.
- 물은 것: 기존 에셋 → "자동이관 말고 어셋에 직접", Magnitude → "구조체로 뽑아 같이 씀".
- 막힌 곳: 유저가 에디터에서 바꿔 두고 저장 안 한 에셋을 Unity가 새 코드로 리로드하며 일부 값이 빠졌다 → 디스크 원본을 변환·재임포트해 원래 값으로 돌림(유저의 저장 안 한 변경 하나는 사라짐).
- 이어서 Modifiers·Executions·Calculation Modifiers 인스펙터 규격 통일(유저 "가독성과 일관성"), 라벨 정렬(범용 드로어가 스스로 들여쓰던 것 제거 — "하드코딩으로 고치지 말고"), 접힌 머리 줄·선택기 잘림 수정. 셀프체크 `CalculationModifierSelfCheck`.

### 2026-08-07 — Executions 목록에 검색형 Add
- 요소 드로어 때문에 "+"가 안 떠 Size로만 늘려야 했다 → AttributeSet 목록과 같은 검색형 목록. New Script는 제외 — `Execute`가 abstract라 빈 템플릿이 컴파일되지 않는다.

### 2026-08-05 — 캡처 디버그 도구 + UE Execution 실행 조건 원문 확인
- 캡처를 눈으로 볼 수단이 없었다 → ASC 인스펙터 그리기를 공용화하고 활성 GE마다 Modifiers·Executions·캡처(출처 역추적) 표시, 별도 ASC 창.
- UE 원문: period 0 Duration/Infinite에선 Execution이 돌지 않는다(`ExecuteGameplayEffect`의 check). WebFetch 요약이 분기 중첩을 오독해 원문을 직접 봐야 했다.
- 버그: 목록에 modifier를 추가하면 Unity가 필드 이니셜라이저를 안 돌려 coefficient가 0 → add 콜백에서 1로.

### 2026-08-04 — AttributeBased 런타임 평가 + ModifierSpec UE 정렬 (치운 D21)
- 유저가 `CalculateModifierMagnitudes`의 이름-내용 불일치를 지적("이 함수는 magnitude를 계산하지도 않잖아", "attribute가 modifier의 key면 안 된다") → UE 원문 대조: `FModifierSpec`은 `EvaluatedMagnitude`만 갖고 정의와 평행 배열. 그대로 재설계.
- 캡처 → 평가 순서가 거꾸로라 AttributeBased가 캡처값을 못 읽던 버그도 같이 고침. 여기서 처음 캡처값이 실제 수치를 만들었다.

### 2026-08-04 — Spec 복사(Clone) + Target 캡처 배선 (→D19·D20)
- 같은 Spec을 여러 대상에 넘기면 Target 캡처가 서로 덮어쓴다 → 복사 생성자 + `Clone()`, 적용 경계 한 곳에서 복제 후 Target 캡처.
- 유저 "Active에만 캡처 아니냐" → UE 원문으로 Instant도 복사본에 캡처함을 확인.
- Spec을 struct로? → 아니오(C# struct는 얕은 복사라 UE 값 의미를 못 얻음). "clone이 지금 의미없잖아" → 맞지만 SetByCaller 도입이 확정이라 유지(D20).

### 2026-08-01~03 — 캡처 계층 신설 (→D14·D15)
- UE `FGameplayEffectAttributeCaptureDefinition`/`Spec`/`SpecContainer` 3계층. snapshot(고정)·non-snapshot(라이브) 둘 다(유저 확정).
- 정의 공급원이 0개라 무엇을 먼저 배선할지 물어 "AttributeBased가 캡처 정의를 품게"(D14).
- 유저 "코드 근거는 진짜 의미 없다. 가장 좋은 건 UE 원본, 그다음이 내 의도" → 컨테이너의 별도 정의 목록이 UE에 없음을 원문으로 확인하고 지움. 이 피드백은 하네스 원칙("왜"의 근거 순서)이 됐다.
- 어트리뷰트 참조를 `GameplayAttribute` 한 타입 + 전용 드로어로(D15) — 문자열 저장은 못 없애고(FieldInfo 직렬화 불가), 없앨 수 있는 건 팝업 배선 반복.
- Execution이 `Defs()`로 캡처 대상을 선언하고 Spec이 등록(치운 D17). 파라미터 조회 API는 UE 이름 그대로.

### 2026-07-18~19 — AttributeBased 편집기, Execution 프레임워크, Target 적용 경로
- AttributeBased magnitude 데이터·드로어(`(v + Pre) * Coef + Post`).
- Execution을 SO에서 순수 클래스로(D7) — SO는 공유할 데이터용인데 Execution은 로직이다. 입력·출력 타입 분리, 쓰기 지점을 하나로(Execution 출력이 2종 연산만 지원하던 결함 해소), Execution만 가진 GE가 무시되던 문제 수정.
- `ApplyGameplayEffectToTarget`·컨텍스트/Spec 팩토리, `MakeOutgoingSpec`은 Spec을 직접 반환(D5).
- 에이전트가 승인 없이 쓴 "캡처 미도입" 결정은 유저가 도입을 확정해 결번(치운 D6).

### 2026-06-13 — PR #3 (Ability System - GameplayEffect)
- GE(SO)·Modifier·Spec·ActiveGameplayEffect·Context, ASC 적용/제거·Modifier 6종·Duration/Period (→D1·D2·D4).
