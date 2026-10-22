using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// 상체는 그대로 두고 하체(골반)만 수평으로 돌린다. 정방향 이동 클립을 대각선 이동에 맞출 때 쓴다.
    /// 애니메이션이 뼈를 쓴 뒤(LateUpdate)에 돌리므로 매 프레임 새로 적용되고 누적되지 않는다.
    /// </summary>
    /// <remarks>UE: FAnimNode_OrientationWarping</remarks>
    [RequireComponent(typeof(Animator))]
    public class OrientationWarping : MonoBehaviour
    {
        [Tooltip("목표 각도로 따라가는 빠르기(초당). 클수록 바로 돈다.")]
        [SerializeField] private float rotationInterpSpeed = 10f;

        private Transform _hips;
        private Transform[] _spineBones;
        private float _targetAngle;
        private float _angle;

        private void Awake()
        {
            var animator = GetComponent<Animator>();
            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            // UpperChest는 아바타에 따라 비어 있다
            Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            _spineBones = upperChest != null ? new[] { spine, chest, upperChest } : new[] { spine, chest };
        }

        private void LateUpdate()
        {
            _angle = Mathf.LerpAngle(_angle, _targetAngle, 1f - Mathf.Exp(-rotationInterpSpeed * Time.deltaTime));
            if (Mathf.Abs(_angle) < 0.01f)
            {
                return;
            }

            // 골반을 돌리면 그 위 척추도 같이 돌므로, 척추 마디에 나눠 되돌려 상체 방향을 지킨다
            _hips.rotation = Quaternion.AngleAxis(_angle, Vector3.up) * _hips.rotation;
            Quaternion counter = Quaternion.AngleAxis(-_angle / _spineBones.Length, Vector3.up);
            foreach (Transform bone in _spineBones)
            {
                bone.rotation = counter * bone.rotation;
            }
        }

        /// <summary>골반을 돌릴 목표 각도(도, 오른쪽이 +). 0이면 돌리지 않는다.</summary>
        public void SetTargetAngle(float angle)
        {
            _targetAngle = angle;
        }
    }
}
