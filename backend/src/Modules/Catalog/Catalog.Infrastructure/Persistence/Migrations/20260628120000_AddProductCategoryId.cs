using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCategoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.products p
                SET category_id = (
                    SELECT c.id FROM catalog.categories c ORDER BY c.name LIMIT 1
                )
                WHERE p.category_id IS NULL
                  AND EXISTS (SELECT 1 FROM catalog.categories);
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM catalog.products
                WHERE category_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                schema: "catalog",
                table: "products",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                schema: "catalog",
                table: "products",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_products_categories_category_id",
                schema: "catalog",
                table: "products",
                column: "category_id",
                principalSchema: "catalog",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_products_categories_category_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_category_id",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "category_id",
                schema: "catalog",
                table: "products");
        }
    }
}
