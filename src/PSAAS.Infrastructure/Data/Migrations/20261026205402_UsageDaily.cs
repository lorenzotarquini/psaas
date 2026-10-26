using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSAAS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UsageDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageDaily",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    ModuleId = table.Column<string>(type: "text", nullable: false),
                    Metric = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageDaily", x => new { x.TenantId, x.ModuleId, x.Metric, x.Day });
                    table.ForeignKey(
                        name: "FK_UsageDaily_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageDaily");
        }
    }
}
