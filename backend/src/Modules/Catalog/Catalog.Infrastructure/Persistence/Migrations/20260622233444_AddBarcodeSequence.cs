using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBarcodeSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "barcode_allocations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    barcode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allocated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barcode_allocations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "barcode_sequence",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    next_value = table.Column<long>(type: "bigint", nullable: false),
                    client_allocation_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barcode_sequence", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_barcode_allocations_barcode",
                schema: "catalog",
                table: "barcode_allocations",
                column: "barcode",
                unique: true);

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "barcode_sequence",
                columns: ["id", "next_value", "client_allocation_required", "updated_at"],
                values: [1, 1L, false, DateTimeOffset.UtcNow]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "barcode_allocations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "barcode_sequence",
                schema: "catalog");
        }
    }
}
