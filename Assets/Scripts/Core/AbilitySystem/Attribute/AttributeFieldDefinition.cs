using System;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    [Serializable]
    public sealed class AttributeFieldDefinition
    {
        [SerializeField]
        private string fieldName;

        [SerializeField]
        private float baseValue;

        public string FieldName => fieldName;
        public AttributeData Data => new(baseValue, baseValue);
    }
}
