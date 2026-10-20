namespace Core.AbilitySystem.Attribute
{
    public abstract class AttributeSet
    {
        private AbilitySystemComponent owner;

        internal void SetOwner(AbilitySystemComponent abilitySystemComponent)
        {
            this.owner = abilitySystemComponent;
        }


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
