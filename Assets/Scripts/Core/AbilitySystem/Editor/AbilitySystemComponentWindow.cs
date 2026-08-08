using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Editor
{
    /// <summary>
    /// 현재 선택(하이라이트)된 GameObject의 <see cref="AbilitySystemComponent"/>를 독립 창에서 보여주는 디버그 뷰.
    /// 컴포넌트 인스펙터와 같은 표시(<see cref="AbilitySystemInspectorGUI"/>)를 쓰되, 인스펙터가 다른 대상으로
    /// 바뀌어도 ASC 상태를 계속 띄워 둘 수 있게 별도 창으로 뺀 것이다. 선택 대상에 ASC가 없으면 null로 두고 안내만 낸다.
    /// </summary>
    public sealed class AbilitySystemComponentWindow : EditorWindow
    {
        private bool _showRuntimeAttributes = true;
        private bool _showActiveEffects = true;
        private Vector2 _scroll;

        /// <summary>ASC 인스펙터 창을 연다(없으면 새로 만들고, 있으면 포커스). 메뉴와 컴포넌트 버튼이 공유한다.</summary>
        [MenuItem("Window/Ability System/ASC Inspector")]
        public static void Open()
        {
            GetWindow<AbilitySystemComponentWindow>("ASC Inspector");
        }

        private void OnEnable()
        {
            // 선택이 바뀌면 대상 ASC가 달라지므로 다시 그린다.
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void OnInspectorUpdate()
        {
            // Play 모드에서 어트리뷰트·Active Effect 값이 매 프레임 변하므로 주기적으로 갱신한다.
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            AbilitySystemComponent asc = ResolveSelectedAsc();

            EditorGUILayout.LabelField(
                "Target",
                Selection.activeGameObject != null ? Selection.activeGameObject.name : "(none)",
                EditorStyles.boldLabel);

            if (asc == null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(
                    "선택된 GameObject에 AbilitySystemComponent가 없습니다.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            AbilitySystemInspectorGUI.DrawRuntimeAttributes(asc, ref _showRuntimeAttributes);
            EditorGUILayout.Space(4f);
            AbilitySystemInspectorGUI.DrawActiveEffects(asc, ref _showActiveEffects);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>선택된 GameObject의 ASC. 선택이 없거나 ASC가 없으면 null.</summary>
        private static AbilitySystemComponent ResolveSelectedAsc()
        {
            GameObject selected = Selection.activeGameObject;
            return selected == null ? null : selected.GetComponent<AbilitySystemComponent>();
        }
    }
}
