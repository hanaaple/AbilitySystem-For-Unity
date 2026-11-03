# 테스트 환경

Unity Test Framework(NUnit) 기반 테스트 환경의 구조와 구축 절차. 실행 규약(격리·Play 종료·결과 수집)은 `dev-docs/agent/test-harness.md`, Unity 없이 컴파일만 보는 방법은 `dev-tools.md`.

## 목적

- 자동 검증: 케이스 단위 `[Test]`로 결과가 Pass/Fail과 XML(NUnit 형식)로 남는다. 메뉴 셀프체크(`Tools/Ability System/Run … Self-Check`)는 로그를 사람이 읽어야 했다.
- 성능 회귀 감지: Performance Testing 패키지의 `Measure.*`로 워밍업·측정 횟수·통계(중앙값·편차)가 표준화된 수치를 남긴다.
- CLI 실행: `Unity -runTests -batchmode …` 한 줄로 전체 또는 카테고리 단위 실행.

## 어셈블리 구조

Unity Test Framework는 테스트 어셈블리가 사전 정의 어셈블리(`Assembly-CSharp`)를 참조하지 못하게 한다. 그래서 테스트 대상 코드를 어셈블리 정의(asmdef)로 분리한다.

```
Assets/Scripts/Core/            Core                 (런타임, 참조 없음)
Assets/Scripts/Core/Editor/     Core.Editor          (Editor 전용, → Core)
  …/AbilitySystem/Editor/       ┐
  …/AbilitySystem/*/Editor/     ├ Core.Editor.asmref  (같은 어셈블리에 합류)
  …/Common/Editor/              ┘
Assets/Scripts/Game/            Game                 (→ Core, Cinemachine, Unity.InputSystem, UnityEngine.UI, Unity.RenderPipelines.Core.Runtime)
Assets/Scripts/Game/Abilities/Editor/  Game.Editor   (Editor 전용, → Game, Core)
Assets/Tests/EditMode/          Core.Tests.EditMode  (Editor 전용, → Core, Core.Editor, Unity.PerformanceTesting, TestRunner, nunit)
Assets/Tests/PlayMode/          Core.Tests.PlayMode  (모든 플랫폼, → Core, Game, Unity.PerformanceTesting, TestRunner, nunit)
Assets/Scripts/Test/            Assembly-CSharp(-Editor)  (기존 셀프체크·PlayMode 컴포넌트 — 사전 정의 어셈블리는 모든 asmdef를 자동 참조)
```

- 의존 방향은 `Core ← Game ← (Test)` 한 방향. `Core`는 `Game`·`Test`를 모르고, Editor 코드는 전부 `Editor/` 폴더 안에만 있어 Editor 전용 어셈블리로 떼어낼 수 있다.
- 흩어진 Editor 폴더는 각각 새 어셈블리를 만들지 않고 Assembly Definition Reference(`.asmref`)로 `Core.Editor` 하나에 합류시킨다 — 어셈블리 수를 늘리지 않고 Editor 코드끼리 `internal` 접근을 유지하기 위해서다.
- `Core`의 `internal`은 `Core.Editor`·두 테스트 어셈블리에만 `InternalsVisibleTo`로 연다(`Assets/Scripts/Core/AssemblyInfo.cs`). `Game`에는 열지 않는다 — 게임 코드는 공개 API만 쓰는 경계를 유지한다.
- 테스트 어셈블리는 `defineConstraints: UNITY_INCLUDE_TESTS`로 플레이어 빌드에서 제외된다(Test Runner가 테스트 실행 빌드에서만 정의).
- `Core.Tests.PlayMode`는 플랫폼을 제한하지 않아 필요하면 Player에서도 돌릴 수 있다(`-testPlatform <BuildTarget>`).

## 테스트 종류와 자리

| 종류 | 어셈블리 | 속성 | 용도 |
|---|---|---|---|
| 단위 | `Core.Tests.EditMode` | `[Test]`, `[TestCase]` | 순수 로직(`InternedName`, 태그 컨테이너 연산 등). Play 불필요 |
| 에디터 통합 | `Core.Tests.EditMode` | `[Test]`, `[UnityTest]` | 에셋 생성·저장·재로드(직렬화), 도메인 리로드(`EnterPlayMode`/`ExitPlayMode`) |
| 런타임 통합 | `Core.Tests.PlayMode` | `[UnityTest]` | 씬 로드·ASC·GE 적용 등 프레임이 필요한 동작 |
| 성능 | 두 어셈블리 | `[Test, Performance]` + `Measure.Method` / `[UnityTest, Performance]` + `Measure.ProfilerMarkers`·`Measure.Frames` | 마이크로 벤치(EditMode), 인게임 로딩·프레임(PlayMode) |

- 파일 이름 `<대상>Tests.cs`, 성능은 `<대상>PerformanceTests.cs`. 클래스에 `[Category("<대상>")]`을 붙여 CLI에서 `-testCategory`로 고를 수 있게 한다.
- 임시 에셋은 `Assets/Tests/_Temp/` 아래에 만들고 `[TearDown]`/`[OneTimeTearDown]`에서 지운다.
- 기존 셀프체크·PlayMode 컴포넌트(`Assets/Scripts/Test/`)는 그대로 둔다. 새 테스트는 `Assets/Tests/`에만 쓴다.

## 성능 테스트 규약

Performance Testing 패키지(6000.3 기준 3.2.0) 문서의 안정성 지침을 그대로 따른다.

- `WarmupCount(10)` 이상, `MeasurementCount(20)`(기본 9 → 20이 안정), 한 측정이 1 ms 이상이 되도록 `IterationsPerMeasurement`를 잡는다(1 ms 미만 측정은 환경 노이즈에 민감). 할당은 `.GC()`.
- 판정은 **중앙값**. 한 세트의 편차(Deviation)가 5%를 넘으면 그 세트는 버리고 재측정.
- A/B 비교는 두 구현을 같은 파일에 `#if <DEFINE>`으로 두고 scripting define만 바꿔 같은 테스트를 두 번 돌린다. 실행 로그에 실제 구현 이름을 남겨 재컴파일 실패로 옛 어셈블리가 남은 경우를 가려낸다.
- 인게임 구간은 `Unity.Profiling.ProfilerMarker`로 발행하고 `Measure.ProfilerMarkers`로 모은다(`Profiler.BeginSample`·딥 프로파일과 호환되지 않음).
- 결과는 Window ▸ Analysis ▸ Performance Test Report. CLI에서는 `-perfTestResults <path>`로 JSON.

## 실행

- Test Runner 창: Window ▸ General ▸ Test Runner → EditMode / PlayMode 탭.
- CLI(프로젝트 루트):
  ```
  "C:/Program Files/Unity/Hub/Editor/6000.3.12f1/Editor/Unity.exe" -runTests -batchmode -projectPath . -testPlatform EditMode -testResults <out>.xml [-testCategory <이름>] [-testFilter <정규식>]
  ```
  PlayMode는 `-testPlatform PlayMode`. 결과 XML은 NUnit 3 형식. 에디터가 이미 열려 있으면 같은 프로젝트를 CLI로 열 수 없다 — 그때는 Test Runner 창 또는 `Unity_RunCommand`에서 `TestRunnerApi`로 실행한다.

## 구축 절차

0단계는 한 번만 한다. 이후 새 테스트는 `Assets/Tests/<Mode>/`에 파일을 추가하면 된다.

1. asmdef 작성: `Core`, `Core.Editor`(+ `.asmref` 6개), `Game`, `Game.Editor`. Editor 전용 어셈블리는 `includePlatforms: ["Editor"]`.
2. `Assets/Scripts/Core/AssemblyInfo.cs`에 `InternalsVisibleTo` 3개.
3. 외부 패키지 어셈블리 이름은 `Library/PackageCache/<패키지>/…/*.asmdef`의 `name`을 읽어 넣는다(Cinemachine 2.x = `Cinemachine`, Input System = `Unity.InputSystem`, uGUI = `UnityEngine.UI`, RP Core = `Unity.RenderPipelines.Core.Runtime`).
4. `Packages/manifest.json`에 `com.unity.test-framework.performance` 추가(이미 `com.unity.collections`의 간접 의존으로 설치돼 있어도 직접 의존으로 명시한다 — 다른 패키지 변경에 끌려 사라지지 않게).
5. 테스트 어셈블리 2개: `Assets/Tests/EditMode/Core.Tests.EditMode.asmdef`, `Assets/Tests/PlayMode/Core.Tests.PlayMode.asmdef` — `references`에 TestRunner 2종(PlayMode는 `UnityEngine.TestRunner`만)·`Unity.PerformanceTesting`, `precompiledReferences: ["nunit.framework.dll"]`, `overrideReferences: true`, `autoReferenced: false`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`.
6. 검증: 컴파일 에러 0 → 기존 셀프체크·PlayMode 테스트 기준값 동일 → Test Runner 창에 두 테스트 어셈블리가 보임.
7. `dev-tools.md` '컴파일 확인'의 응답 파일이 어셈블리별(`Core.rsp`, `Core.Editor.rsp`, `Game.rsp`, …)로 늘어난다 — 바뀐 어셈블리 것만 컴파일한다.

## 참고

- Unity Test Framework — Edit/Play 모드 요건, 사전 정의 어셈블리 참조 불가: https://docs.unity.com/en-us/engine/6000.3/manual/programming-environment/test-framework-introduction/getting-started/edit-mode-vs-play-mode-tests
- Unity Test Framework — CLI 인자: https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html
- Performance Testing — `Measure.Method`: https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.1/manual/measure-method.html · 안정성 지침: https://docs.unity3d.com/Packages/com.unity.test-framework.performance@3.1/manual/writing-tests.html
- Assembly Definition Reference: https://docs.unity.com/en-us/engine/current/manual/programming-environment/script-compilation/assembly-definition-files/class-assembly-definition-reference-importer
