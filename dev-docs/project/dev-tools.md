# 개발 도구 설정

## 컴파일 확인 — Unity 없이 (에이전트용)

Unity 에디터를 띄우지 않고 C# **컴파일 에러만** 잡는 방법이다. Unity가 마지막으로 컴파일할 때 남긴 컴파일러 응답 파일(`.rsp`)을 복사해 Unity에 들어 있는 Roslyn으로 다시 돌린다. 에디터 동작·런타임 동작은 확인하지 못한다(그건 Unity MCP로 — [agent/test-harness.md](../agent/test-harness.md)).

### 재료

| 것 | 위치 |
|---|---|
| 런타임 응답 파일 | `Library/Bee/artifacts/<dag>/Assembly-CSharp.rsp` (`<dag>`는 환경마다 다름, 예: `1900b0aE.dag`) |
| 에디터 응답 파일 | `Library/Bee/artifacts/<dag>/Assembly-CSharp-Editor.rsp` |
| 컴파일러 | `<Unity 설치>/Editor/Data/NetCoreRuntime/dotnet.exe` + `<Unity 설치>/Editor/Data/DotNetSdkRoslyn/csc.dll` (이 프로젝트: `C:/Program Files/Unity/Hub/Editor/6000.3.12f1`) |

### 절차

작업 디렉터리는 **프로젝트 루트**다(응답 파일 안의 경로가 상대 경로).

1. 두 응답 파일을 임시 폴더(에이전트 scratchpad)로 복사한다 — 예: `runtime.rsp`, `editor.rsp`.
2. 각 사본에서 출력 줄을 고친다. **Library 쪽 산출물을 덮어쓰지 않게** 하기 위해서다.
   - `-out:"…"` → 임시 폴더 경로(`Runtime.dll`·`Editor.dll`).
   - `-refout:"…"` 줄은 지운다(남기면 Unity의 `Assembly-CSharp.ref.dll`을 덮어쓴다).
3. Unity가 마지막으로 컴파일한 뒤 새로 만든 `.cs`는 따옴표 경로로 한 줄씩 추가하고, 지운 파일은 줄을 지운다. 런타임 파일은 `runtime.rsp`에만, `Editor/` 폴더 파일은 `editor.rsp`에만 넣는다(양쪽에 넣으면 같은 타입이 두 번 정의돼 가짜 에러가 난다).
4. 런타임을 먼저 컴파일한다.
5. 에디터는 `editor.rsp`의 `-r:"Library/Bee/artifacts/<dag>/Assembly-CSharp.ref.dll"` 줄을 **4에서 만든 `Runtime.dll`로 바꾼 사본**으로 컴파일한다. 바꾸지 않으면 Unity가 예전에 만든 런타임 API로 검사해, 바뀐 시그니처를 놓치거나 가짜 에러가 난다.
6. 판정: 종료 코드 0이고 출력에 `error`가 없으면 통과. 경고는 이번에 손댄 파일 것만 본다(기존 경고가 많다).

```bash
S=<임시 폴더>; U="C:/Program Files/Unity/Hub/Editor/6000.3.12f1/Editor/Data"
"$U/NetCoreRuntime/dotnet.exe" "$U/DotNetSdkRoslyn/csc.dll" @"$S/runtime.rsp" > "$S/runtime.log" 2>&1; echo "runtime exit=$?"
sed "s#^-r:\"Library/Bee/artifacts/<dag>/Assembly-CSharp.ref.dll\"#-r:\"$S/Runtime.dll\"#" "$S/editor.rsp" > "$S/editor2.rsp"
"$U/NetCoreRuntime/dotnet.exe" "$U/DotNetSdkRoslyn/csc.dll" @"$S/editor2.rsp" > "$S/editor.log" 2>&1; echo "editor exit=$?"
grep -E "error" "$S/runtime.log" "$S/editor.log"
```

### 한계

- 응답 파일은 Unity가 **마지막으로 컴파일한 시점**의 설정이다. 패키지·define·asmdef가 바뀌었으면 Unity를 한 번 열어 컴파일시킨 뒤 다시 복사한다.
- 컴파일 통과 ≠ 동작 확인. 직렬화·에디터 GUI·도메인 리로드 문제는 잡지 못한다.

## Unity MCP (Claude Code 연동)

Claude Code에서 Unity 에디터를 직접 조작하기 위한 연동. 현재는 **Unity 공식 MCP**(릴레이 기반)를 쓴다.

### 구성 요소

| 쪽 | 실체 | 위치 |
|---|---|---|
| 릴레이(서버) | `relay_win.exe --mcp` | `~/.unity/relay/relay_win.exe` (머신 로컬, 리포에 없음) |
| 서버 정의 | `mcpServers.unity-mcp` | `~/.claude.json` 최상위 (유저 스코프) |
| Unity 쪽 | 에디터 내장 기능이 릴레이에 붙음 | Unity 에디터 (별도 `Assets/` 패키지 없음) |

- 서버 정의가 프로젝트가 아니라 **유저 스코프**에 있어, 이 리포의 `.mcp.json`은 `{"mcpServers": {}}`로 **비어 있는 게 정상**이다.
- 도구 프리픽스는 **`mcp__unity-mcp__*`**.

### 켜고 쓰기

평소엔 비연결로 두고(도구 스키마가 컨텍스트를 차지) 필요할 때만 연결한다.

1. Unity 에디터를 연다 → 릴레이에 연결되면 `~/.unity/mcp/connections/`에 항목이 생긴다.
2. Claude Code 세션 재시작 → `mcp__unity-mcp__*` 도구가 로드된다.

세션에 도구가 안 보이면 = 에디터 미연결이거나 세션 재시작 전이다.

### MCP에서 막히거나 조용히 틀리는 것

- **씬을 다시 열면 그 전에 불러 둔 에셋 객체가 무효가 된다.** `EditorSceneManager.OpenScene` 앞에서 `LoadAssetAtPath`로 받아 둔 에셋을 그 뒤 직렬화 필드에 넣으면 에러 없이 `{fileID: 0}`으로 저장된다(2026-10-22). 씬을 연 **뒤에** 다시 불러 쓰고, 저장한 씬 파일에서 참조를 확인한다.
- **사용자 상호작용이 걸린 API는 "User interactions are not supported"로 거부된다.** 확인된 것: `AssetDatabase.DeleteAsset`(2026-10-21), `EditorSceneManager.SaveScene(scene, path, saveAsCopy: true)`(2026-10-22). 지우기는 디스크에서 파일·meta를 지운 뒤 `AssetDatabase.Refresh()`.

### 에셋 배선 — MCP 한계와 YAML 직접 편집

에셋/씬/프리팹의 **일부 값은 MCP 도구로 안 붙는다**(시도해도 실패하거나 조용히 `fileID: 0`). 아래는 **처음부터 `.asset`/`.prefab`/`.unity` YAML을 직접 Edit**하는 게 낭비가 적다.

| 대상 | 대응 |
|---|---|
| `[SerializeReference] List<T>` 요소 | YAML `references.RefIds` 블록 직접 작성 |
| 리스트 요소 추가 (`Array.size`) | YAML로 요소 추가 |
| 오브젝트 참조 주입 (SO/프리팹 참조 필드) | YAML에 `{fileID: 11400000, guid: <asset guid>, type: 2}` 직접 |
| 프리팹 자산 프로퍼티 | `.prefab` YAML 직접 편집 |

**YAML 형식:**
- 오브젝트 참조(SO 자산): `{fileID: 11400000, guid: <32자리 guid>, type: 2}`. (컴포넌트/스크립트 참조는 fileID·type이 다름 — 기존 유사 자산에서 형식 복사.)
- SerializeReference 요소:
  ```
  <listField>:
  - rid: 7000000000000000000        # 임의의 unique long
  references:
    version: 2
    RefIds:
    - rid: 7000000000000000000      # 위 rid와 일치
      type: {class: <ClassName>, ns: <Namespace>, asm: Assembly-CSharp}
      data: { <field>: <value>, ... }
  ```

**조합:** 자산/GameObject **생성은 MCP**(guid·meta 자동), **세부 값 배선은 YAML**. 편집 후 에셋 refresh — 스크립트 추가/삭제는 도메인 리로드로 연결이 끊길 수 있다(정상, 재시도).

> **에셋별 오소링 레시피:** GameplayEffect 에셋은 → [architecture/ability-system/gameplay-effect.md](architecture/ability-system/gameplay-effect.md) "에셋 생성·세팅".

### Play 테스트

Unity MCP로 셀프체크·PlayMode 테스트·Play 확인을 돌리는 방법은 → [agent/test-harness.md](../agent/test-harness.md).
