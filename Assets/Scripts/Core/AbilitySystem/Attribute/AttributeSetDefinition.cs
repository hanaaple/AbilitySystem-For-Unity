using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    [Serializable]
    public sealed class AttributeSetDefinition
    {
        [SerializeField]
        private string attributeSetTypeName;

        [SerializeField]
        private List<AttributeFieldDefinition> attributes = new();

        public IReadOnlyList<AttributeFieldDefinition> Attributes => attributes;

        public Type GetAttributeSetType()
        {
            if (string.IsNullOrEmpty(attributeSetTypeName))
            {
                return null;
            }

            return Type.GetType(attributeSetTypeName);
        }
    }
}
