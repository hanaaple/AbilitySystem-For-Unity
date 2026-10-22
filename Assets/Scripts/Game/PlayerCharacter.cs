using System;
using System.Collections.Generic;
using Cinemachine;
using Game.Abilities;
using Game.Character;
using Core.AbilitySystem;
using Core.AbilitySystem.Abilities;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AbilitySystemComponent))]
    public class PlayerCharacter : CharacterBase, IProjectileCaster
    {
        private const float SpeedOffset = 0.1f;
        private const float MoveDirectionSmoothTime = 0.12f;
        private const float GroundedVerticalVelocity = -2f;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveDirectionXHash = Animator.StringToHash("MoveDirection X");
        private static readonly int MoveDirectionZHash = Animator.StringToHash("MoveDirection Z");
        private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int RollMotionSpeedHash = Animator.StringToHash("RollMotionSpeed");

        [SerializeField] private Transform model;
        [SerializeField] private Animator animator;
        [SerializeField] private OrientationWarping orientationWarping;
        [Tooltip("시작할 때 자신에게 적용하는 초기 스탯 GE(Instant, Override).")]
        [SerializeField] private GameplayEffectAsset defaultAttributes;
        [SerializeField] private GameplayEffectAsset possessEffect;
        [Tooltip("초기 스탯 다음에 자신에게 거는 지속 GE들(마나 재생 등). 계속 걸려 있어야 하므로 Infinite로 만든다.")]
        [SerializeField] private List<GameplayEffectAsset> passiveEffects = new();

        [Header("Ability")]
        [Tooltip("모델 안에서의 시전 지점(높이·앞 거리). 쏠 때 몸이 아직 덜 돌았어도 겨누는 방향으로 돌린 이 위치에서 나간다.")]
        [SerializeField] private Transform castPoint;
        [FormerlySerializedAs("ability1")]
        [SerializeField] private GameplayAbilityAsset normalAttackAbility;
        [Tooltip("키 1 스킬.")]
        [SerializeField] private GameplayAbilityAsset skillAbility;
        [SerializeField] private GameplayAbilityAsset ability2;
        [SerializeField] private GameplayAbilityAsset jumpAbility;
        [SerializeField] private GameplayAbilityAsset sprintAbility;
        [SerializeField] private GameplayAbilityAsset aimAbility;

        [Tooltip("이 태그가 있는 동안 달리기 애니메이션을 쓴다(속도 자체는 달리기 어빌리티의 GE가 speed 어트리뷰트로 바꾼다). 달리기 어빌리티의 Activation Owned Tags와 같아야 한다.")]
        [SerializeField] private GameplayTag sprintingTag;

        [Tooltip("이 중 하나라도 있으면 카메라 방향을 보고 옆·뒤로 걷는다(조준, 쏜 직후 전투 자세). 조준 어빌리티의 Activation Owned Tags·전투 자세 GE의 부여 태그와 같아야 한다.")]
        [SerializeField] private GameplayTagContainer faceViewTags;

        [Header("Aim")]
        [Tooltip("화면 가운데로 쏜 조준 레이가 아무것도 맞히지 않을 때 겨누는 거리.")]
        [SerializeField] private float aimDistance = 100f;
        [SerializeField] private LayerMask aimLayers = ~0;

        // 이동 속도는 speed 어트리뷰트가 정한다. 아래 두 값은 애니메이션 블렌드 트리의 걷기·달리기 기준값이다(클립에 맞춘 값이라 속도와 따로 둔다).
        [Header("Move")]
        [FormerlySerializedAs("walkSpeed")]
        [SerializeField] private float walkAnimationSpeed = 2f;
        [FormerlySerializedAs("runSpeed")]
        [SerializeField] private float runAnimationSpeed = 5.335f;
        [Range(0f, 0.3f)] [SerializeField] private float rotationSmoothTime = 0.12f;
        [Tooltip("카메라 방향을 볼 때(조준·전투 자세)의 회전 시간. 쏘는 순간 뒤를 보고 있었으면 이 시간에 걸쳐 돌아선다.")]
        [Range(0f, 0.3f)] [SerializeField] private float viewRotationSmoothTime = 0.05f;
        [SerializeField] private float speedChangeRate = 10f;

        [Header("Jump · Gravity")]
        [SerializeField] private float jumpTimeout = 0.2f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float fallTimeout = 0.15f;
        [SerializeField] private LayerMask groundLayers = 1;

        [Header("Dash")]
        [Tooltip("구르기 애니메이션에서 구르는 동작이 1배속으로 걸리는 시간. 돌진 시간에 맞춰 재생 속도를 이 비율로 바꾼다. Roll 클립에서 일어서기까지의 길이와 같아야 한다.")]
        [SerializeField] private float rollAnimationDuration = 1.25f;

        // 둘 중 하나만 늘 붙어 있다. Animator는 GameplayTagAnimatorParameterMap이 이 태그로 갱신한다.
        [Header("Movement Mode Tags")]
        [Tooltip("땅에 있는 동안 붙는 태그.")]
        [SerializeField] private GameplayTag walkingModeTag;
        [Tooltip("공중에 있는 동안(점프·낙하) 붙는 태그.")]
        [SerializeField] private GameplayTag fallingModeTag;

        private AbilitySystemComponent _asc;
        private CharacterController _characterController;
        private ActiveGameplayEffectHandle _possessEffectHandle;

        private bool _isGrounded;
        private bool _isFreeFalling;
        private bool _hasMovementModeTag;
        private bool _isTaggedGrounded;
        private float _moveSpeed;
        private float _animationBlend;
        private float _targetRotation;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private Vector2 _moveDirection;
        private Vector2 _moveDirectionVelocity;
        private Vector3 _moveWorldDirection = Vector3.forward;
        private Vector3 _moveInputDirection;
        private float _viewYaw;
        private Camera _viewCamera;
        private CinemachineBrain _cameraBrain;
        private readonly List<Action<Vector3, Vector3>> _pendingLaunches = new();

        private float _dashRemainingTime;
        private Vector3 _dashVelocity;
        private Action _onDashFinished;
        private Action _onLanded;

        protected virtual void Awake()
        {
            _asc = GetComponent<AbilitySystemComponent>();
            _characterController = GetComponent<CharacterController>();
            _asc.AddSpawnedAttribute(new CharacterAttributeSet());
            _asc.AddSpawnedAttribute(new PlayerAttributeSet());

            _targetRotation = model.eulerAngles.y;
            _viewCamera = Camera.main;
            _cameraBrain = _viewCamera != null ? _viewCamera.GetComponent<CinemachineBrain>() : null;
            _jumpTimeoutDelta = jumpTimeout;
            _fallTimeoutDelta = fallTimeout;
        }

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }

        private void Start()
        {
            // GE 컨테이너가 ASC Awake에서 만들어지므로 Start에서 적용한다
            if (defaultAttributes != null)
            {
                _asc.ApplyGameplayEffectToSelf(defaultAttributes);
            }

            foreach (GameplayEffectAsset passiveEffect in passiveEffects)
            {
                if (passiveEffect != null)
                {
                    _asc.ApplyGameplayEffectToSelf(passiveEffect);
                }
            }

            GiveInputAbility(normalAttackAbility, AbilityInputID.NormalAttack);
            GiveInputAbility(skillAbility, AbilityInputID.Ability1);
            GiveInputAbility(ability2, AbilityInputID.Ability2);
            GiveInputAbility(jumpAbility, AbilityInputID.Jump);
            GiveInputAbility(sprintAbility, AbilityInputID.Sprint);
            GiveInputAbility(aimAbility, AbilityInputID.Aim);
        }

        private void Update()
        {
            ActivateHeldAbilities();
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
            _pendingLaunches.Clear();
        }

        public override void OnPossessed(ControllerBase controller)
        {
            base.OnPossessed(controller);

            if (possessEffect != null)
            {
                _possessEffectHandle = _asc.ApplyGameplayEffectToSelf(possessEffect);
            }
        }

        public override void OnUnPossessed()
        {
            base.OnUnPossessed();

            if (_possessEffectHandle.IsValid)
            {
                _asc.RemoveActiveGameplayEffect(_possessEffectHandle);
                _possessEffectHandle = ActiveGameplayEffectHandle.Invalid;
            }
        }

        public override void MoveDelta(Vector3 normailizedDirection, float delta)
        {
            Locomote(normailizedDirection, delta);
        }

        public override void StopMoving()
        {
            Locomote(Vector3.zero, Time.deltaTime);
        }

        /// <summary>입력이 눌렸음을 ASC에 알린다. 그 입력에 묶인 어빌리티가 비활성이면 발동을 시도한다.</summary>
        public void AbilityInputPressed(AbilityInputID inputID)
        {
            _asc.AbilityLocalInputPressed((int)inputID);
        }

        /// <summary>입력이 떼졌음을 ASC에 알린다.</summary>
        public void AbilityInputReleased(AbilityInputID inputID)
        {
            _asc.AbilityLocalInputReleased((int)inputID);
        }

        /// <summary>이동 입력 방향(입력이 없으면 모델이 바라보는 방향)으로 구르며 duration 동안 distance만큼 이동한다. 그동안 이동 입력은 무시하고, 끝나면 onFinished를 부른다.</summary>
        /// <summary>지금 가려는 수평 방향. 이동 입력이 있으면 그 방향, 없으면 몸이 보는 정면.</summary>
        public Vector3 GetMoveIntentDirection()
        {
            // _moveWorldDirection은 입력을 떼도 마지막 방향이 남으므로, 지금 입력 중인지는 _moveInputDirection으로 본다
            Vector3 direction = _moveInputDirection.sqrMagnitude > 0.01f ? _moveInputDirection : model.forward;
            direction.y = 0f;
            return direction.normalized;
        }

        /// <summary>duration 동안 velocity로 밀어낸다(돌진 거리·방향은 어빌리티가 정한다). 끝나면 onFinished를 부른다.</summary>
        public void Push(Vector3 velocity, float duration, Action onFinished)
        {
            _dashVelocity = velocity;
            _dashRemainingTime = duration;
            _onDashFinished = onFinished;

            // 구르기 애니메이션은 앞으로 구르므로 몸을 미는 방향으로 바로 돌린다
            if (velocity.sqrMagnitude > 0.0001f)
            {
                _targetRotation = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
                _rotationVelocity = 0f;
                model.rotation = Quaternion.Euler(0f, _targetRotation, 0f);
            }

            // 구르기 애니메이션은 돌진 어빌리티 태그로 켜고 끈다(GameplayTagAnimatorParameterMap). 여기선 그 길이에 맞게 재생 속도만 맞춘다
            if (animator != null)
            {
                animator.SetFloat(RollMotionSpeedHash, rollAnimationDuration / duration);
            }
        }

        /// <summary>카메라가 보는 수평 방향(도). 조준 중엔 캐릭터가 이 방향을 본다.</summary>
        public void SetViewYaw(float yaw)
        {
            _viewYaw = yaw;
        }

        /// <summary>
        /// 카메라가 이번 프레임 위치로 옮겨진 뒤 화면 가운데(십자선)가 가리키는 지점을 찾아 launch에 넘긴다.
        /// 입력은 카메라가 움직이기 전(프레임 앞)에 오므로, 바로 겨누면 카메라를 돌리는 중엔 한 프레임 전 십자선 쪽으로 나간다.
        /// </summary>
        public void RequestLaunch(Action<Vector3, Vector3> launch)
        {
            if (_cameraBrain == null)
            {
                Launch(launch);
                return;
            }

            _pendingLaunches.Add(launch);
        }

        public bool CanJump()
        {
            return _isGrounded && _jumpTimeoutDelta <= 0f;
        }

        /// <summary>캐릭터에 걸리는 중력 가속도(음수).</summary>
        public float Gravity => gravity;

        /// <summary>위로 띄운다(점프 높이 계산은 점프 어빌리티가 한다). 착지하면 onLanded를 부른다. 땅에 있는지는 <see cref="CanJump"/>로 먼저 확인한다.</summary>
        public void Launch(float verticalVelocity, Action onLanded)
        {
            _onLanded = onLanded;
            _isGrounded = false;
            _verticalVelocity = verticalVelocity;

            if (animator != null)
            {
                animator.SetTrigger(JumpHash);
            }
        }

        private void Locomote(Vector3 direction, float delta)
        {
            _moveInputDirection = direction;
            CheckGrounded();
            CheckLanded();
            ApplyGravity(delta);

            if (_dashRemainingTime > 0f)
            {
                TickDash(delta);
                UpdateAnimator();
                return;
            }

            bool hasInput = direction.sqrMagnitude > 0.01f;
            float targetSpeed = hasInput ? _asc.GetAttributeCurrentValue(CharacterAttributeSet.Speed) : 0f;
            bool isSprinting = _asc.HasMatchingGameplayTag(sprintingTag);
            float targetBlend = hasInput ? (isSprinting ? runAnimationSpeed : walkAnimationSpeed) : 0f;

            UpdateAnimationBlend(targetBlend, delta);
            UpdateMoveSpeed(targetSpeed, delta);

            if (_asc.HasAnyMatchingGameplayTags(faceViewTags))
            {
                LocomoteFacingView(direction, hasInput);
            }
            else if (hasInput)
            {
                LocomoteFacingMove(direction);
            }
            else
            {
                UpdateMoveDirection(Vector2.zero);
                SetWarpAngle(0f);
            }

            Translate(delta);
            UpdateAnimator();
        }

        // 몸이 이동 방향으로 돌아 앞으로 걷거나 달린다. 카메라는 따로 돌려도 된다.
        private void LocomoteFacingMove(Vector3 direction)
        {
            RotateTo(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, rotationSmoothTime);
            _moveWorldDirection = direction.normalized;
            UpdateMoveDirection(Vector2.up);
            SetWarpAngle(0f);
        }

        // 몸은 카메라 방향에 고정하고, 입력 방향으로 옆·뒤 걸음을 섞어 움직인다
        private void LocomoteFacingView(Vector3 direction, bool hasInput)
        {
            RotateTo(_viewYaw, viewRotationSmoothTime);
            if (hasInput)
            {
                _moveWorldDirection = direction.normalized;
                Vector3 local = Quaternion.Euler(0f, -_viewYaw, 0f) * _moveWorldDirection;
                float moveAngle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                float cardinalAngle = ToCardinalAngle(moveAngle);
                UpdateMoveDirection(new Vector2(Mathf.Sin(cardinalAngle * Mathf.Deg2Rad), Mathf.Cos(cardinalAngle * Mathf.Deg2Rad)));
                SetWarpAngle(Mathf.DeltaAngle(cardinalAngle, moveAngle));
            }
            else
            {
                UpdateMoveDirection(Vector2.zero);
                SetWarpAngle(0f);
            }
        }

        // 몸은 바로 돌리지 않고(홱 돌지 않게) 전투 자세로 빠르게 돌아서게 둔다.
        // 그래서 나갈 위치는 지금 몸 방향이 아니라 겨누는 방향으로 돌린 시전 지점이다 — 뒤를 보고 있어도 몸 앞에서 나간다.
        private void Launch(Action<Vector3, Vector3> launch)
        {
            Vector3 aimPoint = FindAimPoint();
            Vector3 toAim = aimPoint - model.position;
            toAim.y = 0f;
            float aimYaw = toAim.sqrMagnitude > 0.0001f ? Mathf.Atan2(toAim.x, toAim.z) * Mathf.Rad2Deg : model.eulerAngles.y;
            Vector3 origin = model.position + Quaternion.Euler(0f, aimYaw, 0f) * castPoint.localPosition;
            launch(origin, aimPoint);
        }

        /// <summary>십자선이 가리키는 캐릭터 부위(머리 등). 캐릭터가 아니거나 부위가 없는 곳을 가리키면 null.</summary>
        public HitZone FindAimedHitZone()
        {
            if (_viewCamera == null)
            {
                return null;
            }

            // 부위는 몸 콜라이더 안쪽 트리거라 트리거까지 맞힌다
            RaycastHit[] hits = CastAimRay(QueryTriggerInteraction.Collide, out Ray ray, out float minDistance);
            if (!TryFindAimBlocker(hits, minDistance, out RaycastHit blocker))
            {
                return null;
            }

            var target = blocker.collider.GetComponentInParent<AbilitySystemComponent>();
            if (target == null)
            {
                return null;
            }

            HitZone closestZone = null;
            float closestDistance = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                var zone = hit.collider.GetComponent<HitZone>();
                if (zone == null || hit.distance >= closestDistance || hit.collider.GetComponentInParent<AbilitySystemComponent>() != target)
                {
                    continue;
                }

                closestZone = zone;
                closestDistance = hit.distance;
            }

            return closestZone;
        }

        // 화면 가운데로 레이를 쏴 처음 맞는 곳
        private Vector3 FindAimPoint()
        {
            if (_viewCamera == null)
            {
                return castPoint.position + model.forward * aimDistance;
            }

            RaycastHit[] hits = CastAimRay(QueryTriggerInteraction.Ignore, out Ray ray, out float minDistance);
            return TryFindAimBlocker(hits, minDistance, out RaycastHit blocker) ? blocker.point : ray.GetPoint(aimDistance);
        }

        // minDistance — 카메라~시전 지점 거리. 이보다 가까운 것(카메라와 캐릭터 사이)은 겨눌 대상이 아니다.
        private RaycastHit[] CastAimRay(QueryTriggerInteraction triggerInteraction, out Ray ray, out float minDistance)
        {
            ray = _viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            minDistance = Vector3.Dot(castPoint.position - ray.origin, ray.direction);
            return Physics.RaycastAll(ray, aimDistance, aimLayers, triggerInteraction);
        }

        // 트리거·자기 몸·카메라~캐릭터 사이에 걸린 것을 건너뛴 가장 가까운 충돌
        private bool TryFindAimBlocker(RaycastHit[] hits, float minDistance, out RaycastHit blocker)
        {
            blocker = default;
            bool found = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.isTrigger || hit.distance < minDistance || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!found || hit.distance < blocker.distance)
                {
                    blocker = hit;
                    found = true;
                }
            }

            return found;
        }

        private void CheckGrounded()
        {
            Vector3 center = _characterController.center;
            Vector3 spherePosition = transform.position + new Vector3(center.x, center.y - _characterController.height / 2f - _characterController.radius, center.z);
            _isGrounded = Physics.CheckSphere(spherePosition, _characterController.radius, groundLayers, QueryTriggerInteraction.Ignore);
            UpdateMovementModeTag();
        }

        // 이동 모드가 바뀔 때만 이전 태그를 떼고 새 태그를 붙인다
        private void UpdateMovementModeTag()
        {
            if (_hasMovementModeTag && _isTaggedGrounded == _isGrounded)
            {
                return;
            }

            if (_hasMovementModeTag)
            {
                SetMovementModeTag(_isTaggedGrounded, false);
            }

            SetMovementModeTag(_isGrounded, true);
            _isTaggedGrounded = _isGrounded;
            _hasMovementModeTag = true;
        }

        /// <remarks>UE(Lyra): ALyraCharacter::SetMovementModeTag</remarks>
        private void SetMovementModeTag(bool isGrounded, bool isTagEnabled)
        {
            GameplayTag movementModeTag = isGrounded ? walkingModeTag : fallingModeTag;
            if (movementModeTag.IsValid())
            {
                _asc.SetLooseGameplayTagCount(movementModeTag, isTagEnabled ? 1 : 0);
            }
        }

        // 뛰어오른 직후엔 아직 땅이 감지될 수 있어, 떨어지는 중(속도 ≤ 0)에 땅에 닿아야 착지로 본다
        private void CheckLanded()
        {
            if (_onLanded == null || !_isGrounded || _verticalVelocity > 0f)
            {
                return;
            }

            // 콜백 안에서 다시 점프할 수 있으므로 비운 뒤에 부른다
            Action onLanded = _onLanded;
            _onLanded = null;
            onLanded();
        }

        // 누르는 동안 유지하는 어빌리티는 입력을 누른 순간 다른 어빌리티·태그에 막혔으면 계속 눌러도 켜지지 않고,
        // 연사는 쿨다운이 끝날 때마다 다시 발동해야 한다. 그래서 입력이 눌려 있는 동안 매 프레임 다시 시도한다
        // — 막혀 있거나 쿨다운 중이면 그동안 매 프레임 실패가 알려진다.
        private void ActivateHeldAbilities()
        {
            foreach (GameplayAbilitySpec spec in _asc.GetActivatableAbilities())
            {
                if (spec.InputPressed && !spec.IsActive && spec.Ability.AbilityTemplate is CharacterGameplayAbility { ActivationPolicy: AbilityActivationPolicy.WhileInputActive })
                {
                    _asc.TryActivateAbility(spec.Handle);
                }
            }
        }

        private void GiveInputAbility(GameplayAbilityAsset ability, AbilityInputID inputID)
        {
            if (ability != null)
            {
                _asc.GiveAbility(new GameplayAbilitySpec(ability, inputID: (int)inputID));
            }
        }

        private void ApplyGravity(float delta)
        {
            // 뛰어오른 직후 몇 프레임은 아직 땅이 감지된다 — 올라가는 중이면 공중으로 보고 중력을 건다(안 그러면 점프 높이 어트리뷰트보다 높이 뜬다)
            if (_isGrounded && _verticalVelocity <= 0f)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = GroundedVerticalVelocity;
                }

                _fallTimeoutDelta = fallTimeout;
                _isFreeFalling = false;

                if (_jumpTimeoutDelta >= 0f)
                {
                    _jumpTimeoutDelta -= delta;
                }
            }
            else
            {
                _verticalVelocity += gravity * delta;
                _jumpTimeoutDelta = jumpTimeout;

                // 계단·턱처럼 잠깐 뜨는 경우엔 낙하 모션으로 넘어가지 않게 유예를 둔다
                if (_fallTimeoutDelta >= 0f)
                {
                    _fallTimeoutDelta -= delta;
                }
                else
                {
                    _isFreeFalling = true;
                }
            }
        }

        private void UpdateAnimationBlend(float targetBlend, float delta)
        {
            _animationBlend = Mathf.Lerp(_animationBlend, targetBlend, delta * speedChangeRate);

            if (_animationBlend < 0.01f)
            {
                _animationBlend = 0f;
            }
        }

        private void UpdateMoveSpeed(float targetSpeed, float delta)
        {
            Vector3 velocity = _characterController.velocity;
            float currentHorizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;

            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > SpeedOffset)
            {
                _moveSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, delta * speedChangeRate);
                _moveSpeed = Mathf.Round(_moveSpeed * 1000f) / 1000f;
            }
            else
            {
                _moveSpeed = targetSpeed;
            }
        }

        // 블렌드 트리 입력: 몸 기준 이동 방향(x = 오른쪽, y = 앞)
        private void UpdateMoveDirection(Vector2 target)
        {
            _moveDirection.x = Mathf.SmoothDamp(_moveDirection.x, target.x, ref _moveDirectionVelocity.x, MoveDirectionSmoothTime);
            _moveDirection.y = Mathf.SmoothDamp(_moveDirection.y, target.y, ref _moveDirectionVelocity.y, MoveDirectionSmoothTime);

            if (Mathf.Abs(_moveDirection.x) < 0.01f)
            {
                _moveDirection.x = 0f;
            }

            if (Mathf.Abs(_moveDirection.y) < 0.01f)
            {
                _moveDirection.y = 0f;
            }
        }

        // 이동 클립은 앞·뒤·옆 네 방향뿐이고, 둘을 섞으면 다리 동작이 서로 상쇄돼 발이 미끄러진다.
        // 그래서 가장 가까운 한 방향 클립만 틀고, 남는 각도는 골반을 돌려(OrientationWarping) 맞춘다.
        private static float ToCardinalAngle(float moveAngle)
        {
            float absAngle = Mathf.Abs(moveAngle);
            if (absAngle < 67.5f)
            {
                return 0f;
            }

            if (absAngle > 112.5f)
            {
                return 180f;
            }

            return Mathf.Sign(moveAngle) * 90f;
        }

        private void SetWarpAngle(float angle)
        {
            if (orientationWarping != null)
            {
                orientationWarping.SetTargetAngle(angle);
            }
        }

        private void RotateTo(float yaw, float smoothTime)
        {
            _targetRotation = yaw;
            float rotation = Mathf.SmoothDampAngle(model.eulerAngles.y, _targetRotation, ref _rotationVelocity, smoothTime);
            model.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        // 회전이 다 돌기 전에도 입력 방향으로 바로 움직인다. 입력이 끊겨 감속하는 동안은 마지막 방향을 유지한다.
        private void Translate(float delta)
        {
            _characterController.Move(_moveWorldDirection * (_moveSpeed * delta) + new Vector3(0f, _verticalVelocity, 0f) * delta);
        }

        private void TickDash(float delta)
        {
            _characterController.Move(_dashVelocity * delta + new Vector3(0f, _verticalVelocity, 0f) * delta);
            _dashRemainingTime -= delta;
            if (_dashRemainingTime > 0f)
            {
                return;
            }

            // 감속 보간은 컨트롤러 velocity에서 시작하므로, 돌진 속도가 남아 있으면 끝난 뒤에 미끄러진다
            _characterController.Move(Vector3.zero);

            // 콜백 안에서 다시 돌진을 시작할 수 있으므로 비운 뒤에 부른다
            Action onFinished = _onDashFinished;
            _onDashFinished = null;
            onFinished?.Invoke();
        }

        private void UpdateAnimator()
        {
            // 기존 Player 프리팹처럼 Animator를 연결하지 않은 캐릭터도 있다
            if (animator == null)
            {
                return;
            }

            animator.SetFloat(SpeedHash, _animationBlend);
            animator.SetFloat(MoveDirectionXHash, _moveDirection.x);
            animator.SetFloat(MoveDirectionZHash, _moveDirection.y);
            // 땅·공중은 이동 모드 태그로 넘긴다. FreeFall은 공중에 잠깐 뜬 것(계단·턱)을 거르는 시간 판정이라 모드가 아니어서 여기서 직접 넣는다
            animator.SetBool(FreeFallHash, _isFreeFalling);
        }

        private void OnCameraUpdated(CinemachineBrain brain)
        {
            if (brain != _cameraBrain || _pendingLaunches.Count == 0)
            {
                return;
            }

            // 콜백 안에서 다시 요청할 수 있으므로 목록을 비운 뒤에 부른다
            var launches = new List<Action<Vector3, Vector3>>(_pendingLaunches);
            _pendingLaunches.Clear();
            foreach (Action<Vector3, Vector3> launch in launches)
            {
                Launch(launch);
            }
        }
    }
}
