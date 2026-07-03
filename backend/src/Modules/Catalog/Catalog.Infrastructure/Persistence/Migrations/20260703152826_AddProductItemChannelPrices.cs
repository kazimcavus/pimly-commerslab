using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductItemChannelPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_item_channel_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    price = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    compare_at_price = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_item_channel_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_item_channel_prices_product_items_product_item_id",
                        column: x => x.product_item_id,
                        principalSchema: "catalog",
                        principalTable: "product_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_item_channel_prices_product_item_id_marketplace_key",
                schema: "catalog",
                table: "product_item_channel_prices",
                columns: new[] { "product_item_id", "marketplace_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_item_channel_prices_tenant_id_marketplace_key",
                schema: "catalog",
                table: "product_item_channel_prices",
                columns: new[] { "tenant_id", "marketplace_key" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_item_channel_prices",
                schema: "catalog");
        }
    }
}
