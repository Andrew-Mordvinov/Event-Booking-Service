using Domain.Users.Exceptions;

using Microsoft.EntityFrameworkCore;

using Npgsql;

using Shared.Infrastructure.Abstract.ExceptionPatterns;

namespace Infrastructure.Users.Ef.ExceptionPatterns;

/// <summary>
/// Паттерн для поиска исключения, возникшего при попытке вставить неуникальный логин пользователю
/// </summary>
internal class LoginNotUniquePattern : ExceptionPattern
{
    public override void RethrowIfMatch(Exception exception)
    {
        if (exception is not DbUpdateException dbUpdateEx)
        {
            return;
        }

        if (dbUpdateEx.InnerException is not PostgresException pgEx)
        {
            return;
        }

        if (pgEx.SqlState == "23505"
            && pgEx.TableName == TableNames.Users
            && pgEx.ConstraintName == ConstraintNames.LoginUnique)
        {
            throw new LoginNotUniqueException();
        }
    }
}
