using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                TRUNCATE TABLE
                    catalog.product_images,
                    catalog.product_items,
                    catalog.products,
                    catalog.category_attributes,
                    catalog.categories,
                    catalog.attribute_values,
                    catalog.attributes,
                    catalog.variant_values,
                    catalog.variants,
                    catalog.barcode_allocations,
                    catalog.barcode_sequence,
                    catalog.sku_generator_config
                CASCADE;
                """);

            migrationBuilder.DropIndex(
                name: "IX_variants_key",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropIndex(
                name: "IX_variants_name",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropIndex(
                name: "IX_variants_slicer",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sku_generator_config",
                schema: "catalog",
                table: "sku_generator_config");

            migrationBuilder.DropIndex(
                name: "IX_products_product_sku",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_product_items_barcode",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.DropIndex(
                name: "IX_product_items_sku",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_barcode_sequence",
                schema: "catalog",
                table: "barcode_sequence");

            migrationBuilder.DropIndex(
                name: "IX_barcode_allocations_barcode",
                schema: "catalog",
                table: "barcode_allocations");

            migrationBuilder.DropIndex(
                name: "IX_attributes_key",
                schema: "catalog",
                table: "attributes");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "variants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "sku_generator_config",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "product_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "product_images",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "categories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "barcode_sequence",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "barcode_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "catalog",
                table: "attributes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_sku_generator_config",
                schema: "catalog",
                table: "sku_generator_config",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_barcode_sequence",
                schema: "catalog",
                table: "barcode_sequence",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_variants_tenant_id_key",
                schema: "catalog",
                table: "variants",
                columns: new[] { "tenant_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variants_tenant_id_name",
                schema: "catalog",
                table: "variants",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variants_tenant_id_slicer",
                schema: "catalog",
                table: "variants",
                columns: new[] { "tenant_id", "slicer" },
                unique: true,
                filter: "slicer = true");

            migrationBuilder.CreateIndex(
                name: "IX_products_tenant_id_product_sku",
                schema: "catalog",
                table: "products",
                columns: new[] { "tenant_id", "product_sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_items_tenant_id_barcode",
                schema: "catalog",
                table: "product_items",
                columns: new[] { "tenant_id", "barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_items_tenant_id_sku",
                schema: "catalog",
                table: "product_items",
                columns: new[] { "tenant_id", "sku" },
                unique: true,
                filter: "sku IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_barcode_allocations_tenant_id_barcode",
                schema: "catalog",
                table: "barcode_allocations",
                columns: new[] { "tenant_id", "barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attributes_tenant_id_key",
                schema: "catalog",
                table: "attributes",
                columns: new[] { "tenant_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_variants_tenant_id_key",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropIndex(
                name: "IX_variants_tenant_id_name",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropIndex(
                name: "IX_variants_tenant_id_slicer",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sku_generator_config",
                schema: "catalog",
                table: "sku_generator_config");

            migrationBuilder.DropIndex(
                name: "IX_products_tenant_id_product_sku",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_product_items_tenant_id_barcode",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.DropIndex(
                name: "IX_product_items_tenant_id_sku",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_barcode_sequence",
                schema: "catalog",
                table: "barcode_sequence");

            migrationBuilder.DropIndex(
                name: "IX_barcode_allocations_tenant_id_barcode",
                schema: "catalog",
                table: "barcode_allocations");

            migrationBuilder.DropIndex(
                name: "IX_attributes_tenant_id_key",
                schema: "catalog",
                table: "attributes");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "sku_generator_config");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "product_images");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "barcode_sequence");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "barcode_allocations");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "catalog",
                table: "attributes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sku_generator_config",
                schema: "catalog",
                table: "sku_generator_config",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_barcode_sequence",
                schema: "catalog",
                table: "barcode_sequence",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_variants_key",
                schema: "catalog",
                table: "variants",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variants_name",
                schema: "catalog",
                table: "variants",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variants_slicer",
                schema: "catalog",
                table: "variants",
                column: "slicer",
                unique: true,
                filter: "slicer = true");

            migrationBuilder.CreateIndex(
                name: "IX_products_product_sku",
                schema: "catalog",
                table: "products",
                column: "product_sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_items_barcode",
                schema: "catalog",
                table: "product_items",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_items_sku",
                schema: "catalog",
                table: "product_items",
                column: "sku",
                unique: true,
                filter: "sku IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_barcode_allocations_barcode",
                schema: "catalog",
                table: "barcode_allocations",
                column: "barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attributes_key",
                schema: "catalog",
                table: "attributes",
                column: "key",
                unique: true);
        }
    }
}
