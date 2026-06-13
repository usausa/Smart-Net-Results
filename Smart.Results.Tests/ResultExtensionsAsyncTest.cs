namespace Smart.Results;

public sealed class ResultExtensionsAsyncTest
{
    private static ValueTask<Result<int>> SuccessSource(int value) => new(Result.Success(value));

    private static ValueTask<Result<int>> FailureSource(Error error) => new(Result.Failure<int>(error));

    private static ValueTask<Result> SuccessSourceUnit() => new(Result.Success());

    private static ValueTask<Result> FailureSourceUnit(Error error) => new(Result.Failure(error));

    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task MapAsyncTransformsWhenSuccess()
    {
        // Act
        var result = await SuccessSource(2).MapAsync(static x => x * 10).ConfigureAwait(true);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncWithStateTransformsWhenSuccess()
    {
        // Act
        var result = await SuccessSource(2).MapAsync(10, static (x, state) => x * state).ConfigureAwait(true);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = await FailureSource(error).MapAsync(static x => x * 10).ConfigureAwait(true);

        // Assert
        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task BindAsyncSyncContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSource(2).BindAsync(static x => Result.Success(x * 10)).ConfigureAwait(true);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task BindAsyncValueTaskContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSource(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return Result.Success(x * 10);
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task BindAsyncSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = await FailureSource(error).BindAsync(static x => Result.Success(x * 10)).ConfigureAwait(true);

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task BindAsyncToUnitSyncContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSource(2).BindAsync(static x => x > 0 ? Result.Success() : Result.Failure(new Error("negative"))).ConfigureAwait(true);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsyncToUnitValueTaskContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSource(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return x > 0 ? Result.Success() : Result.Failure(new Error("negative"));
        }).ConfigureAwait(true);

        // Assert
        Assert.True(result.IsSuccess);
    }

    //--------------------------------------------------------------------------------
    // Ensure / MapError
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task EnsureAsyncFailsWhenPredicateFails()
    {
        // Arrange
        var error = new Error("invalid");

        // Act
        var result = await SuccessSource(-1).EnsureAsync(static x => x > 0, error).ConfigureAwait(true);

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task MapErrorAsyncTransformsWhenFailure()
    {
        // Act
        var result = await FailureSource(new Error("test")).MapErrorAsync(static e => new Error($"wrapped: {e.Message}")).ConfigureAwait(true);

        // Assert
        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task MatchAsyncSelectsBranch()
    {
        // Act
        var onSuccess = await SuccessSource(123).MatchAsync(static x => $"ok: {x}", static e => $"ng: {e.Message}").ConfigureAwait(true);
        var onFailure = await FailureSource(new Error("test")).MatchAsync(static x => $"ok: {x}", static e => $"ng: {e.Message}").ConfigureAwait(true);

        // Assert
        Assert.Equal("ok: 123", onSuccess);
        Assert.Equal("ng: test", onFailure);
    }

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task IfSuccessAsyncCallsWhenSuccess()
    {
        // Arrange
        var received = 0;

        // Act
        await SuccessSource(123).IfSuccessAsync(async x =>
        {
            await Task.Yield();
            received = x;
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(123, received);
    }

    [Fact]
    public async Task IfSuccessAsyncSkipsWhenFailure()
    {
        // Arrange
        var called = false;

        // Act
        await FailureSource(new Error("test")).IfSuccessAsync(async _ =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        // Assert
        Assert.False(called);
    }

    [Fact]
    public async Task IfFailureAsyncCallsWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act
        await FailureSource(error).IfFailureAsync(async e =>
        {
            await Task.Yield();
            received = e;
        }).ConfigureAwait(true);

        // Assert
        Assert.Same(error, received);
    }

    //--------------------------------------------------------------------------------
    // Chain
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task ChainComposesMultipleStages()
    {
        // Act
        var message = await SuccessSource(2)
            .EnsureAsync(static x => x > 0, new Error("range"))
            .MapAsync(static x => x + 1)
            .BindAsync(static async x =>
            {
                await Task.Yield();
                return Result.Success(x * 10);
            })
            .MapErrorAsync(static e => new Error($"rejected: {e.Message}"))
            .MatchAsync(static x => $"ok: {x}", static e => e.Message)
            .ConfigureAwait(true);

        // Assert
        Assert.Equal("ok: 30", message);
    }

    [Fact]
    public async Task ChainShortCircuitsOnFailure()
    {
        // Arrange
        var mapped = false;

        // Act
        var message = await SuccessSource(-1)
            .EnsureAsync(static x => x > 0, new Error("range"))
            .MapAsync(x =>
            {
                mapped = true;
                return x + 1;
            })
            .MatchAsync(static x => $"ok: {x}", static e => $"ng: {e.Message}")
            .ConfigureAwait(true);

        // Assert
        Assert.False(mapped);
        Assert.Equal("ng: range", message);
    }

    //--------------------------------------------------------------------------------
    // Result (non-generic)
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task BindAsyncFromUnitSyncContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSourceUnit().BindAsync(static () => Result.Failure(new Error("next"))).ConfigureAwait(true);

        // Assert
        Assert.Equal("next", result.Error?.Message);
    }

    [Fact]
    public async Task BindAsyncFromUnitValueTaskContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSourceUnit().BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success();
        }).ConfigureAwait(true);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsyncFromUnitSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = await FailureSourceUnit(error).BindAsync(static () => Result.Success()).ConfigureAwait(true);

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task MapErrorAsyncFromUnitTransformsWhenFailure()
    {
        // Act
        var result = await FailureSourceUnit(new Error("test")).MapErrorAsync(static e => new Error($"wrapped: {e.Message}")).ConfigureAwait(true);

        // Assert
        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    [Fact]
    public async Task IfFailureAsyncFromUnitCallsWhenFailure()
    {
        // Arrange
        var error = new Error("test");
        Error? received = null;

        // Act
        await FailureSourceUnit(error).IfFailureAsync(async e =>
        {
            await Task.Yield();
            received = e;
        }).ConfigureAwait(true);

        // Assert
        Assert.Same(error, received);
    }

    [Fact]
    public async Task BindAsyncFromUnitToGenericSyncContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSourceUnit().BindAsync(static () => Result.Success(123)).ConfigureAwait(true);

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task BindAsyncFromUnitToGenericSkipsWhenFailure()
    {
        // Arrange
        var error = new Error("test");

        // Act
        var result = await FailureSourceUnit(error).BindAsync(static () => Result.Success(123)).ConfigureAwait(true);

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task BindAsyncFromUnitToGenericValueTaskContinuationChainsWhenSuccess()
    {
        // Act
        var result = await SuccessSourceUnit().BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success(123);
        }).ConfigureAwait(true);

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task MatchAsyncFromUnitSelectsBranch()
    {
        // Act
        var message = await FailureSourceUnit(new Error("test")).MatchAsync(static () => "ok", static e => $"ng: {e.Message}").ConfigureAwait(true);

        // Assert
        Assert.Equal("ng: test", message);
    }

    [Fact]
    public async Task IfSuccessAsyncFromUnitCallsWhenSuccess()
    {
        // Arrange
        var called = false;

        // Act
        await SuccessSourceUnit().IfSuccessAsync(async () =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        // Assert
        Assert.True(called);
    }
}
