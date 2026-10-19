using System;
using System.Collections.Generic;
using Core.Common;
using UnityEditor;

namespace Core.AbilitySystem.Tag.Editor
{
    /// <summary>
    /// 레지스트리 에셋에 태그를 추가·삭제·이름 변경하고, 태그가 어디서 정의됐는지(레지스트리·native) 알려 준다.
    /// 편집은 에셋에 바로 저장하고 에디터의 태그 트리를 다시 만든다. Undo에는 남기지 않는다 — Undo로 에셋만 되돌아가면 다시 만든 트리와 어긋나기 때문.
    /// </summary>
    internal static class GameplayTagRegistryEditing
    {
        public const string RegistrySourceName = "Registry";
        public const string NativeSourceName = "Native";

        // GameplayTagRegistryAsset._tags. 런타임 직렬화 필드 이름을 바꾸면 여기도 바꿔야 한다.
        private const string RegistryTagsPath = "_tags";

        private static HashSet<string> _registryTagNames;
        private static HashSet<string> _nativeTagNames;

        static GameplayTagRegistryEditing()
        {
            GameplayTagsManager.OnEditorRefreshGameplayTagTree += OnTagTreeRefreshed;
        }

        /// <summary>레지스트리에 명시된 태그인지(편집할 수 있는 태그).</summary>
        public static bool IsRegistryTag(string tagName)
        {
            if (_registryTagNames == null)
            {
                GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.Load();
                _registryTagNames = registry != null ? new HashSet<string>(registry.Tags) : new HashSet<string>();
            }

            return _registryTagNames.Contains(tagName);
        }

        /// <summary>코드(<see cref="NativeTagAttribute"/>)로 정의된 태그인지.</summary>
        public static bool IsNativeTag(string tagName)
        {
            if (_nativeTagNames == null)
            {
                _nativeTagNames = new HashSet<string>();
                foreach (NativeGameplayTag nativeTag in NativeGameplayTag.GetRegisteredNativeTags())
                {
                    InternedName nativeTagName = nativeTag.TagName;
                    _nativeTagNames.Add(nativeTagName.ToString());
                }
            }

            return _nativeTagNames.Contains(tagName);
        }

        /// <summary>명시적으로 정의된 태그인지. 중간 부모로만 생긴(암시) 태그는 false.</summary>
        /// <remarks>UE: UGameplayTagsManager::IsDictionaryTag</remarks>
        public static bool IsDictionaryTag(string tagName)
        {
            return IsRegistryTag(tagName) || IsNativeTag(tagName);
        }

        /// <summary>이 노드를 지나는 모든 태그 정의의 출처를 모은다. 암시 노드는 자손의 출처를 갖는다. native를 먼저 둔다(트리 구성 순서).</summary>
        /// <remarks>UE: FGameplayTagNode::GetAllSourceNames</remarks>
        public static List<string> GetAllSourceNames(GameplayTagNode node)
        {
            bool hasNative = false;
            bool hasRegistry = false;
            CollectSources(node, ref hasNative, ref hasRegistry);

            var sourceNames = new List<string>();
            if (hasNative)
            {
                sourceNames.Add(NativeSourceName);
            }

            if (hasRegistry)
            {
                sourceNames.Add(RegistrySourceName);
            }

            return sourceNames;
        }

        /// <summary>레지스트리가 첫 출처인 태그만 이름 변경·삭제할 수 있다. native가 섞인 태그는 코드 정의라 바꿀 수 없다.</summary>
        /// <remarks>UE: SGameplayTagPicker::CanModifyTag</remarks>
        public static bool CanModifyTag(GameplayTagNode node)
        {
            List<string> sourceNames = GetAllSourceNames(node);
            return sourceNames.Count > 0 && sourceNames[0] == RegistrySourceName;
        }

        /// <summary>레지스트리에 새 태그를 추가한다.</summary>
        /// <param name="outMessage">실패하면 사유, 성공하면 null.</param>
        /// <remarks>UE: IGameplayTagsEditorModule::AddNewGameplayTagToINI</remarks>
        public static bool AddNewGameplayTagToRegistry(string newTag, out string outMessage)
        {
            if (string.IsNullOrEmpty(newTag))
            {
                outMessage = "You must specify tag name.";
                return false;
            }

            GameplayTagsManager manager = GameplayTagsManager.Get();
            if (!manager.IsValidGameplayTagString(newTag, out string error, out string fixedString))
            {
                outMessage = $"Failed to add gameplay tag {newTag}: {error}, try {fixedString} instead!";
                return false;
            }

            // 암시 태그(A.B.C가 만든 A.B)는 명시로 더할 수 있다
            if (IsDictionaryTag(newTag))
            {
                outMessage = $"Failed to add gameplay tag {newTag}, already exists!";
                return false;
            }

            ModifyRegistry(tags =>
            {
                tags.Add(newTag);
                tags.Sort(GameplayTagEditorUtility.CompareTagNames);
            });

            manager.EditorRefreshGameplayTagTree();
            outMessage = null;
            return true;
        }

        /// <summary>레지스트리에서 태그를 지운다. 자식이 명시돼 있으면 노드는 암시 태그로 남는다. 참조 검사는 하지 않는다.</summary>
        /// <param name="outMessage">결과 알림.</param>
        /// <remarks>UE: IGameplayTagsEditorModule::DeleteTagFromINI</remarks>
        public static bool DeleteTagFromRegistry(GameplayTagNode tagNodeToDelete, out string outMessage)
        {
            string tagName = tagNodeToDelete.GetCompleteTagString();
            if (IsNativeTag(tagName))
            {
                outMessage = $"Cannot delete tag {tagName}, it is defined in code (native).";
                return false;
            }

            if (!IsRegistryTag(tagName))
            {
                outMessage = $"Cannot delete tag {tagName} as it is implicit, remove children manually";
                return false;
            }

            ModifyRegistry(tags => tags.Remove(tagName));

            GameplayTagsManager manager = GameplayTagsManager.Get();
            manager.EditorRefreshGameplayTagTree();
            outMessage = $"Deleted tag {tagName}";
            return true;
        }

        /// <summary>레지스트리의 태그 이름을 바꾼다. 자식 태그는 따라오지 않고, 기존 에셋에 저장된 옛 이름도 바뀌지 않는다(리다이렉터 없음).</summary>
        /// <param name="outMessage">결과 알림.</param>
        /// <remarks>UE: IGameplayTagsEditorModule::RenameTagInINI</remarks>
        public static bool RenameTagInRegistry(string tagToRename, string tagToRenameTo, out string outMessage)
        {
            GameplayTagsManager manager = GameplayTagsManager.Get();
            if (!manager.IsValidGameplayTagString(tagToRenameTo, out string error, out string fixedString))
            {
                outMessage = $"Failed to rename gameplay tag {tagToRename} to {tagToRenameTo}: {error}, try {fixedString} instead!";
                return false;
            }

            bool addNewTag = !IsDictionaryTag(tagToRenameTo);
            bool removeOldTag = IsRegistryTag(tagToRename);
            ModifyRegistry(tags =>
            {
                if (removeOldTag)
                {
                    tags.Remove(tagToRename);
                }

                if (addNewTag)
                {
                    tags.Add(tagToRenameTo);
                }

                tags.Sort(GameplayTagEditorUtility.CompareTagNames);
            });

            manager.EditorRefreshGameplayTagTree();

            outMessage = removeOldTag ? $"Renamed tag {tagToRename} to {tagToRenameTo}. Assets that reference {tagToRename} are not updated." : $"Added tag {tagToRenameTo}, but {tagToRename} was not removed as it has children";
            return true;
        }

        private static void CollectSources(GameplayTagNode node, ref bool hasNative, ref bool hasRegistry)
        {
            string tagName = node.GetCompleteTagString();
            hasNative |= IsNativeTag(tagName);
            hasRegistry |= IsRegistryTag(tagName);

            foreach (GameplayTagNode childNode in node.GetChildTagNodes())
            {
                CollectSources(childNode, ref hasNative, ref hasRegistry);
            }
        }

        private static void ModifyRegistry(Action<List<string>> modify)
        {
            GameplayTagRegistryAsset registry = GameplayTagRegistryAsset.LoadOrCreate();
            var serializedObject = new SerializedObject(registry);
            SerializedProperty tagsProperty = serializedObject.FindProperty(RegistryTagsPath);

            var tags = new List<string>(tagsProperty.arraySize);
            for (int i = 0; i < tagsProperty.arraySize; i++)
            {
                SerializedProperty element = tagsProperty.GetArrayElementAtIndex(i);
                tags.Add(element.stringValue);
            }

            modify(tags);

            tagsProperty.arraySize = tags.Count;
            for (int i = 0; i < tags.Count; i++)
            {
                SerializedProperty element = tagsProperty.GetArrayElementAtIndex(i);
                element.stringValue = tags[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.Dispose();

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssetIfDirty(registry);
            _registryTagNames = null;
        }

        private static void OnTagTreeRefreshed()
        {
            _registryTagNames = null;
            _nativeTagNames = null;
        }
    }
}
