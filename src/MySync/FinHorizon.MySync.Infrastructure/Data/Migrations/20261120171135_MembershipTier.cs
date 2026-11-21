using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.MySync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MembershipTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Plan_Tier",
                table: "Memberships",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan_Tier",
                table: "Memberships");
        }
    }
}
