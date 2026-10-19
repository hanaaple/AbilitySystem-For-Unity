using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>레지스트리 인스펙터를 읽기 전용으로 그린다. 태그 목록을 인스펙터에서 직접 고칠 수 없고, 편집은 태그 관리 창에서 한다.</summary>
    [CustomEditor(typeof(GameplayTagRegistryAsset))]
    public sealed class GameplayTagRegistryAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Manage Gameplay Tags..."))
            {
                GameplayTagManagerWindow.Open(string.Empty, null);
                GUIUtility.ExitGUI();
            }

            var registry = (GameplayTagRegistryAsset)target;
            IReadOnlyList<string> tags = registry.Tags;

            EditorGUILayout.LabelField($"Tags ({tags.Count})", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    EditorGUILayout.TextField(tags[i]);
                }
            }
        }
    }
}
