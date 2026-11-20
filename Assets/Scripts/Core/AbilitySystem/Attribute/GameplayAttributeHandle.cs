using System;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;

namespace Core.AbilitySystem.Attribute
{
    // 검토 (2026-11-14)
    /// <summary>
    /// AttributeSet의 AttributeData 필드 런타임 핸들. 직렬화·저작은 <see cref="GameplayAttribute"/>가 맡는다.
    /// 검증을 통과해야만 만들어지므로 무효 핸들은 <c>default</c>뿐이다.
    /// </summary>
    public readonly struct GameplayAttributeHandle : IEquatable<GameplayAttributeHandle>
    {
        // Field Offset (Header 제외)
        private readonly int _dataOffset;

        public Type AttributeSetType { get; }
        public string FieldName { get; }
        public bool IsValid { get; }

        private GameplayAttributeHandle(FieldInfo field)
        {
            _dataOffset = UnsafeUtility.GetFieldOffset(field) - RawData.DataOffset;
            AttributeSetType = field.DeclaringType;
            FieldName = field.Name;
            IsValid = true;
        }

        public override bool Equals(object obj)
        {
            return obj is GameplayAttributeHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AttributeSetType, FieldName);
        }

        public override string ToString()
        {
            return IsValid ? $"{AttributeSetType.Name}.{FieldName}" : "(invalid)";
        }

        public bool Equals(GameplayAttributeHandle other)
        {
            return AttributeSetType == other.AttributeSetType && FieldName == other.FieldName;
        }

        public static bool operator ==(GameplayAttributeHandle left, GameplayAttributeHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameplayAttributeHandle left, GameplayAttributeHandle right)
        {
            return !left.Equals(right);
        }

        public static GameplayAttributeHandle Create<T>(string fieldName) where T : AttributeSet
        {
            FieldInfo field = FindAttributeField(typeof(T), fieldName);
            if (field == null)
            {
                throw new ArgumentException($"{typeof(T).Name}.{fieldName}: AttributeData 인스턴스 필드가 아니다");
            }

            return new GameplayAttributeHandle(field);
        }

        public static bool TryCreate(Type attributeSetType, string fieldName, out GameplayAttributeHandle handle)
        {
            FieldInfo field = FindAttributeField(attributeSetType, fieldName);
            handle = field != null ? new GameplayAttributeHandle(field) : default;
            return field != null;
        }

        internal bool CanAccess(AttributeSet attributeSet)
        {
            return IsValid && attributeSet != null && attributeSet.GetType() == AttributeSetType;
        }

        internal float GetBaseValue(AttributeSet attributeSet)
        {
            return CanAccess(attributeSet) ? ReadBaseValue(attributeSet, _dataOffset) : 0f;
        }

        internal float GetCurrentValue(AttributeSet attributeSet)
        {
            return CanAccess(attributeSet) ? ReadCurrentValue(attributeSet, _dataOffset) : 0f;
        }

        internal bool SetBaseValueRaw(AttributeSet attributeSet, float newValue)
        {
            if (!CanAccess(attributeSet))
            {
                return false;
            }

            WriteBaseValue(attributeSet, _dataOffset, newValue);
            return true;
        }

        internal bool SetCurrentValueRaw(AttributeSet attributeSet, float newValue)
        {
            if (!CanAccess(attributeSet))
            {
                return false;
            }

            WriteCurrentValue(attributeSet, _dataOffset, newValue);
            return true;
        }

        private static FieldInfo FindAttributeField(Type attributeSetType, string fieldName)
        {
            if (attributeSetType == null || !attributeSetType.IsSubclassOf(typeof(AttributeSet)) || attributeSetType.ContainsGenericParameters || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            for (Type type = attributeSetType; type != typeof(AttributeSet); type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field.FieldType == typeof(AttributeData) ? field : null;
                }
            }

            return null;
        }

        private static unsafe float ReadBaseValue(AttributeSet attributeSet, int dataOffset)
        {
            RawData raw = UnsafeUtility.As<AttributeSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                return ((AttributeData*)(dataStart + dataOffset))->GetBaseValue();
            }
        }

        private static unsafe float ReadCurrentValue(AttributeSet attributeSet, int dataOffset)
        {
            RawData raw = UnsafeUtility.As<AttributeSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                return ((AttributeData*)(dataStart + dataOffset))->GetCurrentValue();
            }
        }

        private static unsafe void WriteBaseValue(AttributeSet attributeSet, int dataOffset, float newValue)
        {
            RawData raw = UnsafeUtility.As<AttributeSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                ((AttributeData*)(dataStart + dataOffset))->SetBaseValue(newValue);
            }
        }

        private static unsafe void WriteCurrentValue(AttributeSet attributeSet, int dataOffset, float newValue)
        {
            RawData raw = UnsafeUtility.As<AttributeSet, RawData>(ref attributeSet);
            fixed (byte* dataStart = &raw.Data)
            {
                ((AttributeData*)(dataStart + dataOffset))->SetCurrentValue(newValue);
            }
        }

        private sealed class RawData
        {
            public static readonly int DataOffset = UnsafeUtility.GetFieldOffset(typeof(RawData).GetField(nameof(Data)));

            public byte Data;
        }
    }
}
