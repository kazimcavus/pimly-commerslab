using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuGeneratorConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sku_generator_config",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    counter_next_value = table.Column<long>(type: "bigint", nullable: false),
                    segments = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sku_generator_config", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "sku_generator_config",
                columns: ["id", "enabled", "counter_next_value", "segments"],
                values: [1, false, 1L, "[]"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sku_generator_config",
                schema: "catalog");
        }
    }
}
