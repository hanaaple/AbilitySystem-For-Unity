using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Core.Common.Editor
{
    [CustomPropertyDrawer(typeof(ConditionalShowAttribute))]
    public sealed class ConditionalShowDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!IsConditionMet(property))
            {
                return;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!IsConditionMet(property))
            {
                return 0f;
            }

            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool IsConditionMet(SerializedProperty property)
        {
            ConditionalShowAttribute attr = (ConditionalShowAttribute)attribute;

            object targetObject = GetParentObject(property);
            if (targetObject == null)
            {
                return true;
            }

            Type targetType = targetObject.GetType();
            FieldInfo conditionField = FindField(targetType, attr.ConditionFieldName);
            if (conditionField == null)
            {
                Debug.LogWarning($"[ConditionalShow] '{targetType.Name}'에서 '{attr.ConditionFieldName}' 필드를 찾을 수 없습니다.");
                return true;
            }

            object fieldValue = conditionField.GetValue(targetObject);

            if (attr.UseOperator)
            {
                return EvaluateOperator(fieldValue, attr.Op, (float)attr.CompareValue);
            }

            return Equals(fieldValue, attr.CompareValue);
        }

        private static bool EvaluateOperator(object fieldValue, ConditionalOp op, float compareValue)
        {
            float value = fieldValue switch
            {
                float f  => f,
                int i    => i,
                double d => (float)d,
                _        => 0f,
            };

            return op switch
            {
                ConditionalOp.Equal        => Mathf.Approximately(value, compareValue),
                ConditionalOp.NotEqual     => !Mathf.Approximately(value, compareValue),
                ConditionalOp.Greater      => value > compareValue,
                ConditionalOp.GreaterEqual => value >= compareValue,
                ConditionalOp.Less         => value < compareValue,
                ConditionalOp.LessEqual    => value <= compareValue,
                _                          => true,
            };
        }

        /// <summary>상속 계층 전체에서 필드를 검색한다.</summary>
        private static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            while (type != null && type != typeof(object))
            {
                FieldInfo field = type.GetField(fieldName, flags);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// SerializedProperty의 경로를 역추적해 직접 부모 오브젝트를 반환한다.
        /// 배열/리스트 요소, 중첩 구조체 모두 지원.
        /// </summary>
        private static object GetParentObject(SerializedProperty property)
        {
            string path = property.propertyPath;
            object current = property.serializedObject.targetObject;

            int lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
            {
                return current;
            }

            string parentPath = path[..lastDot];
            foreach (string part in parentPath.Split('.'))
            {
                if (current == null)
                {
                    return null;
                }

                if (part == "Array")
                {
                    continue;
                }

                if (part.StartsWith("data[", StringComparison.Ordinal))
                {
                    if (current is System.Collections.IList list)
                    {
                        int idx = int.Parse(part[5..^1]);
                        current = list[idx];
                    }
                    continue;
                }

                FieldInfo field = FindField(current.GetType(), part);
                current = field?.GetValue(current);
            }

            return current;
        }
    }
}
