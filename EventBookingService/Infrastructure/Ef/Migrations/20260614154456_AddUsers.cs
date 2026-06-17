using Domain.Users;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var adminId = Guid.NewGuid();

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            var manager = new DefautPasswordManager();
            // Добавление админа с дефолтным логин/паролем
            migrationBuilder.InsertData(
                table: "users",
                columns: ["Id", "Login", "PasswordHash", "Role"],
                values: [adminId, "admin", manager.HashPassword("admin"), (int)Roles.Admin]);
            
            // Сначала nullable
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "bookings",
                type: "uuid",
                nullable: true);

            // Вставка всем бронированиям дефолтного юзера, если есть
            migrationBuilder.Sql($"UPDATE bookings SET \"UserId\" = '{adminId}'");

            // Теперь not null
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "bookings",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                table: "bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Login",
                table: "users",
                column: "Login",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_UserId",
                table: "bookings",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_UserId",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "IX_bookings_UserId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "bookings");
        }
    }
}
