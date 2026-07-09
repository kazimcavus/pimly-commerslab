using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropCatalogPriceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_item_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "price_definitions",
                schema: "catalog");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_definitions",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_item_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    price_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_item_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_item_prices_price_definitions_price_definition_id",
                        column: x => x.price_definition_id,
                        principalSchema: "catalog",
                        principalTable: "price_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_item_prices_product_items_product_item_id",
                        column: x => x.product_item_id,
                        principalSchema: "catalog",
                        principalTable: "product_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_price_definitions_tenant_id_name",
                schema: "catalog",
                table: "price_definitions",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_item_prices_price_definition_id",
                schema: "catalog",
                table: "product_item_prices",
                column: "price_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_item_prices_product_item_id_price_definition_id",
                schema: "catalog",
                table: "product_item_prices",
                columns: new[] { "product_item_id", "price_definition_id" },
                unique: true);
        }
    }
}
