using System;
using System.Collections.Generic;
using Core.AbilitySystem;
using Core.AbilitySystem.Tag;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// ASC 태그 개수를 Animator 파라미터로 옮긴다. 파라미터 종류에 따라 Bool은 태그가 있는지(개수 &gt; 0), Int·Float는 개수를 넣는다.
    /// 태그 이벤트로만 갱신하므로, 어떤 상태를 애니메이션에 보여 줄지는 인스펙터 목록으로 정하고 캐릭터 코드는 태그만 붙인다.
    /// </summary>
    /// <remarks>UE(Lyra): ULyraAnimInstance::GameplayTagPropertyMap(FGameplayTagBlueprintPropertyMap)</remarks>
    public class GameplayTagAnimatorParameterMap : MonoBehaviour
    {
        [SerializeField] private AbilitySystemComponent abilitySystemComponent;
        [SerializeField] private Animator animator;
        [SerializeField] private List<TagParameter> mappings = new();

        // 같은 태그를 여러 파라미터에 걸 수 있어 태그별로 묶는다
        private readonly Dictionary<GameplayTag, List<BoundParameter>> _parametersByTag = new();

        // ASC가 Awake에서 태그 컨테이너를 만들므로 Start에서 구독한다
        private void Start()
        {
            Dictionary<string, AnimatorControllerParameterType> parameterTypes = new();
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                parameterTypes[parameter.name] = parameter.type;
            }

            foreach (TagParameter mapping in mappings)
            {
                if (!mapping.Tag.IsValid() || !parameterTypes.TryGetValue(mapping.ParameterName, out AnimatorControllerParameterType type))
                {
                    Debug.LogWarning($"GameplayTagAnimatorParameterMap: 태그가 비었거나 Animator에 '{mapping.ParameterName}' 파라미터가 없습니다.", this);
                    continue;
                }

                if (!_parametersByTag.TryGetValue(mapping.Tag, out List<BoundParameter> parameters))
                {
                    parameters = new List<BoundParameter>();
                    _parametersByTag.Add(mapping.Tag, parameters);
                    abilitySystemComponent.RegisterGameplayTagEvent(mapping.Tag, OnTagCountChanged, GameplayTagEventType.AnyCountChange);
                }

                parameters.Add(new BoundParameter(Animator.StringToHash(mapping.ParameterName), type));
            }

            // 구독 전에 이미 붙어 있던 태그도 반영한다
            foreach (GameplayTag tag in _parametersByTag.Keys)
            {
                OnTagCountChanged(tag, abilitySystemComponent.GetTagCount(tag));
            }
        }

        private void OnDestroy()
        {
            if (abilitySystemComponent == null)
            {
                return;
            }

            foreach (GameplayTag tag in _parametersByTag.Keys)
            {
                abilitySystemComponent.UnregisterGameplayTagEvent(OnTagCountChanged, tag, GameplayTagEventType.AnyCountChange);
            }
        }

        private void OnTagCountChanged(GameplayTag changedTag, int newCount)
        {
            if (!_parametersByTag.TryGetValue(changedTag, out List<BoundParameter> parameters))
            {
                return;
            }

            foreach (BoundParameter parameter in parameters)
            {
                switch (parameter.Type)
                {
                    case AnimatorControllerParameterType.Bool:
                    {
                        animator.SetBool(parameter.Hash, newCount > 0);
                        break;
                    }
                    case AnimatorControllerParameterType.Int:
                    {
                        animator.SetInteger(parameter.Hash, newCount);
                        break;
                    }
                    case AnimatorControllerParameterType.Float:
                    {
                        animator.SetFloat(parameter.Hash, newCount);
                        break;
                    }
                    default:
                    {
                        // Trigger는 개수로 표현할 수 없어 쓰지 않는다
                        break;
                    }
                }
            }
        }

        [Serializable]
        private struct TagParameter
        {
            [SerializeField] private GameplayTag tag;
            [SerializeField] private string parameterName;

            public GameplayTag Tag => tag;
            public string ParameterName => parameterName;
        }

        private readonly struct BoundParameter
        {
            public int Hash { get; }
            public AnimatorControllerParameterType Type { get; }

            public BoundParameter(int hash, AnimatorControllerParameterType type)
            {
                Hash = hash;
                Type = type;
            }
        }
    }
}
