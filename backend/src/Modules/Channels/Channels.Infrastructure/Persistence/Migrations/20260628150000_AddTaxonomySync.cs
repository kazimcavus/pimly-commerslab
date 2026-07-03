using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxonomySync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_categories",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    parent_external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    path = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    is_leaf = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "taxonomy_sync_runs",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processed_count = table.Column<int>(type: "integer", nullable: false),
                    total_estimate = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_taxonomy_sync_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_marketplace_id",
                schema: "channels",
                table: "external_categories",
                column: "marketplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_marketplace_id_external_id",
                schema: "channels",
                table: "external_categories",
                columns: new[] { "marketplace_id", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_name",
                schema: "channels",
                table: "external_categories",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_taxonomy_sync_runs_marketplace_id",
                schema: "channels",
                table: "taxonomy_sync_runs",
                column: "marketplace_id");

            migrationBuilder.CreateIndex(
                name: "IX_taxonomy_sync_runs_marketplace_id_status",
                schema: "channels",
                table: "taxonomy_sync_runs",
                columns: new[] { "marketplace_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_categories",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "taxonomy_sync_runs",
                schema: "channels");
        }
    }
}
