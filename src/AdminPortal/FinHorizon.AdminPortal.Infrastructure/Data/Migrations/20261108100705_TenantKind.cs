using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.AdminPortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_NormalizedCompanyName",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Tenants",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Business");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_NormalizedCompanyName",
                table: "Tenants",
                column: "NormalizedCompanyName",
                unique: true,
                filter: "\"Kind\" = 'Business'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_NormalizedCompanyName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Tenants");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_NormalizedCompanyName",
                table: "Tenants",
                column: "NormalizedCompanyName",
                unique: true);
        }
    }
}
