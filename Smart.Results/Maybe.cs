namespace Smart.Results;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

public static class Maybe
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Maybe<T> Of<T>(T? value) =>
        value is null ? default : new Maybe<T>(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Maybe<T> None<T>() => default;
}

public readonly struct Maybe<T> : IEquatable<Maybe<T>>
{
    private readonly T value;

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    public static Maybe<T> None => default;

    public bool HasValue { get; }

    public T Value => HasValue ? value : throw new InvalidOperationException("Maybe has no value.");

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    internal Maybe(T value)
    {
        HasValue = true;
        this.value = value;
    }

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    // ReSharper disable ParameterHidesMember
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = this.value;
        return HasValue;
    }
    // ReSharper restore ParameterHidesMember

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? GetValueOrDefault() =>
        HasValue ? value : default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue) =>
        HasValue ? value : defaultValue;

    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    public Maybe<TResult> Map<TResult>(Func<T, TResult> func) =>
        HasValue ? Maybe.Of(func(value)) : default;

    public Maybe<TResult> Map<TState, TResult>(TState state, Func<T, TState, TResult> func) =>
        HasValue ? Maybe.Of(func(value, state)) : default;

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    public Maybe<TResult> Bind<TResult>(Func<T, Maybe<TResult>> func) =>
        HasValue ? func(value) : default;

    public Maybe<TResult> Bind<TState, TResult>(TState state, Func<T, TState, Maybe<TResult>> func) =>
        HasValue ? func(value, state) : default;

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    public TResult Match<TResult>(Func<T, TResult> onValue, Func<TResult> onNone) =>
        HasValue ? onValue(value) : onNone();

    public TResult Match<TState, TResult>(TState state, Func<T, TState, TResult> onValue, Func<TState, TResult> onNone) =>
        HasValue ? onValue(value, state) : onNone(state);

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    public void IfHasValue(Action<T> action)
    {
        if (HasValue)
        {
            action(value);
        }
    }

    public void IfHasValue<TState>(TState state, Action<T, TState> action)
    {
        if (HasValue)
        {
            action(value, state);
        }
    }

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

#pragma warning disable CA2225
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Maybe<T>(T? value) =>
        value is null ? default : new Maybe<T>(value);
#pragma warning restore CA2225

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    public bool Equals(Maybe<T> other) =>
        (HasValue == other.HasValue) && EqualityComparer<T>.Default.Equals(value, other.value);

    public override bool Equals(object? obj) =>
        obj is Maybe<T> other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(HasValue, value);

    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);

    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    //--------------------------------------------------------------------------------
    // ToString
    //--------------------------------------------------------------------------------

    public override string ToString() =>
        HasValue ? $"Some({value})" : "None";
}
