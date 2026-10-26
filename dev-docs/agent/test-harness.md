# test-harness — 테스트 규약

**에이전트가 테스트(셀프체크·PlayMode 테스트·Play 확인)를 돌릴 때 따른다.** 상위: [HARNESS.md](HARNESS.md).
Unity 없이 컴파일만 보는 방법은 → [dev-tools.md](../dev/dev-tools.md) '컴파일 확인'. Unity MCP 연결은 → 같은 문서 'Unity MCP'.

---

## 원칙

1. **눈으로 확인할 변경은 에이전트가 직접 테스트한다.** 이동·카메라·스킬 같은 변경을 "확인해 주세요"로 넘기지 않는다(유저 "테스트 직접 해").
   - **단, 매 작업마다 돌리지 않는다 — TODO-BOARD '데모 마무리 → 포트폴리오' 4단계(모아 둔 Play 확인)에 모아 두고 한꺼번에 한다**(유저 "토큰이 아까워서 앞으로는 테스트를 매번하지마. TODO로 모아두고 한꺼번에 해"). 컴파일 확인은 매번 한다.
2. **테스트 중 유저 조작은 결과를 망칠 때만 막는다.** (유저 "만약 Test중에 내가 건들면 문제 없다면 냅두고, 건들어서 문제가 있으면 애초에 그건 내 입력을 막아둬")
   - **Play가 걸린 테스트(Play 확인·PlayMode 테스트)는 입력을 쓰지 않는 테스트여도 시작할 때 아래 '격리'를 모두 적용한다** — 유저의 입력·포커스가 테스트에 닿지 않게 한다.
   - 격리 밖에서 유저가 건드려도 테스트에 문제가 없으면 그대로 둔다. 문제가 되는 조작을 새로 찾으면 다음 테스트부터 미리 막고 '격리'에 더한다.
3. **테스트가 끝나면 Play를 항상 끈다 — 유저가 조종 중이어도.** (유저 "테스트 시 항상 끝나고나면 Play Mode 꺼버려 내가 조종하는거 상관없이")

---

## 셀프체크·PlayMode 테스트

- **셀프체크(에디터):** `EditorApplication.ExecuteMenuItem("Tools/Ability System/Run … Self-Check")` + `Application.logMessageReceived`로 결과 줄(`passed N / failed N`)을 모은다. 새 파일을 만든 뒤엔 `AssetDatabase.Refresh()` 먼저. GA 셀프체크는 `Assets/_Test` 폴더에 임시 에셋을 만든다 — 폴더가 없으면 만들고 돌린다.
- **PlayMode 테스트:** `Assets/Scripts/Test/*PlayModeTest.cs` 컴포넌트를 저장하지 않는 씬에 붙이고 Play → 결과 줄 `[… Self-Check] passed N / failed N`(Editor.log). Aggregator PlayMode는 ASC를 같은 오브젝트에 붙이고 `Assets/_Test/` GE 5개(`GE_DefaultAttributes_PlayModeTest`·`GE_HealthBoost`·`GE_InstantHeal`·`GE_SpeedFromHealth_*`)를 직렬화 필드에 배선한다 — `Setup PlayMode Test` 메뉴는 열린 씬을 저장하므로 쓰지 않는다.
  - 열린 씬에 저장 안 된 변경이 있으면 씬을 바꾸지 않고 임시 씬을 **추가(Additive)**로 열어 거기에 붙인다. Play를 끈 뒤 임시 씬은 저장하지 않고 닫는다.
  - 열린 씬에 설정 창이 있으면 포커스가 빠질 때 `timeScale`이 0이 돼 시간 기반 테스트가 멈춘다 — '격리'를 Play 직후 적용해 막는다.
- **임시 에셋 지우기:** MCP에선 `DeleteAsset`이 거부된다 → [dev-tools.md](../dev/dev-tools.md) 'MCP에서 막히거나 조용히 틀리는 것'.

---

## Play 테스트 — `Unity_RunCommand`에서 통한 방법

- **컴파일 최신 확인:** 연달아 편집하면 Unity가 중간에 컴파일해 Play가 옛 코드로 돈다. Play 전에 `Library/ScriptAssemblies/Assembly-CSharp.dll` 시각이 마지막 편집보다 뒤인지 본다.
- **프레임 진행:** `EditorApplication.update` 콜백에서 `Time.frameCount`가 바뀔 때만 한 단계 진행, 결과는 `Debug.Log`. 한 명령 안 for 루프로 이동을 돌리면 `CharacterController.velocity`가 실제 deltaTime 기준이라 값이 폭주한다.
- **포커스:** 에디터가 포커스를 잃으면 Play 시간이 거의 안 흐른다 → `Application.runInBackground = true`.
- **격리 — 테스트는 유저의 입력·포커스와 무관하게 돈다**(유저 "그 테스트 중에는 내가 화면 밖으로 나가거나 키보드 마우스를 해도 영향을 안받게 해줘"). Play에 **들어간 뒤** 한 명령에서(시작 직후 유저 입력이 새지 않게 `isPaused = true`로 Play에 들어가 격리한 뒤 풀어 준다 — 격리 전 몇 프레임에 진짜 마우스 입력으로 플레이어가 쏴 `Dummy_B`가 맞은 적이 있다):
  1. `InputSystem.settings.backgroundBehavior = IgnoreFocus`, `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView`. Play 안에서 바꾸면 Play가 끝날 때 패키지가 원래 값으로 되돌린다(`InputSystem.cs` `OnPlayModeChange` — 에디트 모드를 떠날 때 저장, 돌아올 때 복원). **Play 전에 바꾸면 그 값이 "원래 값"으로 저장돼 남는다.**
  2. 진짜 `Keyboard`·`Mouse`를 `InputSystem.DisableDevice`, 테스트용 가상 장치를 `InputSystem.AddDevice<Keyboard>/<Mouse>`로 만들어 그 장치에만 `QueueStateEvent`(여러 프레임 반복).
  3. 설정 창은 **오브젝트째** 끈다(`SettingsMenu.gameObject.SetActive(false)`) — 컴포넌트만 끄면 `OnApplicationFocus`는 여전히 받아 창을 열고 `timeScale` 0으로 만든다.
  4. `EditorApplication.playModeStateChanged`의 `ExitingPlayMode`에서 가상 장치 제거·진짜 장치 `EnableDevice`. Play를 끈 뒤 장치가 켜졌는지·입력 설정이 돌아왔는지 확인한다 — Play를 끈 직후 명령에선 복원 전 값이 읽힐 수 있어 다음 명령에서 다시 본다. 이 처리기가 안 불려 진짜 장치가 꺼진 채 남은 적이 있다 — 한 번은 Play 중 재컴파일로 도메인이 다시 로드돼 처리기가 사라진 것이었다. 그래서 **Play를 끄는 명령 안에서 먼저** 이름이 `Test`로 시작하는 장치를 지우고 꺼진 장치를 `EnableDevice`로 복원한 뒤 `isPlaying = false`로 한다(처리기는 예비로 둔다). 그 전에 `EditorApplication.update` 콜백을 떼거나 끝난 뒤 입력을 넣지 않게 한다 — 지운 장치에 입력을 넣으면 예외가 난다.
- **Play 중엔 `AssetDatabase.Refresh`·코드 편집을 하지 않는다.** Play를 끈 명령 바로 다음 명령에선 아직 `isPlaying`이 참일 수 있다 — 꺼진 것을 따로 확인한 뒤 Refresh한다. Play 중 재컴파일되면 런타임 필드가 비어 null 참조가 쏟아지고(ASC.Update 등) 게임 버그처럼 보인다.
- 테스트 명령마다 위 4의 종료 정리를 넣는다. 빠뜨리면 가상 장치가 남아 다음 명령이 `TestKB1`처럼 이름이 바뀐 장치를 만들고, 이름으로 찾은 옛 장치(꺼짐)에 입력을 넣게 된다.
- **화면 확인:** `Camera.main`을 RenderTexture로 Render → PNG를 scratchpad에 저장 → Read(UI Toolkit 오버레이는 안 찍힌다). UI까지 보려면 `ScreenCapture.CaptureScreenshot`(Game 뷰 크기 그대로). Scene 뷰는 `Unity_Camera_Capture`(카메라 ID 없이). 에디터 창(인스펙터 등)을 `InternalEditorUtility.ReadScreenPixel`로 찍지 않는다 — 모니터 화면을 그대로 읽어 Unity를 가린 유저의 다른 창이 찍힌다(찍힌 파일은 바로 지움). 에디터 창은 다시 그려도 예외가 없는지만 보고, 화면 모양은 유저 확인으로 남긴다.
- **구간(트리거) 테스트에서 순간이동하지 않는다** — `CharacterController`를 껐다 켜 옮기면 `OnTriggerExit`이 오지 않아 구간이 캐릭터를 안에 있는 것으로 남긴다. 들어가는 건 순간이동해도 되지만 나가는 건 입력으로 걸어 나간다.
- **결과 수집:** 콘솔이 다른 로그로 덮이면 `%LOCALAPPDATA%/Unity/Editor/Editor.log`를 grep한다.

### `Unity_RunCommand` 제약

- `System.Reflection` 사용(using·정규화 이름 모두)을 막는다.
- Cinemachine 타입을 못 쓴다(`using Cinemachine`을 `Unity.Cinemachine`으로 바꾸고 참조도 없음). 읽기는 `SerializedObject`로, 만들기는 메뉴 항목을 단 임시 에디터 스크립트를 `ExecuteMenuItem`으로 부른 뒤 지운다.
