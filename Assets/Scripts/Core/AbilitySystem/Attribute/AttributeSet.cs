namespace Core.AbilitySystem.Attribute
{
    public abstract class AttributeSet
    {
        private AbilitySystemComponent owner;

        internal void SetOwner(AbilitySystemComponent abilitySystemComponent)
        {
            this.owner = abilitySystemComponent;
        }


        public virtual void PreAttributeBaseChange(GameplayAttributeHandle attributeHandle, float newValue) { }

        public virtual void PostAttributeBaseChange(GameplayAttributeHandle attributeHandle, float oldValue, float newValue) { }

        public void SetBaseValue(GameplayAttributeHandle attributeHandle, float newValue)
        {
            owner.SetAttributeBaseValue(attributeHandle, newValue);
        }
    }
}
