using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Common;
using UnityEngine;

namespace Core.AbilitySystem.Tag
{
    /// <summary>코드에서 정의하는 태그. <see cref="NativeTagAttribute"/>를 붙인 static 필드로 선언하면 매니저가 트리 구성 시 모아 등록한다. 레지스트리 에셋에 저장하지 않고 구성마다 코드에서 다시 모은다.</summary>
    /// <example><code>[NativeTag] public static readonly NativeGameplayTag Stun = new("Status.Debuff.Stun");</code></example>
    public sealed class NativeGameplayTag
    {
        private readonly InternedName _tagName;

        public InternedName TagName => _tagName;

        // 매니저가 트리 구성 중 필드를 읽을 때 이 생성자가 실행된다. 여기서 매니저에 접근하면 구성이 재귀하므로 이름만 저장한다.
        /// <remarks>UE: FNativeGameplayTag::FNativeGameplayTag</remarks>
        public NativeGameplayTag(string tagName)
        {
            _tagName = new InternedName(tagName);
        }

        /// <remarks>UE: FNativeGameplayTag::operator FGameplayTag</remarks>
        public static implicit operator GameplayTag(NativeGameplayTag nativeTag)
        {
            return nativeTag.GetTag();
        }

        /// <summary>등록된 태그. 마커 없이 만든 인스턴스는 트리에 없으므로 빈 태그가 된다.</summary>
        /// <remarks>UE: FNativeGameplayTag::GetTag</remarks>
        public GameplayTag GetTag()
        {
            return GameplayTag.RequestGameplayTag(_tagName);
        }

        /// <summary>마커가 붙은 static 필드에서 native 태그를 모은다. 필드를 읽으면서 선언 클래스의 static 초기화가 실행된다.</summary>
        /// <remarks>UE: FNativeGameplayTag::GetRegisteredNativeTags</remarks>
        public static List<NativeGameplayTag> GetRegisteredNativeTags()
        {
            var nativeTags = new List<NativeGameplayTag>();

            foreach (FieldInfo field in FindMarkedFields())
            {
                if (!field.IsStatic || field.FieldType != typeof(NativeGameplayTag))
                {
                    Debug.LogWarning($"[{nameof(NativeTagAttribute)}]는 static {nameof(NativeGameplayTag)} 필드에만 붙일 수 있습니다: {field.DeclaringType}.{field.Name}");
                    continue;
                }

                if (field.GetValue(null) is NativeGameplayTag nativeTag)
                {
                    nativeTags.Add(nativeTag);
                }
            }

            return nativeTags;
        }

        private static IEnumerable<FieldInfo> FindMarkedFields()
        {
#if UNITY_EDITOR
            // 에디터 도메인은 어셈블리가 많아 전수 리플렉션이 느리므로 Unity가 인덱싱해 둔 TypeCache를 쓴다.
            return UnityEditor.TypeCache.GetFieldsWithAttribute<NativeTagAttribute>();
#else
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var fields = new List<FieldInfo>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    foreach (FieldInfo field in type.GetFields(flags))
                    {
                        if (field.IsDefined(typeof(NativeTagAttribute), false))
                        {
                            fields.Add(field);
                        }
                    }
                }
            }

            return fields;
#endif
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                // 일부 타입만 로드 실패한 어셈블리도 나머지 타입은 탐색한다.
                var loadedTypes = new List<Type>();
                foreach (Type type in exception.Types)
                {
                    if (type != null)
                    {
                        loadedTypes.Add(type);
                    }
                }

                return loadedTypes;
            }
        }
    }
}
