using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game.Character
{
    /// <summary>캐릭터가 태그를 가진 동안 세기를 0→1로 서서히 올리는 화면 연출의 공통 부분. 켜져 있는 동안 세기가 맥박처럼 오르내릴 수 있다. 세기를 어떻게 보여 줄지는 하위 클래스가 정한다.</summary>
    public abstract class TagFadeEffect : MonoBehaviour
    {
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private GameplayTag tag;
        [SerializeField] private float fadeSpeed = 4f;

        [Tooltip("켜져 있는 동안 세기가 이 폭만큼 오르내린다.")]
        [Range(0f, 0.5f)] [SerializeField] private float pulseAmount = 0.2f;
        [SerializeField] private float pulseSpeed = 3f;

        private bool _isActive;
        private float _weight;

        protected virtual void Start()
        {
            ApplyWeight(0f);
            _isActive = abilitySystemComponent.HasMatchingGameplayTag(tag);
            abilitySystemComponent.RegisterGameplayTagEvent(tag, OnTagCountChanged);
        }

        private void Update()
        {
            float target = _isActive ? 1f - pulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed)) : 0f;
            _weight = Mathf.MoveTowards(_weight, target, fadeSpeed * Time.deltaTime);
            ApplyWeight(_weight);
        }

        protected virtual void OnDestroy()
        {
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, tag);
            }
        }

        protected abstract void ApplyWeight(float weight);

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            _isActive = newCount > 0;
        }
    }
}
