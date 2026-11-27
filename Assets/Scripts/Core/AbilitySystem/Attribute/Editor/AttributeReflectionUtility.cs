using System;
using System.Linq;
using Core.AbilitySystem.Attribute;
using UnityEditor;

namespace Core.AbilitySystem.Attribute.Editor
{
    public static class AttributeReflectionUtility
    {
        private static Type[] AttributeSetTypeCache;

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
    }
}
