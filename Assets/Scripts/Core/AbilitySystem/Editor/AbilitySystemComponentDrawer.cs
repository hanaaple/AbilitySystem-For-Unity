using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Editor
{
    [CustomEditor(typeof(AbilitySystemComponent))]
    public sealed class AbilitySystemComponentDrawer : UnityEditor.Editor
    {
        private bool _showRuntimeAttributes = true;
        private bool _showActiveEffects = true;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var asc = (AbilitySystemComponent)target;

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Open in ASC Window"))
            {
                AbilitySystemComponentWindow.Open();
            }

            EditorGUILayout.Space(8f);
            AbilitySystemInspectorGUI.DrawRuntimeAttributes(asc, ref _showRuntimeAttributes);
            EditorGUILayout.Space(4f);
            AbilitySystemInspectorGUI.DrawActiveEffects(asc, ref _showActiveEffects);
        }
    }
}
