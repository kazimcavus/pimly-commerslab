using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductGroupCodeAndSlicerValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "group_code",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "slicer_value",
                schema: "catalog",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_tenant_id_group_code",
                schema: "catalog",
                table: "products",
                columns: new[] { "tenant_id", "group_code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_tenant_id_group_code",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "group_code",
                schema: "catalog",
                table: "products");

            migrationBuilder.DropColumn(
                name: "slicer_value",
                schema: "catalog",
                table: "products");
        }
    }
}
