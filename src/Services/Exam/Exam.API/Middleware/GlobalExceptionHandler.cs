using Exam.Application.Exceptions;
using Exam.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Exam.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;
        int statusCode;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                problemDetails = BuildValidationProblemDetails(validationException);
                _logger.LogWarning(exception, "Validation failed for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                break;

            case NotFoundException notFoundException:
                statusCode = StatusCodes.Status404NotFound;
                problemDetails = new ProblemDetails
                {
                    Title = "Resource not found",
                    Detail = notFoundException.Message,
                    Status = statusCode,
                };
                _logger.LogWarning(exception, "Resource not found for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                break;

            case ExamDomainException domainException:
                statusCode = StatusCodes.Status400BadRequest;
                problemDetails = new ProblemDetails
                {
                    Title = "Business rule violation",
                    Detail = domainException.Message,
                    Status = statusCode,
                };
                _logger.LogWarning(exception, "Domain rule violated for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Title = "An unexpected error occurred",
                    Status = statusCode,
                };
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                break;
        }

        problemDetails.Instance = httpContext.Request.Path;
        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync<object>(problemDetails, cancellationToken);

        return true;
    }

    private static ValidationProblemDetails BuildValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
        };
    }
}
