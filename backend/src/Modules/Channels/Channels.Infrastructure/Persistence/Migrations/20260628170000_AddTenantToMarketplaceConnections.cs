using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Channels.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantToMarketplaceConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_marketplace_connections_marketplace_id",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "channels",
                table: "marketplace_connections",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                DELETE FROM channels.marketplace_connections;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "tenant_id",
                schema: "channels",
                table: "marketplace_connections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_id",
                schema: "channels",
                table: "marketplace_connections",
                columns: new[] { "tenant_id", "marketplace_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_marketplace_connections_tenant_id_marketplace_id",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "channels",
                table: "marketplace_connections");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_connections_marketplace_id",
                schema: "channels",
                table: "marketplace_connections",
                column: "marketplace_id",
                unique: true);
        }
    }
}
