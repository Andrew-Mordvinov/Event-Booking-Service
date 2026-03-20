namespace EventBookingService.Middleware;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder application) => application.UseMiddleware<ExceptionHandlingMiddleware>();
}
