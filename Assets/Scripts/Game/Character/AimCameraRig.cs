using Cinemachine;
using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Character
{
    /// <summary>캐릭터가 조준 태그를 가진 동안 FreeLook 카메라를 좁은 화각·어깨 너머 시점으로 옮긴다.</summary>
    public class AimCameraRig : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("abilitySystemComponent")] private AbilitySystemComponent _abilitySystemComponent;
        [SerializeField] [FormerlySerializedAs("aimingTag")] private GameplayTag _aimingTag;
        [SerializeField] [FormerlySerializedAs("freeLook")] private CinemachineFreeLook _freeLook;

        [Tooltip("FreeLook이 따라가고 바라보는 지점. 조준 중엔 카메라 오른쪽으로 옮겨 캐릭터가 십자선을 가리지 않게 한다.")]
        [SerializeField] [FormerlySerializedAs("cameraTarget")] private Transform _cameraTarget;

        [SerializeField] [FormerlySerializedAs("aimFieldOfView")] private float _aimFieldOfView = 28f;
        [SerializeField] [FormerlySerializedAs("shoulderOffset")] private float _shoulderOffset = 0.6f;
        [SerializeField] [FormerlySerializedAs("blendSpeed")] private float _blendSpeed = 6f;

        private float _defaultFieldOfView;
        private Vector3 _defaultTargetLocalPosition;
        private Transform _viewCamera;
        private bool _isAiming;
        private float _weight;

        private void Awake()
        {
            _defaultFieldOfView = _freeLook.m_Lens.FieldOfView;
            _defaultTargetLocalPosition = _cameraTarget.localPosition;
        }

        private void Start()
        {
            Camera mainCamera = Camera.main;
            _viewCamera = mainCamera != null ? mainCamera.transform : null;
            _isAiming = _abilitySystemComponent.HasMatchingGameplayTag(_aimingTag);
            _abilitySystemComponent.RegisterGameplayTagEvent(_aimingTag, OnTagCountChanged);
        }

        // 카메라(Cinemachine Brain)는 LateUpdate에 움직이므로 그 전에 목표 지점과 화각을 정해 둔다
        private void Update()
        {
            _weight = Mathf.MoveTowards(_weight, _isAiming ? 1f : 0f, _blendSpeed * Time.deltaTime);
            float blend = Mathf.SmoothStep(0f, 1f, _weight);
            _freeLook.m_Lens.FieldOfView = Mathf.Lerp(_defaultFieldOfView, _aimFieldOfView, blend);

            Vector3 shoulder = Vector3.zero;
            if (_viewCamera != null)
            {
                Vector3 right = _viewCamera.right;
                right.y = 0f;
                shoulder = right.normalized * (_shoulderOffset * blend);
            }

            _cameraTarget.position = _cameraTarget.parent.TransformPoint(_defaultTargetLocalPosition) + shoulder;
        }

        private void OnDestroy()
        {
            if (_abilitySystemComponent != null)
            {
                _abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, _aimingTag);
            }
        }

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            _isAiming = newCount > 0;
        }
    }
}
