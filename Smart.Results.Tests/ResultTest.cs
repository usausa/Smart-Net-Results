namespace Smart.Results;

public sealed class ResultTest
{
    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [Fact]
    public void SuccessIsSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureIsFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void DefaultIsFailure()
    {
        // Act
        var result = default(Result);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error.", result.Error?.Message);
    }

    //--------------------------------------------------------------------------------
    // Try
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryReturnsSuccessWhenNoException()
    {
        // Arrange
        var called = false;

        // Act
        var result = Result.Try(() => { called = true; });

        // Assert
        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void TryReturnsFailureWhenException()
    {
        // Arrange
        var exception = new InvalidOperationException("oops");

        // Act
        var result = Result.Try(() => throw exception);

        // Assert
        Assert.True(result.IsFailure);
        var exceptionError = Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(exception, exceptionError.Exception);
    }

    [Fact]
    public async Task TryAsyncReturnsSuccessWhenNoException()
    {
        // Arrange
        var called = false;

        // Act
        var result = await Result.TryAsync(async () =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        // Assert
        Assert.True(called);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TryAsyncReturnsFailureWhenException()
    {
        // Act
        var result = await Result.TryAsync(static async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("oops");
        }).ConfigureAwait(true);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("oops", result.Error?.Message);
    }

    [Fact]
    public void TryRethrowsOperationCanceledException()
    {
        // Act & Assert
        Assert.Throws<OperationCanceledException>(
            () => Result.Try(static () => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TryAsyncRethrowsOperationCanceledException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await Result.TryAsync(static async () =>
            {
                await Task.Yield();
                throw new OperationCanceledException();
            }).ConfigureAwait(true)).ConfigureAwait(true);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    [Fact]
    public void BindCallsFuncWhenSuccess()
    {
        // Act
        var result = Result.Success().Bind(static () => Result.Failure(new Error("next")));

        // Assert
        Assert.Equal("next", result.Error?.Message);
    }

    [Fact]
    public void BindSkipsFuncWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        var called = false;

        // Act
        var result = Result.Failure(error).Bind(() =>
        {
            called = true;
            return Result.Success();
        });

        // Assert
        Assert.False(called);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void BindWithStateCallsFunc()
    {
        // Act
        var result = Result.Success().Bind(1, static state => state > 0 ? Result.Success() : Result.Failure(new Error("negative")));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void BindToGenericCallsFuncWhenSuccess()
    {
        // Act
        var result = Result.Success().Bind(static () => Result.Success(123));

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void BindToGenericWithStateCallsFunc()
    {
        // Act
        var result = Result.Success().Bind(123, static state => Result.Success(state));

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void BindToGenericSkipsFuncWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = Result.Failure(error).Bind(static () => Result.Success(123));

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task BindAsyncCallsFuncWhenSuccess()
    {
        // Act
        var result = await Result.Success().BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success(123);
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task BindAsyncSkipsFuncWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = await Result.Failure(error).BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success();
        }).ConfigureAwait(true);

        // Assert
        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // MapError
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapErrorTransformsErrorWhenFailure()
    {
        // Act
        var result = Result.Failure(new Error("test")).MapError(static e => new Error($"wrapped: {e.Message}"));

        // Assert
        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    [Fact]
    public void MapErrorSkipsWhenSuccess()
    {
        // Arrange
        var called = false;

        // Act
        var result = Result.Success().MapError(e =>
        {
            called = true;
            return e;
        });

        // Assert
        Assert.False(called);
        Assert.True(result.IsSuccess);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    [Fact]
    public void MatchCallsOnSuccessWhenSuccess()
    {
        // Act
        var message = Result.Success().Match(static () => "ok", static _ => "ng");

        // Assert
        Assert.Equal("ok", message);
    }

    [Fact]
    public void MatchCallsOnFailureWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act
        var message = Result.Failure(error).Match(static () => "ok", e =>
        {
            received = e;
            return "ng";
        });

        // Assert
        Assert.Equal("ng", message);
        Assert.Same(error, received);
    }

    [Fact]
    public void MatchWithStateCallsOnSuccess()
    {
        // Act
        var value = Result.Success().Match(10, static state => state + 1, static (_, state) => state - 1);

        // Assert
        Assert.Equal(11, value);
    }

    [Fact]
    public async Task MatchAsyncCallsOnSuccessWhenSuccess()
    {
        // Act
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

        // Assert
        Assert.Equal("ok", message);
    }

    [Fact]
    public async Task MatchAsyncCallsOnFailureWhenFailure()
    {
        // Act
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

        // Assert
        Assert.Equal("ng: test", message);
    }

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    [Fact]
    public void IfSuccessCallsActionWhenSuccess()
    {
        // Arrange
        var called = false;

        // Act
        Result.Success().IfSuccess(() => { called = true; });

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void IfSuccessSkipsWhenFailure()
    {
        // Arrange
        var called = false;

        // Act
        Result.Failure(new Error("test")).IfSuccess(() => { called = true; });

        // Assert
        Assert.False(called);
    }

    [Fact]
    public void IfSuccessWithStateCallsAction()
    {
        // Arrange
        var received = 0;

        // Act
        Result.Success().IfSuccess(10, state => { received = state; });

        // Assert
        Assert.Equal(10, received);
    }

    [Fact]
    public async Task IfSuccessAsyncCallsActionWhenSuccess()
    {
        // Arrange
        var called = false;

        // Act
        await Result.Success().IfSuccessAsync(async () =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void IfFailureCallsActionWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act
        Result.Failure(error).IfFailure(e => { received = e; });

        // Assert
        Assert.Same(error, received);
    }

    [Fact]
    public void IfFailureSkipsWhenSuccess()
    {
        // Arrange
        var called = false;

        // Act
        Result.Success().IfFailure(_ => { called = true; });

        // Assert
        Assert.False(called);
    }

    [Fact]
    public void IfFailureWithStateCallsAction()
    {
        // Arrange
        var received = 0;

        // Act
        Result.Failure(new Error("test")).IfFailure(10, (_, state) => { received = state; });

        // Assert
        Assert.Equal(10, received);
    }

    [Fact]
    public async Task IfFailureAsyncCallsActionWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act
        await Result.Failure(error).IfFailureAsync(async e =>
        {
            await Task.Yield();
            received = e;
        }).ConfigureAwait(true);

        // Assert
        Assert.Same(error, received);
    }

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

    [Fact]
    public void ImplicitConversionFromError()
    {
        // Arrange
        var error = new Error("test");

        // Act
        Result result = error;

        // Assert
        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    [Fact]
    public void EqualityForSuccess()
    {
        // Act & Assert
        Assert.Equal(Result.Success(), Result.Success());
        Assert.Equal(Result.Success().GetHashCode(), Result.Success().GetHashCode());
    }

    [Fact]
    public void EqualityForFailure()
    {
        // Act & Assert
        Assert.Equal(Result.Failure(new Error("test")), Result.Failure(new Error("test")));
        Assert.NotEqual(Result.Failure(new Error("test")), Result.Failure(new Error("other")));
        Assert.NotEqual(Result.Success(), Result.Failure(new Error("test")));
    }

    [Fact]
    public void EqualityOperators()
    {
        // Act
        // ReSharper disable once EqualExpressionComparison
        var equal = Result.Success() == Result.Success();
        var notEqual = Result.Success() != Result.Failure(new Error("test"));

        // Assert
        Assert.True(equal);
        Assert.True(notEqual);
    }

    [Fact]
    public void EqualsObjectOverride()
    {
        // Arrange
        object other = "other";

        // Act & Assert
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
        // Act & Assert
        Assert.Equal("Success", Result.Success().ToString());
        Assert.Equal("Failure(test)", Result.Failure(new Error("test")).ToString());
    }

    //--------------------------------------------------------------------------------
    // Factory (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void SuccessOfTHasValue()
    {
        // Act
        var result = Result.Success(123);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(123, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureOfTHasError()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = Result.Failure<int>(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ValueThrowsWhenFailure()
    {
        // Arrange
        var result = Result.Failure<int>(new Error("test"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void DefaultOfTIsFailure()
    {
        // Act
        var result = default(Result<int>);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Error.", result.Error?.Message);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    //--------------------------------------------------------------------------------
    // Value (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryGetValueReturnsValueWhenSuccess()
    {
        // Act
        var ret = Result.Success(123).TryGetValue(out var value);

        // Assert
        Assert.True(ret);
        Assert.Equal(123, value);
    }

    [Fact]
    public void TryGetValueReturnsFalseWhenFailure()
    {
        // Act
        var ret = Result.Failure<int>(new Error("test")).TryGetValue(out _);

        // Assert
        Assert.False(ret);
    }

    [Fact]
    public void GetValueOrDefaultReturnsValueWhenSuccess()
    {
        // Act & Assert
        Assert.Equal(123, Result.Success(123).GetValueOrDefault());
        Assert.Equal(123, Result.Success(123).GetValueOrDefault(-1));
        Assert.Equal(123, Result.Success(123).GetValueOrDefault(static _ => -1));
    }

    [Fact]
    public void GetValueOrDefaultReturnsDefaultWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act & Assert
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
    // Deconstruct (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void DeconstructReturnsState()
    {
        // Act
        var (isSuccess, value) = Result.Success(123);

        // Assert
        Assert.True(isSuccess);
        Assert.Equal(123, value);
    }

    [Fact]
    public void DeconstructReturnsError()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var (isSuccess, value, e) = Result.Failure<int>(error);

        // Assert
        Assert.False(isSuccess);
        Assert.Equal(0, value);
        Assert.Same(error, e);
    }

    //--------------------------------------------------------------------------------
    // Try (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void TryOfTReturnsValueWhenNoException()
    {
        // Act
        var result = Result.Try(static () => 123);

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void TryOfTReturnsFailureWhenException()
    {
        // Arrange
        var exception = new InvalidOperationException("oops");

        // Act
        var result = Result.Try<int>(() => throw exception);

        // Assert
        var exceptionError = Assert.IsType<ExceptionError>(result.Error);
        Assert.Same(exception, exceptionError.Exception);
    }

    [Fact]
    public async Task TryAsyncOfTReturnsValueWhenNoException()
    {
        // Act
        var result = await Result.TryAsync(static async () =>
        {
            await Task.Yield();
            return 123;
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task TryAsyncOfTReturnsFailureWhenException()
    {
        // Act
        var result = await Result.TryAsync<int>(static async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("oops");
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal("oops", result.Error?.Message);
    }

    [Fact]
    public void TryOfTRethrowsOperationCanceledException()
    {
        // Act & Assert
        Assert.Throws<OperationCanceledException>(
            () => Result.Try<int>(static () => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TryAsyncOfTRethrowsOperationCanceledException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await Result.TryAsync<int>(static async () =>
            {
                await Task.Yield();
                throw new OperationCanceledException();
            }).ConfigureAwait(true)).ConfigureAwait(true);
    }

    //--------------------------------------------------------------------------------
    // Map (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapTransformsValueWhenSuccess()
    {
        // Act
        var result = Result.Success(2).Map(static x => x * 10);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void MapSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        var called = false;

        // Act
        var result = Result.Failure<int>(error).Map(x =>
        {
            called = true;
            return x * 10;
        });

        // Assert
        Assert.False(called);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void MapWithStateTransformsValue()
    {
        // Act
        var result = Result.Success(2).Map(10, static (x, state) => x * state);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncTransformsValueWhenSuccess()
    {
        // Act
        var result = await Result.Success(2).MapAsync(static async x =>
        {
            await Task.Yield();
            return x * 10;
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = await Result.Failure<int>(error).MapAsync(static async x =>
        {
            await Task.Yield();
            return x * 10;
        }).ConfigureAwait(true);

        // Assert
        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Bind (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void BindOfTCallsFuncWhenSuccess()
    {
        // Act
        var result = Result.Success(2).Bind(static x => Result.Success(x * 10));

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void BindOfTSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = Result.Failure<int>(error).Bind(static x => Result.Success(x * 10));

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void BindOfTWithStateCallsFunc()
    {
        // Act
        var result = Result.Success(2).Bind(10, static (x, state) => Result.Success(x * state));

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void BindToResultCallsFuncWhenSuccess()
    {
        // Act
        var result = Result.Success(2).Bind(static x => x > 0 ? Result.Success() : Result.Failure(new Error("negative")));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void BindToResultSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = Result.Failure<int>(error).Bind(static _ => Result.Success());

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void BindToResultWithStateCallsFunc()
    {
        // Act
        var result = Result.Success(2).Bind(0, static (x, state) => x > state ? Result.Success() : Result.Failure(new Error("negative")));

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsyncOfTCallsFuncWhenSuccess()
    {
        // Act
        var result = await Result.Success(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return Result.Success(x * 10);
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task BindAsyncToResultCallsFuncWhenSuccess()
    {
        // Act
        var result = await Result.Success(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return x > 0 ? Result.Success() : Result.Failure(new Error("negative"));
        }).ConfigureAwait(true);

        // Assert
        Assert.True(result.IsSuccess);
    }

    //--------------------------------------------------------------------------------
    // Ensure (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void EnsureKeepsSuccessWhenPredicatePasses()
    {
        // Act
        var result = Result.Success(123).Ensure(static x => x > 0, new Error("invalid"));

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void EnsureFailsWhenPredicateFails()
    {
        // Arrange
        var error = new Error("invalid");

        // Act
        var result = Result.Success(-1).Ensure(static x => x > 0, error);

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void EnsureSkipsPredicateWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        var called = false;

        // Act
        var result = Result.Failure<int>(error).Ensure(
            _ =>
            {
                called = true;
                return true;
            },
            new Error("other"));

        // Assert
        Assert.False(called);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void EnsureWithStateKeepsSuccessWhenPredicatePasses()
    {
        // Act
        var result = Result.Success(123).Ensure(100, static (x, state) => x > state, new Error("invalid"));

        // Assert
        Assert.Equal(123, result.Value);
    }

    //--------------------------------------------------------------------------------
    // MapError (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapErrorOfTTransformsErrorWhenFailure()
    {
        // Act
        var result = Result.Failure<int>(new Error("test")).MapError(static e => new Error($"wrapped: {e.Message}"));

        // Assert
        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    [Fact]
    public void MapErrorOfTSkipsWhenSuccess()
    {
        // Act
        var result = Result.Success(123).MapError(static _ => new Error("other"));

        // Assert
        Assert.Equal(123, result.Value);
    }

    //--------------------------------------------------------------------------------
    // Match (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void MatchOfTCallsOnSuccessWhenSuccess()
    {
        // Act
        var message = Result.Success(123).Match(static x => $"ok: {x}", static e => $"ng: {e.Message}");

        // Assert
        Assert.Equal("ok: 123", message);
    }

    [Fact]
    public void MatchOfTCallsOnFailureWhenFailure()
    {
        // Act
        var message = Result.Failure<int>(new Error("test")).Match(static x => $"ok: {x}", static e => $"ng: {e.Message}");

        // Assert
        Assert.Equal("ng: test", message);
    }

    [Fact]
    public void MatchOfTWithStateCallsOnSuccess()
    {
        // Act
        var value = Result.Success(2).Match(10, static (x, state) => x * state, static (_, _) => -1);

        // Assert
        Assert.Equal(20, value);
    }

    [Fact]
    public async Task MatchAsyncOfTCallsOnSuccessWhenSuccess()
    {
        // Act
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

        // Assert
        Assert.Equal("ok: 123", message);
    }

    [Fact]
    public async Task MatchAsyncOfTCallsOnFailureWhenFailure()
    {
        // Act
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

        // Assert
        Assert.Equal("ng: test", message);
    }

    //--------------------------------------------------------------------------------
    // If (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void IfSuccessOfTCallsActionWhenSuccess()
    {
        // Arrange
        var received = 0;

        // Act
        Result.Success(123).IfSuccess(x => { received = x; });

        // Assert
        Assert.Equal(123, received);
    }

    [Fact]
    public void IfSuccessOfTSkipsWhenFailure()
    {
        // Arrange
        var called = false;

        // Act
        Result.Failure<int>(new Error("test")).IfSuccess(_ => { called = true; });

        // Assert
        Assert.False(called);
    }

    [Fact]
    public void IfSuccessOfTWithStateCallsAction()
    {
        // Arrange
        var received = 0;

        // Act
        Result.Success(2).IfSuccess(10, (x, state) => { received = x * state; });

        // Assert
        Assert.Equal(20, received);
    }

    [Fact]
    public async Task IfSuccessAsyncOfTCallsActionWhenSuccess()
    {
        // Arrange
        var received = 0;

        // Act
        await Result.Success(123).IfSuccessAsync(async x =>
        {
            await Task.Yield();
            received = x;
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(123, received);
    }

    [Fact]
    public void IfFailureOfTCallsActionWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act
        Result.Failure<int>(error).IfFailure(e => { received = e; });

        // Assert
        Assert.Same(error, received);
    }

    [Fact]
    public void IfFailureOfTSkipsWhenSuccess()
    {
        // Arrange
        var called = false;

        // Act
        Result.Success(123).IfFailure(_ => { called = true; });

        // Assert
        Assert.False(called);
    }

    [Fact]
    public void IfFailureOfTWithStateCallsAction()
    {
        // Arrange
        var received = 0;

        // Act
        Result.Failure<int>(new Error("test")).IfFailure(10, (_, state) => { received = state; });

        // Assert
        Assert.Equal(10, received);
    }

    [Fact]
    public async Task IfFailureAsyncOfTCallsActionWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act
        await Result.Failure<int>(error).IfFailureAsync(async e =>
        {
            await Task.Yield();
            received = e;
        }).ConfigureAwait(true);

        // Assert
        Assert.Same(error, received);
    }

    //--------------------------------------------------------------------------------
    // Operator (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void ImplicitConversionFromValue()
    {
        // Act
        Result<int> result = 123;

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void ImplicitConversionFromErrorOfT()
    {
        // Arrange
        var error = new Error("test");

        // Act
        Result<int> result = error;

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ImplicitConversionToResult()
    {
        // Arrange
        var error = new Error("test");

        // Act
        Result success = Result.Success(123);
        Result failure = Result.Failure<int>(error);

        // Assert
        Assert.True(success.IsSuccess);
        Assert.Same(error, failure.Error);
    }

    //--------------------------------------------------------------------------------
    // Equality (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void EqualityOfTForSuccess()
    {
        // Act & Assert
        Assert.Equal(Result.Success(123), Result.Success(123));
        Assert.NotEqual(Result.Success(123), Result.Success(456));
        Assert.Equal(Result.Success(123).GetHashCode(), Result.Success(123).GetHashCode());
    }

    [Fact]
    public void EqualityOfTForFailure()
    {
        // Act & Assert
        Assert.Equal(Result.Failure<int>(new Error("test")), Result.Failure<int>(new Error("test")));
        Assert.NotEqual(Result.Failure<int>(new Error("test")), Result.Failure<int>(new Error("other")));
        Assert.NotEqual(Result.Success(0), default);
    }

    [Fact]
    public void EqualityOfTOperators()
    {
        // Act
        // ReSharper disable once EqualExpressionComparison
        var equal = Result.Success(123) == Result.Success(123);
        var notEqual = Result.Success(123) != Result.Failure<int>(new Error("test"));

        // Assert
        Assert.True(equal);
        Assert.True(notEqual);
    }

    [Fact]
    public void EqualsObjectOverrideOfT()
    {
        // Arrange
        object other = "other";

        // Act & Assert
        Assert.True(Result.Success(123).Equals((object)Result.Success(123)));
        Assert.False(Result.Success(123).Equals(other));
        Assert.False(Result.Success(123).Equals((object?)null));
    }

    //--------------------------------------------------------------------------------
    // ToString (Result<T>)
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToStringOfTRepresentsState()
    {
        // Act & Assert
        Assert.Equal("Success(123)", Result.Success(123).ToString());
        Assert.Equal("Failure(test)", Result.Failure<int>(new Error("test")).ToString());
    }
}
