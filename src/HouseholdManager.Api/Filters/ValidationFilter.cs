using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HouseholdManager.Api.Filters
{
    /// <summary>
    /// Action filter that validates model state and returns ValidationProblemDetails (422) on failure
    /// Works in conjunction with FluentValidation
    /// </summary>
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<ValidationFilter> _logger;

        public ValidationFilter(IServiceProvider sp, ILogger<ValidationFilter> logger)
        {
            _sp = sp;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // 1) Проганяємо FluentValidation для всіх аргументів екшену
            foreach (var (name, arg) in context.ActionArguments)
            {
                if (arg is null) continue;

                // знайти IValidator<T> для типу аргумента
                var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
                var validatorObj = _sp.GetService(validatorType);

                if (validatorObj is IValidator validator) // не generic інтерфейс також реалізується
                {
                    ValidationResult result = await validator.ValidateAsync(new ValidationContext<object>(arg));
                    if (!result.IsValid)
                    {
                        foreach (var failure in result.Errors)
                        {
                            var key = ToCamelCase(string.IsNullOrEmpty(failure.PropertyName) ? name : failure.PropertyName);
                            context.ModelState.AddModelError(key, failure.ErrorMessage);
                        }
                    }
                }
            }

            // 2) Якщо є помилки — віддаємо 422 ValidationProblemDetails
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => ToCamelCase(kvp.Key),
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var problem = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Validation Failed",
                    Instance = context.HttpContext.Request.Path
                };
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                problem.Extensions["timestamp"] = DateTime.UtcNow;

                context.Result = new UnprocessableEntityObjectResult(problem);
                return;
            }

            await next();
        }

        private static string ToCamelCase(string s) =>
            string.IsNullOrEmpty(s) || char.IsLower(s[0]) ? s : char.ToLowerInvariant(s[0]) + s[1..];
    }
}
