using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "channels");

            migrationBuilder.CreateTable(
                name: "marketplaces",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_connections",
                schema: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    api_secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketplace_connections", x => x.id);
                    table.ForeignKey(
                        name: "FK_marketplace_connections_marketplaces_marketplace_id",
                        column: x => x.marketplace_id,
                        principalSchema: "channels",
                        principalTable: "marketplaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_marketplaces_key",
                schema: "channels",
                table: "marketplaces",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_connections_marketplace_id",
                schema: "channels",
                table: "marketplace_connections",
                column: "marketplace_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketplace_connections",
                schema: "channels");

            migrationBuilder.DropTable(
                name: "marketplaces",
                schema: "channels");
        }
    }
}
