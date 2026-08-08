using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Core.Common.Editor
{
    /// <summary>
    /// New Script로 만든 타입을, 컴파일·도메인 리로드가 끝난 뒤 원래 위치에 배정하는 <b>내부 로직</b>.
    /// 리로드로 팝업·SerializedProperty가 모두 무효화되므로, 배정에 필요한 정보는 SessionState에 저장해 둔다.
    /// 대상 오브젝트는 리로드를 넘겨도 유효한 GlobalObjectId로 식별한다(직렬화 참조는 리로드 후 무효).
    ///
    /// 두 모드:
    ///  - Field: 지정한 propertyPath 문자열 필드에 AQN을 넣는다(기존 요소의 타입 변경).
    ///  - Add: 배열에 요소를 추가하고, 그 요소의 typeRelPath에 AQN·clearArrayRelPath 배열을 비운다(새 요소 추가).
    ///
    /// 진입점 파사드는 <see cref="NewSubclassScript"/>. 호출부는 이 클래스를 직접 다루지 않는다.
    /// </summary>
    internal static class PendingSubclassAssignment
    {
        private const string KeyMode = "SubclassNewScript.Mode";
        private const string KeyBase = "SubclassNewScript.BaseType";
        private const string KeyType = "SubclassNewScript.TypeName";
        private const string KeyObject = "SubclassNewScript.ObjectId";
        private const string KeyScript = "SubclassNewScript.ScriptPath";
        private const string KeyPath = "SubclassNewScript.PropertyPath";      // Field 모드: 대상 문자열 필드
        private const string KeyArrayPath = "SubclassNewScript.ArrayPath";    // Add 모드: 대상 배열
        private const string KeyTypeRel = "SubclassNewScript.TypeRelPath";    // Add 모드: 새 요소 안 typeName 상대경로
        private const string KeyClearRel = "SubclassNewScript.ClearRelPath";  // Add 모드: 새 요소 안 비울 배열 상대경로(선택)

        private const string ModeField = "field";
        private const string ModeAdd = "add";

        // 기존 요소(문자열 필드)에 새 타입을 배정.
        public static void StashFieldAssign(Type baseType, string typeName, UnityEngine.Object target, string propertyPath, string scriptPath)
        {
            StashCommon(ModeField, baseType, typeName, target, scriptPath);
            SessionState.SetString(KeyPath, propertyPath);
        }

        // 배열에 새 요소를 추가하고 그 요소에 새 타입을 배정.
        public static void StashListAdd(Type baseType, string typeName, UnityEngine.Object target, string arrayPath, string typeRelPath, string clearArrayRelPath, string scriptPath)
        {
            StashCommon(ModeAdd, baseType, typeName, target, scriptPath);
            SessionState.SetString(KeyArrayPath, arrayPath);
            SessionState.SetString(KeyTypeRel, typeRelPath);
            SessionState.SetString(KeyClearRel, clearArrayRelPath ?? string.Empty);
        }

        private static void StashCommon(string mode, Type baseType, string typeName, UnityEngine.Object target, string scriptPath)
        {
            SessionState.SetString(KeyMode, mode);
            SessionState.SetString(KeyBase, baseType.AssemblyQualifiedName);
            SessionState.SetString(KeyType, typeName);
            SessionState.SetString(KeyObject, GlobalObjectId.GetGlobalObjectIdSlow(target).ToString());
            SessionState.SetString(KeyScript, scriptPath);
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            string mode = SessionState.GetString(KeyMode, string.Empty);
            if (string.IsNullOrEmpty(mode))
            {
                return;
            }

            string baseAqn = SessionState.GetString(KeyBase, string.Empty);
            string typeName = SessionState.GetString(KeyType, string.Empty);
            string objectId = SessionState.GetString(KeyObject, string.Empty);
            string scriptPath = SessionState.GetString(KeyScript, string.Empty);
            string propertyPath = SessionState.GetString(KeyPath, string.Empty);
            string arrayPath = SessionState.GetString(KeyArrayPath, string.Empty);
            string typeRel = SessionState.GetString(KeyTypeRel, string.Empty);
            string clearRel = SessionState.GetString(KeyClearRel, string.Empty);

            // 한 번만 시도하도록 즉시 비운다(재시도 루프·이후 리로드에서의 오적용 방지).
            foreach (string key in new[] { KeyMode, KeyBase, KeyType, KeyObject, KeyScript, KeyPath, KeyArrayPath, KeyTypeRel, KeyClearRel })
            {
                SessionState.EraseString(key);
            }

            // 에셋 DB·TypeCache가 완전히 준비된 다음 프레임에 배정한다.
            EditorApplication.delayCall += () => Assign(mode, baseAqn, typeName, objectId, scriptPath, propertyPath, arrayPath, typeRel, clearRel);
        }

        private static void Assign(string mode, string baseAqn, string typeName, string objectId, string scriptPath,
            string propertyPath, string arrayPath, string typeRel, string clearRel)
        {
            Type baseType = Type.GetType(baseAqn);
            if (baseType == null)
            {
                return;
            }

            Type created = EditorTypeUtility.GetConcreteSubclasses(baseType).FirstOrDefault(type => type.Name == typeName);
            if (created == null)
            {
                Debug.LogWarning($"[New Script] '{typeName}' 컴파일 후 타입을 찾지 못해 자동 배정을 건너뜁니다. 드롭다운에서 직접 선택하세요.");
                return;
            }

            if (!GlobalObjectId.TryParse(objectId, out GlobalObjectId gid))
            {
                return;
            }

            UnityEngine.Object target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            if (target == null)
            {
                return;
            }

            var serializedObject = new SerializedObject(target);

            if (mode == ModeField)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    return;
                }

                property.stringValue = created.AssemblyQualifiedName;
            }
            else // ModeAdd
            {
                SerializedProperty array = serializedObject.FindProperty(arrayPath);
                if (array == null || !array.isArray)
                {
                    return;
                }

                int index = array.arraySize;
                array.InsertArrayElementAtIndex(index);
                SerializedProperty element = array.GetArrayElementAtIndex(index);

                element.FindPropertyRelative(typeRel).stringValue = created.AssemblyQualifiedName;

                if (!string.IsNullOrEmpty(clearRel))
                {
                    SerializedProperty clearArray = element.FindPropertyRelative(clearRel);
                    if (clearArray != null && clearArray.isArray)
                    {
                        clearArray.ClearArray();
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script != null)
            {
                EditorGUIUtility.PingObject(script);
            }
        }
    }
}
