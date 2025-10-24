using HouseholdManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HouseholdManager.Api.Middleware
{
    /// <summary>
    /// Global exception handling middleware that converts all exceptions to RFC 7807 ProblemDetails
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";

            var problemDetails = exception switch
            {
                NotFoundException notFoundEx => new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Instance = context.Request.Path
                },

                ValidationException validationEx => new ValidationProblemDetails(validationEx.Errors)
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Validation Error",
                    Detail = validationEx.Message,
                    Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                    Instance = context.Request.Path
                },

                UnauthorizedException unauthorizedEx => new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = unauthorizedEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Instance = context.Request.Path
                },

                ForbiddenException forbiddenEx => new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = forbiddenEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Instance = context.Request.Path
                },

                DomainException domainEx => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Bad Request",
                    Detail = domainEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = context.Request.Path
                },

                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred. Please try again later.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Instance = context.Request.Path
                }
            };

            // Add traceId for correlation
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            // Add timestamp
            problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

            // In development, include stack trace
            if (_environment.IsDevelopment() && exception is not DomainException)
            {
                problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            }

            context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
        }
    }

    /// <summary>
    /// Extension methods for registering exception handling middleware
    /// </summary>
    public static class ExceptionHandlingMiddlewareExtensions
    {
        /// <summary>
        /// Adds global exception handling middleware to the application pipeline
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder for chaining</returns>
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
