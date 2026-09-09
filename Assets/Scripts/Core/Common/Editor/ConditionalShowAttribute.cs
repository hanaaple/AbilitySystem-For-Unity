using System;
using UnityEngine;

namespace Core.Common.Editor
{
    public enum ConditionalOp
    {
        Equal,
        NotEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
    }

    /// <summary>같은 클래스 내 다른 필드 값에 따라 인스펙터에서 필드를 표시하거나 숨긴다.</summary>
    /// <remarks>
    /// bool  : [ConditionalShow("_flag")]
    ///         [ConditionalShow("_flag", false)]
    /// enum  : [ConditionalShow("_type", MyEnum.Value)]
    /// 숫자  : [ConditionalShow("_period", ConditionalOp.Greater, 0f)]
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class ConditionalShowAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; }
        public object CompareValue { get; }
        public ConditionalOp Op { get; }
        public bool UseOperator { get; }

        /// <summary>bool 필드용.</summary>
        public ConditionalShowAttribute(string conditionFieldName, bool expectedValue = true)
        {
            ConditionFieldName = conditionFieldName;
            CompareValue = expectedValue;
        }

        /// <summary>enum 동등 비교용.</summary>
        public ConditionalShowAttribute(string conditionFieldName, object enumValue)
        {
            ConditionFieldName = conditionFieldName;
            CompareValue = enumValue;
        }

        /// <summary>숫자 비교용. int·float 필드에 ConditionalOp으로 조건을 지정한다.</summary>
        public ConditionalShowAttribute(string conditionFieldName, ConditionalOp op, float compareValue)
        {
            ConditionFieldName = conditionFieldName;
            Op = op;
            CompareValue = compareValue;
            UseOperator = true;
        }
    }
}
