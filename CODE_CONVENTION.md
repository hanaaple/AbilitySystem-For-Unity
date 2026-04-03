## Code Convention

- PascalCase
  - 파일명 `Foo.cs`
  - 함수명 `void Foo()`
  - 구조체 / 클래스명 `class Foo`
  - 정적 변수 `static int FooVariable`
- ALL_CAPS
  - 상수 `const int MAX_COUNT = 4`
- camelCase
  - 지역 변수, 매개변수 `int enemyCount`
- _case
  - private 멤버 변수
- 들여쓰기
  - 스페이스 4칸 (탭 X)
- 중괄호
  - 생략 금지
  - BSD 스타일
````
// X
if (condition) return true;
if (condition)
    return true;
if (condition) {
    return true;
}

// O
if (condition)
{
    return true;
}
````