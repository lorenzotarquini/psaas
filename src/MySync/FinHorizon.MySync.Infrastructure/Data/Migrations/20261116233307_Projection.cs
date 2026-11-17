using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.MySync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Projection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPayment",
                table: "Debts",
                type: "numeric(28,8)",
                precision: 28,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Fixed",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProjectionSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HorizonMonths = table.Column<int>(type: "integer", nullable: false),
                    Income = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FixedExpenses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VariableExpenses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Saving = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnualReturn = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    Inflation = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    InflationOn = table.Column<bool>(type: "boolean", nullable: false),
                    Goal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    OwnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Events = table.Column<string>(type: "jsonb", nullable: true),
                    Payments = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectionSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectionSettings_OwnerId",
                table: "ProjectionSettings",
                column: "OwnerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectionSettings");

            migrationBuilder.DropColumn(
                name: "MonthlyPayment",
                table: "Debts");

            migrationBuilder.DropColumn(
                name: "Fixed",
                table: "Categories");
        }
    }
}
