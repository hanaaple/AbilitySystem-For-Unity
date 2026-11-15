using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Character
{
    /// <summary>캐릭터가 태그를 가진 동안 파티클을 켠다. 태그가 빠지면 새로 뿜는 것만 멈추고 이미 나온 입자는 수명대로 사라진다.</summary>
    public class TagParticleEffect : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("abilitySystemComponent")] private AbilitySystemComponent _abilitySystemComponent;
        [SerializeField] [FormerlySerializedAs("tag")] private GameplayTag _tag;
        [SerializeField] [FormerlySerializedAs("particles")] private ParticleSystem _particles;

        private void Start()
        {
            SetPlaying(_abilitySystemComponent.HasMatchingGameplayTag(_tag));
            _abilitySystemComponent.RegisterGameplayTagEvent(_tag, OnTagCountChanged);
        }

        private void OnDestroy()
        {
            if (_abilitySystemComponent != null)
            {
                _abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, _tag);
            }
        }

        private void SetPlaying(bool isPlaying)
        {
            if (isPlaying)
            {
                _particles.Play(true);
            }
            else
            {
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            SetPlaying(newCount > 0);
        }
    }
}
