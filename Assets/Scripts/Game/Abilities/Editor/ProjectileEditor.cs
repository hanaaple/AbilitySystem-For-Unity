using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Abilities.Editor
{
    /// <summary>프리팹·씬에서 VFX 이벤트를 버튼으로 보내 미리 본다. 게임에선 이벤트를 코드가 보내므로 VFX에 시작 이벤트가 없다.</summary>
    [CustomEditor(typeof(Projectile))]
    public class ProjectileEditor : UnityEditor.Editor
    {
        private readonly List<string> _eventNames = new();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var visualEffect = serializedObject.FindProperty("visualEffect").objectReferenceValue as VisualEffect;
            if (visualEffect == null || visualEffect.visualEffectAsset == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VFX Preview", EditorStyles.boldLabel);

            _eventNames.Clear();
            visualEffect.visualEffectAsset.GetEvents(_eventNames);

            EditorGUILayout.BeginHorizontal();
            foreach (string eventName in _eventNames)
            {
                if (GUILayout.Button(eventName))
                {
                    visualEffect.SendEvent(eventName);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Reset"))
            {
                visualEffect.Reinit();
                SceneView.RepaintAll();
            }
        }
    }
}
