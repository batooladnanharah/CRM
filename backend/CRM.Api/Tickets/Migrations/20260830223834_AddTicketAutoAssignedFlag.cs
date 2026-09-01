using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Api.Tickets.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketAutoAssignedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoAssigned",
                table: "Tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoAssigned",
                table: "Tickets");
        }
    }
}
