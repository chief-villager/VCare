namespace VCare.SharedKernel.Results;

/// <summary>
/// Why a result failed. The endpoint layer maps this to a status code, so it is
/// the one part of an error a caller can act on programmatically: 400 means
/// "fix the request and retry", 403 means "retrying will never help".
/// </summary>
public enum ErrorKind
{
    /// <summary>The request was malformed. Fix it and retry. -> 400</summary>
    Validation = 0,

    /// <summary>The caller is not authenticated, or their credentials are stale. -> 401</summary>
    Unauthorized,

    /// <summary>Well-formed, but the caller is not permitted. -> 403</summary>
    Forbidden,

    /// <summary>No such resource — or none this caller may see. -> 404</summary>
    NotFound,

    /// <summary>Clashes with the current state of the resource. -> 409</summary>
    Conflict
}

public sealed record Error(string Code, string Description, ErrorKind Kind = ErrorKind.Validation)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string code, string description) => new(code, description, ErrorKind.Validation);
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorKind.Unauthorized);
    public static Error Forbidden(string code, string description) => new(code, description, ErrorKind.Forbidden);
    public static Error NotFound(string code, string description) => new(code, description, ErrorKind.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorKind.Conflict);

    /// <summary>
    /// Lets a plain message stand in for an error, which is what the domain layer
    /// returns for the ordinary "this field is required" case. An uncoded message
    /// is a validation error; anything else states its kind explicitly.
    /// </summary>
    public static implicit operator Error(string description) => new(string.Empty, description);

    public override string ToString() =>
        Code.Length == 0 ? Description : $"{Code}: {Description}";
}
