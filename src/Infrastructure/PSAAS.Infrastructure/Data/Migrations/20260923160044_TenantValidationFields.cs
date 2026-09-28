using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSAAS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantValidationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_CertifiedEmail",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_CompleteVatNumber",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_VatNumber",
                table: "Tenants");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCertifiedEmail",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedCompanyName",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "Tenants"
                SET "NormalizedCertifiedEmail" = upper(trim("CertifiedEmail")),
                    "NormalizedCompanyName" = lower(regexp_replace("CompanyName", '[^[:alnum:]]', '', 'g')),
                    "CompleteVatNumber" = upper(trim("CompleteVatNumber"));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CompleteVatNumber",
                table: "Tenants",
                column: "CompleteVatNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_NormalizedCertifiedEmail",
                table: "Tenants",
                column: "NormalizedCertifiedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_NormalizedCompanyName",
                table: "Tenants",
                column: "NormalizedCompanyName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_NormalizedCertifiedEmail",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_NormalizedCompanyName",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_CompleteVatNumber",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NormalizedCertifiedEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "NormalizedCompanyName",
                table: "Tenants");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CertifiedEmail",
                table: "Tenants",
                column: "CertifiedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CompleteVatNumber",
                table: "Tenants",
                column: "CompleteVatNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_VatNumber",
                table: "Tenants",
                column: "VatNumber",
                unique: true);
        }
    }
}
