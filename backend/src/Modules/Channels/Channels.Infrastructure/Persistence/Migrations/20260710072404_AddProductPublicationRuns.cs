using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPublicationRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_publication_runs",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    total_items = table.Column<int>(type: "integer", nullable: true),
                    processed_items = table.Column<int>(type: "integer", nullable: false),
                    published_items = table.Column<int>(type: "integer", nullable: false),
                    failed_items = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_publication_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_publication_run_errors",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    product_publication_run_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_publication_run_errors", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_publication_run_errors_product_publication_runs_pro~",
                        column: x => x.product_publication_run_id,
                        principalSchema: "channels",
                        principalTable: "product_publication_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_publication_run_errors_product_publication_run_id",
                schema: "channels",
                table: "product_publication_run_errors",
                column: "product_publication_run_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_publication_runs_status_created_at",
                schema: "channels",
                table: "product_publication_runs",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_product_publication_runs_tenant_id_marketplace_code_created~",
                schema: "channels",
                table: "product_publication_runs",
                columns: new[] { "tenant_id", "marketplace_code", "created_at" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_publication_run_errors",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "product_publication_runs",
                schema: "channels");
        }
    }
}
