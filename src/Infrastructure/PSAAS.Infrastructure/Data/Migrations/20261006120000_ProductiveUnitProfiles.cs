using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSAAS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductiveUnitProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductiveUnitProfiles",
                columns: table => new
                {
                    ProductiveUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductiveUnitProfiles", x => new { x.ProductiveUnitId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_ProductiveUnitProfiles_ProductiveUnits_ProductiveUnitId",
                        column: x => x.ProductiveUnitId,
                        principalTable: "ProductiveUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductiveUnitProfiles_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "ProductiveUnitProfiles" ("ProductiveUnitId", "ProfileId")
                SELECT "Id", "ProfileId"
                FROM "ProductiveUnits"
                WHERE "ProfileId" IS NOT NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ProductiveUnits_Profiles_ProfileId",
                table: "ProductiveUnits");

            migrationBuilder.DropIndex(
                name: "IX_ProductiveUnits_ProfileId",
                table: "ProductiveUnits");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "ProductiveUnits");

            migrationBuilder.CreateIndex(
                name: "IX_ProductiveUnitProfiles_ProfileId",
                table: "ProductiveUnitProfiles",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileId",
                table: "ProductiveUnits",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "ProductiveUnits" AS pu
                SET "ProfileId" = pup."ProfileId"
                FROM (
                    SELECT DISTINCT ON ("ProductiveUnitId") "ProductiveUnitId", "ProfileId"
                    FROM "ProductiveUnitProfiles"
                    ORDER BY "ProductiveUnitId", "ProfileId"
                ) AS pup
                WHERE pu."Id" = pup."ProductiveUnitId";
                """);

            migrationBuilder.DropTable(
                name: "ProductiveUnitProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_ProductiveUnits_ProfileId",
                table: "ProductiveUnits",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductiveUnits_Profiles_ProfileId",
                table: "ProductiveUnits",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
