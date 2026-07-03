using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryChannelMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_channel_mappings",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_channel_mappings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_channel_mappings_tenant_id_catalog_category_id_mar~",
                schema: "channels",
                table: "category_channel_mappings",
                columns: new[] { "tenant_id", "catalog_category_id", "marketplace_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_key",
                schema: "channels",
                table: "category_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_key" });

            migrationBuilder.CreateIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_key_externa~",
                schema: "channels",
                table: "category_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_key", "external_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_channel_mappings",
                schema: "channels");
        }
    }
}
