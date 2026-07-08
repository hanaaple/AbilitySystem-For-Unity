# attribute — 작업 로그

> 시간순 작업 이력(아카이브). progress.md의 `## 다음 작업`이 재개 앵커이며, 과거 맥락이 필요할 때만 이 파일을 연다. 최신이 위로.
> 결정을 가리킬 땐 `(→D#)`(decisions.md)로 링크한다.

### 2026-07-08 — 버그픽스: `AttributeSetInitDataDrawer` Attributes 필드 간헐 미렌더

**증상(유저 리포트).** `AttributeInitData`에서 Attribute Set Add/Remove를 반복하면 가끔 특정 요소의 "Attributes" 라벨 아래 필드가 안 그려짐(빈칸). 지웠다 다시 하면 보이기도 하는 간헐성.

**원인.** `SyncAttributeFieldsIfChanged`가 `_lastSyncedTypeName`(static)에 **배열 인덱스 경로(`...data[i]...`)를 키로** "이미 sync함"을 캐싱 → Add/Remove로 요소가 밀리면 같은 경로가 다른 요소를 가리키는데도 sync를 스킵. `AttributeInitDataDrawer`의 add가 새 요소의 attributes를 `ClearArray()`하므로, "같은 인덱스·같은 타입" 재생성 시 캐시가 일치로 오판 → 빈 attributes가 그대로 렌더. 높이는 리플렉션 필드 수로 계산돼 빈칸만 남음(증상 일치).

**수정.** 인덱스 경로 캐시(`_lastSyncedTypeName`)와 `SyncAttributeFieldsIfChanged` 제거 → OnGUI에서 `SyncAttributeFields` 직접 호출. 이 메서드는 불일치가 있을 때만 배열을 변형하는 self-guard라 이미 맞으면 no-op → 매 프레임 호출해도 안전·저비용(에디터·소형 리스트). 성급한 최적화가 낳은 버그라 단순한 쪽으로 환원. 변경: `Editor/AbilitySystem/AttributeSetInitDataDrawer.cs` 1개.

**검증.** V1 정합성 OK(잔여 참조 0, 미사용 심볼 제거). V2 = 유저: 같은 타입 Add→Remove→같은 인덱스 Add 반복 시 매번 필드 즉시 렌더 확인. (선재 버그 — 같은 세션 다른 작업인 다형 리스트 빌더 리팩터와 무관, item worklog 2026-07-08 참조.)

### 2026-05-26 — PR #2 (Ability System - Attribute)
- Attribute 계층 구현: `AttributeSet`(빈 마커)·`AttributeData`(Base/Current)·`AttributeHandle`(FieldInfo 캐싱 struct), `CharacterAttributeSet`·`CombatAttributeSet` (→D1·D2)
- SO 초기화 파이프라인(`AttributeInitData`→`AttributeSetInitData`→`AttributeFieldInitData`) + `ASC.Awake` Reflection 세팅 (→D3)
- Editor 드로어: `AttributeInitDataDrawer`·`AttributeSetInitDataDrawer`·`AttributeReflectionUtility`
- ASC 최소 API: `AddAttributeSet`/`RemoveAttributeSet`, `GetAttributeBaseValue`, `SetBaseAttributeValue` (초기 109줄)
