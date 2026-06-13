namespace Smart.Results;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T value;

    private readonly Error? error;

    internal Result(T value)
    {
        IsSuccess = true;
        this.value = value;
        error = null;
    }

    internal Result(Error error)
    {
        IsSuccess = false;
        value = default!;
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

    public T Value => IsSuccess ? value : throw new InvalidOperationException($"Result is failure. message=[{ResolveError().Message}]");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Error ResolveError() => error ?? Error.Default;

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    // ReSharper disable ParameterHidesMember
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = this.value;
        return IsSuccess;
    }
    // ReSharper restore ParameterHidesMember

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetValueOrDefault() =>
        IsSuccess ? value : default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue) =>
        IsSuccess ? value : defaultValue;

    public T GetValueOrDefault(Func<Error, T> defaultFactory) =>
        IsSuccess ? value : defaultFactory(ResolveError());

    //--------------------------------------------------------------------------------
    // Deconstruct
    //--------------------------------------------------------------------------------

    // ReSharper disable ParameterHidesMember
    public void Deconstruct(out bool isSuccess, out T? value)
    {
        isSuccess = IsSuccess;
        value = this.value;
    }
    // ReSharper restore ParameterHidesMember

    // ReSharper disable ParameterHidesMember
    public void Deconstruct(out bool isSuccess, out T? value, out Error? error)
    {
        isSuccess = IsSuccess;
        value = this.value;
        error = Error;
    }
    // ReSharper restore ParameterHidesMember

    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    public Result<TResult> Map<TResult>(Func<T, TResult> func) =>
        IsSuccess ? new Result<TResult>(func(value)) : new Result<TResult>(ResolveError());

    public Result<TResult> Map<TState, TResult>(TState state, Func<T, TState, TResult> func) =>
        IsSuccess ? new Result<TResult>(func(value, state)) : new Result<TResult>(ResolveError());

    public async ValueTask<Result<TResult>> MapAsync<TResult>(Func<T, ValueTask<TResult>> func) =>
        IsSuccess ? new Result<TResult>(await func(value).ConfigureAwait(false)) : new Result<TResult>(ResolveError());

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> func) =>
        IsSuccess ? func(value) : new Result<TResult>(ResolveError());

    public Result<TResult> Bind<TState, TResult>(TState state, Func<T, TState, Result<TResult>> func) =>
        IsSuccess ? func(value, state) : new Result<TResult>(ResolveError());

    public Result Bind(Func<T, Result> func) =>
        IsSuccess ? func(value) : new Result(false, ResolveError());

    public Result Bind<TState>(TState state, Func<T, TState, Result> func) =>
        IsSuccess ? func(value, state) : new Result(false, ResolveError());

    public ValueTask<Result<TResult>> BindAsync<TResult>(Func<T, ValueTask<Result<TResult>>> func) =>
        IsSuccess ? func(value) : new ValueTask<Result<TResult>>(new Result<TResult>(ResolveError()));

    public ValueTask<Result> BindAsync(Func<T, ValueTask<Result>> func) =>
        IsSuccess ? func(value) : new ValueTask<Result>(new Result(false, ResolveError()));

    //--------------------------------------------------------------------------------
    // Ensure
    //--------------------------------------------------------------------------------

    // ReSharper disable ParameterHidesMember
    public Result<T> Ensure(Func<T, bool> predicate, Error error) =>
        !IsSuccess || predicate(value) ? this : new Result<T>(error);
    // ReSharper restore ParameterHidesMember

    // ReSharper disable ParameterHidesMember
    public Result<T> Ensure<TState>(TState state, Func<T, TState, bool> predicate, Error error) =>
        !IsSuccess || predicate(value, state) ? this : new Result<T>(error);
    // ReSharper restore ParameterHidesMember

    //--------------------------------------------------------------------------------
    // MapError
    //--------------------------------------------------------------------------------

    public Result<T> MapError(Func<Error, Error> func) =>
        IsSuccess ? this : new Result<T>(func(ResolveError()));

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(value) : onFailure(ResolveError());

    public TResult Match<TState, TResult>(TState state, Func<T, TState, TResult> onSuccess, Func<Error, TState, TResult> onFailure) =>
        IsSuccess ? onSuccess(value, state) : onFailure(ResolveError(), state);

    public async ValueTask<TResult> MatchAsync<TResult>(Func<T, ValueTask<TResult>> onSuccess, Func<Error, ValueTask<TResult>> onFailure) =>
        IsSuccess ? await onSuccess(value).ConfigureAwait(false) : await onFailure(ResolveError()).ConfigureAwait(false);

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    public void IfSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(value);
        }
    }

    public void IfSuccess<TState>(TState state, Action<T, TState> action)
    {
        if (IsSuccess)
        {
            action(value, state);
        }
    }

    public ValueTask IfSuccessAsync(Func<T, ValueTask> action) =>
        IsSuccess ? action(value) : default;

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
    public static implicit operator Result<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result<T>(Error error) => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Result(Result<T> result) =>
        new(result.IsSuccess, result.IsSuccess ? null : result.ResolveError());
#pragma warning restore CA2225

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    public bool Equals(Result<T> other) =>
        (IsSuccess == other.IsSuccess) && Equals(error, other.error) && EqualityComparer<T>.Default.Equals(value, other.value);

    public override bool Equals(object? obj) =>
        obj is Result<T> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(IsSuccess, error, value);

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    //--------------------------------------------------------------------------------
    // ToString
    //--------------------------------------------------------------------------------

    public override string ToString() =>
        IsSuccess ? $"Success({value})" : $"Failure({ResolveError().Message})";
}
