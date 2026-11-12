using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TailorSpadi.Application.Common;

namespace TailorSpadi.Api.Errors;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is ValidationException validationException)
        {
            Dictionary<string, string[]> errors = validationException.Errors
                .GroupBy(error => ToCamelCase(error.PropertyName))
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

            problem = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            };
            problem.Extensions["code"] = "validation_failed";
        }
        else if (exception is ProblemException business)
        {
            problem = ProblemFactory.Create(httpContext, business);
        }
        else
        {
            logger.LogError("Unhandled {ExceptionType}; trace {TraceId}",
                exception.GetType().Name, httpContext.TraceIdentifier);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            };
        }

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }

    private static string ToCamelCase(string propertyName)
    {
        if (propertyName.StartsWith("Business.", StringComparison.Ordinal))
        {
            propertyName = propertyName["Business.".Length..];
        }
        // "Business.BusinessName" -> "business.businessName"; keeps client field names predictable.
        return string.Join('.', propertyName.Split('.').Select(part =>
            part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));
    }
}
