# 개발 도구 설정

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
