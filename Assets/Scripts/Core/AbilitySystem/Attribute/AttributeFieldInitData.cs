using System;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    [Serializable]
    public sealed class AttributeFieldInitData
    {
        [SerializeField]
        private string fieldName;

        [SerializeField]
        private float baseValue;

        public string FieldName => fieldName;
        public AttributeData Data => new(baseValue, baseValue);
    }
}
