# design.md — GAS 데모 방향

현재 활성 기획 = **Ability System(GAS 재구현) 데모**. 전체 게임 비전(컨셉·빙의·아이템·전투·씬·입력 등 비-GAS)은 보관됨 → [_parked/project/design.md](../_parked/project/design.md).

## 포트폴리오 방향 (2026-08-09 결정)

**메인 포트폴리오 = "완성된 게임"이 아니라 Ability System(GAS 재구현)이 온전히 작동하는, 완성도 높은 게임 데모.** 완성된 게임에서 **완성도 높은 데모(수직 슬라이스)**로 범위를 줄인다. GAS의 알맹이(Attribute·GameplayEffect·집계·캡처·Execution)는 이미 동작하므로, 이를 중심으로 주요 GAS 기능이 **한 데모 씬에서 엮여 작동**하는 것을 보여준다.

- **운영 원칙(논의):** 시스템(아키텍처)은 깊게, 콘텐츠는 데모 씬이 요구하는 만큼만 얕게. 각 기능은 UE-complete가 아니라 "최소한 진짜 도는 버전".
- **목표 기능(현재 미구현 대분류):** GameplayAbility(GA) · Gameplay Tag · Gameplay Cue · Gameplay Event · GE Stack · AttributeSet Hook(PreAttributeChange/PostGameplayEffectExecute·meta attribute). **Tag가 Cue·Event·GE 태그조건의 선행이라 레버리지가 큼.**
- **범위 밖(로컬 포트폴리오):** 네트워크/복제·예측, AbilityActorInfo.

### 기술 선택 (이 데모 한정)
- **UI Toolkit** — 데모 HUD(어트리뷰트 바·쿨다운·상태 아이콘)의 **구현 수단으로만**. "무엇"이 아니라 "어떻게". 깊게 공부하는 스킬 투자 대상 아님(AI 스캐폴딩으로 저비용, 우대사항 정도). 씨름이 시작되면 uGUI로 후퇴.
- **DOTween** — Cue/연출(juice) 레이어 **전용**. GAS 코어엔 넣지 않는다. 이력서 스킬로 내세우지 않고 Cue 이음새 뒤에 가둬 크레딧은 아키텍처로. 시뮬레이션↔표현 분리를 코드로 드러냄.
- **에셋 그래프(노드) 에디터** — 시각적 payoff 크나 GraphView 직렬화가 rabbit hole. **맨 마지막 선택적 보너스**로만. 1~2일 스파이크로 리스크 먼저 확인, 가장 좁은 한 조각(예: GE 하나 저작→SO)으로 한정. 데모 성공을 여기 걸지 않는다.

### 다음 단계
**데모 씬 한 장면을 먼저 확정** → 거기서 **필요한 GAS 최소 집합을 역산**해 기능별 스코프·순서를 정한다. (씬 초안: 플레이어가 도트뎀+스턴 / 자버프 / 적 피격→반격, 스탯·상태·쿨다운은 UI Toolkit HUD 표시.) 씬 확정 전엔 GA/Tag/… feature 폴더를 신설하지 않는다(등록이 먼저 — feature/HARNESS '폴더 구조·네이밍').
