using Core.AbilitySystem;
using Core.AbilitySystem.Effect;
using Core.AbilitySystem.Tag;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>픽업이 다시 나타날 때까지 받침대 위에 차오르는 막대. 픽업 자신의 재생성 쿨다운 GE가 걸려 있는 동안만 보이고, 카메라를 본다.</summary>
    public class PickupRespawnBar : MonoBehaviour
    {
        [Tooltip("픽업 자신의 ASC(재생성 쿨다운 GE가 걸리는 곳).")]
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [Tooltip("재생성 쿨다운 GE가 주는 태그. 픽업의 respawnCooldownTag와 같아야 한다.")]
        [SerializeField] private GameplayTag respawnCooldownTag;
        [Tooltip("쿨다운 동안만 켜는 막대 전체.")]
        [SerializeField] private GameObject barRoot;
        [Tooltip("Filled 이미지. 지난 쿨다운 비율만큼 찬다.")]
        [SerializeField] private Image fill;

        private GameplayEffectQuery _query;
        private Transform _cameraTransform;

        private void Start()
        {
            var tags = new GameplayTagContainer();
            tags.AddTag(respawnCooldownTag);
            _query = GameplayEffectQuery.MakeQuery_MatchAnyOwningTags(tags);
            Camera mainCamera = Camera.main;
            _cameraTransform = mainCamera != null ? mainCamera.transform : null;
            barRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            float remaining = 0f;
            float duration = 0f;
            foreach ((float timeRemaining, float total) in abilitySystemComponent.GetActiveEffectsTimeRemainingAndDuration(_query))
            {
                if (timeRemaining > remaining)
                {
                    remaining = timeRemaining;
                    duration = total;
                }
            }

            bool isCoolingDown = remaining > 0f && duration > 0f;
            barRoot.SetActive(isCoolingDown);
            if (!isCoolingDown)
            {
                return;
            }

            fill.fillAmount = 1f - remaining / duration;
            if (_cameraTransform != null)
            {
                barRoot.transform.rotation = Quaternion.LookRotation(barRoot.transform.position - _cameraTransform.position);
            }
        }
    }
}
