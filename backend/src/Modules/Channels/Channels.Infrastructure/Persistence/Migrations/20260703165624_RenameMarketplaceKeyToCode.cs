using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameMarketplaceKeyToCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "taxonomy_sync_runs",
                newName: "marketplace_code");

            migrationBuilder.RenameIndex(
                name: "IX_taxonomy_sync_runs_marketplace_key_status",
                schema: "channels",
                table: "taxonomy_sync_runs",
                newName: "IX_taxonomy_sync_runs_marketplace_code_status");

            migrationBuilder.RenameIndex(
                name: "IX_taxonomy_sync_runs_marketplace_key",
                schema: "channels",
                table: "taxonomy_sync_runs",
                newName: "IX_taxonomy_sync_runs_marketplace_code");

            migrationBuilder.RenameColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "marketplace_connections",
                newName: "marketplace_code");

            migrationBuilder.RenameIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_key",
                schema: "channels",
                table: "marketplace_connections",
                newName: "IX_marketplace_connections_tenant_id_marketplace_code");

            migrationBuilder.RenameColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "external_category_attributes",
                newName: "marketplace_code");

            migrationBuilder.RenameIndex(
                name: "IX_external_category_attributes_marketplace_key_external_categ~",
                schema: "channels",
                table: "external_category_attributes",
                newName: "IX_external_category_attributes_marketplace_code_external_cate~");

            migrationBuilder.RenameIndex(
                name: "IX_external_category_attributes_marketplace_key_external_cate~1",
                schema: "channels",
                table: "external_category_attributes",
                newName: "IX_external_category_attributes_marketplace_code_external_cat~1");

            migrationBuilder.RenameColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "external_categories",
                newName: "marketplace_code");

            migrationBuilder.RenameIndex(
                name: "IX_external_categories_marketplace_key_external_id",
                schema: "channels",
                table: "external_categories",
                newName: "IX_external_categories_marketplace_code_external_id");

            migrationBuilder.RenameIndex(
                name: "IX_external_categories_marketplace_key",
                schema: "channels",
                table: "external_categories",
                newName: "IX_external_categories_marketplace_code");

            migrationBuilder.RenameColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "external_attribute_values",
                newName: "marketplace_code");

            migrationBuilder.RenameIndex(
                name: "IX_external_attribute_values_marketplace_key_external_category~",
                schema: "channels",
                table: "external_attribute_values",
                newName: "IX_external_attribute_values_marketplace_code_external_categor~");

            migrationBuilder.RenameColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "category_channel_mappings",
                newName: "marketplace_code");

            migrationBuilder.RenameIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_key_externa~",
                schema: "channels",
                table: "category_channel_mappings",
                newName: "IX_category_channel_mappings_tenant_id_marketplace_code_extern~");

            migrationBuilder.RenameIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_key",
                schema: "channels",
                table: "category_channel_mappings",
                newName: "IX_category_channel_mappings_tenant_id_marketplace_code");

            migrationBuilder.RenameColumn(
                name: "marketplace_key",
                schema: "channels",
                table: "attribute_channel_mappings",
                newName: "marketplace_code");

            migrationBuilder.RenameIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_key_catalo~",
                schema: "channels",
                table: "attribute_channel_mappings",
                newName: "IX_attribute_channel_mappings_tenant_id_marketplace_code_catal~");

            migrationBuilder.RenameIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_key_catal~1",
                schema: "channels",
                table: "attribute_channel_mappings",
                newName: "IX_attribute_channel_mappings_tenant_id_marketplace_code_cata~1");

            var tables = new[]
            {
                "taxonomy_sync_runs",
                "marketplace_connections",
                "external_category_attributes",
                "external_categories",
                "external_attribute_values",
                "category_channel_mappings",
                "attribute_channel_mappings",
            };

            foreach (var table in tables)
            {
                migrationBuilder.Sql(
                    $"""
                    UPDATE channels.{table}
                    SET marketplace_code = 'TY'
                    WHERE marketplace_code = 'trendyol';
                    """);
            }

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_code",
                schema: "channels",
                table: "taxonomy_sync_runs",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_code",
                schema: "channels",
                table: "marketplace_connections",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_code",
                schema: "channels",
                table: "external_category_attributes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_code",
                schema: "channels",
                table: "external_categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_code",
                schema: "channels",
                table: "external_attribute_values",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_code",
                schema: "channels",
                table: "category_channel_mappings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_code",
                schema: "channels",
                table: "attribute_channel_mappings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "marketplace_code",
                schema: "channels",
                table: "taxonomy_sync_runs",
                newName: "marketplace_key");

            migrationBuilder.RenameIndex(
                name: "IX_taxonomy_sync_runs_marketplace_code_status",
                schema: "channels",
                table: "taxonomy_sync_runs",
                newName: "IX_taxonomy_sync_runs_marketplace_key_status");

            migrationBuilder.RenameIndex(
                name: "IX_taxonomy_sync_runs_marketplace_code",
                schema: "channels",
                table: "taxonomy_sync_runs",
                newName: "IX_taxonomy_sync_runs_marketplace_key");

            migrationBuilder.RenameColumn(
                name: "marketplace_code",
                schema: "channels",
                table: "marketplace_connections",
                newName: "marketplace_key");

            migrationBuilder.RenameIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_code",
                schema: "channels",
                table: "marketplace_connections",
                newName: "IX_marketplace_connections_tenant_id_marketplace_key");

            migrationBuilder.RenameColumn(
                name: "marketplace_code",
                schema: "channels",
                table: "external_category_attributes",
                newName: "marketplace_key");

            migrationBuilder.RenameIndex(
                name: "IX_external_category_attributes_marketplace_code_external_cate~",
                schema: "channels",
                table: "external_category_attributes",
                newName: "IX_external_category_attributes_marketplace_key_external_categ~");

            migrationBuilder.RenameIndex(
                name: "IX_external_category_attributes_marketplace_code_external_cat~1",
                schema: "channels",
                table: "external_category_attributes",
                newName: "IX_external_category_attributes_marketplace_key_external_cate~1");

            migrationBuilder.RenameColumn(
                name: "marketplace_code",
                schema: "channels",
                table: "external_categories",
                newName: "marketplace_key");

            migrationBuilder.RenameIndex(
                name: "IX_external_categories_marketplace_code_external_id",
                schema: "channels",
                table: "external_categories",
                newName: "IX_external_categories_marketplace_key_external_id");

            migrationBuilder.RenameIndex(
                name: "IX_external_categories_marketplace_code",
                schema: "channels",
                table: "external_categories",
                newName: "IX_external_categories_marketplace_key");

            migrationBuilder.RenameColumn(
                name: "marketplace_code",
                schema: "channels",
                table: "external_attribute_values",
                newName: "marketplace_key");

            migrationBuilder.RenameIndex(
                name: "IX_external_attribute_values_marketplace_code_external_categor~",
                schema: "channels",
                table: "external_attribute_values",
                newName: "IX_external_attribute_values_marketplace_key_external_category~");

            migrationBuilder.RenameColumn(
                name: "marketplace_code",
                schema: "channels",
                table: "category_channel_mappings",
                newName: "marketplace_key");

            migrationBuilder.RenameIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_code_extern~",
                schema: "channels",
                table: "category_channel_mappings",
                newName: "IX_category_channel_mappings_tenant_id_marketplace_key_externa~");

            migrationBuilder.RenameIndex(
                name: "IX_category_channel_mappings_tenant_id_marketplace_code",
                schema: "channels",
                table: "category_channel_mappings",
                newName: "IX_category_channel_mappings_tenant_id_marketplace_key");

            migrationBuilder.RenameColumn(
                name: "marketplace_code",
                schema: "channels",
                table: "attribute_channel_mappings",
                newName: "marketplace_key");

            migrationBuilder.RenameIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_code_catal~",
                schema: "channels",
                table: "attribute_channel_mappings",
                newName: "IX_attribute_channel_mappings_tenant_id_marketplace_key_catalo~");

            migrationBuilder.RenameIndex(
                name: "IX_attribute_channel_mappings_tenant_id_marketplace_code_cata~1",
                schema: "channels",
                table: "attribute_channel_mappings",
                newName: "IX_attribute_channel_mappings_tenant_id_marketplace_key_catal~1");

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "taxonomy_sync_runs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "marketplace_connections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "external_category_attributes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "external_categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "external_attribute_values",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "category_channel_mappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "marketplace_key",
                schema: "channels",
                table: "attribute_channel_mappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);
        }
    }
}
