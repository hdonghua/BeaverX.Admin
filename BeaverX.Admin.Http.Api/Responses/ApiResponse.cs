using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace BeaverX.Admin.Http.Api.Responses;

public class ApiResponse<T>
{
    public int Code { get; set; }
    public string Msg { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? TraceId { get; set; }
    public long Timestamp { get; set; }
    public object? Details { get; set; }

    public static ApiResponse<T> Success(T? data, HttpContext httpContext) => new()
    {
        Code = ApiResponseCodes.Success,
        Msg = "success",
        Data = data,
        TraceId = GetTraceId(httpContext),
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    public static ApiResponse<T> Fail(int code, string msg, HttpContext httpContext, object? details = null) => new()
    {
        Code = code,
        Msg = msg,
        Data = default,
        TraceId = GetTraceId(httpContext),
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Details = details
    };

    private static string? GetTraceId(HttpContext httpContext) => Activity.Current?.Id ?? httpContext.TraceIdentifier;
}

public static class ApiResponseCodes
{
    public const int Success = 0;
    public const int BadRequest = 40000;
    public const int Unauthorized = 40100;
    public const int Forbidden = 40300;
    public const int NotFound = 40400;
    public const int ServerError = 50000;
}
