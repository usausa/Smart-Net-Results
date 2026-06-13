namespace Smart.Results;

public static partial class ResultExtensions
{
    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    public static async ValueTask<Result<TResult>> MapAsync<T, TResult>(this ValueTask<Result<T>> source, Func<T, TResult> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.Map(func);
    }

    public static async ValueTask<Result<TResult>> MapAsync<T, TState, TResult>(this ValueTask<Result<T>> source, TState state, Func<T, TState, TResult> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.Map(state, func);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    public static async ValueTask<Result<TResult>> BindAsync<T, TResult>(this ValueTask<Result<T>> source, Func<T, Result<TResult>> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.Bind(func);
    }

    public static async ValueTask<Result<TResult>> BindAsync<T, TResult>(this ValueTask<Result<T>> source, Func<T, ValueTask<Result<TResult>>> func)
    {
        var result = await source.ConfigureAwait(false);
        return await result.BindAsync(func).ConfigureAwait(false);
    }

    public static async ValueTask<Result> BindAsync<T>(this ValueTask<Result<T>> source, Func<T, Result> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.Bind(func);
    }

    public static async ValueTask<Result> BindAsync<T>(this ValueTask<Result<T>> source, Func<T, ValueTask<Result>> func)
    {
        var result = await source.ConfigureAwait(false);
        return await result.BindAsync(func).ConfigureAwait(false);
    }

    //--------------------------------------------------------------------------------
    // Ensure
    //--------------------------------------------------------------------------------

    public static async ValueTask<Result<T>> EnsureAsync<T>(this ValueTask<Result<T>> source, Func<T, bool> predicate, Error error)
    {
        var result = await source.ConfigureAwait(false);
        return result.Ensure(predicate, error);
    }

    //--------------------------------------------------------------------------------
    // MapError
    //--------------------------------------------------------------------------------

    public static async ValueTask<Result<T>> MapErrorAsync<T>(this ValueTask<Result<T>> source, Func<Error, Error> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.MapError(func);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    public static async ValueTask<TResult> MatchAsync<T, TResult>(this ValueTask<Result<T>> source, Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        var result = await source.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    public static async ValueTask IfSuccessAsync<T>(this ValueTask<Result<T>> source, Func<T, ValueTask> action)
    {
        var result = await source.ConfigureAwait(false);
        await result.IfSuccessAsync(action).ConfigureAwait(false);
    }

    public static async ValueTask IfFailureAsync<T>(this ValueTask<Result<T>> source, Func<Error, ValueTask> action)
    {
        var result = await source.ConfigureAwait(false);
        await result.IfFailureAsync(action).ConfigureAwait(false);
    }

    //--------------------------------------------------------------------------------
    // Bind (Result)
    //--------------------------------------------------------------------------------

    public static async ValueTask<Result> BindAsync(this ValueTask<Result> source, Func<Result> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.Bind(func);
    }

    public static async ValueTask<Result> BindAsync(this ValueTask<Result> source, Func<ValueTask<Result>> func)
    {
        var result = await source.ConfigureAwait(false);
        return await result.BindAsync(func).ConfigureAwait(false);
    }

    public static async ValueTask<Result<T>> BindAsync<T>(this ValueTask<Result> source, Func<Result<T>> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.Bind(func);
    }

    public static async ValueTask<Result<T>> BindAsync<T>(this ValueTask<Result> source, Func<ValueTask<Result<T>>> func)
    {
        var result = await source.ConfigureAwait(false);
        return await result.BindAsync(func).ConfigureAwait(false);
    }

    //--------------------------------------------------------------------------------
    // MapError (Result)
    //--------------------------------------------------------------------------------

    public static async ValueTask<Result> MapErrorAsync(this ValueTask<Result> source, Func<Error, Error> func)
    {
        var result = await source.ConfigureAwait(false);
        return result.MapError(func);
    }

    //--------------------------------------------------------------------------------
    // Match (Result)
    //--------------------------------------------------------------------------------

    public static async ValueTask<TResult> MatchAsync<TResult>(this ValueTask<Result> source, Func<TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        var result = await source.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    //--------------------------------------------------------------------------------
    // If (Result)
    //--------------------------------------------------------------------------------

    public static async ValueTask IfSuccessAsync(this ValueTask<Result> source, Func<ValueTask> action)
    {
        var result = await source.ConfigureAwait(false);
        await result.IfSuccessAsync(action).ConfigureAwait(false);
    }

    public static async ValueTask IfFailureAsync(this ValueTask<Result> source, Func<Error, ValueTask> action)
    {
        var result = await source.ConfigureAwait(false);
        await result.IfFailureAsync(action).ConfigureAwait(false);
    }
}
