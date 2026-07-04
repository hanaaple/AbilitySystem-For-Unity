# 컨트롤러-캐릭터 분리 (Possession 패턴)

```
ControllerBase (abstract)              CharacterBase (abstract)
├── _possessedCharacter (protected)    ├── PlayerCharacter
├── Possess(CharacterBase)             └── MonsterCharacter
├── HasAuthority() — 항상 true (멀티 TODO)
└── PlayerController
    └── OnEnable/OnDisable 입력 구독·해제
```

- `ControllerBase` — `_possessedCharacter`, `Possess()`, `UnPossess()`, `HasAuthority()`
- `PlayerController` — `OnEnable`/`OnDisable`에서 `PlayerInputActions` 구독·해제, 입력을 `PlayerCharacter` 메서드 호출로 전달
- 캐릭터가 이동 로직 소유 (`PlayerCharacter.MoveDelta(Vector3 normalizedDirection, float delta)`)