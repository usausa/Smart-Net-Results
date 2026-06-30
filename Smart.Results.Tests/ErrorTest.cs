namespace Smart.Results;

public sealed class ErrorTest
{
    //--------------------------------------------------------------------------------
    // Error
    //--------------------------------------------------------------------------------

    [Fact]
    public void MessageIsStored()
    {
        // Arrange
        var error = new Error("test");

        // Act & Assert
        Assert.Equal("test", error.Message);
    }

    [Fact]
    public void ToStringReturnsMessage()
    {
        // Arrange
        var error = new Error("test");

        // Act & Assert
        Assert.Equal("test", error.ToString());
    }

    [Fact]
    public void EqualityIsByValue()
    {
        // Act & Assert
        Assert.Equal(new Error("test"), new Error("test"));
        Assert.NotEqual(new Error("test"), new Error("other"));
    }

    [Fact]
    public void ImplicitConversionFromString()
    {
        // Act
        Error error = "test";

        // Assert
        Assert.Equal("test", error.Message);
    }

    [Fact]
    public void ImplicitConversionFromException()
    {
        // Arrange
        var exception = new InvalidOperationException("oops");

        // Act
        Error error = exception;

        // Assert
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
        // Arrange
        var error1 = new Error("error1");
        var error2 = new Error("error2");

        // Act
        var aggregateError = new AggregateError([error1, error2]);

        // Assert
        Assert.Equal(2, aggregateError.Errors.Count);
        Assert.Same(error1, aggregateError.Errors[0]);
        Assert.Same(error2, aggregateError.Errors[1]);
        Assert.Equal("error1", aggregateError.Message);
    }

    [Fact]
    public void AggregateErrorMessageIsDefaultWhenEmpty()
    {
        // Act
        var aggregateError = new AggregateError([]);

        // Assert
        Assert.Equal("Error.", aggregateError.Message);
    }

    [Fact]
    public void AggregateErrorCopiesSourceList()
    {
        // Arrange
        var error1 = new Error("error1");
        var list = new List<Error> { error1 };

        // Act
        var aggregateError = new AggregateError(list);
        list.Add(new Error("error2"));
        list[0] = new Error("changed");

        // Assert
        Assert.Single(aggregateError.Errors);
        Assert.Same(error1, aggregateError.Errors[0]);
        Assert.Equal("error1", aggregateError.Message);
    }
}
