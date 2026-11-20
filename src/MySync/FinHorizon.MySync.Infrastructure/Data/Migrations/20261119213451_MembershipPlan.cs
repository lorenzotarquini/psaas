using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.MySync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MembershipPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Plan_Amount",
                table: "Memberships",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plan_Currency",
                table: "Memberships",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plan_Interval",
                table: "Memberships",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plan_Name",
                table: "Memberships",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan_Amount",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "Plan_Currency",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "Plan_Interval",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "Plan_Name",
                table: "Memberships");
        }
    }
}
