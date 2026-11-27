namespace Test.Editor.FieldOffset
{
    // 아무 객체 참조를 이 타입으로 재해석해(UnsafeUtility.As) &Data로 그 객체의 데이터 시작 주소를 fixed로 고정한다.
    // 필드가 하나뿐이라 Data가 곧 데이터 시작 위치다.
    internal sealed class RawData
    {
        public byte Data;
    }
}
