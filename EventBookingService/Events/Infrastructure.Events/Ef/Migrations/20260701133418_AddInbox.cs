using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Events.Ef.Migrations
{
    /// <inheritdoc />
    public partial class AddInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_confirmed_inbox",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false),
                    Approved = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_confirmed_inbox", x => new { x.BookingId, x.EventId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_confirmed_inbox_EventId",
                table: "booking_confirmed_inbox",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_confirmed_inbox");
        }
    }
}
