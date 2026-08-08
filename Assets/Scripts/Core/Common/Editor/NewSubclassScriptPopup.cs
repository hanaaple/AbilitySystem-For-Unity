using System;
using UnityEditor;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>
    /// New Script의 <b>입력 UI</b>(표현부)만 담당하는 팝업. Unity Add Component → New Script 흐름을 흉내낸다:
    /// 이름·폴더를 받아 baseType의 빈 서브클래스 .cs를 만든다.
    ///
    /// 이 창은 "어떻게 생겼나 · 무엇을 입력받나"만 안다 — 소스 조립/검증은 <see cref="SubclassScriptTemplate"/>,
    /// 생성 후 "어디에 배정하나"는 Show에 넘어온 onCreated 콜백(→ <see cref="NewSubclassScript"/>)이 정한다.
    /// 진입점은 <see cref="NewSubclassScript"/>이며, 드로어가 이 창을 직접 여는 대신 그 파사드를 쓴다.
    /// </summary>
    internal sealed class NewSubclassScriptPopup : EditorWindow
    {
        private const string NameControl = "NewSubclassScriptPopup.Name";
        private const string DefaultFolder = "Assets/Scripts";

        private Type _baseType;
        private Action<string, string> _onCreated; // (className, scriptPath) — 파일 생성 직후(리로드 전) 호출
        private string _className;
        private string _folder;
        private bool _focusPending;

        // onCreated: 스크립트 생성 직후(리로드 전) 호출된다. 인자는 (만든 클래스명, 생성된 .cs 경로).
        public static void Show(Rect activatorScreenRect, Type baseType, Action<string, string> onCreated)
        {
            var window = CreateInstance<NewSubclassScriptPopup>();
            window._baseType = baseType;
            window._onCreated = onCreated;
            window._className = SubclassScriptTemplate.SuggestClassName(baseType);
            window._folder = DefaultFolder;
            window._focusPending = true;

            float width = Mathf.Max(activatorScreenRect.width, 320f);
            window.ShowAsDropDown(activatorScreenRect, new Vector2(width, 128f));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField($"New {_baseType.Name} Script", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            GUI.SetNextControlName(NameControl);
            _className = EditorGUILayout.TextField("Name", _className);
            _folder = EditorGUILayout.TextField("Folder", _folder);

            if (_focusPending && Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl(NameControl);
                _focusPending = false;
            }

            string error = SubclassScriptTemplate.Validate(_folder, _className, _baseType);
            if (error != null)
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }

            EditorGUILayout.Space(2f);

            bool clicked;
            using (new EditorGUI.DisabledScope(error != null))
            {
                clicked = GUILayout.Button("Create and Assign");
            }

            if (error == null && (clicked || IsEnterPressed()))
            {
                CreateAndClose();
            }
        }

        private void CreateAndClose()
        {
            string path = SubclassScriptTemplate.CreateFile(_folder, _className, _baseType);

            // 리로드 전에 "무엇을 어디에 배정할지"를 호출부(파사드)가 stash한다. 그 다음 Refresh로 컴파일·리로드를 태운다.
            _onCreated?.Invoke(_className, path);

            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();

            Close();
            GUIUtility.ExitGUI();
        }

        private static bool IsEnterPressed()
        {
            Event e = Event.current;
            return e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
        }
    }
}
