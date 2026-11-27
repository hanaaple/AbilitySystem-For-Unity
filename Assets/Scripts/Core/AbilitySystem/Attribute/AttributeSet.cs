using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.AbilitySystem.Effect;
using UnityEngine;

namespace Core.AbilitySystem.Attribute
{
    public abstract class AttributeSet
    {
        private static readonly Dictionary<Type, GameplayAttributeHandle[]> AttributesBySetType = new();
        private static readonly Dictionary<Type, FieldInfo[]> AttributeDataFieldsBySetType = new();

        private AbilitySystemComponent _owner;

        /// <summary>
        /// GE 실행(Instant·Periodic·Execution 출력)이 이 Set의 Base 값을 바꾸기 직전. <c>data.EvaluatedData</c>를 바꾸면 바꾼 수치로 적용되고, false를 돌려주면 이번 변경을 버린다.
        /// Duration/Infinite GE의 mod 등록·해제에는 불리지 않는다.
        /// </summary>
        /// <remarks>UE: UAttributeSet::PreGameplayEffectExecute</remarks>
        public virtual bool PreGameplayEffectExecute(GameplayEffectModCallbackData data)
        {
            return true;
        }

        /// <summary>GE 실행이 이 Set의 Base 값을 바꾼 직후 — meta attribute(Damage 등)를 실제 값에 반영하는 등의 후처리 자리. 여기서 <c>data</c>를 고쳐도 이미 적용된 변경은 그대로다.</summary>
        /// <remarks>UE: UAttributeSet::PostGameplayEffectExecute</remarks>
        public virtual void PostGameplayEffectExecute(GameplayEffectModCallbackData data)
        {
        }

        /// <summary>CurrentValue가 바뀌기 직전 — 이유(GE 실행·mod 등록·해제·Base 설정)를 가리지 않는다. <paramref name="newValue"/>를 고치면(클램프 등) 고친 값이 쓰이고 알려진다.</summary>
        /// <remarks>UE: UAttributeSet::PreAttributeChange</remarks>
        public virtual void PreAttributeChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
        }

        /// <summary>CurrentValue가 바뀐 직후.</summary>
        /// <remarks>UE: UAttributeSet::PostAttributeChange</remarks>
        public virtual void PostAttributeChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue)
        {
        }

        /// <summary>Base 값이 바뀌기 직전. <paramref name="newValue"/>를 고치면(클램프 등) 고친 값이 Base에 쓰인다.</summary>
        /// <remarks>UE: UAttributeSet::PreAttributeBaseChange</remarks>
        public virtual void PreAttributeBaseChange(GameplayAttributeHandle attributeHandle, ref float newValue)
        {
        }

        public virtual void PostAttributeBaseChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue)
        {
        }

        /// <summary>AttributeSet이 가진 어트리뷰트를 부모 Set 것까지 반환한다. 순서는 <see cref="GetAttributeDataFields"/>.</summary>
        /// <remarks>UE: UAttributeSet::GetAttributesFromSetClass</remarks>
        public static ReadOnlySpan<GameplayAttributeHandle> GetAttributesFromSetClass(Type attributeSetType)
        {
            // 타입별 캐시 배열을 복사 없이 내주므로 호출측이 고치지 못하게 읽기 전용으로 감싼다
            if (attributeSetType == null)
            {
                return ReadOnlySpan<GameplayAttributeHandle>.Empty;
            }

            if (!AttributesBySetType.TryGetValue(attributeSetType, out GameplayAttributeHandle[] handles))
            {
                // 핸들은 필드를 선언한 Set 기준이라, 어느 파생 Set에서 모아도 부모 필드는 같은 핸들이 된다
                handles = GetAttributeDataFields(attributeSetType)
                    .Select(field => GameplayAttributeHandle.TryCreate(field.DeclaringType, field.Name, out GameplayAttributeHandle handle) ? handle : default)
                    .Where(handle => handle.IsValid)
                    .ToArray();
                AttributesBySetType[attributeSetType] = handles;
            }

            return handles;
        }

        /// <summary>
        /// 어트리뷰트 필드(공개이거나 [SerializeField]인 AttributeData)를 부모 Set 것까지 모은다. 자식 Set 필드가 먼저, 같은 Set 안에선 선언 순서.
        /// 부모 어트리뷰트를 같은 이름의 필드로 가리면 에셋의 (Set 타입, 필드명)으로 부모 쪽을 가리킬 수 없어, 에러를 내고 가려진 필드를 뺀다.
        /// </summary>
        internal static FieldInfo[] GetAttributeDataFields(Type attributeSetType)
        {
            if (attributeSetType == null || !attributeSetType.IsSubclassOf(typeof(AttributeSet)))
            {
                return Array.Empty<FieldInfo>();
            }

            if (AttributeDataFieldsBySetType.TryGetValue(attributeSetType, out FieldInfo[] cached))
            {
                return cached;
            }

            var fields = new List<FieldInfo>();
            var descendantFieldNames = new HashSet<string>();
            for (Type type = attributeSetType; type != typeof(AttributeSet); type = type.BaseType)
            {
                // 부모 private 필드는 파생 타입의 GetFields에 안 나와 Set마다 DeclaredOnly로 모은다. MetadataToken은 같은 타입 안에서 선언 순서
                FieldInfo[] declaredFields = type
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .OrderBy(field => field.MetadataToken)
                    .ToArray();

                foreach (FieldInfo field in declaredFields)
                {
                    if (!IsAttributeDataField(field))
                    {
                        continue;
                    }

                    if (descendantFieldNames.Contains(field.Name))
                    {
                        Debug.LogError($"{attributeSetType.Name}: 파생 Set의 같은 이름 필드가 {type.Name}.{field.Name} 어트리뷰트를 가린다 — 가려진 필드는 어트리뷰트에서 빠진다");
                        continue;
                    }

                    fields.Add(field);
                }

                foreach (FieldInfo field in declaredFields)
                {
                    descendantFieldNames.Add(field.Name);
                }
            }

            FieldInfo[] result = fields.ToArray();
            AttributeDataFieldsBySetType[attributeSetType] = result;
            return result;
        }

        internal void SetOwner(AbilitySystemComponent abilitySystemComponent)
        {
            _owner = abilitySystemComponent;
        }

        public void SetBaseValue(GameplayAttributeHandle attributeHandle, float newValue)
        {
            _owner.SetAttributeBaseValue(attributeHandle, newValue);
        }

        private static bool IsAttributeDataField(FieldInfo field)
        {
            return field.FieldType == typeof(AttributeData) && (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null);
        }
    }
}
