using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.MySync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Currencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transactions",
                type: "numeric(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "CounterAmount",
                table: "Transactions",
                type: "numeric(28,8)",
                precision: 28,
                scale: 8,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Postings",
                type: "numeric(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "InitialResidual",
                table: "Debts",
                type: "numeric(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Debts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                // Every amount before UC13 was in euro (RNF3).
                defaultValue: "EUR");

            migrationBuilder.AlterColumn<decimal>(
                name: "InitialBalance",
                table: "Accounts",
                type: "numeric(28,8)",
                precision: 28,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Accounts",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                // Every amount before UC13 was in euro (RNF3).
                defaultValue: "EUR");

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Preferences_OwnerId",
                table: "Preferences",
                column: "OwnerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Preferences");

            migrationBuilder.DropColumn(
                name: "CounterAmount",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Accounts");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldPrecision: 28,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Postings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldPrecision: 28,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "InitialResidual",
                table: "Debts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldPrecision: 28,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "InitialBalance",
                table: "Accounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(28,8)",
                oldPrecision: 28,
                oldScale: 8);
        }
    }
}
