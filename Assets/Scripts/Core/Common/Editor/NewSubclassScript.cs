using System;
using UnityEditor;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>New Script 흐름의 진입점(파사드). 호출부(드로어)는 "어디에 배정할지"만 말하고 팝업 열기·리로드 넘김·SessionState stash 같은 내부는 몰라도 된다. 내부 협력자: 입력 UI=<see cref="NewSubclassScriptPopup"/>, 리로드 후 배정=<see cref="PendingSubclassAssignment"/>.</summary>
    internal static class NewSubclassScript
    {
        /// <summary>기존 요소(문자열 필드)의 타입을 새로 만든 스크립트로 바꾼다.</summary>
        public static void OpenForField(Rect activatorScreenRect, Type baseType, UnityEngine.Object target, string propertyPath)
        {
            Open(activatorScreenRect, baseType,
                (className, scriptPath) => PendingSubclassAssignment.StashFieldAssign(baseType, className, target, propertyPath, scriptPath));
        }

        /// <summary>배열에 새 요소를 추가하며 그 타입을 새로 만든 스크립트로 채운다.</summary>
        public static void OpenForListAdd(Rect activatorScreenRect, Type baseType, UnityEngine.Object target, string arrayPath, string typeRelPath, string clearArrayRelPath)
        {
            Open(activatorScreenRect, baseType,
                (className, scriptPath) => PendingSubclassAssignment.StashListAdd(baseType, className, target, arrayPath, typeRelPath, clearArrayRelPath, scriptPath));
        }

        // 팝업을 연다. onCreated는 파일 생성 직후(리로드 전) 호출돼 배정을 stash한다.
        // 드롭다운의 항목 선택 콜백에서 곧바로 창을 열면 삼켜질 수 있어, 한 프레임 미뤄 연다.
        private static void Open(Rect activatorScreenRect, Type baseType, Action<string, string> onCreated)
        {
            EditorApplication.delayCall += () => NewSubclassScriptPopup.Show(activatorScreenRect, baseType, onCreated);
        }
    }
}
