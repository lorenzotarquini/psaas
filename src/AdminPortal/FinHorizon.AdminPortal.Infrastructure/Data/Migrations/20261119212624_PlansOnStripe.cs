using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.AdminPortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlansOnStripe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Plans made before UC16-R4 were sold with 22% VAT included.
            migrationBuilder.AddColumn<bool>(
                name: "PriceIncludesVat",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "Plans",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeProductId",
                table: "Plans",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeTaxRateId",
                table: "Plans",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNature",
                table: "Plans",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Plans",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 22m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceIncludesVat",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "StripeProductId",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "StripeTaxRateId",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "VatNature",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Plans");
        }
    }
}
