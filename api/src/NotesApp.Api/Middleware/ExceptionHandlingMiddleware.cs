using FluentValidation;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using NotesApp.Application.Common.Exceptions;

namespace NotesApp.Api.Middleware;

/// <summary>
/// Centralised error handling. Rather than wrapping every controller action in
/// try/catch, this middleware sits in the request pipeline and converts known
/// exceptions into clean HTTP responses:
///   - <see cref="ValidationException"/> -> 400 Bad Request (with field errors)
///   - <see cref="NotFoundException"/>   -> 404 Not Found
///   - <see cref="InvalidJwtException"/> -> 401 Unauthorized (bad Google token)
///   - anything else                     -> 500 Internal Server Error
///
/// Responses use ProblemDetails, the standard machine-readable error shape.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Validation failed: {Errors}", string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await WriteProblem(context, StatusCodes.Status400BadRequest, "Validation failed", errors);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Resource not found: {Message}", ex.Message);
            await WriteProblem(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Invalid Google token: {Message}", ex.Message);
            await WriteProblem(context, StatusCodes.Status401Unauthorized, "The Google token is invalid.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(
        HttpContext context,
        int statusCode,
        string title,
        IDictionary<string, string[]>? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        if (errors is not null)
        {
            var validationProblem = new ValidationProblemDetails(errors)
            {
                Status = statusCode,
                Title = title
            };
            await context.Response.WriteAsJsonAsync(validationProblem);
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };
        await context.Response.WriteAsJsonAsync(problem);
    }
}
