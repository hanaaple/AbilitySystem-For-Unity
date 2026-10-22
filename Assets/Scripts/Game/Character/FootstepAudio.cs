using UnityEngine;

namespace Game.Character
{
    /// <summary> 이동 클립의 OnFootstep 애니메이션 이벤트를 받아 발소리를 재생한다. Animator와 같은 오브젝트에 붙인다. </summary>
    public class FootstepAudio : MonoBehaviour
    {
        // 블렌드 트리에 섞인 클립마다 이벤트가 오므로, 주로 재생 중인 클립의 이벤트만 소리를 낸다
        private const float MinClipWeight = 0.5f;

        [SerializeField] private AudioClip[] footstepClips;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight < MinClipWeight || footstepClips.Length == 0)
            {
                return;
            }

            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
    }
}
