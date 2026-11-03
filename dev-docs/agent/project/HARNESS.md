# project/HARNESS — 프로젝트 규약 허브

이 프로젝트(Unity GAS)에 고유한 규약: 설계 원칙, 코드 작성, 테스트 실행, git 세부, feature 문서. 에이전트 자체 규약은 [../core/HARNESS.md](../core/HARNESS.md). 인덱스는 [../HARNESS.md](../HARNESS.md).

## 프로젝트 핵심 원칙

1. **설계 결정에는 근거와 선택 이유를 남긴다.**

   설계 결정에는 기술적 근거와 선택 이유가 드러나야 하고, **판단이 문서화 안 된 코드는 산출물로 인정하지 않는다.**

2. **과설계하지 않는다.**

   "필요 없지만 인상적이라 추가" 금지, **절제한 판단(왜 단순한 쪽을 택했나)도 기록 대상이다.**

3. **"왜"는 결과·맥락에 연결한다.**

   트레이드오프는 다운사이드까지 인정해 서술한다(❌ "캐시가 빠르니까" / ✅ "읽기 지연은 줄지만 최종 일관성 감수 — 속도가 중요해 허용"). **한 결정을 한 문장으로 "왜"까지 말할 수 있어야 한다**(막히면 근거 부족).

4. **"왜"의 근거는 유저 의도, 그다음 UE 원본이다.**

   ① 유저 의도가 최상위(지어내지 않고 묻는다 → core 원칙 8), ② 그다음 UE 원본(원문 확인 규율 → core 원칙 5). divergence는 의도된 선택일 수 있다. **"UE를 따른다"=본질·방향성이지 세부 구현 복제가 아니다** — UE 고유 제약(네트워크 복제·legacy)에서 온 구현은 그 제약이 없으면 따르지 않고, **"UE가 왜 이렇게 했는지 → 우리에게도 성립하는지"**를 먼저 판단한다.
   - **UE 원본은 로컬에 있다: `D:\GitHub\UnrealEngine` (UE 5.6.0).** gh(`EpicGames/UnrealEngine`는 비공개)·WebFetch보다 **이 경로를 Grep/Read로 직접** 본다. 프로젝트가 미러링하는 버전이라 버전 일치도 이쪽이 정확. (예: GameplayTags → `Engine/Source/Runtime/GameplayTags/Classes/`)
   - **Lyra도 로컬에 있다: `C:\Users\Uisoo\Documents\Unreal Projects\LyraStarterGame`**(유저). 게임 쪽 GAS 사용 예(입력 처리 등)는 여기서 원문을 본다. (예: 입력 → `Source/LyraGame/AbilitySystem/LyraAbilitySystemComponent.cpp`)

5. **슬라이스는 에디터에서 눈으로 확인돼야 완료다.**

   슬라이스는 완료 시 에디터에서 눈으로 확인 가능해야 하고(수직 슬라이스), **`worklog.md`는 시간순 "판단의 이야기"로 쓴다** — 무엇을 왜 했고, 어디서 막혀 어떻게 풀었나.

> **결정 기록(decisions.md)의 기준(무엇을·어떻게 적나)**은 `private/feature/HARNESS.md`('결정 기록 기준')에 있다.


---

## 하위 문서·폴더

- **규약 문서 네이밍:**
  - **폴더 규약 = 폴더 안 `HARNESS.md`, 또는 폴더 옆 `<폴더>.harness.md`**(폴더 안에 규약 파일을 두지 않을 때 — 예: `feature/ability-system.harness.md`) — 그 폴더·하위 폴더에 적용(경로 위의 것을 모두 적용).
  - **파일 규약 = `<원본 파일>.harness.md`** — 원본 파일 하나에만 대한 규약, **원본 옆에 둔다**(원본이 private이면 private — 예: `private/TODO-BOARD.harness.md`·`private/feature/feature-list.harness.md`). 원본 문서는 상단에서 자기 `.harness.md`로 링크한다. 그 파일을 만들거나 수정하기 전 확인한다.
  - **종류·행위 규약 = `<이름>-harness.md`** — 같은 종류의 여러 파일(`progress-harness.md`)이나 행위(`docs-harness.md` — 문서 write)에 대한 규약. 적용 대상 문서는 상단에서 링크한다.
- **하위 폴더는 그 폴더의 `HARNESS.md`에 위임한다** — 내부 파일을 여기서 나열·설명하지 않는다.

| 대상 | 내용 |
|---|---|
| `unity-test-harness.md` | Unity에서 테스트 돌리는 절차 — 셀프체크·NUnit·Play 테스트(Unity MCP), 격리, RunCommand 제약. 원칙은 [../core/test-harness.md](../core/test-harness.md). |
| `code-guide/` | 코드 작성 규약(설계·연쇄 호출·주석) + 하위 실전 가이드 → [code-guide/HARNESS.md](code-guide/HARNESS.md) |
| `feature/` | feature 규약 파일(공개: 그룹·feature 전용 하네스) → [feature/HARNESS.md](feature/HARNESS.md) |
| `private/` | 비공개(submodule) — 작업 보드·feature 내용 문서와 그 규약 → [private/HARNESS.md](private/HARNESS.md). 새 환경에서는 `git clone --recurse-submodules`, 이미 받았으면 `git submodule update --init`. 비공개 저장소 접근 권한이 없으면 `private/`만 비어 있다. |
