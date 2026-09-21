using Microsoft.AspNetCore.Http;

namespace VCare.SharedKernel.Results;

/// <summary>
/// Turns a failed result into an HTTP response. The status code comes from the
/// error's kind, so endpoints no longer pick one status for every failure a
/// service can produce.
/// </summary>
public static class ResultExtensions
{
    public static int ToStatusCode(this ErrorKind kind) => kind switch
    {
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };

    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("A successful result has no problem to report.");

        var error = result.Error;

        return TypedResults.Problem(
            detail: error.Description,
            statusCode: error.Kind.ToStatusCode(),
            title: Title(error.Kind),
            extensions: error.Code.Length == 0
                ? null
                : new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private static string Title(ErrorKind kind) => kind switch
    {
        ErrorKind.Unauthorized => "Unauthorized",
        ErrorKind.Forbidden => "Forbidden",
        ErrorKind.NotFound => "Not Found",
        ErrorKind.Conflict => "Conflict",
        _ => "Bad Request"
    };
}
