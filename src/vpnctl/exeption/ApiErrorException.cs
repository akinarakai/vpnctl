public class ApiErrorException : Exception
{
    public int StatusCode { get; }

    public string? ApiError { get; }

    public ApiErrorException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ApiError = message;
    }
}