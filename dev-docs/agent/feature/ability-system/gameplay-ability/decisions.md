# gameplay-ability — 결정 기록

> 무엇을·어떻게 적나 → [feature/HARNESS.md](../../HARNESS.md) '결정 기록 기준'. `D#`는 feature 내 고유·불변 코드다.

---

## D1 — 정의(단일 SO)와 런타임(일반 C# 클래스) 분리

- 날짜: 2026-10-20
- 결정: UE `UGameplayAbility` 하나를 둘로 나눈다. `GameplayAbilityAsset`(sealed SO, 메뉴 1개, 불변 정의) + `GameplayAbility`(abstract 일반 C# 클래스, 부여될 때마다 새로 만들어 액터별 상태·동작을 가짐). 하위 클래스는 런타임 쪽만 만든다.
- 왜: (유저) "GA는 이벤트 함수, field를 직접 사용하는 게 중요" → SO 상속 + `Instantiate` 복제를 검토했으나, SO를 객체마다 복제하는 것에 대한 웹 조사(Unity 매뉴얼: SO 주 용도 = 공유 데이터 / 커뮤니티: 상태는 런타임 객체에) 뒤 "아무리 그래도 그렇지? Runtime Class 별도로 두는거로 가고, SO Asset은 단일로 가자".
- 대안: ① SO 상속 + 부여 시 `Instantiate`(클래스 1개·`this`로 정의값까지 / SO 본래 용도에서 벗어남·원본 실행 사고·`Destroy` 관리) ② SO 상속(메뉴·하위 클래스마다 에셋 작업).
- 대가: UE와 달리 "에셋 = 클래스"가 아니다. 런타임 클래스는 정의값을 에셋을 거쳐 읽는다.

## D2 — 에셋은 런타임 클래스 타입만 저장(클래스별 정의값 없음) — ⛔ D3로 번복

- 날짜: 2026-10-20
- 결정: `GameplayAbilityAsset`은 `[SubclassSelector(typeof(GameplayAbility))] string abilityClass`(AQN)만 들고, 부여 시 `Activator`로 생성한다(GE Execution과 같은 방식). 하위 클래스 고유의 직렬화 정의값(예: 반경·투사체)은 두지 않는다.
- 왜: 유저 선택(세 안 중 — 이유 미기록).
- 대안: ① 에셋이 `[SerializeReference] GameplayAbility` 템플릿을 들고 부여 시 복제(클래스별 값 가능 / 복제 구현 필요) ② 에셋이 타입 + `[SerializeReference]` 정의 데이터 객체(어빌리티당 클래스 2개).
- 대가: 어빌리티별 값은 에셋 공통 필드나 GE로만 표현한다.

## D3 — 에셋은 런타임 클래스 인스턴스를 `[SerializeReference]` 원형으로 저장, 부여 때 깊은 복사 (D2 번복)

- 날짜: 2026-10-20
- 결정: `GameplayAbilityAsset`이 `[SerializeReference] [SubclassSelector(typeof(GameplayAbility))] GameplayAbility abilityTemplate`을 든다. 하위 클래스의 직렬화 필드가 에셋 인스펙터에 그려지고 에셋마다 저장된다. 부여(`CreateNewInstanceOfAbility`) 때 `SerializedDeepCopy.Clone`으로 원형을 복제해 액터별 인스턴스를 만든다 — 직렬화 필드만 복사(값·List·배열·[Serializable] 중첩은 새로, UnityEngine.Object 참조는 유지, SerializeReference 공유 참조 유지), 나머지 필드는 새 인스턴스 초기값.
- 왜: (유저) "ability Class SubClassSelector 말고 SerializeReference로 하자 하위 field draw하고 저장할 필요가 있겠어". 복제 방식은 유저가 세 안(깊은 복사 / JsonUtility 왕복 / MemberwiseClone) 중 깊은 복사를 고름(이유 미기록 — 안 설명에 "UE `NewObject(원형)`과 같은 의미"를 적었음).
- 대안: D2(타입만, Activator) / JsonUtility 왕복(짧음 / UnityEngine.Object·중첩 SerializeReference 복원을 빌드에서 확인 필요) / MemberwiseClone(한 줄 / List·중첩 객체를 원형과 공유해 에셋 오염 위험).
- 대가: 하위 클래스마다 `[Serializable]` 필요(없으면 저장 안 됨). 클래스 이름을 바꾸면 SerializeReference 참조가 깨진다(`[MovedFrom]` 필요). 엔진 클래스(AnimationCurve·Gradient 등)는 복제하지 않고 원형과 공유한다. 원형 인스턴스를 직접 실행하지 않는다는 규칙.
