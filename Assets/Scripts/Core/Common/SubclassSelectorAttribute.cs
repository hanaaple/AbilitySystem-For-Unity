using System;
using UnityEngine;

namespace Core.Common
{
    /// <summary>AQN(AssemblyQualifiedName) 문자열 필드에 붙여 <see cref="BaseType"/>의 구체 서브클래스를 드롭다운으로 고르게 하는 속성(None=빈 문자열). 자주 쓰는 베이스는 이 속성을 상속한 별칭 속성으로 감쌀 수 있다(드로어는 useForChildren로 공용).</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SubclassSelectorAttribute : PropertyAttribute
    {
        public Type BaseType { get; }

        public SubclassSelectorAttribute(Type baseType)
        {
            BaseType = baseType;
        }
    }
}
