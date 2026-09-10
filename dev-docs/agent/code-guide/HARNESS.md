# HARNESS.md — 코드 작성 규약

코드를 **어떻게 짤지**에 대한 설계·실천 규약. 이름·포맷·멤버 순서 등 **스타일**은 [`../../project/CODE_CONVENTION.md`](../../project/CODE_CONVENTION.md).

> 이 폴더의 하위 문서(에디터 가이드 등)는 **이 규약을 상위로 두고** 따른다. 하위로 바로 점프하지 말고 이 문서를 먼저 본다.

---

## 설계 원칙

- 필요할 때만 범용화한다. 실제 유스케이스가 요구하는 범위만 구현한다.
- 좁은 스코프·설계 갈림은 착수 전에 표면화·확인한다.
- 범용 코드에 특정 사용처 정책을 심지 않는다. 범용 쪽은 결과를 인자로 받고, "무엇을"은 호출측이 정한다.

> 관련 상위 규약(중복 두지 않고 링크): 과설계 지양·"왜"의 품질 → [`../HARNESS.md`](../HARNESS.md) '프로젝트 핵심 원칙'. 스코프 표면화·공통화 크기·구조적 개념 도입 순서 → [`../session-protocol.md`](../session-protocol.md) '작업 중'.

---

## 연쇄 호출

- 타입을 건너뛰는 연쇄 호출 금지. 중간 결과를 지역 변수로 받아 한 단계씩 접근한다.
- LINQ 외 fluent 메서드 체인은 쓰지 않는다. (LINQ 쿼리 체인의 줄바꿈은 CODE_CONVENTION '긴 줄' 참고.)

```csharp
// ❌ 타입을 건너뛰는 연쇄 호출
var ammo = player.GetGameObject().GetWeapon().GetAmmo();

// ✅ 한 단계씩 지역 변수로
var weapon = player.GetWeapon();
var ammo = weapon.GetAmmo();
```

---

## 주석

코드가 말하지 못하는 "왜"만 적는다. 없어도 되면 없앤다.

- 이름이 먼저. 주석은 이름이 담을 수 없는 "왜"에만.
- "왜"를 적고 "무엇"은 적지 않는다.
- 외부 포인터(`→D8`·`(UE: FAggregator)`) 금지. 결정 근거·설계 이력은 `dev-docs/`에 둔다.
- 변하기 쉬운 값(임계값·경계)을 주석에 복제하지 않는다.
- "여길 바꾸면 저기도"인 상호 의존은 주석으로 명시한다.
- 동작 명세는 테스트로 고정한다.
- 공개 API는 XML 주석(`/// <summary>`).

```csharp
// ✅ 이유(왜)를 적음
// Rigidbody 이동은 FixedUpdate에서 처리해야 물리 스텝과 어긋나지 않음
private void FixedUpdate() { }

// ❌ 코드를 되뇜 (무엇)
// FixedUpdate 함수
private void FixedUpdate() { }

/// <summary> 플레이어에게 데미지를 준다. </summary>
/// <param name="damage">입힐 데미지 양</param>
public void TakeDamage(int damage) { }
```

---

## 하위 문서

| 문서 | 내용 |
|---|---|
| [`editor-drawer-guide.md`](editor-drawer-guide.md) | 에디터 커스텀 인스펙터·PropertyDrawer 실전 규칙·안티패턴·재사용 유틸. 새 드로어 전에 먼저 본다. |
