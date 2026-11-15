using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

namespace Game.Character
{
    /// <summary>캐릭터가 태그를 얻고 잃을 때 VFX Graph 이벤트를 보낸다(예: 낙인이 붙으면 표식, 빠지면 폭발). 태그가 있는 동안의 모양은 VFX가 정한다.</summary>
    public class TagVfxEffect : MonoBehaviour
    {
        [SerializeField] [FormerlySerializedAs("abilitySystemComponent")] private AbilitySystemComponent _abilitySystemComponent;
        [SerializeField] [FormerlySerializedAs("gameplayTag")] private GameplayTag _gameplayTag;
        [SerializeField] [FormerlySerializedAs("visualEffect")] private VisualEffect _visualEffect;

        [Tooltip("태그가 붙을 때 차례로 보내는 이벤트.")]
        [SerializeField] [FormerlySerializedAs("addedEvents")] private List<string> _addedEvents = new() { "create" };

        [Tooltip("태그가 빠질 때 차례로 보내는 이벤트.")]
        [SerializeField] [FormerlySerializedAs("removedEvents")] private List<string> _removedEvents = new() { "stop" };

        private bool _hasTag;

        private void Start()
        {
            _hasTag = _abilitySystemComponent.HasMatchingGameplayTag(_gameplayTag);
            if (_hasTag)
            {
                SendEvents(_addedEvents);
            }

            _abilitySystemComponent.RegisterGameplayTagEvent(_gameplayTag, OnTagCountChanged);
        }

        private void OnDestroy()
        {
            if (_abilitySystemComponent != null)
            {
                _abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, _gameplayTag);
            }
        }

        // 개수만 바뀔 때(같은 태그를 주는 GE가 하나 더 걸릴 때 등)는 보내지 않는다
        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            bool hasTag = newCount > 0;
            if (hasTag == _hasTag)
            {
                return;
            }

            _hasTag = hasTag;
            SendEvents(hasTag ? _addedEvents : _removedEvents);
        }

        private void SendEvents(List<string> eventNames)
        {
            foreach (string eventName in eventNames)
            {
                _visualEffect.SendEvent(eventName);
            }
        }
    }
}
