FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["EventBookingService/Shared/Shared.Entities/Shared.Entities.csproj", "EventBookingService/Shared/Shared.Entities/"]
COPY ["EventBookingService/Shared/Shared.Exceptions/Shared.Exceptions.csproj", "EventBookingService/Shared/Shared.Exceptions/"]
COPY ["EventBookingService/Shared/Shared.Helpers/Shared.Helpers.csproj", "EventBookingService/Shared/Shared.Helpers/"]
COPY ["EventBookingService/Shared/Shared.Infrastructure.Abstract/Shared.Infrastructure.Abstract.csproj", "EventBookingService/Shared/Shared.Infrastructure.Abstract/"]
COPY ["EventBookingService/Shared/Shared.Infrastructure.Ef/Shared.Infrastructure.Ef.csproj", "EventBookingService/Shared/Shared.Infrastructure.Ef/"]
COPY ["EventBookingService/Shared/Shared.Infrastructure.Kafka/Shared.Infrastructure.Kafka.csproj", "EventBookingService/Shared/Shared.Infrastructure.Kafka/"]
COPY ["EventBookingService/Shared/Shared.Roles/Shared.Roles.csproj", "EventBookingService/Shared/Shared.Roles/"]
COPY ["EventBookingService/Shared/Shared.ValidationAttributes/Shared.ValidationAttributes.csproj", "EventBookingService/Shared/Shared.ValidationAttributes/"]

COPY ["EventBookingService/Contracts/Contracts.csproj", "EventBookingService/Contracts/"]

COPY ["EventBookingService/Bookings/Domain.Bookings/Domain.Bookings.csproj", "EventBookingService/Bookings/Domain.Bookings/"]
COPY ["EventBookingService/Bookings/Application.Bookings/Application.Bookings.csproj", "EventBookingService/Bookings/Application.Bookings/"]
COPY ["EventBookingService/Bookings/Infrastructure.Bookings/Infrastructure.Bookings.csproj", "EventBookingService/Bookings/Infrastructure.Bookings/"]
COPY ["EventBookingService/Bookings/Presentation.Bookings/Presentation.Bookings.csproj", "EventBookingService/Bookings/Presentation.Bookings/"]

RUN dotnet restore "EventBookingService/Bookings/Presentation.Bookings/Presentation.Bookings.csproj"

COPY . .

WORKDIR "/src/EventBookingService/Bookings/Presentation.Bookings"
RUN dotnet build "Presentation.Bookings.csproj" -c Release -o /app/build
RUN dotnet publish "Presentation.Bookings.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Presentation.Bookings.dll"]