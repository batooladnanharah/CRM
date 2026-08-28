using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CRM.Api.KnowledgeBase.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeBaseCategories : Migration
    {
        // Fixed id for the seeded "Uncategorized" fallback category, so the
        // backfill below and any future migration referencing it agree on
        // the same row without needing to look it up by name first.
        private static readonly Guid UncategorizedCategoryId = new("00000000-0000-0000-0000-00000000c47e");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeBaseCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeBaseCategories", x => x.Id);
                });

            // Seed the fallback category before any article ever needs it.
            // Idempotent (ON CONFLICT DO NOTHING) so re-running this
            // migration — e.g. after a partial failure — never duplicates it
            // or errors on the unique Name index created below.
            migrationBuilder.Sql($"""
                INSERT INTO "KnowledgeBaseCategories" ("Id", "Name", "Description", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
                VALUES ('{UncategorizedCategoryId:D}', 'Uncategorized', NULL, TRUE, now(), now())
                ON CONFLICT ("Id") DO NOTHING;
                """);

            // Add CategoryId nullable first, backfill every existing article
            // to the seeded fallback category, then tighten to NOT NULL —
            // this is the only sequence that works once the table already
            // has rows (a straight non-nullable ADD COLUMN with a literal
            // default would work too, but this mirrors the plan's explicit
            // add/backfill/alter-not-null steps and stays correct even if
            // KnowledgeBaseArticles is non-empty when this runs).
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "KnowledgeBaseArticles",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql($"""
                UPDATE "KnowledgeBaseArticles"
                SET "CategoryId" = '{UncategorizedCategoryId:D}'
                WHERE "CategoryId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "KnowledgeBaseArticles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseArticles_CategoryId",
                table: "KnowledgeBaseArticles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeBaseCategories_Name",
                table: "KnowledgeBaseCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeBaseArticles_KnowledgeBaseCategories_CategoryId",
                table: "KnowledgeBaseArticles",
                column: "CategoryId",
                principalTable: "KnowledgeBaseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeBaseArticles_KnowledgeBaseCategories_CategoryId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseArticles_CategoryId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeBaseCategories_Name",
                table: "KnowledgeBaseCategories");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "KnowledgeBaseArticles");

            migrationBuilder.DropTable(
                name: "KnowledgeBaseCategories");
        }
    }
}
