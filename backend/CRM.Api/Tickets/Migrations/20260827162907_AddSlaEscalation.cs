using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Api.Tickets.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaEscalation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EscalationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Trigger = table.Column<int>(type: "integer", nullable: false),
                    Objective = table.Column<int>(type: "integer", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AgentNotified = table.Column<bool>(type: "boolean", nullable: false),
                    ManagerNotified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Trigger = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyAgent = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyManager = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationEvents_TicketId_RuleId_Trigger_Objective",
                table: "EscalationEvents",
                columns: new[] { "TicketId", "RuleId", "Trigger", "Objective" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_IsActive",
                table: "EscalationRules",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscalationEvents");

            migrationBuilder.DropTable(
                name: "EscalationRules");
        }
    }
}
