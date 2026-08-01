public static class ApiResult
{
    public static IResult Ok<T>(T data)
    {
        return Results.Ok(new ApiResponse<T>
        {
            Data = data
        });
    }

    public static IResult Ok()
    {
        return Results.Ok(new ApiResponse<object>());
    }

    public static IResult Bad(string error)
    {
        Logger.Warn($"Bad Request: {error}");

        return Results.BadRequest(new ApiResponse<object>
        {
            Error = error
        });
    }
}