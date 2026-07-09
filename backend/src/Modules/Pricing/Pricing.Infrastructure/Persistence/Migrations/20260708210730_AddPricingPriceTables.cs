using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pricing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingPriceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "pricing");

            migrationBuilder.CreateTable(
                name: "price_definitions",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_item_prices",
                schema: "pricing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_item_prices", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_item_prices_price_definitions_price_definition_id",
                        column: x => x.price_definition_id,
                        principalSchema: "pricing",
                        principalTable: "price_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_price_definitions_tenant_id_name",
                schema: "pricing",
                table: "price_definitions",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_item_prices_price_definition_id",
                schema: "pricing",
                table: "product_item_prices",
                column: "price_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_item_prices_product_item_id_price_definition_id",
                schema: "pricing",
                table: "product_item_prices",
                columns: new[] { "product_item_id", "price_definition_id" },
                unique: true);

            // Expand-contract: Catalog'un fiyat verisini Pricing şemasına kopyala. Kaynak tablolar
            // dormant kalır (contract dilime kadar drop edilmez). to_regclass guard'ı, catalog fiyat
            // tablolarının henüz oluşturulmadığı ortamlarda migration'ın güvenle geçmesini sağlar;
            // ON CONFLICT tekrar çalıştırmalarda çifte ekleme yapmaz.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('catalog.price_definitions') IS NOT NULL THEN
                        INSERT INTO pricing.price_definitions (id, name, code, tenant_id)
                        SELECT id, name, code, tenant_id
                        FROM catalog.price_definitions
                        ON CONFLICT (id) DO NOTHING;
                    END IF;

                    IF to_regclass('catalog.product_item_prices') IS NOT NULL THEN
                        INSERT INTO pricing.product_item_prices
                            (id, product_item_id, price_definition_id, amount, currency, updated_at, tenant_id)
                        SELECT id, product_item_id, price_definition_id, amount, currency, updated_at, tenant_id
                        FROM catalog.product_item_prices
                        ON CONFLICT (id) DO NOTHING;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_item_prices",
                schema: "pricing");

            migrationBuilder.DropTable(
                name: "price_definitions",
                schema: "pricing");
        }
    }
}
