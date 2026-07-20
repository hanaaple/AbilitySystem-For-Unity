using System;
using System.Collections.Generic;
using System.Linq;
using Core.AbilitySystem.Attribute.Editor;

namespace Core.AbilitySystem.Effect.Editor
{
    /// <summary>
    /// AttributeSet(AssemblyQualifiedName) + Field 이름 한 쌍을 팝업으로 고르는 공용 에디터 헬퍼.
    /// modifier 대상 어트리뷰트와 AttributeBased 캡처 어트리뷰트가 같은 로직을 공유한다.
    /// popup index 0 = "None", 1+ = GetSetTypes() 순서.
    /// </summary>
    internal static class AttributeReferenceGUI
    {
        private static string[] _setDisplayNames;
        private static readonly Dictionary<Type, string[]> _fieldNameCache = new();

        public static Type[] GetSetTypes() => AttributeReflectionUtility.GetAttributeSetTypes();

        public static string[] GetSetDisplayNames(Type[] setTypes)
        {
            return _setDisplayNames ??= new[] { "None" }.Concat(setTypes.Select(t => t.Name)).ToArray();
        }

        public static int GetSetPopupIndex(Type[] setTypes, string assemblyQualifiedName)
        {
            if (string.IsNullOrEmpty(assemblyQualifiedName))
            {
                return 0;
            }

            for (int i = 0; i < setTypes.Length; i++)
            {
                if (setTypes[i].AssemblyQualifiedName == assemblyQualifiedName)
                {
                    return i + 1;
                }
            }
            return 0;
        }

        public static string[] GetFieldNames(Type setType)
        {
            if (_fieldNameCache.TryGetValue(setType, out string[] cached))
            {
                return cached;
            }

            string[] names = AttributeReflectionUtility.GetAttributeDataFields(setType)
                .Select(f => f.Name)
                .ToArray();

            _fieldNameCache[setType] = names;
            return names;
        }
    }
}
