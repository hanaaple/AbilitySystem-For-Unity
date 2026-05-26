namespace Core.AbilitySystem.Attribute
{
    public struct AttributeData
    {
        public float BaseValue;
        public float CurrentValue;

        public AttributeData(float baseValue, float currentValue)
        {
            BaseValue = baseValue;
            CurrentValue = currentValue;
        }
    }
}
