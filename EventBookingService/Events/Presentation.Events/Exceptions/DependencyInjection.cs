using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace Presentation.Events.Exceptions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddExceptionMap(this IServiceCollection services)
        {
            services.AddSingleton(new ExceptionHandlerRegistry()
                .Register<ValidationException>(
                    StatusCodes.Status400BadRequest,
                    GetValidationProblemDetails)

                .Register<NotFoundException>(
                    StatusCodes.Status404NotFound,
                    GetNotFoundProblemDetails));

            return services;
        }

        #region Problem Details Generators

        private static ValidationProblemDetails GetValidationProblemDetails(HttpContext context, ValidationException exception) =>
            new ValidationProblemDetails
            {
                Title = "Validation error",
                Instance = context.Request.Path,
                Errors = new Dictionary<string, string[]>() { ["general"] = [.. exception.Errors] },
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        private static ProblemDetails GetNotFoundProblemDetails(HttpContext context, NotFoundException exception) =>
            new ProblemDetails
            {
                Title = "Not found",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        #endregion
    }
}
