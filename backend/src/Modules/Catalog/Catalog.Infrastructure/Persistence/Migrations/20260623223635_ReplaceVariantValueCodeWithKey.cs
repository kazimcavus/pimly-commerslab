using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceVariantValueCodeWithKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "key",
                schema: "catalog",
                table: "variant_values",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variant_values
                SET key = trim(code)
                WHERE key IS NULL
                  AND code IS NOT NULL
                  AND trim(code) <> '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variant_values
                SET key = lower(
                    regexp_replace(
                        translate(
                            trim(label),
                            'ıİşŞğĞüÜöÖçÇ',
                            'iissgguuoocc'),
                        '[^a-z0-9]+',
                        '_',
                        'g'))
                WHERE key IS NULL OR trim(key) = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variant_values
                SET key = left(key, 191) || '_' || substr(replace(id::text, '-', ''), 1, 8)
                WHERE key IS NULL OR trim(key) = '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variant_values AS vv
                SET key = left(vv.key, 191) || '_' || substr(replace(vv.id::text, '-', ''), 1, 8)
                FROM (
                    SELECT id, key,
                           row_number() OVER (PARTITION BY variant_id, key ORDER BY id) AS row_num
                    FROM catalog.variant_values
                ) AS duplicates
                WHERE vv.id = duplicates.id
                  AND duplicates.row_num > 1;
                """);

            migrationBuilder.DropColumn(
                name: "code",
                schema: "catalog",
                table: "variant_values");

            migrationBuilder.AlterColumn<string>(
                name: "key",
                schema: "catalog",
                table: "variant_values",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_variant_values_variant_id_key",
                schema: "catalog",
                table: "variant_values",
                columns: new[] { "variant_id", "key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_variant_values_variant_id_key",
                schema: "catalog",
                table: "variant_values");

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "catalog",
                table: "variant_values",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE catalog.variant_values
                SET code = key
                WHERE code IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "key",
                schema: "catalog",
                table: "variant_values");
        }
    }
}
