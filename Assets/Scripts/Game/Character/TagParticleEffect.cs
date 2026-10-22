using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game.Character
{
    /// <summary>캐릭터가 태그를 가진 동안 파티클을 켠다. 태그가 빠지면 새로 뿜는 것만 멈추고 이미 나온 입자는 수명대로 사라진다.</summary>
    public class TagParticleEffect : MonoBehaviour
    {
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private GameplayTag tag;
        [SerializeField] private ParticleSystem particles;

        private void Start()
        {
            SetPlaying(abilitySystemComponent.HasMatchingGameplayTag(tag));
            abilitySystemComponent.RegisterGameplayTagEvent(tag, OnTagCountChanged);
        }

        private void OnDestroy()
        {
            if (abilitySystemComponent != null)
            {
                abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, tag);
            }
        }

        private void SetPlaying(bool isPlaying)
        {
            if (isPlaying)
            {
                particles.Play(true);
            }
            else
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            SetPlaying(newCount > 0);
        }
    }
}
