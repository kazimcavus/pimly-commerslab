using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingBasePrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "base_prices",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    compare_at_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_base_prices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_base_prices_product_item_id",
                schema: "pricing",
                table: "base_prices",
                column: "product_item_id",
                unique: true);

            // Expand-contract: mevcut kalemlerin temel fiyatını (Catalog.product_items.price /
            // compare_at_price) Pricing'e kopyala. Kaynak kolonlar dormant kalır (contract dilime
            // kadar drop edilmez). Kalem başına tek satır; tekrar çalıştırmada çakışma yaşanmaması
            // için önce var olmayan kalemler seçilir. Yeni id üretilir; para birimi TRY varsayılır.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    -- Kolon varlığı da kontrol edilir: Catalog'un price/compare_at_price kolonlarını
                    -- drop eden migration fresh DB'de bu kopyadan önce çalışabilir (tüm Catalog
                    -- migration'ları Pricing'den önce koşar). Kolon yoksa kopya atlanır.
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'catalog'
                          AND table_name = 'product_items'
                          AND column_name = 'price'
                    ) THEN
                        INSERT INTO pricing.base_prices
                            (id, product_item_id, amount, compare_at_amount, currency, updated_at, tenant_id)
                        SELECT gen_random_uuid(), pi.id, pi.price, pi.compare_at_price, 'TRY', now(), pi.tenant_id
                        FROM catalog.product_items pi
                        WHERE NOT EXISTS (
                            SELECT 1 FROM pricing.base_prices bp WHERE bp.product_item_id = pi.id);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "base_prices",
                schema: "pricing");
        }
    }
}
