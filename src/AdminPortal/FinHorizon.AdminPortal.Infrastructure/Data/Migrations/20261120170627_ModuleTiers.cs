using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.AdminPortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModuleTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "Plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tiers",
                table: "Modules",
                type: "jsonb",
                nullable: true);

            // The tiers MySync declares (docs/architettura-moduli.md): its manifest, registered once here.
            migrationBuilder.Sql(
                """
                UPDATE "Modules" SET "Tiers" = '[
                  {"level":1,"name":"Base","features":[],"limits":{"accounts":2,"debts":1,"subscriptions":5}},
                  {"level":2,"name":"Plus","features":["projection"],"limits":{"accounts":10,"debts":10,"subscriptions":20}},
                  {"level":3,"name":"Pro","features":["projection"],"limits":{"accounts":100,"debts":50,"subscriptions":100}}
                ]'::jsonb WHERE "Id" = 'mysync';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Tiers",
                table: "Modules");
        }
    }
}
