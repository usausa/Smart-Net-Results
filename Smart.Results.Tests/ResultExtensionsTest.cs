namespace Smart.Results;

public sealed class ResultExtensionsTest
{
    //--------------------------------------------------------------------------------
    // ToResult
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToResultClassReturnsSuccessWhenNotNull()
    {
        const string value = "test";
        var result = value.ToResult(new Error("null"));

        Assert.Equal("test", result.Value);
    }

    [Fact]
    public void ToResultClassReturnsFailureWhenNull()
    {
        var error = new Error("null");
        string? value = null;
        var result = value.ToResult(error);

        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ToResultStructReturnsSuccessWhenHasValue()
    {
        int? value = 123;
        var result = value.ToResult(new Error("null"));

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void ToResultStructReturnsFailureWhenNull()
    {
        var error = new Error("null");
        int? value = null;
        var result = value.ToResult(error);

        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Combine
    //--------------------------------------------------------------------------------

    [Fact]
    public void CombineReturnsSuccessWhenAllSuccess()
    {
        var results = new[] { Result.Success(), Result.Success() };

        Assert.True(results.Combine().IsSuccess);
    }

    [Fact]
    public void CombineReturnsSuccessWhenEmpty()
    {
        Assert.True(Array.Empty<Result>().Combine().IsSuccess);
    }

    [Fact]
    public void CombineCollectsErrorsWhenFailure()
    {
        var error1 = new Error("error1");
        var error2 = new Error("error2");
        var results = new[] { Result.Failure(error1), Result.Success(), Result.Failure(error2) };
        var combined = results.Combine();

        var aggregateError = Assert.IsType<AggregateError>(combined.Error);
        Assert.Equal(2, aggregateError.Errors.Count);
        Assert.Same(error1, aggregateError.Errors[0]);
        Assert.Same(error2, aggregateError.Errors[1]);
    }

    [Fact]
    public void CombineOfTReturnsValuesWhenAllSuccess()
    {
        var results = new[] { Result.Success(1), Result.Success(2), Result.Success(3) };
        var combined = results.Combine();

        Assert.Equal(new[] { 1, 2, 3 }, combined.Value);
    }

    [Fact]
    public void CombineOfTCollectsErrorsWhenFailure()
    {
        var error1 = new Error("error1");
        var error2 = new Error("error2");
        var results = new[] { Result.Success(1), Result.Failure<int>(error1), Result.Failure<int>(error2) };
        var combined = results.Combine();

        var aggregateError = Assert.IsType<AggregateError>(combined.Error);
        Assert.Equal(2, aggregateError.Errors.Count);
        Assert.Equal("error1", aggregateError.Message);
    }

    [Fact]
    public void CombineOfTReturnsEmptyWhenEmpty()
    {
        var combined = Array.Empty<Result<int>>().Combine();

        Assert.True(combined.IsSuccess);
        Assert.Empty(combined.Value);
    }
}
