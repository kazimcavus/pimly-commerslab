using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameCatalogTablesAndColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_variants_products_product_id",
                schema: "catalog",
                table: "product_variants");

            migrationBuilder.DropForeignKey(
                name: "FK_variant_values_variant_types_variant_type_id",
                schema: "catalog",
                table: "variant_values");

            migrationBuilder.DropPrimaryKey(
                name: "PK_variant_types",
                schema: "catalog",
                table: "variant_types");

            migrationBuilder.DropPrimaryKey(
                name: "PK_product_variants",
                schema: "catalog",
                table: "product_variants");

            migrationBuilder.RenameTable(
                name: "variant_types",
                schema: "catalog",
                newName: "variants",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "product_variants",
                schema: "catalog",
                newName: "product_items",
                newSchema: "catalog");

            migrationBuilder.RenameColumn(
                name: "variant_type_id",
                schema: "catalog",
                table: "variant_values",
                newName: "variant_id");

            migrationBuilder.RenameIndex(
                name: "IX_variant_values_variant_type_id_label",
                schema: "catalog",
                table: "variant_values",
                newName: "IX_variant_values_variant_id_label");

            migrationBuilder.RenameColumn(
                name: "variant_types",
                schema: "catalog",
                table: "products",
                newName: "variants");

            migrationBuilder.RenameColumn(
                name: "attribute_selections",
                schema: "catalog",
                table: "products",
                newName: "attribute_values");

            migrationBuilder.RenameIndex(
                name: "IX_variant_types_name",
                schema: "catalog",
                table: "variants",
                newName: "IX_variants_name");

            migrationBuilder.RenameColumn(
                name: "variant_selections",
                schema: "catalog",
                table: "product_items",
                newName: "variant_values");

            migrationBuilder.RenameColumn(
                name: "attribute_selections",
                schema: "catalog",
                table: "product_items",
                newName: "attribute_values");

            migrationBuilder.RenameIndex(
                name: "IX_product_variants_sku",
                schema: "catalog",
                table: "product_items",
                newName: "IX_product_items_sku");

            migrationBuilder.RenameIndex(
                name: "IX_product_variants_product_id",
                schema: "catalog",
                table: "product_items",
                newName: "IX_product_items_product_id");

            migrationBuilder.RenameIndex(
                name: "IX_product_variants_barcode",
                schema: "catalog",
                table: "product_items",
                newName: "IX_product_items_barcode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_variants",
                schema: "catalog",
                table: "variants",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_product_items",
                schema: "catalog",
                table: "product_items",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_items_products_product_id",
                schema: "catalog",
                table: "product_items",
                column: "product_id",
                principalSchema: "catalog",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_variant_values_variants_variant_id",
                schema: "catalog",
                table: "variant_values",
                column: "variant_id",
                principalSchema: "catalog",
                principalTable: "variants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_product_items_products_product_id",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.DropForeignKey(
                name: "FK_variant_values_variants_variant_id",
                schema: "catalog",
                table: "variant_values");

            migrationBuilder.DropPrimaryKey(
                name: "PK_variants",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_product_items",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.RenameTable(
                name: "variants",
                schema: "catalog",
                newName: "variant_types",
                newSchema: "catalog");

            migrationBuilder.RenameTable(
                name: "product_items",
                schema: "catalog",
                newName: "product_variants",
                newSchema: "catalog");

            migrationBuilder.RenameColumn(
                name: "variant_id",
                schema: "catalog",
                table: "variant_values",
                newName: "variant_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_variant_values_variant_id_label",
                schema: "catalog",
                table: "variant_values",
                newName: "IX_variant_values_variant_type_id_label");

            migrationBuilder.RenameColumn(
                name: "variants",
                schema: "catalog",
                table: "products",
                newName: "variant_types");

            migrationBuilder.RenameColumn(
                name: "attribute_values",
                schema: "catalog",
                table: "products",
                newName: "attribute_selections");

            migrationBuilder.RenameIndex(
                name: "IX_variants_name",
                schema: "catalog",
                table: "variant_types",
                newName: "IX_variant_types_name");

            migrationBuilder.RenameColumn(
                name: "variant_values",
                schema: "catalog",
                table: "product_variants",
                newName: "variant_selections");

            migrationBuilder.RenameColumn(
                name: "attribute_values",
                schema: "catalog",
                table: "product_variants",
                newName: "attribute_selections");

            migrationBuilder.RenameIndex(
                name: "IX_product_items_sku",
                schema: "catalog",
                table: "product_variants",
                newName: "IX_product_variants_sku");

            migrationBuilder.RenameIndex(
                name: "IX_product_items_product_id",
                schema: "catalog",
                table: "product_variants",
                newName: "IX_product_variants_product_id");

            migrationBuilder.RenameIndex(
                name: "IX_product_items_barcode",
                schema: "catalog",
                table: "product_variants",
                newName: "IX_product_variants_barcode");

            migrationBuilder.AddPrimaryKey(
                name: "PK_variant_types",
                schema: "catalog",
                table: "variant_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_product_variants",
                schema: "catalog",
                table: "product_variants",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_variants_products_product_id",
                schema: "catalog",
                table: "product_variants",
                column: "product_id",
                principalSchema: "catalog",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_variant_values_variant_types_variant_type_id",
                schema: "catalog",
                table: "variant_values",
                column: "variant_type_id",
                principalSchema: "catalog",
                principalTable: "variant_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
