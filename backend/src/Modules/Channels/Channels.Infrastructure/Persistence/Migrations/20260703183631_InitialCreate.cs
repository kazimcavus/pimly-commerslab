using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "channels");

            migrationBuilder.CreateTable(
                name: "attribute_channel_mappings",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                name: "category_channel_mappings",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    external_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category_channel_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "external_attribute_values",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                name: "external_categories",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                name: "external_category_attributes",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    external_category_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    external_attribute_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    allow_custom = table.Column<bool>(type: "boolean", nullable: false),
                    is_variant = table.Column<bool>(type: "boolean", nullable: false),
                    is_slicer = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_category_attributes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_connections",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    seller_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    api_secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_connections", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_import_runs",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_products = table.Column<int>(type: "integer", nullable: true),
                    processed_products = table.Column<int>(type: "integer", nullable: false),
                    imported_products = table.Column<int>(type: "integer", nullable: false),
                    skipped_products = table.Column<int>(type: "integer", nullable: false),
                    failed_products = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_import_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "taxonomy_sync_runs",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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

            migrationBuilder.CreateTable(
                name: "product_import_run_errors",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_main_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    barcode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    product_import_run_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_import_run_errors", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_import_run_errors_product_import_runs_product_impor~",
                        column: x => x.product_import_run_id,
                        principalSchema: "channels",
                        principalTable: "product_import_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_code_cata~1",
                schema: "channels",
                table: "attribute_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_code", "catalog_category_id", "source_type", "catalog_source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_code_catal~",
                schema: "channels",
                table: "attribute_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_code", "catalog_category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_attribute_value_channel_mappings_attribute_channel_mapping_~",
                schema: "channels",
                table: "attribute_value_channel_mappings",
                columns: new[] { "attribute_channel_mapping_id", "catalog_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_channel_mappings_tenant_id_catalog_category_id_mar~",
                schema: "channels",
                table: "category_channel_mappings",
                columns: new[] { "tenant_id", "catalog_category_id", "marketplace_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_code",
                schema: "channels",
                table: "category_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_code" });

            migrationBuilder.CreateIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_code_extern~",
                schema: "channels",
                table: "category_channel_mappings",
                columns: new[] { "tenant_id", "marketplace_code", "external_id" });

            migrationBuilder.CreateIndex(
                name: "IX_external_attribute_values_marketplace_code_external_categor~",
                schema: "channels",
                table: "external_attribute_values",
                columns: new[] { "marketplace_code", "external_category_id", "external_attribute_id", "external_value_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_marketplace_code",
                schema: "channels",
                table: "external_categories",
                column: "marketplace_code");

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_marketplace_code_external_id",
                schema: "channels",
                table: "external_categories",
                columns: new[] { "marketplace_code", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_categories_name",
                schema: "channels",
                table: "external_categories",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_external_category_attributes_marketplace_code_external_cat~1",
                schema: "channels",
                table: "external_category_attributes",
                columns: new[] { "marketplace_code", "external_category_id", "external_attribute_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_category_attributes_marketplace_code_external_cate~",
                schema: "channels",
                table: "external_category_attributes",
                columns: new[] { "marketplace_code", "external_category_id" });

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_code",
                schema: "channels",
                table: "marketplace_connections",
                columns: new[] { "tenant_id", "marketplace_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_import_run_errors_product_import_run_id",
                schema: "channels",
                table: "product_import_run_errors",
                column: "product_import_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_import_runs_status_created_at",
                schema: "channels",
                table: "product_import_runs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_product_import_runs_tenant_id_marketplace_code_created_at",
                schema: "channels",
                table: "product_import_runs",
                columns: new[] { "tenant_id", "marketplace_code", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_taxonomy_sync_runs_marketplace_code",
                schema: "channels",
                table: "taxonomy_sync_runs",
                column: "marketplace_code");

            migrationBuilder.CreateIndex(
                name: "IX_taxonomy_sync_runs_marketplace_code_status",
                schema: "channels",
                table: "taxonomy_sync_runs",
                columns: new[] { "marketplace_code", "status" });
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
                name: "category_channel_mappings",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "external_attribute_values",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "external_categories",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "external_category_attributes",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "marketplace_connections",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "product_import_run_errors",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "taxonomy_sync_runs",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "product_import_runs",
                schema: "channels");
        }
    }
}
