using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Api.Tickets.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaAutomationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstResponseBreachedAtUtc",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolutionBreachedAtUtc",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaAutoEscalatedAtUtc",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaLastEvaluatedAtUtc",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstResponseBreachedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ResolutionBreachedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SlaAutoEscalatedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "SlaLastEvaluatedAtUtc",
                table: "Tickets");
        }
    }
}
