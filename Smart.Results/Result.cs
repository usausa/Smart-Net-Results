namespace Smart.Results;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

public readonly struct Result : IEquatable<Result>
{
    private readonly Error? error;

    internal Result(bool success, Error? error)
    {
        IsSuccess = success;
        this.error = error;
    }

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => !IsSuccess;

    public Error? Error => IsSuccess ? null : ResolveError();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Error ResolveError() => error ?? Error.Default;

    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Success() => new(true, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Failure(Error error) => new(false, error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Success<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> Failure<T>(Error error) => new(error);

    //--------------------------------------------------------------------------------
    // Try
    //--------------------------------------------------------------------------------

    public static Result Try(Action action)
    {
        try
        {
            action();
            return new(true, null);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            return new(false, new ExceptionError(ex));
        }
#pragma warning restore CA1031
    }

    public static Result<T> Try<T>(Func<T> func)
    {
        try
        {
            return new(func());
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            return new(new ExceptionError(ex));
        }
#pragma warning restore CA1031
    }

    public static async ValueTask<Result> TryAsync(Func<ValueTask> func)
    {
        try
        {
            await func().ConfigureAwait(false);
            return new(true, null);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            return new(false, new ExceptionError(ex));
        }
#pragma warning restore CA1031
    }

    public static async ValueTask<Result<T>> TryAsync<T>(Func<ValueTask<T>> func)
    {
        try
        {
            return new(await func().ConfigureAwait(false));
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            return new(new ExceptionError(ex));
        }
#pragma warning restore CA1031
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    public Result Bind(Func<Result> func) =>
        IsSuccess ? func() : this;

    public Result Bind<TState>(TState state, Func<TState, Result> func) =>
        IsSuccess ? func(state) : this;

    public Result<T> Bind<T>(Func<Result<T>> func) =>
        IsSuccess ? func() : new Result<T>(ResolveError());

    public Result<T> Bind<TState, T>(TState state, Func<TState, Result<T>> func) =>
        IsSuccess ? func(state) : new Result<T>(ResolveError());

    public ValueTask<Result> BindAsync(Func<ValueTask<Result>> func) =>
        IsSuccess ? func() : new ValueTask<Result>(this);

    public ValueTask<Result<T>> BindAsync<T>(Func<ValueTask<Result<T>>> func) =>
        IsSuccess ? func() : new ValueTask<Result<T>>(new Result<T>(ResolveError()));

    //--------------------------------------------------------------------------------
    // MapError
    //--------------------------------------------------------------------------------

    public Result MapError(Func<Error, Error> func) =>
        IsSuccess ? this : new Result(false, func(ResolveError()));

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(ResolveError());

    public TResult Match<TState, TResult>(TState state, Func<TState, TResult> onSuccess, Func<Error, TState, TResult> onFailure) =>
        IsSuccess ? onSuccess(state) : onFailure(ResolveError(), state);

    public async ValueTask<TResult> MatchAsync<TResult>(Func<ValueTask<TResult>> onSuccess, Func<Error, ValueTask<TResult>> onFailure) =>
        IsSuccess ? await onSuccess().ConfigureAwait(false) : await onFailure(ResolveError()).ConfigureAwait(false);

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    public void IfSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
    }

    public void IfSuccess<TState>(TState state, Action<TState> action)
    {
        if (IsSuccess)
        {
            action(state);
        }
    }

    public ValueTask IfSuccessAsync(Func<ValueTask> action) =>
        IsSuccess ? action() : default;

    public void IfFailure(Action<Error> action)
    {
        if (!IsSuccess)
        {
            action(ResolveError());
        }
    }

    public void IfFailure<TState>(TState state, Action<Error, TState> action)
    {
        if (!IsSuccess)
        {
            action(ResolveError(), state);
        }
    }

    public ValueTask IfFailureAsync(Func<Error, ValueTask> action) =>
        IsSuccess ? default : action(ResolveError());

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

#pragma warning disable CA2225
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result(Error error) => new(false, error);
#pragma warning restore CA2225

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    public bool Equals(Result other) =>
        (IsSuccess == other.IsSuccess) && Equals(error, other.error);

    public override bool Equals(object? obj) =>
        obj is Result other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(IsSuccess, error);

    public static bool operator ==(Result left, Result right) => left.Equals(right);

    public static bool operator !=(Result left, Result right) => !left.Equals(right);

    //--------------------------------------------------------------------------------
    // ToString
    //--------------------------------------------------------------------------------

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({ResolveError().Message})";
}
