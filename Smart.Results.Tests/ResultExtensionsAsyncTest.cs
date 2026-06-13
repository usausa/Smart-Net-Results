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
        var result = await SuccessSource(2).MapAsync(static x => x * 10).ConfigureAwait(true);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncWithStateTransformsWhenSuccess()
    {
        var result = await SuccessSource(2).MapAsync(10, static (x, state) => x * state).ConfigureAwait(true);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task MapAsyncSkipsWhenFailure()
    {
        var error = new Error("test");
        var result = await FailureSource(error).MapAsync(static x => x * 10).ConfigureAwait(true);

        Assert.Same(error, result.Error);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task BindAsyncSyncContinuationChainsWhenSuccess()
    {
        var result = await SuccessSource(2).BindAsync(static x => Result.Success(x * 10)).ConfigureAwait(true);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task BindAsyncValueTaskContinuationChainsWhenSuccess()
    {
        var result = await SuccessSource(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return Result.Success(x * 10);
        }).ConfigureAwait(true);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task BindAsyncSkipsWhenFailure()
    {
        var error = new Error("test");
        var result = await FailureSource(error).BindAsync(static x => Result.Success(x * 10)).ConfigureAwait(true);

        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task BindAsyncToUnitSyncContinuationChainsWhenSuccess()
    {
        var result = await SuccessSource(2).BindAsync(static x => x > 0 ? Result.Success() : Result.Failure(new Error("negative"))).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsyncToUnitValueTaskContinuationChainsWhenSuccess()
    {
        var result = await SuccessSource(2).BindAsync(static async x =>
        {
            await Task.Yield();
            return x > 0 ? Result.Success() : Result.Failure(new Error("negative"));
        }).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
    }

    //--------------------------------------------------------------------------------
    // Ensure / MapError
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task EnsureAsyncFailsWhenPredicateFails()
    {
        var error = new Error("invalid");
        var result = await SuccessSource(-1).EnsureAsync(static x => x > 0, error).ConfigureAwait(true);

        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task MapErrorAsyncTransformsWhenFailure()
    {
        var result = await FailureSource(new Error("test")).MapErrorAsync(static e => new Error($"wrapped: {e.Message}")).ConfigureAwait(true);

        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task MatchAsyncSelectsBranch()
    {
        var onSuccess = await SuccessSource(123).MatchAsync(static x => $"ok: {x}", static e => $"ng: {e.Message}").ConfigureAwait(true);
        var onFailure = await FailureSource(new Error("test")).MatchAsync(static x => $"ok: {x}", static e => $"ng: {e.Message}").ConfigureAwait(true);

        Assert.Equal("ok: 123", onSuccess);
        Assert.Equal("ng: test", onFailure);
    }

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task IfSuccessAsyncCallsWhenSuccess()
    {
        var received = 0;
        await SuccessSource(123).IfSuccessAsync(async x =>
        {
            await Task.Yield();
            received = x;
        }).ConfigureAwait(true);

        Assert.Equal(123, received);
    }

    [Fact]
    public async Task IfSuccessAsyncSkipsWhenFailure()
    {
        var called = false;
        await FailureSource(new Error("test")).IfSuccessAsync(async _ =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        Assert.False(called);
    }

    [Fact]
    public async Task IfFailureAsyncCallsWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;
        await FailureSource(error).IfFailureAsync(async e =>
        {
            await Task.Yield();
            received = e;
        }).ConfigureAwait(true);

        Assert.Same(error, received);
    }

    //--------------------------------------------------------------------------------
    // Chain
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task ChainComposesMultipleStages()
    {
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

        Assert.Equal("ok: 30", message);
    }

    [Fact]
    public async Task ChainShortCircuitsOnFailure()
    {
        var mapped = false;
        var message = await SuccessSource(-1)
            .EnsureAsync(static x => x > 0, new Error("range"))
            .MapAsync(x =>
            {
                mapped = true;
                return x + 1;
            })
            .MatchAsync(static x => $"ok: {x}", static e => $"ng: {e.Message}")
            .ConfigureAwait(true);

        Assert.False(mapped);
        Assert.Equal("ng: range", message);
    }

    //--------------------------------------------------------------------------------
    // Result (non-generic)
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task BindAsyncFromUnitSyncContinuationChainsWhenSuccess()
    {
        var result = await SuccessSourceUnit().BindAsync(static () => Result.Failure(new Error("next"))).ConfigureAwait(true);

        Assert.Equal("next", result.Error?.Message);
    }

    [Fact]
    public async Task BindAsyncFromUnitValueTaskContinuationChainsWhenSuccess()
    {
        var result = await SuccessSourceUnit().BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success();
        }).ConfigureAwait(true);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BindAsyncFromUnitSkipsWhenFailure()
    {
        var error = new Error("test");
        var result = await FailureSourceUnit(error).BindAsync(static () => Result.Success()).ConfigureAwait(true);

        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task MapErrorAsyncFromUnitTransformsWhenFailure()
    {
        var result = await FailureSourceUnit(new Error("test")).MapErrorAsync(static e => new Error($"wrapped: {e.Message}")).ConfigureAwait(true);

        Assert.Equal("wrapped: test", result.Error?.Message);
    }

    [Fact]
    public async Task IfFailureAsyncFromUnitCallsWhenFailure()
    {
        var error = new Error("test");
        Error? received = null;
        await FailureSourceUnit(error).IfFailureAsync(async e =>
        {
            await Task.Yield();
            received = e;
        }).ConfigureAwait(true);

        Assert.Same(error, received);
    }

    [Fact]
    public async Task BindAsyncFromUnitToGenericSyncContinuationChainsWhenSuccess()
    {
        var result = await SuccessSourceUnit().BindAsync(static () => Result.Success(123)).ConfigureAwait(true);

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task BindAsyncFromUnitToGenericSkipsWhenFailure()
    {
        var error = new Error("test");
        var result = await FailureSourceUnit(error).BindAsync(static () => Result.Success(123)).ConfigureAwait(true);

        Assert.Same(error, result.Error);
    }

    [Fact]
    public async Task BindAsyncFromUnitToGenericValueTaskContinuationChainsWhenSuccess()
    {
        var result = await SuccessSourceUnit().BindAsync(static async () =>
        {
            await Task.Yield();
            return Result.Success(123);
        }).ConfigureAwait(true);

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task MatchAsyncFromUnitSelectsBranch()
    {
        var message = await FailureSourceUnit(new Error("test")).MatchAsync(static () => "ok", static e => $"ng: {e.Message}").ConfigureAwait(true);

        Assert.Equal("ng: test", message);
    }

    [Fact]
    public async Task IfSuccessAsyncFromUnitCallsWhenSuccess()
    {
        var called = false;
        await SuccessSourceUnit().IfSuccessAsync(async () =>
        {
            await Task.Yield();
            called = true;
        }).ConfigureAwait(true);

        Assert.True(called);
    }
}
