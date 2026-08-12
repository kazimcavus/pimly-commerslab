using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_listings",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    external_listing_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    submission_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    offer_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    content_dirty_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    offer_dirty_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sync_attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_listings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_listings_product_item_id",
                schema: "channels",
                table: "product_listings",
                column: "product_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_listings_tenant_id_marketplace_code_product_item_id",
                schema: "channels",
                table: "product_listings",
                columns: new[] { "tenant_id", "marketplace_code", "product_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_listings_dirty",
                schema: "channels",
                table: "product_listings",
                columns: new[] { "tenant_id", "marketplace_code" },
                filter: "content_dirty_at IS NOT NULL OR offer_dirty_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_listings",
                schema: "channels");
        }
    }
}
