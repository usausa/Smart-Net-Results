namespace Smart.Results;

public sealed class ErrorTest
{
    //--------------------------------------------------------------------------------
    // Error
    //--------------------------------------------------------------------------------

    [Fact]
    public void MessageIsStored()
    {
        var error = new Error("test");

        Assert.Equal("test", error.Message);
    }

    [Fact]
    public void ToStringReturnsMessage()
    {
        var error = new Error("test");

        Assert.Equal("test", error.ToString());
    }

    [Fact]
    public void EqualityIsByValue()
    {
        Assert.Equal(new Error("test"), new Error("test"));
        Assert.NotEqual(new Error("test"), new Error("other"));
    }

    [Fact]
    public void ImplicitConversionFromString()
    {
        Error error = "test";

        Assert.Equal("test", error.Message);
    }

    [Fact]
    public void ImplicitConversionFromException()
    {
        var exception = new InvalidOperationException("oops");
        Error error = exception;

        var exceptionError = Assert.IsType<ExceptionError>(error);
        Assert.Same(exception, exceptionError.Exception);
        Assert.Equal("oops", exceptionError.Message);
    }

    //--------------------------------------------------------------------------------
    // AggregateError
    //--------------------------------------------------------------------------------

    [Fact]
    public void AggregateErrorHoldsErrors()
    {
        var error1 = new Error("error1");
        var error2 = new Error("error2");
        var aggregateError = new AggregateError([error1, error2]);

        Assert.Equal(2, aggregateError.Errors.Count);
        Assert.Same(error1, aggregateError.Errors[0]);
        Assert.Same(error2, aggregateError.Errors[1]);
        Assert.Equal("error1", aggregateError.Message);
    }

    [Fact]
    public void AggregateErrorMessageIsDefaultWhenEmpty()
    {
        var aggregateError = new AggregateError([]);

        Assert.Equal("An error has occurred.", aggregateError.Message);
    }
}
