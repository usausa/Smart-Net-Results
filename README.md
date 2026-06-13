# Smart-Results

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Results.svg)](https://www.nuget.org/packages/Usa.Smart.Results)

Result pattern and maybe (option) types for the Smart library family.

All types are `readonly struct`. Errors form a `record` hierarchy you branch on with `switch`, and async is unified on `ValueTask`.

## Summary

### Result

* `Result` / `Result<T>` — readonly struct result; the success path allocates nothing and `default` is a failure
* `Result.Success` / `Failure` — factories; a value or an `Error` also converts implicitly (`return value;` / `return new NotFoundError(id);`)
* `Result.Try` / `TryAsync` — run a delegate and capture any thrown exception as an `ExceptionError`
* `Map` / `Bind` / `Ensure` / `MapError` / `Match` / `IfSuccess` / `IfFailure` — combinators, each with a `state` overload to avoid closures

### Maybe

* `Maybe<T>` — readonly struct option type; `default` is None and a null value converts to None
* `Maybe.Of` / `Maybe.None<T>` / `Maybe<T>.None` — factories
* `Map` / `Bind` / `Match` / `IfHasValue` — combinators (use these instead of only checking `HasValue`)

### Error

* `Error` — `record` base error carrying a `Message`; derive application-specific errors and match them with `switch`
* `ExceptionError` — wraps an exception caught by `Try` / `TryAsync`
* `AggregateError` — multiple errors collected by `Combine`

### Extensions

* `ToResult` / `ToMaybe` — convert between a nullable value, `Maybe<T>` and `Result<T>`
* `Combine` — aggregate `IEnumerable<Result>` / `IEnumerable<Result<T>>` into a single result
* Async source chaining over `ValueTask<Result>` / `ValueTask<Result<T>>` — `MapAsync` / `BindAsync` / `EnsureAsync` / `MapErrorAsync` / `MatchAsync` / `IfSuccessAsync` / `IfFailureAsync`

## Usage

### Returning a result

A value or an error can be returned directly. Define errors as `record` types derived from `Error`.

```csharp
public sealed record NotFoundError(int Id) : Error($"User is not found. id=[{Id}]");
public sealed record ValidationError(string Field, string Detail) : Error($"{Field}: {Detail}");

public Result<User> FindUser(int id)
{
    if (id <= 0)
    {
        return new ValidationError(nameof(id), "id must be positive.");
    }

    var user = repository.Find(id);
    if (user is null)
    {
        return new NotFoundError(id);
    }

    return user;   // T -> Result<T>
}
```

### Handling with switch

Rather than only checking `IsSuccess`, pattern match on `Error` with a `switch` to handle success and each error kind exhaustively in one place. The `null` case of `Error` represents success.

```csharp
var result = FindUser(id);
switch (result.Error)
{
    case null:
        // Success; result.Value is available here
        Render(result.Value);
        break;

    case NotFoundError e:
        ShowNotFound(e.Id);
        break;

    case ValidationError e:
        ShowValidation(e.Field, e.Detail);
        break;

    case ExceptionError e:
        logger.LogError(e.Exception, "unexpected");
        break;

    default:
        ShowError(result.Error.Message);
        break;
}
```

A `switch` expression with property patterns folds the same branches into a value:

```csharp
public sealed record ApiError(HttpStatusCode StatusCode) : Error($"API error. status=[{(int)StatusCode}]");

var view = result.Error switch
{
    null => UserView.From(result.Value),
    NotFoundError => UserView.NotFound,
    ApiError { StatusCode: HttpStatusCode.Unauthorized } => UserView.Login,
    ApiError { StatusCode: HttpStatusCode.ServiceUnavailable } => UserView.Retry,
    _ => UserView.Error(result.Error.Message)
};
```

To fold without a `switch`, use `Match`; to extract only the value, use `TryGetValue` / `GetValueOrDefault`.

```csharp
var message = result.Match(
    static user => $"hello {user.Name}",
    static error => error.Message);

if (result.TryGetValue(out var user))
{
    Render(user);
}
```

> `result.Value` throws `InvalidOperationException` on failure (the same contract as `Nullable<T>.Value`). When branching is involved, prefer `switch` / `Match` / `TryGetValue`.

### Maybe

Use `Maybe<T>` to distinguish "nothing found is normal" (e.g. a lookup miss) from "failure". It has no `Error`, so branch with `Match(onValue, onNone)` or with `TryGetValue`.

```csharp
public Maybe<string> FindName(int id) =>
    store.TryGetValue(id, out var name) ? Maybe.Of(name) : Maybe<string>.None;

// Match (None is propagated through Map)
var label = FindName(id)
    .Map(static x => x.ToUpperInvariant())
    .Match(
        static x => $"name=[{x}]",
        static () => "name=[(none)]");

// Turn absence into an error and move into the Result world
Result<string> result = FindName(id).ToResult(new NotFoundError(id));
```

### Async

Async is unified on `ValueTask`. After awaiting, the same `switch` applies:

```csharp
var result = await client.GetUserAsync(id);   // ValueTask<Result<User>>
var view = result.Error switch
{
    null => UserView.From(result.Value),
    NotFoundError => UserView.NotFound,
    ApiError { StatusCode: HttpStatusCode.Unauthorized } => UserView.Login,
    _ => UserView.Error(result.Error.Message)
};
```

When a service returns `ValueTask<Result<T>>`, chain the operations directly — there is a single `await` at the end of the chain:

```csharp
var message = await client.GetUserAsync(id)              // ValueTask<Result<User>>
    .EnsureAsync(static u => u.IsActive, new Error("inactive."))
    .MapAsync(static u => u.Name)                        // synchronous continuation
    .BindAsync(LoadProfileAsync)                         // async continuation: Func<T, ValueTask<Result<Profile>>>
    .MatchAsync(
        static profile => profile.DisplayName,
        static error => error.Message);
```

| Method | Continuation shape |
|---|---|
| `MapAsync` | synchronous (`Func<T, TResult>`, with a state overload) |
| `BindAsync` | synchronous (`Result<K>`) / asynchronous (`ValueTask<Result<K>>`) |
| `EnsureAsync` / `MapErrorAsync` | synchronous |
| `MatchAsync` | synchronous (fold) |
| `IfSuccessAsync` / `IfFailureAsync` | asynchronous (`ValueTask` side effect) |

> Only `Bind` offers both a synchronous and a `ValueTask` continuation under the same name (they are distinguished by whether the continuation returns `Result<K>` or `ValueTask<Result<K>>`). `Map` / `Match` provide only the synchronous continuation; express a mid-chain async transform with `BindAsync(async x => Result.Success(await f(x)))`.

### Combinators

Each is applied only on success; a failure passes straight through. To avoid closures, use the `state` overloads together with `static` lambdas.

```csharp
var total = ParseQuantity(input)                                   // Result<int>
    .Ensure(static x => x is > 0 and <= 99, new Error("out of range."))
    .Map(unitPrice, static (x, price) => x * price)                // with state (no closure)
    .Bind(CheckCreditLimit)                                        // returns Result<int>
    .MapError(static e => new Error($"rejected: {e.Message}"))
    .Match(static x => $"accepted: {x}", static e => e.Message);
```

### Errors

`Result.Try` captures a thrown exception as an `ExceptionError`, and `Combine` aggregates many results into one.

```csharp
var read = Result.Try(() => File.ReadAllText(path));   // ExceptionError on throw

var combined = new[] { ValidateName(a), ValidateName(b) }.Combine();   // Result<string[]>
if (combined.Error is AggregateError aggregate)
{
    foreach (var error in aggregate.Errors)
    {
        ShowValidation(error.Message);
    }
}
```

`string` and `Exception` convert implicitly to `Error`:

```csharp
Error error = "invalid.";                         // -> new Error("invalid.")
Error wrapped = new InvalidOperationException();   // -> new ExceptionError(...)
```
