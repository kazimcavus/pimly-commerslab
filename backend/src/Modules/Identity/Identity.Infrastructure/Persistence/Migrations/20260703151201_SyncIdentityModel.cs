using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncIdentityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tenant_memberships_tenants_tenant_id",
                schema: "identity",
                table: "tenant_memberships");

            migrationBuilder.DropForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                schema: "identity",
                table: "tenant_memberships");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_tenant_memberships_tenants_tenant_id",
                schema: "identity",
                table: "tenant_memberships",
                column: "tenant_id",
                principalSchema: "identity",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tenant_memberships_users_user_id",
                schema: "identity",
                table: "tenant_memberships",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
