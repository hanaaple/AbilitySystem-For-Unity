using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Core.Common.Editor
{
    /// <summary>에디터 드로어에서 되풀이되는 "구체 서브클래스 열거"를 한 곳에 모은 헬퍼.</summary>
    public static class EditorTypeUtility
    {
        private static readonly Dictionary<Type, IReadOnlyList<Type>> _cache = new();
        private static readonly Dictionary<Type, MonoScript> _scriptCache = new();

        /// <summary>타입이 정의된 소스 스크립트(MonoScript)를 찾는다(못 찾으면 null). MonoScript.GetClass()는 파일명=클래스명인 최상위 클래스만 반환하므로 파일명≠클래스명이면 못 찾는다. 결과(null 포함)는 도메인 리로드까지 캐시.</summary>
        public static MonoScript FindScript(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (_scriptCache.TryGetValue(type, out MonoScript cached))
            {
                return cached;
            }

            MonoScript found = null;
            foreach (string guid in AssetDatabase.FindAssets($"{type.Name} t:script"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == type)
                {
                    found = script;
                    break;
                }
            }

            _scriptCache[type] = found;
            return found;
        }

        /// <summary>baseType의 인스턴스화 가능한(non-abstract·non-generic·무인자 생성자) 서브클래스를 이름순으로 반환. 도메인 리로드까지 캐시.</summary>
        public static IReadOnlyList<Type> GetConcreteSubclasses(Type baseType)
        {
            if (_cache.TryGetValue(baseType, out IReadOnlyList<Type> cached))
            {
                return cached;
            }

            IReadOnlyList<Type> types = TypeCache.GetTypesDerivedFrom(baseType)
                .Where(type => !type.IsAbstract)
                .Where(type => !type.IsGenericType)
                .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.Name)
                .ToList();

            _cache[baseType] = types;
            return types;
        }
    }
}
