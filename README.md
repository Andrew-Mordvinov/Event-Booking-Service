# Event Booking Service

Сервис бронирования мероприятий на .NET 10 + ASP.NET Core.

## Запуск

Для запуска необходима среда исполнения для .NET 10. Для редактирования нужна
Visual Studio 2026 (не ниже)

1. Клонируйте репозиторий:

`git clone https://github.com/Andrew-Mordvinov/Event-Booking-Service.git`

2. Перейдите в каталог с проектом (из корня репозитория в `/EventBookingService`) и запустите команду:

`dotnet run`

или с параметрами, чтобы явно выбрать профиль http/https:

`dotnet run --launch-profile https`

`dotnet run --launch-profile http`

Сервер запустится на http://localhost:5271 и https://localhost:7240

## API Endpoints

| Метод   | URL           | Описание                                                           |
|---------|---------------|--------------------------------------------------------------------|
| GET     | /events       | Возвращает список всех мероприятий                                 |
| GET     | /events/{id}  | Возвращает мероприятие с указанным ID                              |
| POST    | /events       | Создает новое мероприятие. При успехе возвращает 201 Created       |
| PUT     | /events/{id}  | Полностью обновляет мероприятие с указанным ID                     |
| DELETE  | /events/{id}  | Удаляет мероприятие с указанным ID                                 |

Документация API доступна по адресу `/swagger` при запуске проекта в режиме Development