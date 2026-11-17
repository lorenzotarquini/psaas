using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.MySync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Memberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    OwnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CurrentPeriodEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    Card_Brand = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Card_Last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Card_ExpMonth = table.Column<int>(type: "integer", nullable: true),
                    Card_ExpYear = table.Column<int>(type: "integer", nullable: true),
                    LastFailure_Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastFailure_Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastFailure_FailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PayUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.OwnerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_CustomerId",
                table: "Memberships",
                column: "CustomerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Memberships");
        }
    }
}
