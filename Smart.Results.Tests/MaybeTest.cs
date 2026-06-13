namespace Smart.Results;

public sealed class MaybeTest
{
    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    [Fact]
    public void OfReturnsSomeWhenNotNull()
    {
        // Act
        var maybe = Maybe.Of(123);

        // Assert
        Assert.True(maybe.HasValue);
        Assert.Equal(123, maybe.Value);
    }

    [Fact]
    public void OfReturnsNoneWhenNull()
    {
        // Act
        var maybe = Maybe.Of<string>(null);

        // Assert
        Assert.False(maybe.HasValue);
    }

    [Fact]
    public void NoneIsNone()
    {
        // Act & Assert
        Assert.False(Maybe.None<int>().HasValue);
        Assert.False(Maybe<int>.None.HasValue);
    }

    [Fact]
    public void DefaultIsNone()
    {
        // Arrange
        var maybe = default(Maybe<int>);

        // Act & Assert
        Assert.False(maybe.HasValue);
        Assert.Throws<InvalidOperationException>(() => maybe.Value);
    }

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    [Fact]
    public void ValueThrowsWhenNone()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Maybe<int>.None.Value);
    }

    [Fact]
    public void TryGetValueReturnsValueWhenSome()
    {
        // Act
        var ret = Maybe.Of(123).TryGetValue(out var value);

        // Assert
        Assert.True(ret);
        Assert.Equal(123, value);
    }

    [Fact]
    public void TryGetValueReturnsFalseWhenNone()
    {
        // Act
        var ret = Maybe<int>.None.TryGetValue(out _);

        // Assert
        Assert.False(ret);
    }

    [Fact]
    public void GetValueOrDefaultReturnsValueWhenSome()
    {
        // Act & Assert
        Assert.Equal(123, Maybe.Of(123).GetValueOrDefault());
        Assert.Equal(123, Maybe.Of(123).GetValueOrDefault(-1));
    }

    [Fact]
    public void GetValueOrDefaultReturnsDefaultWhenNone()
    {
        // Act & Assert
        Assert.Equal(0, Maybe<int>.None.GetValueOrDefault());
        Assert.Equal(-1, Maybe<int>.None.GetValueOrDefault(-1));
    }

    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    [Fact]
    public void MapTransformsValueWhenSome()
    {
        // Act
        var maybe = Maybe.Of(2).Map(static x => x * 10);

        // Assert
        Assert.Equal(20, maybe.Value);
    }

    [Fact]
    public void MapSkipsWhenNone()
    {
        // Arrange
        var called = false;

        // Act
        var maybe = Maybe<int>.None.Map(x =>
        {
            called = true;
            return x * 10;
        });

        // Assert
        Assert.False(called);
        Assert.False(maybe.HasValue);
    }

    [Fact]
    public void MapWithStateTransformsValue()
    {
        // Act
        var maybe = Maybe.Of(2).Map(10, static (x, state) => x * state);

        // Assert
        Assert.Equal(20, maybe.Value);
    }

    [Fact]
    public void MapToNullProducesNone()
    {
        // Act
        var maybe = Maybe.Of("test").Map(static _ => (string?)null);

        // Assert
        Assert.False(maybe.HasValue);
    }

    //--------------------------------------------------------------------------------
    // Bind
    //--------------------------------------------------------------------------------

    [Fact]
    public void BindChainsWhenSome()
    {
        // Act
        var maybe = Maybe.Of(2).Bind(static x => Maybe.Of(x * 10));

        // Assert
        Assert.Equal(20, maybe.Value);
    }

    [Fact]
    public void BindSkipsWhenNone()
    {
        // Act
        var maybe = Maybe<int>.None.Bind(static x => Maybe.Of(x * 10));

        // Assert
        Assert.False(maybe.HasValue);
    }

    [Fact]
    public void BindWithStateChainsWhenSome()
    {
        // Act
        var maybe = Maybe.Of(2).Bind(10, static (x, state) => Maybe.Of(x * state));

        // Assert
        Assert.Equal(20, maybe.Value);
    }

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    [Fact]
    public void MatchCallsOnValueWhenSome()
    {
        // Act
        var message = Maybe.Of(123).Match(static x => $"some: {x}", static () => "none");

        // Assert
        Assert.Equal("some: 123", message);
    }

    [Fact]
    public void MatchCallsOnNoneWhenNone()
    {
        // Act
        var message = Maybe<int>.None.Match(static x => $"some: {x}", static () => "none");

        // Assert
        Assert.Equal("none", message);
    }

    [Fact]
    public void MatchWithStateCallsBranch()
    {
        // Act
        var value = Maybe.Of(2).Match(10, static (x, state) => x * state, static _ => -1);

        // Assert
        Assert.Equal(20, value);
    }

    //--------------------------------------------------------------------------------
    // If
    //--------------------------------------------------------------------------------

    [Fact]
    public void IfHasValueCallsActionWhenSome()
    {
        // Arrange
        var received = 0;

        // Act
        Maybe.Of(123).IfHasValue(x => { received = x; });

        // Assert
        Assert.Equal(123, received);
    }

    [Fact]
    public void IfHasValueSkipsWhenNone()
    {
        // Arrange
        var called = false;

        // Act
        Maybe<int>.None.IfHasValue(_ => { called = true; });

        // Assert
        Assert.False(called);
    }

    [Fact]
    public void IfHasValueWithStateCallsAction()
    {
        // Arrange
        var received = 0;

        // Act
        Maybe.Of(2).IfHasValue(10, (x, state) => { received = x * state; });

        // Assert
        Assert.Equal(20, received);
    }

    //--------------------------------------------------------------------------------
    // Operator
    //--------------------------------------------------------------------------------

    [Fact]
    public void ImplicitConversionFromValue()
    {
        // Act
        Maybe<int> maybe = 123;

        // Assert
        Assert.Equal(123, maybe.Value);
    }

    [Fact]
    public void ImplicitConversionFromNullProducesNone()
    {
        // Act
        Maybe<string> maybe = null;

        // Assert
        Assert.False(maybe.HasValue);
    }

    //--------------------------------------------------------------------------------
    // Equality
    //--------------------------------------------------------------------------------

    [Fact]
    public void EqualityForSome()
    {
        // Act & Assert
        Assert.Equal(Maybe.Of(123), Maybe.Of(123));
        Assert.NotEqual(Maybe.Of(123), Maybe.Of(456));
        Assert.Equal(Maybe.Of(123).GetHashCode(), Maybe.Of(123).GetHashCode());
    }

    [Fact]
    public void EqualityForNone()
    {
        // Act & Assert
        Assert.Equal(Maybe<int>.None, Maybe<int>.None);
        Assert.NotEqual(Maybe.Of(0), Maybe<int>.None);
    }

    [Fact]
    public void EqualityOperators()
    {
        // Act
        // ReSharper disable once EqualExpressionComparison
        var equal = Maybe.Of(123) == Maybe.Of(123);
        var notEqual = Maybe.Of(123) != Maybe<int>.None;

        // Assert
        Assert.True(equal);
        Assert.True(notEqual);
    }

    [Fact]
    public void EqualsObjectOverride()
    {
        // Arrange
        object other = "other";

        // Act & Assert
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
        // Act & Assert
        Assert.Equal("Some(123)", Maybe.Of(123).ToString());
        Assert.Equal("None", Maybe<int>.None.ToString());
    }

    //--------------------------------------------------------------------------------
    // Conversion
    //--------------------------------------------------------------------------------

    [Fact]
    public void ToResultReturnsSuccessWhenSome()
    {
        // Act
        var result = Maybe.Of(123).ToResult(new Error("none"));

        // Assert
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public void ToResultReturnsFailureWhenNone()
    {
        // Arrange
        var error = new Error("none");

        // Act
        var result = Maybe<int>.None.ToResult(error);

        // Assert
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void ToMaybeReturnsSomeWhenSuccess()
    {
        // Act
        var maybe = Result.Success(123).ToMaybe();

        // Assert
        Assert.Equal(123, maybe.Value);
    }

    [Fact]
    public void ToMaybeReturnsNoneWhenFailure()
    {
        // Act
        var maybe = Result.Failure<int>(new Error("test")).ToMaybe();

        // Assert
        Assert.False(maybe.HasValue);
    }
}
