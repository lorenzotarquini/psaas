using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinHorizon.MySync.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Transfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Transactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CounterAccountId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Postings",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postings", x => new { x.TransactionId, x.Id });
                    table.ForeignKey(
                        name: "FK_Postings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Postings_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Movements recorded before postings existed get theirs: balances stay exactly as they were.
            migrationBuilder.Sql("""
                INSERT INTO "Postings" ("TransactionId", "AccountId", "Amount")
                SELECT "Id", "AccountId", CASE WHEN "Kind" = 'Expense' THEN -"Amount" ELSE "Amount" END
                FROM "Transactions";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CounterAccountId",
                table: "Transactions",
                column: "CounterAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Postings_AccountId",
                table: "Postings",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Accounts_CounterAccountId",
                table: "Transactions",
                column: "CounterAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Accounts_CounterAccountId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Postings");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CounterAccountId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CounterAccountId",
                table: "Transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
