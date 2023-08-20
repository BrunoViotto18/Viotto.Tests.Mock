using FluentAssertions;

namespace Viotto.Tests.Mock.UnitTest;


public class MockTests
{
    [Fact]
    public void Create()
    {
        var test = Create<ITest>.Mock();

        test.Log("Teste");
        test.Sum(1, 1).Should().Be(2);
    }
}
