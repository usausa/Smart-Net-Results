namespace Smart.Results;

using System.Runtime.CompilerServices;

public static class Maybe
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Maybe<T> Of<T>(T? value) =>
        value is null ? default : new Maybe<T>(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Maybe<T> None<T>() => default;
}
