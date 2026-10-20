using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSAAS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfilesPerTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profiles_Name",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_ProductiveUnits_Name",
                table: "ProductiveUnits");

            migrationBuilder.DropIndex(
                name: "IX_ProductiveUnits_TenantId",
                table: "ProductiveUnits");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE "_ProfileTenants" AS
                SELECT "ProfileId", "TenantId",
                       ROW_NUMBER() OVER (PARTITION BY "ProfileId" ORDER BY "TenantId") AS rn,
                       gen_random_uuid()::text AS "NewId"
                FROM (
                    SELECT DISTINCT pup."ProfileId", pu."TenantId"
                    FROM "ProductiveUnitProfiles" pup
                    JOIN "ProductiveUnits" pu ON pu."Id" = pup."ProductiveUnitId"
                ) pairs;

                UPDATE "Profiles" p
                SET "TenantId" = t."TenantId"
                FROM "_ProfileTenants" t
                WHERE t."ProfileId" = p."Id" AND t.rn = 1;

                INSERT INTO "Profiles" ("Id", "Name", "Description", "TenantId")
                SELECT t."NewId", p."Name", p."Description", t."TenantId"
                FROM "_ProfileTenants" t
                JOIN "Profiles" p ON p."Id" = t."ProfileId"
                WHERE t.rn > 1;

                UPDATE "ProductiveUnitProfiles" pup
                SET "ProfileId" = t."NewId"
                FROM "_ProfileTenants" t, "ProductiveUnits" pu
                WHERE t.rn > 1
                  AND pu."Id" = pup."ProductiveUnitId"
                  AND pu."TenantId" = t."TenantId"
                  AND pup."ProfileId" = t."ProfileId";

                DROP TABLE "_ProfileTenants";

                DELETE FROM "Profiles" WHERE "TenantId" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "Profiles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_TenantId_Name",
                table: "Profiles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductiveUnits_TenantId_Name",
                table: "ProductiveUnits",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Tenants_TenantId",
                table: "Profiles",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_Tenants_TenantId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_TenantId_Name",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_ProductiveUnits_TenantId_Name",
                table: "ProductiveUnits");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Profiles");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Name",
                table: "Profiles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductiveUnits_Name",
                table: "ProductiveUnits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductiveUnits_TenantId",
                table: "ProductiveUnits",
                column: "TenantId");
        }
    }
}
