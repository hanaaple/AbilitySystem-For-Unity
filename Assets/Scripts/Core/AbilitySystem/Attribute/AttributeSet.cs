using Core.AbilitySystem.Effect;

namespace Core.AbilitySystem.Attribute
{
    public abstract class AttributeSet
    {
        private AbilitySystemComponent owner;

        internal void SetOwner(AbilitySystemComponent abilitySystemComponent)
        {
            this.owner = abilitySystemComponent;
        }


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
        public virtual void PostGameplayEffectExecute(GameplayEffectModCallbackData data) { }

        /// <summary>CurrentValue가 바뀌기 직전 — 이유(GE 실행·mod 등록·해제·Base 설정)를 가리지 않는다. <paramref name="newValue"/>를 고치면(클램프 등) 고친 값이 쓰이고 알려진다.</summary>
        /// <remarks>UE: UAttributeSet::PreAttributeChange</remarks>
        public virtual void PreAttributeChange(GameplayAttributeHandle attributeHandle, ref float newValue) { }

        /// <summary>CurrentValue가 바뀐 직후.</summary>
        /// <remarks>UE: UAttributeSet::PostAttributeChange</remarks>
        public virtual void PostAttributeChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue) { }

        /// <summary>Base 값이 바뀌기 직전. <paramref name="newValue"/>를 고치면(클램프 등) 고친 값이 Base에 쓰인다.</summary>
        /// <remarks>UE: UAttributeSet::PreAttributeBaseChange</remarks>
        public virtual void PreAttributeBaseChange(GameplayAttributeHandle attributeHandle, ref float newValue) { }

        public virtual void PostAttributeBaseChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue) { }

        public void SetBaseValue(GameplayAttributeHandle attributeHandle, float newValue)
        {
            owner.SetAttributeBaseValue(attributeHandle, newValue);
        }
    }
}
