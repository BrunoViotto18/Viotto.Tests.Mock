namespace Viotto.Tests.Mock;


public static class Create<T>
{
    public static T Mock()
    {
        return AssemblyHelper.CreateMockedType<T>();
    }
}

/*  
*  ** Possible syntaxes **
*  
*  DbContext _sut = Create<DbContext>.Mock();
*  
*  _sut.SaveChanges().Returns(2)
*  _sut.SaveChanges().Returns(2).When(1, 1)
*  _sut.SaveChanges().Returns(2.When(1, 1))
*  _sut.SaveChanges().Returns(2.When(1, 1), 3.When(1, 2), 5.When(3, 2), -1.Otherwise())
*  _sut.SaveChanges().Returns(-1.ByDefault(), 2.When(1, 1), 3.When(1, 2), 5.When(3, 2))
*  _sut.SaveChanges().Returns(2.When((a, b) => a >= 0 && b >= 0))
*  
*  _sut.SaveChanges().Returns(2)
*  _sut.SaveChanges().Returns(2.WithArgs(1, 1), );
*  
*  _sut.SaveChanges().WasCalled().AtLeast(2);
*/
