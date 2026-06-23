using Domain.Exceptions.Users;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Ef.ExceptionPatterns;

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
