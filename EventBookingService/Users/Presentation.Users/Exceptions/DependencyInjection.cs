using Domain.Users.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace Presentation.Users.Exceptions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddExceptionMap(this IServiceCollection services)
        {
            services.AddSingleton(new ExceptionHandlerRegistry()
                .Register<ValidationException>(
                    StatusCodes.Status400BadRequest,
                    GetValidationProblemDetails)

                .Register<LoginNotUniqueException>(
                    StatusCodes.Status409Conflict,
                    GetLoginNotUniqueProblemDetails)

                .Register<AuthFailedException>(
                    StatusCodes.Status404NotFound,
                    GetAuthFailedProblemDetails));

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

        private static ProblemDetails GetLoginNotUniqueProblemDetails(HttpContext context, LoginNotUniqueException exception) =>
            new ProblemDetails
            {
                Title = "Login not unique",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        private static ProblemDetails GetAuthFailedProblemDetails(HttpContext context, AuthFailedException exception) =>
            new ProblemDetails
            {
                Title = "Auth failed",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        #endregion
    }
}
