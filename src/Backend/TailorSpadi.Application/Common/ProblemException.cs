namespace TailorSpadi.Application.Common;

/// <summary>
/// A business failure with an HTTP status and a machine-readable code. The title is shown to users,
/// so it must never contain secrets or internal details.
/// </summary>
public sealed class ProblemException(int status, string code, string title) : Exception(title)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public string Title { get; } = title;

    public static ProblemException BadRequest(string code, string title) => new(400, code, title);
    public static ProblemException Unauthorized(string code, string title) => new(401, code, title);
    public static ProblemException Forbidden(string code, string title) => new(403, code, title);
    public static ProblemException NotFound(string code, string title) => new(404, code, title);
    public static ProblemException Conflict(string code, string title) => new(409, code, title);
}
