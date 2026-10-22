using Cinemachine;
using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game.Character
{
    /// <summary>캐릭터가 조준 태그를 가진 동안 FreeLook 카메라를 좁은 화각·어깨 너머 시점으로 옮긴다.</summary>
    public class AimCameraRig : MonoBehaviour
    {
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private GameplayTag aimingTag;
        [SerializeField] private CinemachineFreeLook freeLook;

        [Tooltip("FreeLook이 따라가고 바라보는 지점. 조준 중엔 카메라 오른쪽으로 옮겨 캐릭터가 십자선을 가리지 않게 한다.")]
        [SerializeField] private Transform cameraTarget;

        [SerializeField] private float aimFieldOfView = 28f;
        [SerializeField] private float shoulderOffset = 0.6f;
        [SerializeField] private float blendSpeed = 6f;

        private float _defaultFieldOfView;
        private Vector3 _defaultTargetLocalPosition;
        private Transform _viewCamera;
        private bool _isAiming;
        private float _weight;

        private void Awake()
        {
            _defaultFieldOfView = freeLook.m_Lens.FieldOfView;
            _defaultTargetLocalPosition = cameraTarget.localPosition;
        }

        private void Start()
        {
            Camera mainCamera = Camera.main;
            _viewCamera = mainCamera != null ? mainCamera.transform : null;
            _isAiming = abilitySystemComponent.HasMatchingGameplayTag(aimingTag);
            abilitySystemComponent.RegisterGameplayTagEvent(aimingTag, OnTagCountChanged);
        }

        // 카메라(Cinemachine Brain)는 LateUpdate에 움직이므로 그 전에 목표 지점과 화각을 정해 둔다
        private void Update()
        {
            _weight = Mathf.MoveTowards(_weight, _isAiming ? 1f : 0f, blendSpeed * Time.deltaTime);
            float blend = Mathf.SmoothStep(0f, 1f, _weight);
            freeLook.m_Lens.FieldOfView = Mathf.Lerp(_defaultFieldOfView, aimFieldOfView, blend);

            Vector3 shoulder = Vector3.zero;
            if (_viewCamera != null)
            {
                Vector3 right = _viewCamera.right;
                right.y = 0f;
                shoulder = right.normalized * (shoulderOffset * blend);
            }

            cameraTarget.position = cameraTarget.parent.TransformPoint(_defaultTargetLocalPosition) + shoulder;
        }

        private void OnDestroy()
        {
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, aimingTag);
            }
        }

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            _isAiming = newCount > 0;
        }
    }
}
