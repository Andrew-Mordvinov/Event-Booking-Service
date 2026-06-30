namespace Presentation.Events.Exceptions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddExceptionMap(this IServiceCollection services)
        {
            //services.AddSingleton(new ExceptionHandlerRegistry()
            //    .Register<ValidationException>(
            //        StatusCodes.Status400BadRequest,
            //        GetValidationProblemDetails)

            //    .Register<ConflictException>(
            //        StatusCodes.Status409Conflict,
            //        GetConflictProblemDetails)

            //    .Register<NotFoundException>(
            //        StatusCodes.Status404NotFound,
            //        GetNotFoundProblemDetails)

            //    .Register<BookingLimitExceededException>(
            //        StatusCodes.Status409Conflict,
            //        GetLimitExceededProblemDetails)

            //    .Register<BookingOwnershipException>(
            //        StatusCodes.Status403Forbidden,
            //        GetBookingOwnershipProblemDetails)

            //    .Register<EventWasStartedException>(
            //        StatusCodes.Status400BadRequest,
            //        GetEventWasStartedProblemDetails)

            //    .Register<InvalidBookingOperationException>(
            //        StatusCodes.Status409Conflict,
            //        GetBookingCancelledProblemDetails)

            //    .Register<LoginNotUniqueException>(
            //        StatusCodes.Status409Conflict,
            //        GetLoginNotUniqueProblemDetails)

            //    .Register<AuthFailedException>(
            //        StatusCodes.Status404NotFound,
            //        GetAuthFailedProblemDetails));

            return services;
        }

        #region Problem Details Generators

        //private static ValidationProblemDetails GetValidationProblemDetails(HttpContext context, ValidationException exception) =>
        //    new ValidationProblemDetails
        //    {
        //        Title = "Validation error",
        //        Instance = context.Request.Path,
        //        Errors = new Dictionary<string, string[]>() { ["general"] = [.. exception.Errors] },
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetNotFoundProblemDetails(HttpContext context, NotFoundException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Not found",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetConflictProblemDetails(HttpContext context, ConflictException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Conflict found",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetBookingOwnershipProblemDetails(HttpContext context, BookingOwnershipException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Booking ownership conflict, operation forbidden",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetEventWasStartedProblemDetails(HttpContext context, EventWasStartedException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Event was started",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetLimitExceededProblemDetails(HttpContext context, BookingLimitExceededException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Booking limit exceed",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetBookingCancelledProblemDetails(HttpContext context, InvalidBookingOperationException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Booking already cancelled",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetLoginNotUniqueProblemDetails(HttpContext context, LoginNotUniqueException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Login not unique",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        //private static ProblemDetails GetAuthFailedProblemDetails(HttpContext context, AuthFailedException exception) =>
        //    new ProblemDetails
        //    {
        //        Title = "Auth failed",
        //        Instance = context.Request.Path,
        //        Detail = exception.Message,
        //        Extensions = new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }
        //    };

        #endregion
    }
}
