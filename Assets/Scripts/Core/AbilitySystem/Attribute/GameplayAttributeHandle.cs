using System;
using System.Reflection;

namespace Core.AbilitySystem.Attribute
{
    /// <summary>AttributeSet의 특정 필드를 FieldInfo로 해석해 든 런타임 핸들. 직렬화·저작은 <see cref="GameplayAttribute"/>가 맡는다.</summary>
    public readonly struct GameplayAttributeHandle : IEquatable<GameplayAttributeHandle>
    {
        public readonly Type SetType;
        public readonly string Name;

        private readonly FieldInfo _field;

        public bool IsValid => _field != null;

        public GameplayAttributeHandle(Type setType, string fieldName)
        {
            SetType = setType;
            Name = fieldName;
            _field = setType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        }

        public bool Equals(GameplayAttributeHandle other)
        {
            return SetType == other.SetType && Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAttributeHandle h && Equals(h);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SetType, Name);
        }

        public static bool operator ==(GameplayAttributeHandle left, GameplayAttributeHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayAttributeHandle left, GameplayAttributeHandle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{SetType.Name}.{Name}";
        }

        internal bool TryGetData(AttributeSet set, out AttributeData data)
        {
            if (set == null || _field == null || set.GetType() != SetType)
            {
                data = default;
                return false;
            }
            data = (AttributeData)_field.GetValue(set);
            return true;
        }

        internal float GetBaseValue(AttributeSet set)
        {
            return TryGetData(set, out AttributeData data) ? data.GetBaseValue() : 0f;
        }

        internal float GetCurrentValue(AttributeSet set)
        {
            return TryGetData(set, out AttributeData data) ? data.GetCurrentValue() : 0f;
        }

        internal bool SetBaseValueRaw(AttributeSet set, float newValue)
        {
            if (!TryGetData(set, out AttributeData data))
            {
                return false;
            }

            data.SetBaseValue(newValue);

            return TrySetData(set, data);
        }

        internal bool SetCurrentValueRaw(AttributeSet set, float value)
        {
            if (!TryGetData(set, out AttributeData data))
            {
                return false;
            }

            data.SetCurrentValue(value);

            return TrySetData(set, data);
        }

        private bool TrySetData(AttributeSet set, AttributeData data)
        {
            if (set == null || _field == null || set.GetType() != SetType)
            {
                return false;
            }

            _field.SetValue(set, data);
            return true;
        }
    }
}
