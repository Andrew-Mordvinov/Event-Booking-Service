using Application.Infrastructure;
using Application.Infrastructure.Enums;
using Application.Interfaces;
using Domain.Bookings;
using Domain.Events;
using Domain.Exceptions;
using Domain.Users;
using FluentAssertions;
using Infrastructure.Ef;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Tests.Integration.Ef.Bookings;

/// <summary>
/// Тесты сервиса бронирований
/// </summary>
[Collection("PostgresTests")]
public class BookingServiceTests(SharedFixture sharedFixture) : IAsyncLifetime
{
    private readonly SharedFixture _sharedFixture = sharedFixture;

    public async ValueTask InitializeAsync()
    {
        await _sharedFixture.PrepareTestDbAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    private async Task<Event?> GetEventAsync(Guid id, CancellationToken token = default)
    {
        using var scope = _sharedFixture.ServiceProvider.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        return await repo.GetByIdAsync(id, GetMode.Readonly, token);
    }

    [Fact]
    public async Task CreateBookingAsync_ParallelBookMoreThanSeats_NoOverbookingOccurs()
    {
        // Arrange
        var (@event, errors) = Event.TryCreate(Guid.NewGuid(), "Test title", DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(1), 5);
        var user = new User(Guid.NewGuid(), "user", "somehash", Roles.User);

        if (@event is null)
        {
            Assert.Fail("Не удалось создать событие: " + string.Join(Environment.NewLine, errors));
        }

        using (var scope = _sharedFixture.ServiceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Добавление тестового события и пользователя
            db.Events.Add(@event);
            db.Users.Add(user);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        var arrayOfRequests = new Task<Booking>[20];

        for (int i = 0; i < 20; i++)
        {
            arrayOfRequests[i] = Task.Run(async () =>
            {
                using var scope = _sharedFixture.ServiceProvider.CreateScope();

                var service = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                // Нужен пользователь в контексте, но мы имитируем, поэтому по факту контекста нет
                var claims = new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
                };

                httpContextAccessor.HttpContext = new DefaultHttpContext()
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "dont care"))
                };

                return await service.CreateBookingAsync(@event.Id, TestContext.Current.CancellationToken);
            });
        }

        try
        {
            // Игнор исключения здесь, проверяем ниже
            await Task.WhenAll(arrayOfRequests);
        }
        catch
        {

        }

        // Assert
        var storedEvent = await GetEventAsync(@event.Id, TestContext.Current.CancellationToken);

        storedEvent.Should().NotBeNull();
        storedEvent.AvailableSeats.Should().Be(0);
        arrayOfRequests.Count(t => t.Status == TaskStatus.RanToCompletion && t.Result is not null).Should().Be(5);
        arrayOfRequests
            .Where(t => t.Status == TaskStatus.RanToCompletion && t.Result is not null)
            .Select(t => t.Result.Id)
            .Distinct().Count()
            .Should().Be(5);
        arrayOfRequests.Count(t => t.Status == TaskStatus.Faulted && t.Exception?.InnerException is ConflictException).Should().Be(15);
    }
}
