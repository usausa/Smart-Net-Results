namespace Smart.Results;

public sealed class MaybeTest
{
    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [Fact]
    public void OfReturnsSomeWhenNotNull()
    {
        var maybe = Maybe.Of(123);

        Assert.True(maybe.HasValue);
        Assert.Equal(123, maybe.Value);
    }

    [Fact]
    public void OfReturnsNoneWhenNull()
    {
        var maybe = Maybe.Of<string>(null);

        Assert.False(maybe.HasValue);
    }

    [Fact]
    public void NoneIsNone()
    {
        Assert.False(Maybe.None<int>().HasValue);
        Assert.False(Maybe<int>.None.HasValue);
    }

    [Fact]
    public void DefaultIsNone()
    {
        var maybe = default(Maybe<int>);

        Assert.False(maybe.HasValue);
        Assert.Throws<InvalidOperationException>(() => maybe.Value);
    }

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    [Fact]
    public void ValueThrowsWhenNone()
    {
        Assert.Throws<InvalidOperationException>(() => Maybe<int>.None.Value);
    }

    [Fact]
    public void TryGetValueReturnsValueWhenSome()
    {
        var ret = Maybe.Of(123).TryGetValue(out var value);

        Assert.True(ret);
        Assert.Equal(123, value);
    }

    [Fact]
    public void TryGetValueReturnsFalseWhenNone()
    {
        var ret = Maybe<int>.None.TryGetValue(out _);

        Assert.False(ret);
    }

    [Fact]
    public void GetValueOrDefaultReturnsValueWhenSome()
    {
        Assert.Equal(123, Maybe.Of(123).GetValueOrDefault());
        Assert.Equal(123, Maybe.Of(123).GetValueOrDefault(-1));
    }

    [Fact]
    public void GetValueOrDefaultReturnsDefaultWhenNone()
    {
        Assert.Equal(0, Maybe<int>.None.GetValueOrDefault());
        Assert.Equal(-1, Maybe<int>.None.GetValueOrDefault(-1));
    }

    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapTransformsValueWhenSome()
    {
        var maybe = Maybe.Of(2).Map(static x => x * 10);

        Assert.Equal(20, maybe.Value);
    }

    [Fact]
    public void MapSkipsWhenNone()
    {
        var called = false;
        var maybe = Maybe<int>.None.Map(x =>
        {
            called = true;
            return x * 10;
        });

        Assert.False(called);
        Assert.False(maybe.HasValue);
    }

    [Fact]
    public void MapWithStateTransformsValue()
    {
        var maybe = Maybe.Of(2).Map(10, static (x, state) => x * state);

        Assert.Equal(20, maybe.Value);
    }

    [Fact]
    public void MapToNullProducesNone()
    {
        var maybe = Maybe.Of("test").Map(static _ => (string?)null);

        Assert.False(maybe.HasValue);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    [Fact]
    public void BindChainsWhenSome()
    {
        var maybe = Maybe.Of(2).Bind(static x => Maybe.Of(x * 10));

        Assert.Equal(20, maybe.Value);
    }

    [Fact]
    public void BindSkipsWhenNone()
    {
        var maybe = Maybe<int>.None.Bind(static x => Maybe.Of(x * 10));

        Assert.False(maybe.HasValue);
    }

    [Fact]
    public void BindWithStateChainsWhenSome()
    {
        var maybe = Maybe.Of(2).Bind(10, static (x, state) => Maybe.Of(x * state));

        Assert.Equal(20, maybe.Value);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    [Fact]
    public void MatchCallsOnValueWhenSome()
    {
        var message = Maybe.Of(123).Match(static x => $"some: {x}", static () => "none");

        Assert.Equal("some: 123", message);
    }

    [Fact]
    public void MatchCallsOnNoneWhenNone()
    {
        var message = Maybe<int>.None.Match(static x => $"some: {x}", static () => "none");

        Assert.Equal("none", message);
    }

    [Fact]
    public void MatchWithStateCallsBranch()
    {
        var value = Maybe.Of(2).Match(10, static (x, state) => x * state, static _ => -1);

        Assert.Equal(20, value);
    }

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    [Fact]
    public void IfHasValueCallsActionWhenSome()
    {
        var received = 0;
        Maybe.Of(123).IfHasValue(x => { received = x; });

        Assert.Equal(123, received);
    }

    [Fact]
    public void IfHasValueSkipsWhenNone()
    {
        var called = false;
        Maybe<int>.None.IfHasValue(_ => { called = true; });

        Assert.False(called);
    }

    [Fact]
    public void IfHasValueWithStateCallsAction()
    {
        var received = 0;
        Maybe.Of(2).IfHasValue(10, (x, state) => { received = x * state; });

        Assert.Equal(20, received);
    }

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

    [Fact]
    public void ImplicitConversionFromValue()
    {
        Maybe<int> maybe = 123;

        Assert.Equal(123, maybe.Value);
    }

    [Fact]
    public void ImplicitConversionFromNullProducesNone()
    {
        Maybe<string> maybe = null;

        Assert.False(maybe.HasValue);
    }

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    [Fact]
    public void EqualityForSome()
    {
        Assert.Equal(Maybe.Of(123), Maybe.Of(123));
        Assert.NotEqual(Maybe.Of(123), Maybe.Of(456));
        Assert.Equal(Maybe.Of(123).GetHashCode(), Maybe.Of(123).GetHashCode());
    }

    [Fact]
    public void EqualityForNone()
    {
        Assert.Equal(Maybe<int>.None, Maybe<int>.None);
        Assert.NotEqual(Maybe.Of(0), Maybe<int>.None);
    }

    [Fact]
    public void EqualityOperators()
    {
        // ReSharper disable once EqualExpressionComparison
        var equal = Maybe.Of(123) == Maybe.Of(123);
        var notEqual = Maybe.Of(123) != Maybe<int>.None;

        Assert.True(equal);
        Assert.True(notEqual);
    }

    [Fact]
    public void EqualsObjectOverride()
    {
        object other = "other";
        Assert.True(Maybe.Of(123).Equals((object)Maybe.Of(123)));
        Assert.False(Maybe.Of(123).Equals(other));
        Assert.False(Maybe.Of(123).Equals(null));
    }

    //--------------------------------------------------------------------------------
    // ToString
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToStringRepresentsState()
    {
        Assert.Equal("Some(123)", Maybe.Of(123).ToString());
        Assert.Equal("None", Maybe<int>.None.ToString());
    }

    //--------------------------------------------------------------------------------
    // Conversion
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToResultReturnsSuccessWhenSome()
    {
        var result = Maybe.Of(123).ToResult(new Error("none"));

        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void ToResultReturnsFailureWhenNone()
    {
        var error = new Error("none");
        var result = Maybe<int>.None.ToResult(error);

        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ToMaybeReturnsSomeWhenSuccess()
    {
        var maybe = Result.Success(123).ToMaybe();

        Assert.Equal(123, maybe.Value);
    }

    [Fact]
    public void ToMaybeReturnsNoneWhenFailure()
    {
        var maybe = Result.Failure<int>(new Error("test")).ToMaybe();

        Assert.False(maybe.HasValue);
    }
}
