using System;
using System.Collections.Generic;
using Core.AbilitySystem.Attribute;
using Core.AbilitySystem.Tag;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.AbilitySystem.Effect
{
    /// <summary>활성 GE를 고르는 조건 묶음. 채운 조건을 모두 맞춰야 고르고, 비운 조건은 보지 않는다.</summary>
    /// <remarks>UE: FGameplayEffectQuery</remarks>
    [Serializable]
    public class GameplayEffectQuery
    {
        [Tooltip("GE의 Asset Tags와 Granted Tags를 합친 것에 대어 본다.")]
        [SerializeField] private GameplayTagQuery _owningTagQuery;

        [Tooltip("GE의 Asset Tags에 대어 본다.")]
        [SerializeField] private GameplayTagQuery _effectTagQuery;

        [Tooltip("시전자가 이 GE에 붙인 Spec 태그에 대어 본다.")]
        [SerializeField] private GameplayTagQuery _sourceTagQuery;

        [Tooltip("시전자의 소유 태그와 Spec 태그를 합친 것에 대어 본다.")]
        [SerializeField] private GameplayTagQuery _sourceAggregateTagQuery;

        [Tooltip("이 어트리뷰트를 바꾸는 Modifier가 있는 GE만 고른다.")]
        [SerializeField] private GameplayAttribute _modifyingAttribute;

        [Tooltip("이 소스 오브젝트에서 나온 GE만 고른다.")]
        [SerializeField] private Object _effectSource;

        [Tooltip("이 GE 에셋으로 만든 GE만 고른다.")]
        [SerializeField] private GameplayEffectAsset _effectDefinition;

        /// <summary>다른 조건보다 먼저 보는 코드 전용 조건. false면 고르지 않는다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::CustomMatchDelegate</remarks>
        [NonSerialized] public Func<ActiveGameplayEffect, bool> CustomMatchDelegate;

        /// <summary>다른 조건을 맞춰도 고르지 않을 핸들.</summary>
        /// <remarks>UE: FGameplayEffectQuery::IgnoreHandles</remarks>
        [NonSerialized] public List<ActiveGameplayEffectHandle> IgnoreHandles = new();

        public GameplayTagQuery OwningTagQuery
        {
            get => _owningTagQuery;
            set
            {
                _owningTagQuery = value;
            }
        }

        public GameplayTagQuery EffectTagQuery
        {
            get => _effectTagQuery;
            set
            {
                _effectTagQuery = value;
            }
        }

        public GameplayTagQuery SourceTagQuery
        {
            get => _sourceTagQuery;
            set
            {
                _sourceTagQuery = value;
            }
        }

        public GameplayTagQuery SourceAggregateTagQuery
        {
            get => _sourceAggregateTagQuery;
            set
            {
                _sourceAggregateTagQuery = value;
            }
        }

        public GameplayAttribute ModifyingAttribute
        {
            get => _modifyingAttribute;
            set
            {
                _modifyingAttribute = value;
            }
        }

        public Object EffectSource
        {
            get => _effectSource;
            set
            {
                _effectSource = value;
            }
        }

        public GameplayEffectAsset EffectDefinition
        {
            get => _effectDefinition;
            set
            {
                _effectDefinition = value;
            }
        }

        public GameplayEffectQuery()
        {
        }

        /// <summary>에셋에 든 질의를 고치지 않고 IgnoreHandles 등을 바꿔 쓰려고 복사한다.</summary>
        public GameplayEffectQuery(GameplayEffectQuery other)
        {
            _owningTagQuery = other._owningTagQuery;
            _effectTagQuery = other._effectTagQuery;
            _sourceTagQuery = other._sourceTagQuery;
            _sourceAggregateTagQuery = other._sourceAggregateTagQuery;
            _modifyingAttribute = other._modifyingAttribute;
            _effectSource = other._effectSource;
            _effectDefinition = other._effectDefinition;
            CustomMatchDelegate = other.CustomMatchDelegate;
            IgnoreHandles = new List<ActiveGameplayEffectHandle>(other.IgnoreHandles);
        }

        /// <remarks>UE: FGameplayEffectQuery::Matches(const FActiveGameplayEffect&amp;)</remarks>
        public bool Matches(ActiveGameplayEffect effect)
        {
            if (IgnoreHandles.Contains(effect.Handle))
            {
                return false;
            }

            if (CustomMatchDelegate != null && !CustomMatchDelegate(effect))
            {
                return false;
            }

            return Matches(effect.Spec);
        }

        /// <summary>Spec만으로 맞춰 본다. 활성 GE가 아니라서 IgnoreHandles·CustomMatchDelegate는 보지 않는다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::Matches(const FGameplayEffectSpec&amp;)</remarks>
        public bool Matches(GameplayEffectSpec spec)
        {
            if (spec.Definition == null)
            {
                return false;
            }

            if (!_owningTagQuery.IsEmpty())
            {
                // UE도 Owning을 Asset+Granted로 본다 — 5.3 전 동작을 유지한 것
                var targetTags = new GameplayTagContainer();
                spec.GetAllAssetTags(targetTags);
                spec.GetAllGrantedTags(targetTags);

                if (!_owningTagQuery.Matches(targetTags))
                {
                    return false;
                }
            }

            if (!_effectTagQuery.IsEmpty())
            {
                var effectTags = new GameplayTagContainer();
                spec.GetAllAssetTags(effectTags);

                if (!_effectTagQuery.Matches(effectTags))
                {
                    return false;
                }
            }

            if (!_sourceAggregateTagQuery.IsEmpty())
            {
                GameplayTagContainer sourceAggregateTags = spec.CapturedSourceTags.GetAggregatedTags();
                if (!_sourceAggregateTagQuery.Matches(sourceAggregateTags))
                {
                    return false;
                }
            }

            if (!_sourceTagQuery.IsEmpty())
            {
                GameplayTagContainer sourceSpecTags = spec.CapturedSourceTags.GetSpecTags();
                if (!_sourceTagQuery.Matches(sourceSpecTags))
                {
                    return false;
                }
            }

            if (_modifyingAttribute.IsSet && !ModifiesAttribute(spec, _modifyingAttribute))
            {
                return false;
            }

            if (_effectSource != null)
            {
                GameplayEffectContextHandle context = spec.Context;
                if (context.GetSourceObject() != _effectSource)
                {
                    return false;
                }
            }

            if (_effectDefinition != null && spec.Definition != _effectDefinition)
            {
                return false;
            }

            return true;
        }

        /// <remarks>UE: FGameplayEffectQuery::IsEmpty</remarks>
        public bool IsEmpty()
        {
            return CustomMatchDelegate == null && _owningTagQuery.IsEmpty() && _effectTagQuery.IsEmpty() && _sourceAggregateTagQuery.IsEmpty() && _sourceTagQuery.IsEmpty() && !_modifyingAttribute.IsSet && _effectSource == null && _effectDefinition == null;
        }

        /// <summary>Owning 태그(Asset+Granted)와 하나라도 겹치는 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchAnyOwningTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchAnyOwningTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _owningTagQuery = GameplayTagQuery.MakeQuery_MatchAnyTags(inTags) };
        }

        /// <summary>Owning 태그(Asset+Granted)가 주어진 태그를 모두 가진 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchAllOwningTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchAllOwningTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _owningTagQuery = GameplayTagQuery.MakeQuery_MatchAllTags(inTags) };
        }

        /// <summary>Owning 태그(Asset+Granted)와 하나도 겹치지 않는 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchNoOwningTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchNoOwningTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _owningTagQuery = GameplayTagQuery.MakeQuery_MatchNoTags(inTags) };
        }

        /// <summary>Asset Tags와 하나라도 겹치는 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchAnyEffectTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchAnyEffectTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _effectTagQuery = GameplayTagQuery.MakeQuery_MatchAnyTags(inTags) };
        }

        /// <summary>Asset Tags가 주어진 태그를 모두 가진 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchAllEffectTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchAllEffectTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _effectTagQuery = GameplayTagQuery.MakeQuery_MatchAllTags(inTags) };
        }

        /// <summary>Asset Tags와 하나도 겹치지 않는 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchNoEffectTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchNoEffectTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _effectTagQuery = GameplayTagQuery.MakeQuery_MatchNoTags(inTags) };
        }

        /// <summary>시전자 Spec 태그와 하나라도 겹치는 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchAnySourceSpecTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchAnySourceSpecTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _sourceTagQuery = GameplayTagQuery.MakeQuery_MatchAnyTags(inTags) };
        }

        /// <summary>시전자 Spec 태그가 주어진 태그를 모두 가진 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchAllSourceSpecTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchAllSourceSpecTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _sourceTagQuery = GameplayTagQuery.MakeQuery_MatchAllTags(inTags) };
        }

        /// <summary>시전자 Spec 태그와 하나도 겹치지 않는 GE를 고른다.</summary>
        /// <remarks>UE: FGameplayEffectQuery::MakeQuery_MatchNoSourceSpecTags</remarks>
        public static GameplayEffectQuery MakeQuery_MatchNoSourceSpecTags(GameplayTagContainer inTags)
        {
            return new GameplayEffectQuery { _sourceTagQuery = GameplayTagQuery.MakeQuery_MatchNoTags(inTags) };
        }

        private static bool ModifiesAttribute(GameplayEffectSpec spec, GameplayAttribute attribute)
        {
            foreach (GameplayModifier modifier in spec.Definition.Modifiers)
            {
                if (modifier.Attribute.Equals(attribute))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
