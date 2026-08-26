namespace Core.AbilitySystem.Attribute
{
    public struct AttributeData
    {
        private float _baseValue;
        private float _currentValue;

        public AttributeData(float baseValue, float currentValue)
        {
            _baseValue = baseValue;
            _currentValue = currentValue;
        }

        public float GetCurrentValue()
        {
            return _currentValue;
        }

        public void SetCurrentValue(float newValue)
        {
            _currentValue = newValue;
        }

        public float GetBaseValue()
        {
            return _baseValue;
        }

        public void SetBaseValue(float newValue)
        {
            _baseValue = newValue;
        }
    }
}
