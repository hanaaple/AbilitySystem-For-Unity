using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// 드로어가 그리는 <see cref="GameplayTag"/>·<see cref="GameplayTagContainer"/> 필드 하나를 선택된 오브젝트마다 따로 읽고 쓴다.
    /// 팝업·메뉴 콜백은 인스펙터 그리기가 끝난 뒤 실행돼 원래 <see cref="SerializedProperty"/>가 무효일 수 있으므로, 대상 오브젝트와 경로만 들고 오브젝트별 <see cref="SerializedObject"/>에서 매번 다시 찾는다.
    /// 값은 명시 태그 이름 목록으로 다룬다. 단일 태그 필드는 비었으면 빈 목록, 아니면 원소 하나다.
    /// </summary>
    internal sealed class GameplayTagPropertyHandle
    {
        // GameplayTag.name(InternedName) → InternedName.name(string). 런타임 직렬화 필드 이름을 바꾸면 여기도 바꿔야 한다.
        private const string TagNamePath = "name.name";
        // GameplayTagContainer._gameplayTags. 런타임 직렬화 필드 이름을 바꾸면 여기도 바꿔야 한다.
        private const string ContainerTagsPath = "_gameplayTags";

        private readonly Object[] _targets;
        private readonly string _propertyPath;
        private readonly SerializedObject[] _serializedObjects;

        public bool IsContainer { get; }

        /// <summary>피커 루트를 제한하는 필터(<see cref="GameplayTagCategoriesAttribute"/>). 없으면 빈 문자열.</summary>
        public string Categories { get; }

        public int TargetCount => _targets.Length;

        public GameplayTagPropertyHandle(SerializedProperty property, bool isContainer, string categories)
        {
            SerializedObject serializedObject = property.serializedObject;
            _targets = serializedObject.targetObjects;
            _propertyPath = property.propertyPath;
            _serializedObjects = new SerializedObject[_targets.Length];
            IsContainer = isContainer;
            Categories = categories ?? string.Empty;
        }

        /// <summary>같은 필드·같은 선택을 가리키는지. 드로어가 캐시한 손잡이를 다시 쓸지 정할 때 쓴다.</summary>
        public bool Matches(SerializedProperty property)
        {
            if (property.propertyPath != _propertyPath)
            {
                return false;
            }

            SerializedObject serializedObject = property.serializedObject;
            Object[] targets = serializedObject.targetObjects;
            if (targets.Length != _targets.Length)
            {
                return false;
            }

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] != _targets[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>오브젝트마다 명시 태그 이름 목록을 읽는다. 반환 목록은 사본이라 고쳐도 데이터에 반영되지 않는다.</summary>
        /// <remarks>UE: SGameplayTagPicker::GetEditableTagContainersFromPropertyHandle</remarks>
        public List<List<string>> GetEditableTagContainers()
        {
            var tagContainers = new List<List<string>>(_targets.Length);
            for (int i = 0; i < _targets.Length; i++)
            {
                SerializedProperty property = FindProperty(i);
                tagContainers.Add(property != null ? ReadTagNames(property, IsContainer) : new List<string>());
            }

            return tagContainers;
        }

        /// <summary>오브젝트마다 태그 이름 목록을 쓴다. 모든 오브젝트의 변경을 Undo 한 번으로 묶는다.</summary>
        /// <remarks>UE: IPropertyHandle::SetPerObjectValues</remarks>
        public void SetPerObjectValues(string undoName, IReadOnlyList<List<string>> perObjectTagNames)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            int undoGroup = Undo.GetCurrentGroup();

            int count = Mathf.Min(_targets.Length, perObjectTagNames.Count);
            for (int i = 0; i < count; i++)
            {
                SerializedProperty property = FindProperty(i);
                if (property == null)
                {
                    continue;
                }

                WriteTagNames(property, IsContainer, perObjectTagNames[i]);
                _serializedObjects[i].ApplyModifiedProperties();
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        /// <summary>프로퍼티 하나에서 명시 태그 이름을 읽는다. 빈 이름·중복은 건너뛴다.</summary>
        public static List<string> ReadTagNames(SerializedProperty property, bool isContainer)
        {
            var tagNames = new List<string>();
            if (!isContainer)
            {
                SerializedProperty tagNameProperty = property.FindPropertyRelative(TagNamePath);
                AddTagName(tagNames, tagNameProperty.stringValue);
                return tagNames;
            }

            SerializedProperty tagsProperty = property.FindPropertyRelative(ContainerTagsPath);
            for (int i = 0; i < tagsProperty.arraySize; i++)
            {
                SerializedProperty element = tagsProperty.GetArrayElementAtIndex(i);
                SerializedProperty tagNameProperty = element.FindPropertyRelative(TagNamePath);
                AddTagName(tagNames, tagNameProperty.stringValue);
            }

            return tagNames;
        }

        private static void WriteTagNames(SerializedProperty property, bool isContainer, List<string> tagNames)
        {
            if (!isContainer)
            {
                SerializedProperty tagNameProperty = property.FindPropertyRelative(TagNamePath);
                tagNameProperty.stringValue = tagNames.Count > 0 ? tagNames[0] : string.Empty;
                return;
            }

            SerializedProperty tagsProperty = property.FindPropertyRelative(ContainerTagsPath);
            tagsProperty.arraySize = tagNames.Count;
            for (int i = 0; i < tagNames.Count; i++)
            {
                SerializedProperty element = tagsProperty.GetArrayElementAtIndex(i);
                SerializedProperty tagNameProperty = element.FindPropertyRelative(TagNamePath);
                tagNameProperty.stringValue = tagNames[i];
            }
        }

        private static void AddTagName(List<string> tagNames, string tagName)
        {
            if (!string.IsNullOrEmpty(tagName) && !tagNames.Contains(tagName))
            {
                tagNames.Add(tagName);
            }
        }

        private SerializedProperty FindProperty(int index)
        {
            Object target = _targets[index];
            if (target == null)
            {
                return null;
            }

            SerializedObject serializedObject = _serializedObjects[index];
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                serializedObject = new SerializedObject(target);
                _serializedObjects[index] = serializedObject;
            }

            serializedObject.UpdateIfRequiredOrScript();
            return serializedObject.FindProperty(_propertyPath);
        }
    }
}
