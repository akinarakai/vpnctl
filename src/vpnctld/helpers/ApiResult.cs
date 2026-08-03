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

    public static IResult NotFound(string error)
    {
        Logger.Warn($"Not Found: {error}");

        return Results.NotFound(new ApiResponse<object>
        {
            Error = error
        });
    }

    public static async Task Unauthorized(HttpContext context, string error = "Unauthorized")
    {
        Logger.Warn($"Unauthorized request from: {context.Connection.RemoteIpAddress}, err: {error}");

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await context.Response.WriteAsJsonAsync(new ApiResponse<object>
        {
            Error = error
        });
    }

    public static async Task Error(HttpContext context, string error)
    {
        Logger.Error(error);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(new ApiResponse<object>
        {
            Error = error
        });
    }
}