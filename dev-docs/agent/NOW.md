# NOW — 단일 재개 지점

> 휘발성 재개 메모. 진실은 코드 + progress/decisions. 세션 종료 시 통째 재작성.
> 최종 갱신: 2026-09-09 (KST)

## ▶ 지금 할 일 — 특정 작업 없음(클린)

지난 세션 산출물은 전부 **main에 머지 완료**. 착수 대기 중인 최우선 백로그는 여전히 **데모 씬 한 장면 확정**(→ `TODO-BOARD.md` ⭐ / `design.md 다음 단계`) — 씬을 정하면 필요한 GAS 최소 집합이 역산된다. 확정 전 GA/Tag feature 폴더 신설 금지.

## ✅ 최근 완료 (이번 세션, main 머지됨)

- **하네스/dev-docs 간결화** (PR#4, `227dc71`) — cases 시스템 제거, 핵심원칙 평면 번호화, 금지 인덱스화, feature 규약 파일 분리(`feature-list-convention.md`·`progress-convention.md`) + `feature/HARNESS.md` 라우팅 허브화.
- **코드 주석 스윕(중도)** (PR#5, `7ff4b39`) — 다중줄 XML 요약 압축, 컨벤션 위반(`(UE:)`·`(→D#)`) 제거, 죽은 코드/자명 라벨 제거. ~40파일.
- **구조 이동:** `AttributeAggregator`를 `Core/AbilitySystem/Aggregator/` → **`Effect/`**로 이동(namespace `Effect`, GUID 보존). `Aggregator/` 폴더 제거됨. 앞으로 이 클래스는 Effect 네임스페이스.

## ⚠ 유의 · 환경

- **feature-list ↔ progress 상태 불일치(미해결):** attribute·gameplay-effect·aggregator가 `feature-list.md`엔 `✅ 임시완료`, 각 `progress.md` 상단은 `🔧 IN-PROGRESS`. 동기화 유저 판단 대기. (`✅ 임시완료`는 정식 상태 아님 — 유지 시 `feature-list-convention.md` 상태 정의에 추가 필요.)
- **GA 작업은 `stash@{0}`**에 그대로(유저 stash, 미착수). GA 스크립트 4개 + `ASC.GiveAbility` 스켈레톤 + `GA_New.asset`.
- **선택 백로그:** 코드 주석을 더 공격적으로 솎는 추가 패스(중도 → 공격적) 가능 — 유저가 원하면.
- **정리 대상(무관):** 오래된 로컬 브랜치 `Feature/Character-Movement-Prototype`·`Feature/GE-Execution`(원격 gone).
- **Unity MCP:** 현재 OFF.
