using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVariantKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "key",
                schema: "catalog",
                table: "variants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variants
                SET key = lower(
                    regexp_replace(
                        translate(
                            trim(name),
                            'ıİşŞğĞüÜöÖçÇ',
                            'iissgguuoocc'),
                        '[^a-z0-9]+',
                        '_',
                        'g'))
                WHERE key IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variants
                SET key = left(key, 191) || '_' || substr(replace(id::text, '-', ''), 1, 8)
                WHERE key IS NULL OR key = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variants AS v
                SET key = left(v.key, 191) || '_' || substr(replace(v.id::text, '-', ''), 1, 8)
                FROM (
                    SELECT id, key,
                           row_number() OVER (PARTITION BY key ORDER BY id) AS row_num
                    FROM catalog.variants
                ) AS duplicates
                WHERE v.id = duplicates.id
                  AND duplicates.row_num > 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "key",
                schema: "catalog",
                table: "variants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_variants_key",
                schema: "catalog",
                table: "variants",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_variants_key",
                schema: "catalog",
                table: "variants");

            migrationBuilder.DropColumn(
                name: "key",
                schema: "catalog",
                table: "variants");
        }
    }
}
