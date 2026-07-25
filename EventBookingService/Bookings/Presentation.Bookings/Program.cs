
using Infrastructure.Bookings.Ef;

using Microsoft.EntityFrameworkCore;

using Presentation.Bookings.Application;
using Presentation.Bookings.Exceptions;
using Presentation.Bookings.Infrastructure;
using Presentation.Bookings.Middleware;
using Presentation.Bookings.Presentation;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddPresentation()
    .AddExceptionMap();

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandlingMiddleware();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPrometheusScrapingEndpoint();
app.MapControllers();

app.Run();
