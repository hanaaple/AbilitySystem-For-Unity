using System;
using System.Collections.Generic;
using System.Reflection;
using Core.Common.Editor;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Abilities.Editor
{
    /// <summary>
    /// GA 정의 에셋 인스펙터 — 원형의 필드를 선언한 클래스별 상자로 나눠, 모든 어빌리티가 공유하는 <see cref="GameplayAbility"/> 필드와 하위 클래스가 더한 필드를 구분해 보여 준다.
    /// 타입 선택기는 범용 <see cref="SubclassSelectorDrawer.DrawManagedReferenceSelector"/>를 그대로 쓴다 — 나누는 정책은 GA 전용이라 범용 드로어에 두지 않는다.
    /// </summary>
    [CustomEditor(typeof(GameplayAbilityAsset))]
    public sealed class GameplayAbilityAssetEditor : UnityEditor.Editor
    {
        private const BindingFlags DeclaredInstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const float SectionGap = 6f;

        private static readonly Color SectionFill = new(0.5f, 0.5f, 0.5f, 0.08f);
        private static readonly Color SectionOutline = new(0.5f, 0.5f, 0.5f, 0.45f);

        private GUIStyle _sectionStyle;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty template = serializedObject.FindProperty("abilityTemplate");
            Rect selectorRect = EditorGUILayout.GetControlRect();
            SubclassSelectorDrawer.DrawManagedReferenceSelector(selectorRect, template, typeof(GameplayAbility), null, new GUIContent(template.displayName, template.tooltip));

            if (template.managedReferenceValue is GameplayAbility ability)
            {
                foreach ((Type declaringType, List<SerializedProperty> fields) in GroupByDeclaringType(template, ability.GetType()))
                {
                    GUILayout.Space(SectionGap);
                    DrawSection(declaringType, ability.GetType(), fields);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // 베이스(GameplayAbility)부터 실제 클래스까지 순서대로, 각 클래스가 선언한 필드만 모은다. 필드가 없는 중간 클래스는 건너뛰고, 실제 클래스는 비어도 상자를 둔다.
        private static List<(Type DeclaringType, List<SerializedProperty> Fields)> GroupByDeclaringType(SerializedProperty template, Type concreteType)
        {
            var hierarchy = new List<Type>();
            for (Type type = concreteType; type != null && typeof(GameplayAbility).IsAssignableFrom(type); type = type.BaseType)
            {
                hierarchy.Insert(0, type);
            }

            var groups = new Dictionary<Type, List<SerializedProperty>>();
            foreach (Type type in hierarchy)
            {
                groups[type] = new List<SerializedProperty>();
            }

            SerializedProperty child = template.Copy();
            SerializedProperty end = template.GetEndProperty();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                groups[FindDeclaringType(hierarchy, child.name) ?? concreteType].Add(child.Copy());
            }

            var result = new List<(Type, List<SerializedProperty>)>();
            foreach (Type type in hierarchy)
            {
                if (groups[type].Count > 0 || type == concreteType)
                {
                    result.Add((type, groups[type]));
                }
            }

            return result;
        }

        private static Type FindDeclaringType(List<Type> hierarchy, string fieldName)
        {
            foreach (Type type in hierarchy)
            {
                if (type.GetField(fieldName, DeclaredInstanceFields) != null)
                {
                    return type;
                }
            }

            return null;
        }

        private void DrawSection(Type declaringType, Type concreteType, List<SerializedProperty> fields)
        {
            _sectionStyle ??= new GUIStyle { padding = new RectOffset(8, 6, 4, 6) };

            Rect box = EditorGUILayout.BeginVertical(_sectionStyle);

            // 칠이 자식 아래에 깔리도록 BeginVertical 바로 뒤, Repaint에서만 그린다(BeginVertical의 Rect는 Repaint에서만 유효)
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(box, SectionFill);
                DrawOutline(box, SectionOutline);
            }

            EditorGUILayout.LabelField(SectionTitle(declaringType, concreteType), EditorStyles.boldLabel);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            if (fields.Count == 0)
            {
                EditorGUILayout.LabelField("이 클래스에 선언한 필드 없음", EditorStyles.miniLabel);
            }

            foreach (SerializedProperty field in fields)
            {
                EditorGUILayout.PropertyField(field, true);
            }

            EditorGUI.indentLevel = indent;
            EditorGUILayout.EndVertical();
        }

        private static GUIContent SectionTitle(Type declaringType, Type concreteType)
        {
            if (declaringType == typeof(GameplayAbility))
            {
                return new GUIContent("GameplayAbility (공통)", "모든 어빌리티가 공유하는 필드 — 인스턴싱 정책, 코스트·쿨다운 GE, 발동 태그 조건.");
            }

            string role = declaringType == concreteType ? "이 어빌리티" : "상위 클래스";
            return new GUIContent($"{declaringType.Name} ({role})", $"{declaringType.Name}에 선언된 필드.");
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }
    }
}
