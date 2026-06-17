namespace Infrastructure.Ef;

public static class TableNames
{
    public const string Events = "events";

    public const string Bookings = "bookings";

    public const string Users = "users";
}

public static class ConstraintNames
{
    public const string LoginUnique = "IX_users_Login";
}
