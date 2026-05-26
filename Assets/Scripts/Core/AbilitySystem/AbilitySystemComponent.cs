using System;
using System.Collections.Generic;
using System.Reflection;
using Core.AbilitySystem.Attribute;
using UnityEngine;

namespace Core.AbilitySystem
{
    public class AbilitySystemComponent : MonoBehaviour
    {
        [SerializeField] private AttributeInitData attributeInitData;

        private readonly Dictionary<Type, AttributeSet> _spawnedAttributeSets = new();
        private readonly Dictionary<int, AttributeEffect> _activeEffects = new();

        private void Awake()
        {
            InitAttributeSets();
        }

        private void InitAttributeSets()
        {
            if (attributeInitData == null)
            {
                return;
            }

            foreach (AttributeSetInitData attributeSetData in attributeInitData.AttributeSets)
            {
                Type attributeSetType = attributeSetData.GetAttributeSetType();
                if (attributeSetType == null || !typeof(AttributeSet).IsAssignableFrom(attributeSetType))
                {
                    Debug.LogWarning($"[ASC] '{attributeSetData.GetType().Name}' 에서 유효하지 않은 AttributeSet 타입.");
                    continue;
                }

                var set = (AttributeSet)Activator.CreateInstance(attributeSetType);
                foreach (AttributeFieldInitData fieldData in attributeSetData.Attributes)
                {
                    FieldInfo field = attributeSetType.GetField(fieldData.FieldName, BindingFlags.Public | BindingFlags.Instance);
                    if (field == null)
                    {
                        Debug.LogWarning($"[ASC] 필드 '{fieldData.FieldName}'을 {attributeSetType.Name}에서 찾을 수 없음.");
                        continue;
                    }
                    field.SetValue(set, fieldData.Data);
                }

                AddAttributeSet(set);
            }
        }

        /// <summary>같은 타입의 AttributeSet은 하나만 등록 가능.</summary>
        public bool AddAttributeSet(AttributeSet set)
        {
            return _spawnedAttributeSets.TryAdd(set.GetType(), set);
        }

        public void RemoveAttributeSet(AttributeSet set)
        {
            _spawnedAttributeSets.Remove(set.GetType());
        }

        /// <summary>핸들이 가리키는 어트리뷰트의 BaseValue를 반환.</summary>
        public float GetAttributeBaseValue(AttributeHandle handle)
        {
            return TryGetAttributeData(handle, out AttributeData data) ? data.BaseValue : 0f;
        }

        /// <summary>핸들이 가리키는 어트리뷰트의 CurrentValue를 반환.</summary>
        public float GetAttributeCurrentValue(AttributeHandle handle)
        {
            return TryGetAttributeData(handle, out AttributeData data) ? data.CurrentValue : 0f;
        }

        /// <summary>핸들이 가리키는 어트리뷰트의 BaseValue를 설정.</summary>
        public void SetBaseAttributeValue(AttributeHandle handle, float value)
        {
            if (!TryGetAttributeData(handle, out AttributeData data))
            {
                return;
            }

            data.BaseValue = value;
            TrySetAttributeData(handle, data);
        }

        private bool TryGetAttributeData(AttributeHandle handle, out AttributeData data)
        {
            if (_spawnedAttributeSets.TryGetValue(handle.SetType, out AttributeSet set))
            {
                return handle.TryGetData(set, out data);
            }

            data = default;
            return false;
        }

        private bool TrySetAttributeData(AttributeHandle handle, AttributeData data)
        {
            if (_spawnedAttributeSets.TryGetValue(handle.SetType, out AttributeSet set))
            {
                return handle.TrySetData(set, data);
            }

            return false;
        }
    }
}
