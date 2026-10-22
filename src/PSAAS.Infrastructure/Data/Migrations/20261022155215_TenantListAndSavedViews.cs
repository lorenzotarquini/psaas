using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSAAS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantListAndSavedViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Area = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Query = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_OwnerId_Area",
                table: "SavedViews",
                columns: new[] { "OwnerId", "Area" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedViews");
        }
    }
}
