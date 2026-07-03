using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCategoryAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "attribute_channel_mappings",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    catalog_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    catalog_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_attribute_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attribute_channel_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attribute_value_channel_mappings",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_channel_mapping_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_value_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_value_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attribute_value_channel_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_attribute_values",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_attribute_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_value_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_attribute_values", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_category_attributes",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_attribute_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    allow_custom = table.Column<bool>(type: "boolean", nullable: false),
                    is_variant = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_category_attributes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_key_catal~1",
                schema: "channels",
                table: "attribute_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_key", "catalog_category_id", "source_type", "catalog_source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_key_catalo~",
                schema: "channels",
                table: "attribute_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_key", "catalog_category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_attribute_value_channel_mappings_attribute_channel_mapping_~",
                schema: "channels",
                table: "attribute_value_channel_mappings",
                columns: new[] { "attribute_channel_mapping_id", "catalog_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_attribute_values_marketplace_key_external_category~",
                schema: "channels",
                table: "external_attribute_values",
                columns: new[] { "marketplace_key", "external_category_id", "external_attribute_id", "external_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_category_attributes_marketplace_key_external_cate~1",
                schema: "channels",
                table: "external_category_attributes",
                columns: new[] { "marketplace_key", "external_category_id", "external_attribute_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_category_attributes_marketplace_key_external_categ~",
                schema: "channels",
                table: "external_category_attributes",
                columns: new[] { "marketplace_key", "external_category_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attribute_channel_mappings",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "attribute_value_channel_mappings",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "external_attribute_values",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "external_category_attributes",
                schema: "channels");
        }
    }
}
