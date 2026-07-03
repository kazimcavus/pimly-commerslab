using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMarketplaceTableWithRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                TRUNCATE TABLE
                    channels.external_categories,
                    channels.taxonomy_sync_runs,
                    channels.marketplace_connections
                CASCADE;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_marketplace_connections_marketplaces_marketplace_id",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.DropTable(
                name: "marketplaces",
                schema: "channels");

            migrationBuilder.DropIndex(
                name: "IX_taxonomy_sync_runs_marketplace_id",
                schema: "channels",
                table: "taxonomy_sync_runs");

            migrationBuilder.DropIndex(
                name: "IX_taxonomy_sync_runs_marketplace_id_status",
                schema: "channels",
                table: "taxonomy_sync_runs");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_id",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.DropIndex(
                name: "IX_external_categories_marketplace_id",
                schema: "channels",
                table: "external_categories");

            migrationBuilder.DropIndex(
                name: "IX_external_categories_marketplace_id_external_id",
                schema: "channels",
                table: "external_categories");

            migrationBuilder.DropColumn(
                name: "marketplace_id",
                schema: "channels",
                table: "taxonomy_sync_runs");

            migrationBuilder.DropColumn(
                name: "marketplace_id",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.DropColumn(
                name: "marketplace_id",
                schema: "channels",
                table: "external_categories");

            migrationBuilder.AddColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "taxonomy_sync_runs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "marketplace_connections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "external_categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_taxonomy_sync_runs_marketplace_key",
                schema: "channels",
                table: "taxonomy_sync_runs",
                column: "marketplace_key");

            migrationBuilder.CreateIndex(
                name: "IX_taxonomy_sync_runs_marketplace_key_status",
                schema: "channels",
                table: "taxonomy_sync_runs",
                columns: new[] { "marketplace_key", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_key",
                schema: "channels",
                table: "marketplace_connections",
                columns: new[] { "tenant_id", "marketplace_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_marketplace_key",
                schema: "channels",
                table: "external_categories",
                column: "marketplace_key");

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_marketplace_key_external_id",
                schema: "channels",
                table: "external_categories",
                columns: new[] { "marketplace_key", "external_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_taxonomy_sync_runs_marketplace_key",
                schema: "channels",
                table: "taxonomy_sync_runs");

            migrationBuilder.DropIndex(
                name: "IX_taxonomy_sync_runs_marketplace_key_status",
                schema: "channels",
                table: "taxonomy_sync_runs");

            migrationBuilder.DropIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_key",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.DropIndex(
                name: "IX_external_categories_marketplace_key",
                schema: "channels",
                table: "external_categories");

            migrationBuilder.DropIndex(
                name: "IX_external_categories_marketplace_key_external_id",
                schema: "channels",
                table: "external_categories");

            migrationBuilder.DropColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "taxonomy_sync_runs");

            migrationBuilder.DropColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.DropColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "external_categories");

            migrationBuilder.AddColumn<Guid>(
                name: "marketplace_id",
                schema: "channels",
                table: "taxonomy_sync_runs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "marketplace_id",
                schema: "channels",
                table: "marketplace_connections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "marketplace_id",
                schema: "channels",
                table: "external_categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "marketplaces",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplaces", x => x.id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_id",
                schema: "channels",
                table: "marketplace_connections",
                columns: new[] { "tenant_id", "marketplace_id" },
                unique: true);

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
                name: "IX_marketplaces_key",
                schema: "channels",
                table: "marketplaces",
                column: "key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_marketplace_connections_marketplaces_marketplace_id",
                schema: "channels",
                table: "marketplace_connections",
                column: "marketplace_id",
                principalSchema: "channels",
                principalTable: "marketplaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
