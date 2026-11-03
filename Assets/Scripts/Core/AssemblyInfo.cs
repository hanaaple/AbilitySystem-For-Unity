using System.Runtime.CompilerServices;

// internal은 에디터 확장과 테스트에만 연다. Game은 공개 API만 쓰는 경계를 유지한다.
[assembly: InternalsVisibleTo("Core.Editor")]
[assembly: InternalsVisibleTo("Core.Tests.EditMode")]
[assembly: InternalsVisibleTo("Core.Tests.PlayMode")]
