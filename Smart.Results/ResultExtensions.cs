namespace Smart.Results;

using System.Runtime.CompilerServices;

public static partial class ResultExtensions
{
    //--------------------------------------------------------------------------------
    // ToResult
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> ToResult<T>(this T? value, Error error)
        where T : class
    {
        return value is null ? Result.Failure<T>(error) : Result.Success(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> ToResult<T>(this T? value, Error error)
        where T : struct
    {
        return value.HasValue ? Result.Success(value.Value) : Result.Failure<T>(error);
    }

    //--------------------------------------------------------------------------------
    // Combine
    //--------------------------------------------------------------------------------

    public static Result Combine(this IEnumerable<Result> results)
    {
        List<Error>? errors = null;
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                errors ??= [];
                errors.Add(result.Error);
            }
        }

        return errors is null ? Result.Success() : Result.Failure(new AggregateError(errors));
    }

    public static Result<T[]> Combine<T>(this IEnumerable<Result<T>> results)
    {
        List<T>? values = null;
        List<Error>? errors = null;
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                errors ??= [];
                errors.Add(result.Error);
                values = null; // 失敗確定後は成功値を破棄
            }
            else if (errors is null)
            {
                values ??= [];
                values.Add(result.Value);
            }
        }

        return errors is null ? Result.Success(values?.ToArray() ?? []) : Result.Failure<T[]>(new AggregateError(errors));
    }

    //--------------------------------------------------------------------------------
    // Maybe
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<T> ToResult<T>(this Maybe<T> maybe, Error error)
    {
        return maybe.TryGetValue(out var value) ? Result.Success(value) : Result.Failure<T>(error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Maybe<T> ToMaybe<T>(this Result<T> result)
    {
        return result.TryGetValue(out var value) ? Maybe.Of(value) : Maybe<T>.None;
    }
}
