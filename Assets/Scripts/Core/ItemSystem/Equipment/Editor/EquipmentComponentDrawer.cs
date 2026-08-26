using System.Collections.Generic;
using System.Reflection;
using Core.ItemSystem.Module;
using UnityEditor;
using UnityEngine;

namespace Core.ItemSystem.Equipment.Editor
{
    // 런타임 장착 상태(슬롯 → 아이템 → 모듈)를 인스펙터에 표시하는 에디터 전용 디버깅 드로어.
    // AbilitySystemComponentDrawer와 동일한 방식: private 필드를 리플렉션으로 읽어 Play 모드에서 노출.
    [CustomEditor(typeof(EquipmentComponent))]
    public sealed class EquipmentComponentDrawer : UnityEditor.Editor
    {
        private const string EquippedFieldName = "_equipped";

        private static readonly FieldInfo EquippedField = typeof(EquipmentComponent)
            .GetField(EquippedFieldName, BindingFlags.Instance | BindingFlags.NonPublic);

        private bool _showEquipped = true;

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            DrawEquipped();
        }

        private void DrawEquipped()
        {
            _showEquipped = EditorGUILayout.Foldout(
                _showEquipped,
                "Equipped Slots",
                true,
                EditorStyles.foldoutHeader);

            if (!_showEquipped)
            {
                return;
            }

            EditorGUI.indentLevel++;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Equipped items are available in Play Mode.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            if (EquippedField == null)
            {
                EditorGUILayout.HelpBox($"Field '{EquippedFieldName}' was not found.", MessageType.Error);
                EditorGUI.indentLevel--;
                return;
            }

            var equipment = (EquipmentComponent)target;
            var equipped = EquippedField.GetValue(equipment) as IReadOnlyDictionary<SlotType, ItemInstance>;

            if (equipped == null || equipped.Count == 0)
            {
                EditorGUILayout.HelpBox("No equipped items.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            foreach (KeyValuePair<SlotType, ItemInstance> pair in equipped)
            {
                DrawSlot(pair.Key, pair.Value);
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawSlot(SlotType slot, ItemInstance instance)
        {
            ItemDataAsset data = instance?.Data;
            string itemName = data == null
                ? "<empty>"
                : (string.IsNullOrEmpty(data.displayName) ? data.name : data.displayName);

            EditorGUILayout.LabelField($"[{slot}]  {itemName}", EditorStyles.boldLabel);

            if (data == null)
            {
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Modules", EditorStyles.miniLabel);

            EditorGUI.indentLevel++;
            if (data.modules == null || data.modules.Count == 0)
            {
                EditorGUILayout.LabelField("<none>");
            }
            else
            {
                foreach (ItemModule module in data.modules)
                {
                    if (module == null)
                    {
                        EditorGUILayout.LabelField("<null>");
                        continue;
                    }

                    string suffix = module.Enabled ? string.Empty : "  (disabled)";
                    EditorGUILayout.LabelField($"{module.GetType().Name}{suffix}");
                }
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
    }
}
