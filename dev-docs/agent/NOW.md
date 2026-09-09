# NOW — 단일 재개 지점

> 휘발성 재개 메모. 진실은 코드 + progress/decisions. 세션 종료 시 통째 재작성.
> 최종 갱신: 2026-09-10 (KST)

## ▶ 지금 할 일 — 특정 작업 없음(클린)

지난 세션 산출물은 전부 **main에 머지 완료**. 착수 대기 중인 최우선 백로그는 여전히 **데모 씬 한 장면 확정**(→ `TODO-BOARD.md` ⭐ / `design.md 다음 단계`) — 씬을 정하면 필요한 GAS 최소 집합이 역산된다. 확정 전 GA/Tag feature 폴더 신설 금지.

## ✅ 최근 완료 (이번 세션)

- **ability-system 4개 progress를 코드에 동기화** (문서만, 미커밋) — 실제 코드+확정 decision 근거, 주석 무시. 상태·수용기준·세부TODO·설계개요·결정 인덱스를 현재 로직에 맞춤:
  - **attribute** → `✅ 임시완료`. owner 주입이 `SetOwner`(등록 시)로 **배선 완료** — 구 "컴파일 불가" 블로커 해소(decisions D6 꼬리도 정정). 런타임 타입은 `ResolvedGameplayAttribute`→**`GameplayAttributeHandle`**로 현행화.
  - **gameplay-effect** → `✅ 임시완료`. GE 3종(Instant/Duration/Infinite) 실행 경로 모두 동작(enum 주석 "미구현"은 낡음). 캡처·AttributeBased·Execution·Clone 배선 반영. D18 컨테이너 "유보"→도입됨 표기.
  - **aggregator** → `✅ 임시완료`. 본문의 "재작성 대기/미구현/미커밋" 경고 제거. D14(base 진실=AttributeData, 지연 생성)·D12(cross-attr 라이브, dependents+cap) **구현 완료** 반영. 2026-08-26 PlayMode 통과 근거.
  - **gameplay-ability** → `📋 PLANNED` 유지. "Execution 미구현" 참조 정정(구현됨), stash 초안 존재 명시.
- **feature-list ↔ progress 상태 불일치 해소** — 셋 다 progress를 `✅ 임시완료`로 맞춰 feature-list와 일치.
- **architecture/ability-system 5개 문서 재정비** (미커밋) — 개념·방향 중심으로 압축, 흐름 다이어그램·재계산 시점 유지, 현재 코드에 맞춤(`GameplayAttributeHandle`·`ApplyModToAttribute`·컨테이너 등). aggregator.md는 옛 경량안 → 현 구현으로 전면 재작성. 필드 나열·Authoring 제거.
- **`architecture/HARNESS.md` 신설** — 아키텍처 문서 규약(규약만, 설명 배제). 반영기준/날짜 규약은 뺌.
- **비-GAS 인게임 문서 보관 처리** (미커밋) — `dev-docs/_parked/`로 `git mv`(경로 미러링): `architecture/{camera,controller-character,item-equipment}.md`, `feature/item-system/`. 혼합 문서(`design.md`·`architecture/overview.md`·`feature-list.md`)는 비-GAS 섹션을 `_parked`의 새 파일로 분리하고 활성본은 GAS 중심으로 축소. `_parked/README.md` + `CLAUDE.md` 라우팅 연결. wiki는 **보류**(빌드 산출물, 별도 확인 필요).

## 직전 세션 (main 머지됨)

- 하네스/dev-docs 간결화(PR#4), 코드 주석 스윕 중도(PR#5), `AttributeAggregator` → `Effect/` 이동(namespace `Effect`, GUID 보존).

## ⚠ 유의 · 환경

- **이번 문서 변경 커밋됨** — 브랜치 `docs/gas-refocus`에 4커밋(chore 프로젝트명 · docs(architecture) 재정비 · docs _parked 보관·동기화 · docs(wiki) 비-GAS 섹션 분리). **아직 push 안 함.**
- **wiki HARNESS.md 비-GAS 섹션 분리 완료** — `arch`·`scenes`·`camera`·`input` 섹션 맵을 `_parked/project/wiki/HARNESS.md`로 이관, 활성 HARNESS.md는 GAS 중심 축소. **단 wiki `index.html`/`src`는 아직 혼합 산출물(html 재작업 보류)** — 활성 맵과 html이 어긋난 상태가 현재는 정상.
- **GA 작업은 `stash@{0}`**에 그대로(유저 stash, 미착수). GA 스크립트 4개 + `ASC.GiveAbility` 스켈레톤 + `GA_New.asset`.
- **선택 백로그:** 코드 주석을 더 공격적으로 솎는 추가 패스(중도 → 공격적) 가능 — 유저가 원하면.
- **정리 대상(무관):** 오래된 로컬 브랜치 `Feature/Character-Movement-Prototype`·`Feature/GE-Execution`(원격 gone).
- **Unity MCP:** 현재 OFF.
