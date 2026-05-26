using System;
using System.Reflection;

namespace Core.AbilitySystem.Attribute
{
    /// <summary>
    /// AttributeSet의 특정 필드를 가리키는 불변 식별자.
    /// 정적 초기화 시 FieldInfo를 캐싱하므로 런타임에 string 탐색 없음.
    /// </summary>
    public readonly struct AttributeHandle : IEquatable<AttributeHandle>
    {
        public readonly Type SetType;
        public readonly string Name;

        private readonly FieldInfo _field;

        public bool IsValid => _field != null;

        public AttributeHandle(Type setType, string fieldName)
        {
            SetType = setType;
            Name = fieldName;
            _field = setType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        }

        internal bool TryGetData(AttributeSet set, out AttributeData data)
        {
            if (_field == null || set.GetType() != SetType)
            {
                data = default;
                return false;
            }
            data = (AttributeData)_field.GetValue(set);
            return true;
        }

        internal bool TrySetData(AttributeSet set, AttributeData data)
        {
            if (_field == null || set.GetType() != SetType)
                return false;
            _field.SetValue(set, data);
            return true;
        }

        public bool Equals(AttributeHandle other) => SetType == other.SetType && Name == other.Name;
        public override bool Equals(object obj) => obj is AttributeHandle h && Equals(h);
        public override int GetHashCode() => HashCode.Combine(SetType, Name);
        public override string ToString() => $"{SetType.Name}.{Name}";
    }
}
