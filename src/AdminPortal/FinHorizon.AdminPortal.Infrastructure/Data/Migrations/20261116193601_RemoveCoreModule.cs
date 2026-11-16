using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinHorizon.AdminPortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCoreModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Core is no longer mandatory: it leaves tenants, plans, add-ons and dependencies before the catalog.
            migrationBuilder.Sql("""DELETE FROM "TenantModules" WHERE "ModuleId" = 'core';""");
            migrationBuilder.Sql("""UPDATE "Plans" SET "ModuleIds" = array_remove("ModuleIds", 'core');""");
            migrationBuilder.Sql("""UPDATE "Subscriptions" SET "AddOnModuleIds" = array_remove("AddOnModuleIds", 'core');""");
            migrationBuilder.Sql("""UPDATE "Modules" SET "Requires" = array_remove("Requires", 'core');""");

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: "core");

            migrationBuilder.DropColumn(
                name: "IsCore",
                table: "Modules");

            // MySync is assigned to single users (realm role module-mysync): it has to be in the catalog to be chosen.
            migrationBuilder.Sql("""
                INSERT INTO "Modules" ("Id", "Name", "Description", "Icon", "Status", "Version", "Requires", "Permissions", "UpdatedAt")
                VALUES ('mysync', 'MySync', 'Finanze personali: conti, debiti, movimenti e patrimonio', 'wallet', 'Available', '1.0.0', '{}', '{}', now())
                ON CONFLICT ("Id") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // MySync stays in the catalog: tenants or users may already use it.
            migrationBuilder.AddColumn<bool>(
                name: "IsCore",
                table: "Modules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "DeprovisionUrl", "Description", "Icon", "IsCore", "Name", "Permissions", "ProvisionUrl", "Requires", "Status", "UpdatedAt", "Version" },
                values: new object[] { "core", null, "Base comune della piattaforma", "cube", true, "Core", new string[0], null, new string[0], "Available", new DateTimeOffset(new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "2.0.3" });

            // The core rows removed from tenants and plans are not restored.
        }
    }
}
