using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Core.Common;
using UnityEditor;
using UnityEngine;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>태그 에디터 UI(드로어·피커)가 함께 쓰는 조회·클립보드·정렬 헬퍼.</summary>
    internal static class GameplayTagEditorUtility
    {
        private static readonly Regex _tagNamePattern = new("TagName\\s*=\\s*\"([^\"]*)\"");
        private static readonly char[] _tagListSeparators = { ',', '\n', '\r' };

        /// <summary>현재 트리에 등록된 태그 이름인지.</summary>
        public static bool IsRegisteredTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                return false;
            }

            GameplayTagsManager manager = GameplayTagsManager.Get();
            return manager.FindTagNode(new InternedName(tagName)) != null;
        }

        /// <summary>표시·저장 순서. 끝의 숫자는 수 크기로 비교한다(<c>A.2</c> &lt; <c>A.10</c>).</summary>
        public static int CompareTagNames(string left, string right)
        {
            return EditorUtility.NaturalCompare(left, right);
        }

        /// <summary>메뉴 항목을 더한다. 쓸 수 없으면 회색으로 둔다.</summary>
        public static void AddMenuItem(GenericMenu menu, string text, bool isEnabled, GenericMenu.MenuFunction onSelected)
        {
            var content = new GUIContent(text);
            if (isEnabled)
            {
                menu.AddItem(content, false, onSelected);
            }
            else
            {
                menu.AddDisabledItem(content);
            }
        }

        /// <summary>클립보드 텍스트를 태그 하나로 읽는다. <c>A.B</c>·<c>"A.B"</c>·<c>(TagName="A.B")</c>를 받으며, 등록된 태그일 때만 성공한다.</summary>
        /// <remarks>UE: UE::GameplayTags::EditorUtilities::GameplayTagTryImportText</remarks>
        public static bool TryImportTag(string text, out string outTagName)
        {
            outTagName = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmedText = text.Trim();
            MatchCollection matches = _tagNamePattern.Matches(trimmedText);
            string candidate;
            if (matches.Count == 0)
            {
                candidate = trimmedText.Trim('"');
            }
            else if (matches.Count == 1)
            {
                candidate = GetCapturedTagName(matches[0]);
            }
            else
            {
                return false;
            }

            if (!IsRegisteredTag(candidate))
            {
                return false;
            }

            outTagName = candidate;
            return true;
        }

        /// <summary>클립보드 텍스트를 태그 목록으로 읽는다. <c>TagName="..."</c> 항목들이나 쉼표·줄바꿈으로 나눈 이름을 받으며, 등록된 태그만 담는다. 하나라도 담기면 성공.</summary>
        /// <remarks>UE: UE::GameplayTags::EditorUtilities::GameplayTagContainerTryImportText</remarks>
        public static bool TryImportTagContainer(string text, List<string> outTagNames)
        {
            outTagNames.Clear();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            MatchCollection matches = _tagNamePattern.Matches(text);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    AddRegisteredTag(outTagNames, GetCapturedTagName(match));
                }
            }
            else
            {
                foreach (string part in text.Split(_tagListSeparators, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmedPart = part.Trim();
                    AddRegisteredTag(outTagNames, trimmedPart.Trim('"'));
                }
            }

            return outTagNames.Count > 0;
        }

        private static string GetCapturedTagName(Match match)
        {
            Group tagNameGroup = match.Groups[1];
            return tagNameGroup.Value;
        }

        private static void AddRegisteredTag(List<string> tagNames, string tagName)
        {
            if (IsRegisteredTag(tagName) && !tagNames.Contains(tagName))
            {
                tagNames.Add(tagName);
            }
        }
    }
}
