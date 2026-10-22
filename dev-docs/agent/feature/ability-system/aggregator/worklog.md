# aggregator — 작업 로그

> 시간순 "무엇을 왜 했나" 기록. progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정은 `(→D#)`(decisions.md)로 링크한다. 2026-10-23 정리로 파일·심볼 나열을 걷어냈다 — 원문은 git 이력.
> 2026-10-16 이후 이 feature 코드의 변경은 다른 feature 작업에서 났다 — progress '다른 feature 작업으로 이 feature 코드 수정'.

### 2026-08-26 — 검증 셀프체크 + Unity MCP 연결
- 유저 지시로 "딸깍 검증" 툴 `AggregatorSelfCheck`(연산 공식·aggregator 경유 조회·snapshot vs 라이브 캡처·실제 GE 다수 케이스·런타임 dirty 재평가). 이걸 MCP로 돌리려고 Unity MCP를 켰다. PlayMode 검증 통과로 cross-attribute 라이브 재평가가 완성됐다.
- 캡슐화 리뷰(보고만): aggregator 변경 메서드가 public이라 파이프라인 우회로 상태가 오염될 수 있다 — internal 권장, 유저 승인 대기.

### 2026-08-25 — 캡처를 aggregator 기반으로 + 의존 등록 배선 (→D12)
- UE `CaptureAttributeForGameplayEffect`처럼 캡처 값의 출처를 aggregator로: snapshot은 그 시점 사본, non-snapshot은 컨테이너 소유 aggregator 참조.
- 의존 등록(non-snapshot일 때만)·해제를 persistent GE 적용/제거 경로에 배선. periodic은 mod가 aggregator에 상주하지 않고 실행마다 다시 계산하므로 대상이 아니다.
- 같이 고침: `BroadcastOnDirty`가 의존 목록을 별칭으로 돌던 것 → 진짜 복사. `Spec.Modifiers`를 배열로 노출(목록 인덱서는 struct 복사본을 돌려줘 제자리 갱신이 안 됨).
- 별건으로 유저 지시 프로젝트 전역 주석·컨벤션 정리(주석에서 외부 문서 포인터 제거 등 — CODE_CONVENTION).

### 2026-08-24 — 저수준 raw 쓰기 계층 정리(설계)
- "SetBaseValue"가 세 계층(ASC·AttributeSet·핸들)에서 이름이 겹쳐 순환 호출이 생김을 코드로 확인 → 고수준(파이프라인 진입)과 저수준 raw 쓰기(핸들 `…Raw`, 전파 없음)를 나눔.
- dirty 콜백 안의 Current 반영은 raw 쓰기로(유저): CurrentValue는 출력이라 다시 dirty 경로에 들어가면 broadcast가 재진입한다.
- UE 캡처 실패 동작 원문 확인: 정의가 없으면 assert(설정 오류는 강하게), 값을 못 내면 조용히 0(런타임 결측은 관용).

### 2026-08-21 — 어트리뷰트 핸들 이름·값 접근자
- 유저 주도로 런타임 핸들을 `GameplayAttributeHandle`로 이름 변경하고, 복사 + write-back을 핸들 접근자 한 곳에 격리(UE `SetNumericValueChecked`처럼, B안).

### 2026-08-16 — 값 경로 전수 감사 + base 진실 반전 (→D14)
- 유저 "Evaluate하는 부분 싹 다 UE처럼 — 모든 경로 확인해봐". UE 원문: aggregator는 캡처·non-periodic GE가 필요로 할 때만 지연 생성되고, base 진실은 `AttributeData`다. WebFetch 요약이 이걸 잘못 나열해 원문으로 반박했다.
- D7(aggregator가 base 진실)이 근거로 삼은 해석이 원문과 어긋났다 — UE의 이중 base는 복제·legacy 제약의 산물이고 우리에겐 그 제약이 없다. 유저가 반전을 확정. 이 판단에서 하네스 원칙 "UE를 따른다 = 본질·방향성, UE 고유 제약에서 온 형태는 따르지 않는다"가 나왔다.

### 2026-08-15 — ModifierSpec 축소 + dirty 진입점 ASC 경유 (→D13)
- 유저 지시: UE `FModifierSpec`처럼 슬롯엔 `EvaluatedMagnitude`만, 대상·연산은 정의를 같은 인덱스로 재해석(대가: 적용·실행마다 리플렉션 해석).
- UE는 aggregator `OnDirty`를 ASC에 바인딩한 뒤 컨테이너로 위임한다 → 우리도 ASC 진입점을 거치게(한 홉 더 들지만 UE 정합·대칭, 유저 지시). 활성 GE 핸들이 소유 ASC를 들게 했다(의존 전파가 핸들만으로 ASC를 찾도록).

### 2026-08-11 — 채널 제거 + 반응성 재설계 + cross-attr 방식 확정 (→D10·D12)
- 유저 "FAggregatorModChannel을 channel별 map으로" → 채널 구조를 넣었다가 같은 날 "채널을 1개만 쓸 거야. Channel이라는 걸 없애고" → flat 목록으로 되돌림(치운 D9 → D10).
- 유저 "container dirty 없애고 OnAttributeAggregatorDirty 만들어. ue 참고해서" → worklist를 없애고 aggregator `OnDirty` → 컨테이너 재계산. 순환 재귀 가드는 UE `MAX_BROADCAST_DIRTY` 그대로(유저 "ue 원문처럼").
- cross-attr 재평가: 에이전트가 위상 정렬 대안을 냈으나 유저가 UE 정합을 골랐다(D12). 재평가 = 의존 mod magnitude를 소스 새 값으로 갱신 후 대상 `Evaluate`. 순환은 mod magnitude에 있어 상한으로만 끊는다.

### 2026-08-10 — 어트리뷰트별 aggregator 본체 이관
- 착수 전 선결 두 갈림을 유저에게 확인(base 소유·mod 소유 — 둘 다 UE 근접 안). 계산 공식을 aggregator `Evaluate`로 옮기고 컨테이너 맵을 어트리뷰트별로. 재계산이 재진입하며 공유 스크래치 목록을 순회 도중 비우던 잠재 크래시도 없앴다.

### 2026-08-09 — 방향 반전: 진짜 어트리뷰트별 Aggregator + 컨테이너 레이어 (→D5)
- 유저가 앞선 구현(08-07·08)의 잘못 3건을 지적: ① 유저가 일부러 안 만든 컨테이너 레이어의 책임을 묻지 않고 ASC에 새로 만들었다 ② 이름은 aggregator인데 실제로는 ASC가 하나 든 재평가 worklist였다(진짜 aggregator는 어트리뷰트별) ③ 그래서 기존 계산을 aggregator로 옮길 범위를 통째로 빠뜨렸다. 원인: worklist를 aggregator로 오인했고, "full aggregator는 과설계"를 유저 목표 확인 전에 단정했다.
- 컨테이너(`ActiveGameplayEffectsContainer`)를 UE 경계대로 도입해 적용·제거·Tick·실행·집계를 ASC에서 옮김. 처음엔 struct였다가 유저가 class로. 유저 "ActiveGameplayEffectsContainer를 만들으라니까" — "우선 struct 만들어"에 배선까지 한 것이 범위 초과였을 수 있었다.
- 같은 날 포트폴리오 방향 전환(유저): 메인 = GAS가 온전히 도는 데모.

### 2026-08-07~08 — feature 신설 + 경량 반응성 구현 (이후 폐기)
- `GE_BoxSelfEffect`의 non-snapshot 모디파이어가 초기값으로만 적용되는 것을 계기로 UE 라이브 재평가 메커니즘을 원문으로 확인, attribute·gameplay-effect를 가로지르는 반응성 계층이라 새 하위 feature로 분리.
- 경량안(반응성만 분리 + 옵저버 + worklist 재평가, 치운 D1·D2·D4)으로 구현 — 2026-08-09 방향 반전으로 폐기.
