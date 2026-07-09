using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropProductItemPriceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "compare_at_price",
                schema: "catalog",
                table: "product_items");

            migrationBuilder.DropColumn(
                name: "price",
                schema: "catalog",
                table: "product_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "compare_at_price",
                schema: "catalog",
                table: "product_items",
                type: "numeric(14,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                schema: "catalog",
                table: "product_items",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
