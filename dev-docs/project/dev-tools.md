# 개발 도구 설정

## Unity MCP (Claude Code 연동)

`mcp-for-unity`로 Claude Code에서 Unity 에디터를 직접 조작할 수 있음. **이 절만 보고 새 환경에서 처음부터 재구성할 수 있게** 구성 요소·설정 위치·복구 절차를 모두 적어 둔다.

### 구성 요소 (두 쪽이 붙어야 동작)

| 쪽 | 실체 | 위치 | git |
|---|---|---|---|
| **Unity 브릿지** | `com.coplaydev.unity-mcp` v9.3.1 (에디터 패키지) | `Assets/MCPForUnity/` — **리포에 커밋됨** | ✅ 추적됨 |
| **MCP 서버** | `mcpforunityserver` (Python, `uvx`로 실행) | 로컬 머신에 설치 (uv 툴체인) | ❌ 개인 환경 |
| **서버 정의** | `mcpServers.UnityMCP` | `.mcp.json` (프로젝트 루트) | ✅ 커밋됨 |
| **On/Off 스위치** | `disabledMcpjsonServers` | `.claude/settings.local.json` | ❌ 로컬 전용 |

> Unity 쪽은 리포에 들어 있으므로 **클론만 하면 준비 완료**다. 새로 세팅할 게 있다면 항상 **머신 쪽(uv) + 스위치**뿐이다.
> 루트의 `MCPForUnity.Editor.csproj`·`MCPForUnity.Runtime.csproj`는 Unity가 이 패키지로부터 생성한 것이라 별도 관리 대상이 아니다.

### 처음부터 세팅 (또는 복구)

1. **uv 설치** — `uvx`가 PATH에 있어야 한다. 없으면:
   ```powershell
   powershell -c "irm https://astral.sh/uv/install.ps1 | iex"
   ```
   확인: `uvx --version` (설치 후 터미널/세션 재시작 필요할 수 있음)
   - `uvx`가 PATH에 안 잡히면 `.mcp.json`의 `command`를 절대 경로(`C:\Users\<username>\.local\bin\uvx.exe`)로 바꾼다.
2. **서버 정의 확인** — `.mcp.json`에 아래가 있어야 한다 (커밋되어 있으니 보통 그대로 있다).
   ```json
   {
     "mcpServers": {
       "UnityMCP": {
         "command": "uvx",
         "args": ["--prerelease", "explicit", "--from", "mcpforunityserver>=0.0.0a0",
                  "mcp-for-unity", "--transport", "stdio"]
       }
     }
   }
   ```
   서버 패키지는 uvx가 실행 시점에 자동으로 받아 온다(사전 설치 불필요).
3. **스위치 켜기** — `.claude/settings.local.json`의 `disabledMcpjsonServers`에서 `"UnityMCP"` 제거 (아래 운영 방침).
4. **Unity 에디터를 연다** — 에디터가 떠 있어야 브릿지가 붙는다.
5. **Claude Code 세션 재시작** → `mcp__UnityMCP__*` 도구가 뜬다.
6. **연결 확인** — `manage_editor` → `telemetry_ping` 이 `success: true`면 정상.

### 운영 방침 — 평소엔 끄고, 요청 시에만 켠다

MCP를 상시 연결하면 도구 스키마가 컨텍스트를 차지하므로 **기본 비연결**로 둔다. 유저가 "Unity MCP 켜라"고 하면:

1. `.claude/settings.local.json`의 `disabledMcpjsonServers`에서 `"UnityMCP"`를 **제거**(`[]`로) — 켜기
   - 끌 때는 반대로 `disabledMcpjsonServers: ["UnityMCP"]` 로 되돌린다
2. **세션 재시작 필요** — 설정만 바꿔선 현재 세션에 도구가 로드되지 않는다. 유저가 세션을 재시작한 뒤에야 `mcp__UnityMCP__*` 도구가 뜬다
3. 재시작 후 `telemetry_ping`으로 연결 확인 → 작업 진행

> 즉, 현재 세션에 `mcp__UnityMCP__*` 도구가 안 보이면 = 아직 꺼져 있거나, 재시작 전이거나, uv가 안 깔린 것이다. **확인할 파일은 `.mcp.json`과 `.claude/settings.local.json` 둘뿐**이고 `~/.claude.json` 전체를 뒤질 필요 없다.
>
> **현재 로컬 상태(2026-07-20 확인):** 스위치 OFF — **위 방침대로 의도적으로 꺼 둔 것**이지 고장이 아니다. 다만 이 머신엔 uv(`uvx`)가 아직 없으므로, 켤 때는 스위치 전에 "처음부터 세팅" 1을 먼저 밟아야 한다.

**주요 MCP 도구:**

| 도구 | 용도 |
|---|---|
| `manage_editor` | 에디터 상태 조회, Play/Pause/Stop, 태그·레이어 추가 |
| `manage_gameobject` | GameObject CRUD (생성·수정·삭제·복제) |
| `manage_scene` | 씬 로드·저장·쿼리 |
| `manage_components` | 컴포넌트 추가·제거·프로퍼티 수정 |
| `manage_script` | 스크립트 생성·수정·삭제 |
| `read_console` | Unity 콘솔 로그 읽기 (컴파일 에러 확인) |
| `find_gameobjects` | 이름·태그·레이어·컴포넌트로 오브젝트 검색 |

### 에셋 배선 — MCP 한계와 YAML 직접 편집 (중요)

에셋/씬/프리팹의 **일부 값은 MCP 도구로 설정되지 않는다.** 아래 케이스는 시도해도 실패하거나 조용히 안 붙으므로(`fileID: 0`), **처음부터 `.asset`/`.prefab`/`.unity` YAML을 직접 Edit**한다. 헛된 호출·재확인 read로 토큰을 낭비하지 않는 게 목적이다.

| 대상 | MCP 상태 | 대응 |
|---|---|---|
| **SerializeReference 리스트** (예: `[SerializeReference] List<T>`) | 완전 미지원 | YAML `references.RefIds` 블록 직접 작성 |
| **리스트 요소 추가** (`Array.size`) | ArraySize 미지원 | YAML로 요소 추가. 단 `manage_scriptable_object modify`로 `field.Array.data[0].<x>`를 쓰면 배열이 **자동 확장**돼 먹히기도 함 — 되면 활용, 안 되면 YAML |
| **오브젝트 참조 주입** (SO/프리팹 참조 필드) | `manage_gameobject.component_properties`·`manage_components.set_property`로 **안 붙는 경우 많음** | YAML에 `{fileID: 11400000, guid: <asset guid>, type: 2}` 직접 |
| **프리팹 자산 프로퍼티** | `manage_components`는 씬 오브젝트만 대상 | `.prefab` YAML 직접 편집 |

**YAML 형식 참고:**
- **오브젝트 참조**(SO 자산): `{fileID: 11400000, guid: <32자리 guid>, type: 2}`. (컴포넌트/스크립트 참조는 fileID·type이 다름 — 기존 유사 자산에서 형식을 복사한다.)
- **SerializeReference 요소** (managed reference):
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

**MCP로 하는 게 나은 것:** 자산/GameObject **생성**(guid·meta 자동)·컴포넌트 추가·씬 저장·refresh. → **생성은 MCP, 세부 값 배선은 YAML**의 조합이 가장 낭비가 적다.

**편집 후:** `refresh_unity`(scope=assets 또는 all). 스크립트 추가/삭제는 도메인 리로드로 연결이 끊길 수 있음(정상 — 재시도). 새 타입 참조 자산은 **컴파일 완료 후** 생성 가능.

> **에셋별 오소링 레시피:** GameplayEffect 에셋(타입·modifier·YAML 예시)은 → [architecture/ability-system/gameplay-effect.md](architecture/ability-system/gameplay-effect.md) "에셋 생성·세팅".