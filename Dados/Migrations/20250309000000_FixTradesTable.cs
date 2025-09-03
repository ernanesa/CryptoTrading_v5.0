using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dados.Migrations
{
    /// <inheritdoc />
    public partial class FixTradesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing primary key constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            // Add new primary key on Tid only
            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                column: "Tid");

            // Add index for Symbol and Date for better query performance
            migrationBuilder.CreateIndex(
                name: "IX_Trades_Symbol_Date",
                table: "Trades",
                columns: new[] { "Symbol", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new index
            migrationBuilder.DropIndex(
                name: "IX_Trades_Symbol_Date",
                table: "Trades");

            // Drop current primary key
            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            // Restore original composite primary key
            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                columns: new[] { "Symbol", "Tid" });
        }
    }
}