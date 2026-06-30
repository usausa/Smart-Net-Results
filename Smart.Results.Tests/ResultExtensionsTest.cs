namespace Smart.Results;

public sealed class ResultExtensionsTest
{
    //--------------------------------------------------------------------------------
    // ToResult
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToResultClassReturnsSuccessWhenNotNull()
    {
        // Arrange
        const string value = "test";

        // Act
        var result = value.ToResult(new Error("null"));

        // Assert
        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void ToResultClassReturnsFailureWhenNull()
    {
        // Arrange
        var error = new Error("null");
        string? value = null;

        // Act
        var result = value.ToResult(error);

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ToResultStructReturnsSuccessWhenHasValue()
    {
        // Arrange
        int? value = 123;

        // Act
        var result = value.ToResult(new Error("null"));

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void ToResultStructReturnsFailureWhenNull()
    {
        // Arrange
        var error = new Error("null");
        int? value = null;

        // Act
        var result = value.ToResult(error);

        // Assert
        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Combine
    //--------------------------------------------------------------------------------

    [Fact]
    public void CombineReturnsSuccessWhenAllSuccess()
    {
        // Arrange
        var results = new[] { Result.Success(), Result.Success() };

        // Act & Assert
        Assert.True(results.Combine().IsSuccess);
    }

    [Fact]
    public void CombineReturnsSuccessWhenEmpty()
    {
        // Act & Assert
        Assert.True(Array.Empty<Result>().Combine().IsSuccess);
    }

    [Fact]
    public void CombineCollectsErrorsWhenFailure()
    {
        // Arrange
        var error1 = new Error("error1");
        var error2 = new Error("error2");
        var results = new[] { Result.Failure(error1), Result.Success(), Result.Failure(error2) };

        // Act
        var combined = results.Combine();

        // Assert
        var aggregateError = Assert.IsType<AggregateError>(combined.Error);
        Assert.Equal(2, aggregateError.Errors.Count);
        Assert.Same(error1, aggregateError.Errors[0]);
        Assert.Same(error2, aggregateError.Errors[1]);
    }

    [Fact]
    public void CombineOfTReturnsValuesWhenAllSuccess()
    {
        // Arrange
        var results = new[] { Result.Success(1), Result.Success(2), Result.Success(3) };

        // Act
        var combined = results.Combine();

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, combined.Value);
    }

    [Fact]
    public void CombineOfTCollectsErrorsWhenFailure()
    {
        // Arrange
        var error1 = new Error("error1");
        var error2 = new Error("error2");
        var results = new[] { Result.Success(1), Result.Failure<int>(error1), Result.Failure<int>(error2) };

        // Act
        var combined = results.Combine();

        // Assert
        var aggregateError = Assert.IsType<AggregateError>(combined.Error);
        Assert.Equal(2, aggregateError.Errors.Count);
        Assert.Equal("error1", aggregateError.Message);
    }

    [Fact]
    public void CombineOfTReturnsEmptyWhenEmpty()
    {
        // Act
        var combined = Array.Empty<Result<int>>().Combine();

        // Assert
        Assert.True(combined.IsSuccess);
        Assert.Empty(combined.Value);
    }

    [Fact]
    public void CombineOfTReturnsFailureWhenTrailingFailure()
    {
        // Arrange
        var error = new Error("tail");
        var results = new[] { Result.Success(1), Result.Success(2), Result.Failure<int>(error) };

        // Act
        var combined = results.Combine();

        // Assert
        Assert.True(combined.IsFailure);
        var aggregate = Assert.IsType<AggregateError>(combined.Error);
        Assert.Single(aggregate.Errors);
        Assert.Same(error, aggregate.Errors[0]);
    }

    [Fact]
    public void CombineOfTReturnsFailureWhenLeadingFailure()
    {
        // Arrange
        var error = new Error("head");
        var results = new[] { Result.Failure<int>(error), Result.Success(1) };

        // Act & Assert
        Assert.True(results.Combine().IsFailure);
    }
}
