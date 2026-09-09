# 카메라

## 씬 설정

- Perspective, FOV 40
- Follow = PlayerCharacter 자식 `CameraTarget` Transform, LookAt = null
- Body: Transposer, Offset (0, 10, -10), BindingMode = WorldSpace → 캐릭터 회전과 무관하게 카메라 방향 고정
- Aim: Do Nothing → 카메라 자체 회전 잠금