namespace Smart.Results;

#pragma warning disable CA1716
public record Error(string Message)
{
    internal static readonly Error Default = new("Error.");

    public override string ToString() => Message;

#pragma warning disable CA2225
    public static implicit operator Error(string message) => new(message);

    public static implicit operator Error(Exception ex) => new ExceptionError(ex);
#pragma warning restore CA2225
}
#pragma warning restore CA1716

public sealed record ExceptionError(Exception Exception) : Error(Exception.Message);

public sealed record AggregateError : Error
{
    public IReadOnlyList<Error> Errors { get; }

    public AggregateError(IReadOnlyList<Error> errors)
        : base(errors.Count > 0 ? errors[0].Message : Default.Message)
    {
        Errors = [.. errors];
    }
}
