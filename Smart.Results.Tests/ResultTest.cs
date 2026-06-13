namespace Smart.Results;

public sealed partial class ResultTest
{
    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [Fact]
    public void SuccessIsSuccess()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureIsFailure()
    {
        var error = new Error("test");
        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void DefaultIsFailure()
    {
        var result = default(Result);

        Assert.True(result.IsFailure);
        Assert.Equal("An error has occurred.", result.Error?.Message);
    }

    //--------------------------------------------------------------------------------
    // Try
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryReturnsSuccessWhenNoException()
    {
        var called = false;
        var result = Result.Try(() => { called = true; });

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void TryReturnsFailureWhenException()
    {
        var exception = new InvalidOperationException("oops");
        var result = Result.Try(() => throw exception);

        Assert.True(result.IsFailure);
        var exceptionError = Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(exception, exceptionError.Exception);
    }

    [Fact]
    public async Task TryAsyncReturnsSuccessWhenNoException()
    {
        var called = false;
        var result = await Result.TryAsync(async () =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TryAsyncReturnsFailureWhenException()
    {
        var result = await Result.TryAsync(static async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("oops");
        }).ConfigureAwait(true);

        Assert.True(result.IsFailure);
        Assert.Equal("oops", result.Error?.Message);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    [Fact]
    public void BindCallsFuncWhenSuccess()
    {
        var result = Result.Success().Bind(static () => Result.Failure(new Error("next")));

        Assert.Equal("next", result.Error?.Message);
    }

    [Fact]
    public void BindSkipsFuncWhenFailure()
    {
        var error = new Error("test");
        var called = false;
        var result = Result.Failure(error).Bind(() =>
        {
            called = true;
            return Result.Success();
        });

        Assert.False(called);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void BindWithStateCallsFunc()
    {
        var result = Result.Success().Bind(1, static state => state > 0 ? Result.Success() : Result.Failure(new Error("negative")));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void BindToGenericCallsFuncWhenSuccess()
    {
        var result = Result.Success().Bind(static () => Result.Success(123));

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void BindToGenericWithStateCallsFunc()
    {
        var result = Result.Success().Bind(123, static state => Result.Success(state));

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void BindToGenericSkipsFuncWhenFailure()
    {
        var error = new Error("test");
        var result = Result.Failure(error).Bind(static () => Result.Success(123));

        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task BindAsyncCallsFuncWhenSuccess()
    {
        var result = await Result.Success().BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success(123);
        }).ConfigureAwait(true);

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task BindAsyncSkipsFuncWhenFailure()
    {
        var error = new Error("test");
        var result = await Result.Failure(error).BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success();
        }).ConfigureAwait(true);

        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // MapError
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapErrorTransformsErrorWhenFailure()
    {
        var result = Result.Failure(new Error("test")).MapError(static e => new Error($"wrapped: {e.Message}"));

        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    [Fact]
    public void MapErrorSkipsWhenSuccess()
    {
        var called = false;
        var result = Result.Success().MapError(e =>
        {
            called = true;
            return e;
        });

        Assert.False(called);
        Assert.True(result.IsSuccess);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    [Fact]
    public void MatchCallsOnSuccessWhenSuccess()
    {
        var message = Result.Success().Match(static () => "ok", static _ => "ng");

        Assert.Equal("ok", message);
    }

    [Fact]
    public void MatchCallsOnFailureWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;
        var message = Result.Failure(error).Match(static () => "ok", e =>
        {
            received = e;
            return "ng";
        });

        Assert.Equal("ng", message);
        Assert.Same(error, received);
    }

    [Fact]
    public void MatchWithStateCallsOnSuccess()
    {
        var value = Result.Success().Match(10, static state => state + 1, static (_, state) => state - 1);

        Assert.Equal(11, value);
    }

    [Fact]
    public async Task MatchAsyncCallsOnSuccessWhenSuccess()
    {
        var message = await Result.Success().MatchAsync(
            static async () =>
            {
                await Task.Yield();
                return "ok";
            },
            static async _ =>
            {
                await Task.Yield();
                return "ng";
            }).ConfigureAwait(true);

        Assert.Equal("ok", message);
    }

    [Fact]
    public async Task MatchAsyncCallsOnFailureWhenFailure()
    {
        var message = await Result.Failure(new Error("test")).MatchAsync(
            static async () =>
            {
                await Task.Yield();
                return "ok";
            },
            static async e =>
            {
                await Task.Yield();
                return $"ng: {e.Message}";
            }).ConfigureAwait(true);

        Assert.Equal("ng: test", message);
    }

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    [Fact]
    public void IfSuccessCallsActionWhenSuccess()
    {
        var called = false;
        Result.Success().IfSuccess(() => { called = true; });

        Assert.True(called);
    }

    [Fact]
    public void IfSuccessSkipsWhenFailure()
    {
        var called = false;
        Result.Failure(new Error("test")).IfSuccess(() => { called = true; });

        Assert.False(called);
    }

    [Fact]
    public void IfSuccessWithStateCallsAction()
    {
        var received = 0;
        Result.Success().IfSuccess(10, state => { received = state; });

        Assert.Equal(10, received);
    }

    [Fact]
    public async Task IfSuccessAsyncCallsActionWhenSuccess()
    {
        var called = false;
        await Result.Success().IfSuccessAsync(async () =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        Assert.True(called);
    }

    [Fact]
    public void IfFailureCallsActionWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;
        Result.Failure(error).IfFailure(e => { received = e; });

        Assert.Same(error, received);
    }

    [Fact]
    public void IfFailureSkipsWhenSuccess()
    {
        var called = false;
        Result.Success().IfFailure(_ => { called = true; });

        Assert.False(called);
    }

    [Fact]
    public void IfFailureWithStateCallsAction()
    {
        var received = 0;
        Result.Failure(new Error("test")).IfFailure(10, (_, state) => { received = state; });

        Assert.Equal(10, received);
    }

    [Fact]
    public async Task IfFailureAsyncCallsActionWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;
        await Result.Failure(error).IfFailureAsync(async e =>
        {
            await Task.Yield();
            received = e;
        }).ConfigureAwait(true);

        Assert.Same(error, received);
    }

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

    [Fact]
    public void ImplicitConversionFromError()
    {
        var error = new Error("test");
        Result result = error;

        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    [Fact]
    public void EqualityForSuccess()
    {
        Assert.Equal(Result.Success(), Result.Success());
        Assert.Equal(Result.Success().GetHashCode(), Result.Success().GetHashCode());
    }

    [Fact]
    public void EqualityForFailure()
    {
        Assert.Equal(Result.Failure(new Error("test")), Result.Failure(new Error("test")));
        Assert.NotEqual(Result.Failure(new Error("test")), Result.Failure(new Error("other")));
        Assert.NotEqual(Result.Success(), Result.Failure(new Error("test")));
    }

    [Fact]
    public void EqualityOperators()
    {
        // ReSharper disable once EqualExpressionComparison
        var equal = Result.Success() == Result.Success();
        var notEqual = Result.Success() != Result.Failure(new Error("test"));

        Assert.True(equal);
        Assert.True(notEqual);
    }

    [Fact]
    public void EqualsObjectOverride()
    {
        object other = "other";
        Assert.True(Result.Success().Equals((object)Result.Success()));
        Assert.False(Result.Success().Equals(other));
        Assert.False(Result.Success().Equals((object?)null));
    }

    //--------------------------------------------------------------------------------
    // ToString
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToStringRepresentsState()
    {
        Assert.Equal("Success", Result.Success().ToString());
        Assert.Equal("Failure(test)", Result.Failure(new Error("test")).ToString());
    }
}
