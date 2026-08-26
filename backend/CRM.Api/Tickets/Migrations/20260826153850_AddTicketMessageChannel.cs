using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Api.Tickets.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketMessageChannel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "TicketMessages",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Web");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "TicketMessages");
        }
    }
}
