using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Core.Common
{
    /// <summary>
    /// Unity 직렬화 규칙대로 관리 객체(일반 C# 클래스)를 깊은 복사한다. 직렬화되는 필드만 복사하고, 나머지 필드는 새 인스턴스의 초기값으로 둔다.
    /// 값·문자열·enum은 값으로, List·배열·[Serializable] 중첩 객체는 새로 만들고, UnityEngine.Object(에셋·씬 참조)는 참조를 그대로 둔다.
    /// [SerializeReference] 필드는 같은 객체를 가리키던 참조끼리 복사본에서도 같은 객체를 가리킨다.
    /// 복사본이 <see cref="ISerializationCallbackReceiver"/>면 필드를 다 채운 뒤 OnAfterDeserialize를 부른다(안쪽 객체가 먼저).
    /// </summary>
    public static class SerializedDeepCopy
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <summary>source를 깊은 복사한다. null이면 null.</summary>
        public static T Clone<T>(T source) where T : class
        {
            return (T)CopyReference(source, new Dictionary<object, object>(ReferenceComparer.Instance));
        }

        // SerializeReference 대상 — 같은 원본은 같은 복사본으로 이어 공유 참조를 유지한다.
        private static object CopyReference(object source, Dictionary<object, object> copies)
        {
            if (source == null)
            {
                return null;
            }

            if (copies.TryGetValue(source, out object existing))
            {
                return existing;
            }

            Type type = source.GetType();
            object copy = Activator.CreateInstance(type, nonPublic: true);
            copies[source] = copy;
            CopyFields(source, copy, type, copies);
            NotifyAfterDeserialize(copy);
            return copy;
        }

        private static void CopyFields(object source, object target, Type type, Dictionary<object, object> copies)
        {
            for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(InstanceFields))
                {
                    if (!IsSerialized(field))
                    {
                        continue;
                    }

                    bool byReference = field.IsDefined(typeof(SerializeReference), false);
                    field.SetValue(target, CopyValue(field.GetValue(source), byReference, copies));
                }
            }
        }

        // Unity가 직렬화하는 필드: static·readonly·const·[NonSerialized]가 아니고, public이거나 [SerializeField]/[SerializeReference].
        private static bool IsSerialized(FieldInfo field)
        {
            if (field.IsStatic || field.IsInitOnly || field.IsLiteral || field.IsNotSerialized)
            {
                return false;
            }

            return field.IsPublic
                || field.IsDefined(typeof(SerializeField), false)
                || field.IsDefined(typeof(SerializeReference), false);
        }

        private static object CopyValue(object value, bool byReference, Dictionary<object, object> copies)
        {
            if (value == null || value is UnityEngine.Object)
            {
                return value;
            }

            Type type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || value is string)
            {
                return value;
            }

            if (value is Array array)
            {
                Array arrayCopy = Array.CreateInstance(type.GetElementType(), array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    arrayCopy.SetValue(CopyValue(array.GetValue(i), byReference, copies), i);
                }

                return arrayCopy;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var listCopy = (IList)Activator.CreateInstance(type);
                foreach (object item in (IList)value)
                {
                    listCopy.Add(CopyValue(item, byReference, copies));
                }

                return listCopy;
            }

            if (byReference)
            {
                return CopyReference(value, copies);
            }

            // 엔진 클래스(AnimationCurve·Gradient 등)는 네이티브 메모리를 가져 필드 복사가 안전하지 않아 참조를 공유한다.
            if (!type.IsDefined(typeof(SerializableAttribute), false) || (!type.IsValueType && IsEngineType(type)))
            {
                return value;
            }

            // [Serializable] 중첩 객체는 Unity가 값처럼 인라인 저장하므로 공유하지 않고 새로 만든다.
            object inlineCopy = Activator.CreateInstance(type, nonPublic: true);
            CopyFields(value, inlineCopy, type, copies);
            NotifyAfterDeserialize(inlineCopy);
            return inlineCopy;
        }

        // 복사는 역직렬화를 대신하므로, [NonSerialized] 캐시(해시·부모 태그 등)를 OnAfterDeserialize로 채우는 타입이 복사본에서도 같은 상태가 되게 한다.
        // 구조체는 박싱된 복사본에 부르므로 그 결과가 그대로 필드에 들어간다.
        private static void NotifyAfterDeserialize(object copy)
        {
            if (copy is ISerializationCallbackReceiver receiver)
            {
                receiver.OnAfterDeserialize();
            }
        }

        private static bool IsEngineType(Type type)
        {
            return type.Namespace != null && type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal);
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
