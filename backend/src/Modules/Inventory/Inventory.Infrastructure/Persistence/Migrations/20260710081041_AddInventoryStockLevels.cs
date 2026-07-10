using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryStockLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "stock_levels",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_levels", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_levels_product_item_id",
                schema: "inventory",
                table: "stock_levels",
                column: "product_item_id",
                unique: true);

            // Expand-contract: mevcut kalemlerin stoğunu (Catalog.product_items.stock) Inventory'ye
            // kopyala. Kaynak kolon dormant kalır (contract dilime kadar drop edilmez). Kolon varlığı
            // da kontrol edilir: Catalog'un stock kolonunu drop eden migration fresh DB'de bu kopyadan
            // önce koşabilir (tüm Catalog migration'ları Inventory'den önce). Kalem başına tek satır.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'catalog'
                          AND table_name = 'product_items'
                          AND column_name = 'stock'
                    ) THEN
                        INSERT INTO inventory.stock_levels
                            (id, product_item_id, quantity, updated_at, tenant_id)
                        SELECT gen_random_uuid(), pi.id, pi.stock, now(), pi.tenant_id
                        FROM catalog.product_items pi
                        WHERE NOT EXISTS (
                            SELECT 1 FROM inventory.stock_levels sl WHERE sl.product_item_id = pi.id);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_levels",
                schema: "inventory");
        }
    }
}
