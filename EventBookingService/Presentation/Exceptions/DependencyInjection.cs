using Domain.Exceptions;

using Microsoft.AspNetCore.Mvc;

namespace Presentation.Exceptions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddExceptionMap(this IServiceCollection services)
        {
            services.AddSingleton(new ExceptionHandlerRegistry()
                .Register<ValidationException>(
                    StatusCodes.Status400BadRequest,
                    GetValidationProblemDetails)

                .Register<ConflictException>(
                    StatusCodes.Status409Conflict,
                    GetConflictProblemDetails)

                .Register<NotFoundException>(
                    StatusCodes.Status404NotFound,
                    GetNotFoundProblemDetails)

                .Register<BookingLimitExceededException>(
                    StatusCodes.Status409Conflict,
                    GetLimitExceededProblemDetails)

                .Register<BookingOwnershipException>(
                    StatusCodes.Status403Forbidden,
                    GetBookingOwnershipProblemDetails)

                .Register<EventWasStartedException>(
                    StatusCodes.Status409Conflict,
                    GetEventWasStartedProblemDetails));

            return services;
        }

        #region Problem Details Generators

        private static ValidationProblemDetails GetValidationProblemDetails(HttpContext context, ValidationException exception) =>
            new ValidationProblemDetails
            {
                Title = "Validation error",
                Instance = context.Request.Path,
                Errors = new Dictionary<string, string[]>() { ["general"] = [.. exception.Errors] },
                Status = StatusCodes.Status400BadRequest,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        private static ProblemDetails GetNotFoundProblemDetails(HttpContext context, NotFoundException exception) =>
            new ProblemDetails
            {
                Title = "Not found",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        private static ProblemDetails GetConflictProblemDetails(HttpContext context, ConflictException exception) =>
            new ProblemDetails
            {
                Title = "Conflict found",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        private static ProblemDetails GetBookingOwnershipProblemDetails(HttpContext context, BookingOwnershipException exception) =>
            new ProblemDetails
            {
                Title = "Booking ownership conflict, operation forbidden",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status403Forbidden,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        private static ProblemDetails GetEventWasStartedProblemDetails(HttpContext context, EventWasStartedException exception) =>
            new ProblemDetails
            {
                Title = "Event was started",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        private static ProblemDetails GetLimitExceededProblemDetails(HttpContext context, BookingLimitExceededException exception) =>
            new ProblemDetails
            {
                Title = "Booking limit exceed",
                Instance = context.Request.Path,
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
                Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
            };

        #endregion
    }
}
