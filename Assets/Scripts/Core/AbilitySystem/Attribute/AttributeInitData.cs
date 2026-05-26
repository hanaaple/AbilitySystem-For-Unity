using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    [CreateAssetMenu(menuName = "GAS/Attribute Init Data")]
    public sealed class AttributeInitData : ScriptableObject
    {
        [SerializeField]
        private List<AttributeSetInitData> attributeSets = new();

        public IReadOnlyList<AttributeSetInitData> AttributeSets => attributeSets;
    }
}
