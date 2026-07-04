# 개발 도구 설정

## Unity MCP (Claude Code 연동)

`mcp-for-unity` 패키지로 Claude Code에서 Unity 에디터를 직접 조작할 수 있음.

- 설정 파일: `.claude/settings.local.json` (git 제외, 개인 로컬 전용)
- Unity 에디터가 열려 있어야 MCP 서버가 연결됨
- Claude Code 세션 재시작 후 활성화됨
- 연결 확인: `manage_editor` → `telemetry_ping` 응답이 `success: true`이면 정상

**현재 로컬 설정:** `C:\Users\<username>\.local\bin\uvx.exe`

팀원이 사용하려면 `.claude/settings.local.json`을 아래 형식으로 직접 생성 (`<uvx 경로>`는 본인 환경에 맞게 수정):
```json
{
  "mcpServers": {
    "unityMCP": {
      "command": "C:\\Users\\<username>\\.local\\bin\\uvx.exe",
      "args": [
        "--prerelease",
        "explicit",
        "--from",
        "mcpforunityserver>=0.0.0a0",
        "mcp-for-unity",
        "--transport",
        "stdio"
      ]
    }
  }
}
```

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