using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    [CreateAssetMenu(menuName = "GAS/Attribute Definition Asset")]
    public sealed class AttributeDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private List<AttributeSetDefinition> attributeSets = new();

        public IReadOnlyList<AttributeSetDefinition> AttributeSets => attributeSets;
    }
}
