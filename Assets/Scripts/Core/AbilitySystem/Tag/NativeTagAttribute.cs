using System;

namespace Core.AbilitySystem.Tag
{
    /// <summary>static <see cref="NativeGameplayTag"/> 필드에 붙여 native 태그로 등록한다. 매니저가 트리 구성 시 이 마커로 정의를 찾는다.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NativeTagAttribute : System.Attribute
    {
    }
}
