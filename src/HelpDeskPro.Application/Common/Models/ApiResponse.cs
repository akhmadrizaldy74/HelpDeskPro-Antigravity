namespace HelpDeskPro.Application.Common.Models;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    IReadOnlyList<string> Errors,
    string TraceId);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string traceId = "") =>
        new(true, data, Array.Empty<string>(), traceId);

    public static ApiResponse<object?> Ok(string traceId = "") =>
        new(true, null, Array.Empty<string>(), traceId);

    public static ApiResponse<T> Failure<T>(IEnumerable<string> errors, string traceId = "") =>
        new(false, default, errors.ToArray(), traceId);

    public static ApiResponse<T> Failure<T>(string error, string traceId = "") =>
        new(false, default, [error], traceId);

    public static ApiResponse<object?> Failure(IEnumerable<string> errors, string traceId = "") =>
        new(false, null, errors.ToArray(), traceId);

    public static ApiResponse<object?> Failure(string error, string traceId = "") =>
        new(false, null, [error], traceId);
}
