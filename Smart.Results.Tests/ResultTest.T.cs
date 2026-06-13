namespace Smart.Results;

public sealed partial class ResultTest
{
    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [Fact]
    public void SuccessOfTHasValue()
    {
        var result = Result.Success(123);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(123, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureOfTHasError()
    {
        var error = new Error("test");
        var result = Result.Failure<int>(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ValueThrowsWhenFailure()
    {
        var result = Result.Failure<int>(new Error("test"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void DefaultOfTIsFailure()
    {
        var result = default(Result<int>);

        Assert.True(result.IsFailure);
        Assert.Equal("An error has occurred.", result.Error?.Message);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryGetValueReturnsValueWhenSuccess()
    {
        var ret = Result.Success(123).TryGetValue(out var value);

        Assert.True(ret);
        Assert.Equal(123, value);
    }

    [Fact]
    public void TryGetValueReturnsFalseWhenFailure()
    {
        var ret = Result.Failure<int>(new Error("test")).TryGetValue(out _);

        Assert.False(ret);
    }

    [Fact]
    public void GetValueOrDefaultReturnsValueWhenSuccess()
    {
        Assert.Equal(123, Result.Success(123).GetValueOrDefault());
        Assert.Equal(123, Result.Success(123).GetValueOrDefault(-1));
        Assert.Equal(123, Result.Success(123).GetValueOrDefault(static _ => -1));
    }

    [Fact]
    public void GetValueOrDefaultReturnsDefaultWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;

        Assert.Equal(0, Result.Failure<int>(error).GetValueOrDefault());
        Assert.Equal(-1, Result.Failure<int>(error).GetValueOrDefault(-1));
        Assert.Equal(-1, Result.Failure<int>(error).GetValueOrDefault(e =>
        {
            received = e;
            return -1;
        }));
        Assert.Same(error, received);
    }

    //--------------------------------------------------------------------------------
    // Deconstruct
    //--------------------------------------------------------------------------------

    [Fact]
    public void DeconstructReturnsState()
    {
        var (isSuccess, value) = Result.Success(123);

        Assert.True(isSuccess);
        Assert.Equal(123, value);
    }

    [Fact]
    public void DeconstructReturnsError()
    {
        var error = new Error("test");
        var (isSuccess, value, e) = Result.Failure<int>(error);

        Assert.False(isSuccess);
        Assert.Equal(0, value);
        Assert.Same(error, e);
    }

    //--------------------------------------------------------------------------------
    // Try
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryOfTReturnsValueWhenNoException()
    {
        var result = Result.Try(static () => 123);

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void TryOfTReturnsFailureWhenException()
    {
        var exception = new InvalidOperationException("oops");
        var result = Result.Try<int>(() => throw exception);

        var exceptionError = Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(exception, exceptionError.Exception);
    }

    [Fact]
    public async Task TryAsyncOfTReturnsValueWhenNoException()
    {
        var result = await Result.TryAsync(static async () =>
        {
            await Task.Yield();
            return 123;
        }).ConfigureAwait(true);

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task TryAsyncOfTReturnsFailureWhenException()
    {
        var result = await Result.TryAsync<int>(static async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("oops");
        }).ConfigureAwait(true);

        Assert.Equal("oops", result.Error?.Message);
    }

    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapTransformsValueWhenSuccess()
    {
        var result = Result.Success(2).Map(static x => x * 10);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void MapSkipsWhenFailure()
    {
        var error = new Error("test");
        var called = false;
        var result = Result.Failure<int>(error).Map(x =>
        {
            called = true;
            return x * 10;
        });

        Assert.False(called);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void MapWithStateTransformsValue()
    {
        var result = Result.Success(2).Map(10, static (x, state) => x * state);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncTransformsValueWhenSuccess()
    {
        var result = await Result.Success(2).MapAsync(static async x =>
        {
            await Task.Yield();
            return x * 10;
        }).ConfigureAwait(true);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncSkipsWhenFailure()
    {
        var error = new Error("test");
        var result = await Result.Failure<int>(error).MapAsync(static async x =>
        {
            await Task.Yield();
            return x * 10;
        }).ConfigureAwait(true);

        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    [Fact]
    public void BindOfTCallsFuncWhenSuccess()
    {
        var result = Result.Success(2).Bind(static x => Result.Success(x * 10));

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void BindOfTSkipsWhenFailure()
    {
        var error = new Error("test");
        var result = Result.Failure<int>(error).Bind(static x => Result.Success(x * 10));

        Assert.Same(error, result.Error);
    }

    [Fact]
    public void BindOfTWithStateCallsFunc()
    {
        var result = Result.Success(2).Bind(10, static (x, state) => Result.Success(x * state));

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void BindToResultCallsFuncWhenSuccess()
    {
        var result = Result.Success(2).Bind(static x => x > 0 ? Result.Success() : Result.Failure(new Error("negative")));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void BindToResultSkipsWhenFailure()
    {
        var error = new Error("test");
        var result = Result.Failure<int>(error).Bind(static _ => Result.Success());

        Assert.Same(error, result.Error);
    }

    [Fact]
    public void BindToResultWithStateCallsFunc()
    {
        var result = Result.Success(2).Bind(0, static (x, state) => x > state ? Result.Success() : Result.Failure(new Error("negative")));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsyncOfTCallsFuncWhenSuccess()
    {
        var result = await Result.Success(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return Result.Success(x * 10);
        }).ConfigureAwait(true);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task BindAsyncToResultCallsFuncWhenSuccess()
    {
        var result = await Result.Success(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return x > 0 ? Result.Success() : Result.Failure(new Error("negative"));
        }).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
    }

    //--------------------------------------------------------------------------------
    // Ensure
    //--------------------------------------------------------------------------------

    [Fact]
    public void EnsureKeepsSuccessWhenPredicatePasses()
    {
        var result = Result.Success(123).Ensure(static x => x > 0, new Error("invalid"));

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void EnsureFailsWhenPredicateFails()
    {
        var error = new Error("invalid");
        var result = Result.Success(-1).Ensure(static x => x > 0, error);

        Assert.Same(error, result.Error);
    }

    [Fact]
    public void EnsureSkipsPredicateWhenFailure()
    {
        var error = new Error("test");
        var called = false;
        var result = Result.Failure<int>(error).Ensure(
            x =>
            {
                called = true;
                return true;
            },
            new Error("other"));

        Assert.False(called);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void EnsureWithStateKeepsSuccessWhenPredicatePasses()
    {
        var result = Result.Success(123).Ensure(100, static (x, state) => x > state, new Error("invalid"));

        Assert.Equal(123, result.Value);
    }

    //--------------------------------------------------------------------------------
    // MapError
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapErrorOfTTransformsErrorWhenFailure()
    {
        var result = Result.Failure<int>(new Error("test")).MapError(static e => new Error($"wrapped: {e.Message}"));

        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    [Fact]
    public void MapErrorOfTSkipsWhenSuccess()
    {
        var result = Result.Success(123).MapError(static _ => new Error("other"));

        Assert.Equal(123, result.Value);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    [Fact]
    public void MatchOfTCallsOnSuccessWhenSuccess()
    {
        var message = Result.Success(123).Match(static x => $"ok: {x}", static e => $"ng: {e.Message}");

        Assert.Equal("ok: 123", message);
    }

    [Fact]
    public void MatchOfTCallsOnFailureWhenFailure()
    {
        var message = Result.Failure<int>(new Error("test")).Match(static x => $"ok: {x}", static e => $"ng: {e.Message}");

        Assert.Equal("ng: test", message);
    }

    [Fact]
    public void MatchOfTWithStateCallsOnSuccess()
    {
        var value = Result.Success(2).Match(10, static (x, state) => x * state, static (_, _) => -1);

        Assert.Equal(20, value);
    }

    [Fact]
    public async Task MatchAsyncOfTCallsOnSuccessWhenSuccess()
    {
        var message = await Result.Success(123).MatchAsync(
            static async x =>
            {
                await Task.Yield();
                return $"ok: {x}";
            },
            static async e =>
            {
                await Task.Yield();
                return $"ng: {e.Message}";
            }).ConfigureAwait(true);

        Assert.Equal("ok: 123", message);
    }

    [Fact]
    public async Task MatchAsyncOfTCallsOnFailureWhenFailure()
    {
        var message = await Result.Failure<int>(new Error("test")).MatchAsync(
            static async x =>
            {
                await Task.Yield();
                return $"ok: {x}";
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
    public void IfSuccessOfTCallsActionWhenSuccess()
    {
        var received = 0;
        Result.Success(123).IfSuccess(x => { received = x; });

        Assert.Equal(123, received);
    }

    [Fact]
    public void IfSuccessOfTSkipsWhenFailure()
    {
        var called = false;
        Result.Failure<int>(new Error("test")).IfSuccess(_ => { called = true; });

        Assert.False(called);
    }

    [Fact]
    public void IfSuccessOfTWithStateCallsAction()
    {
        var received = 0;
        Result.Success(2).IfSuccess(10, (x, state) => { received = x * state; });

        Assert.Equal(20, received);
    }

    [Fact]
    public async Task IfSuccessAsyncOfTCallsActionWhenSuccess()
    {
        var received = 0;
        await Result.Success(123).IfSuccessAsync(async x =>
        {
            await Task.Yield();
            received = x;
        }).ConfigureAwait(true);

        Assert.Equal(123, received);
    }

    [Fact]
    public void IfFailureOfTCallsActionWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;
        Result.Failure<int>(error).IfFailure(e => { received = e; });

        Assert.Same(error, received);
    }

    [Fact]
    public void IfFailureOfTSkipsWhenSuccess()
    {
        var called = false;
        Result.Success(123).IfFailure(_ => { called = true; });

        Assert.False(called);
    }

    [Fact]
    public void IfFailureOfTWithStateCallsAction()
    {
        var received = 0;
        Result.Failure<int>(new Error("test")).IfFailure(10, (_, state) => { received = state; });

        Assert.Equal(10, received);
    }

    [Fact]
    public async Task IfFailureAsyncOfTCallsActionWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;
        await Result.Failure<int>(error).IfFailureAsync(async e =>
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
    public void ImplicitConversionFromValue()
    {
        Result<int> result = 123;

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void ImplicitConversionFromErrorOfT()
    {
        var error = new Error("test");
        Result<int> result = error;

        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ImplicitConversionToResult()
    {
        var error = new Error("test");
        Result success = Result.Success(123);
        Result failure = Result.Failure<int>(error);

        Assert.True(success.IsSuccess);
        Assert.Same(error, failure.Error);
    }

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    [Fact]
    public void EqualityOfTForSuccess()
    {
        Assert.Equal(Result.Success(123), Result.Success(123));
        Assert.NotEqual(Result.Success(123), Result.Success(456));
        Assert.Equal(Result.Success(123).GetHashCode(), Result.Success(123).GetHashCode());
    }

    [Fact]
    public void EqualityOfTForFailure()
    {
        Assert.Equal(Result.Failure<int>(new Error("test")), Result.Failure<int>(new Error("test")));
        Assert.NotEqual(Result.Failure<int>(new Error("test")), Result.Failure<int>(new Error("other")));
        Assert.NotEqual(Result.Success(0), default);
    }

    [Fact]
    public void EqualityOfTOperators()
    {
        // ReSharper disable once EqualExpressionComparison
        var equal = Result.Success(123) == Result.Success(123);
        var notEqual = Result.Success(123) != Result.Failure<int>(new Error("test"));

        Assert.True(equal);
        Assert.True(notEqual);
    }

    [Fact]
    public void EqualsObjectOverrideOfT()
    {
        object other = "other";
        Assert.True(Result.Success(123).Equals((object)Result.Success(123)));
        Assert.False(Result.Success(123).Equals(other));
        Assert.False(Result.Success(123).Equals((object?)null));
    }

    //--------------------------------------------------------------------------------
    // ToString
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToStringOfTRepresentsState()
    {
        Assert.Equal("Success(123)", Result.Success(123).ToString());
        Assert.Equal("Failure(test)", Result.Failure<int>(new Error("test")).ToString());
    }
}
