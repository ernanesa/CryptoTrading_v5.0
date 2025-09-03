using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dados.Migrations
{
    public partial class UpdateTradeCompositeKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing PK
            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            // Ensure there are no duplicate (Symbol,Tid) pairs; for safety delete duplicates keeping the latest
            migrationBuilder.Sql("DELETE FROM \"Trades\" a USING \"Trades\" b WHERE a.ctid < b.ctid AND a.\"Tid\" = b.\"Tid\" AND a.\"Symbol\" = b.\"Symbol\";");

            // Add new composite PK
            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                columns: new[] { "Symbol", "Tid" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Trades",
                table: "Trades");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Trades",
                table: "Trades",
                column: "Tid");
        }
    }
}
