using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.AbilitySystem.Attribute;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Attribute.Editor
{
    public static class AttributeReflectionUtility
    {
        private static Type[] AttributeSetTypeCache;
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();

        public static Type[] GetAttributeSetTypes()
        {
            if (AttributeSetTypeCache == null)
            {
                AttributeSetTypeCache = TypeCache
                    .GetTypesDerivedFrom<AttributeSet>()
                    .Where(t => !t.IsAbstract)
                    .OrderBy(t => t.Name)
                    .ToArray();
            }

            return AttributeSetTypeCache;
        }

        public static FieldInfo[] GetAttributeDataFields(Type attributeSetType)
        {
            if (attributeSetType == null)
            {
                return Array.Empty<FieldInfo>();
            }

            if (FieldCache.TryGetValue(attributeSetType, out FieldInfo[] cached))
            {
                return cached;
            }

            FieldInfo[] fields = attributeSetType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(IsAttributeDataField)
                .OrderBy(f => f.MetadataToken)
                .ToArray();

            FieldCache[attributeSetType] = fields;
            return fields;
        }

        private static bool IsAttributeDataField(FieldInfo field)
        {
            if (!typeof(AttributeData).IsAssignableFrom(field.FieldType))
            {
                return false;
            }

            if (field.IsPublic)
            {
                return true;
            }

            return field.GetCustomAttribute<SerializeField>() != null;
        }
    }
}
