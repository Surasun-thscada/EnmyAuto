namespace EnmyAuto.Api.Exceptions;

public sealed class AiGenerationException : Exception
{
    public int? StatusCode { get; }

    public AiGenerationException(string message) : base(message) { }

    public AiGenerationException(string message, Exception inner)
        : base(message, inner) { }

    public AiGenerationException(string message, int statusCode)
        : base(message) => StatusCode = statusCode;
}
