using Microsoft.AspNetCore.Mvc;
using TailorSpadi.Application.Common;

namespace TailorSpadi.Api.Errors;

public static class ProblemFactory
{
    public static ProblemDetails Create(HttpContext httpContext, int status, string code, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        return problem;
    }

    public static ProblemDetails Create(HttpContext httpContext, ProblemException exception) =>
        Create(httpContext, exception.Status, exception.Code, exception.Title);

    public static IResult Result(HttpContext httpContext, ProblemException exception) =>
        Results.Problem(Create(httpContext, exception));
}
