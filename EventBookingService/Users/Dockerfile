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

COPY ["EventBookingService/Users/Domain.Users/Domain.Users.csproj", "EventBookingService/Users/Domain.Users/"]
COPY ["EventBookingService/Users/Application.Users/Application.Users.csproj", "EventBookingService/Users/Application.Users/"]
COPY ["EventBookingService/Users/Infrastructure.Users/Infrastructure.Users.csproj", "EventBookingService/Users/Infrastructure.Users/"]
COPY ["EventBookingService/Users/Presentation.Users/Presentation.Users.csproj", "EventBookingService/Users/Presentation.Users/"]

RUN dotnet restore "EventBookingService/Users/Presentation.Users/Presentation.Users.csproj"

COPY . .

WORKDIR "/src/EventBookingService/Users/Presentation.Users"
RUN dotnet build "Presentation.Users.csproj" -c Release -o /app/build
RUN dotnet publish "Presentation.Users.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Presentation.Users.dll"]