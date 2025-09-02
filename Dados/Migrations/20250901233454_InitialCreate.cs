using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dados.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetFees",
                columns: table => new
                {
                    Asset = table.Column<string>(type: "text", nullable: false),
                    WithdrawalFee = table.Column<decimal>(type: "numeric", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetFees", x => x.Asset);
                });

            migrationBuilder.CreateTable(
                name: "AssetNetworks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Asset = table.Column<string>(type: "text", nullable: false),
                    Network = table.Column<string>(type: "text", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    WithdrawalFee = table.Column<decimal>(type: "numeric", nullable: false),
                    MinWithdrawalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    WithdrawalEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetNetworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Candles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Open = table.Column<decimal>(type: "numeric", nullable: false),
                    High = table.Column<decimal>(type: "numeric", nullable: false),
                    Low = table.Column<decimal>(type: "numeric", nullable: false),
                    Close = table.Column<decimal>(type: "numeric", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Bids = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Asks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderBooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Symbols",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    BaseCurrency = table.Column<string>(type: "text", nullable: false),
                    QuoteCurrency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    BasePrecision = table.Column<int>(type: "integer", nullable: false),
                    QuotePrecision = table.Column<int>(type: "integer", nullable: false),
                    AmountPrecision = table.Column<int>(type: "integer", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symbols", x => x.Symbol);
                });

            migrationBuilder.CreateTable(
                name: "Tickers",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Last = table.Column<decimal>(type: "numeric", nullable: false),
                    High = table.Column<decimal>(type: "numeric", nullable: false),
                    Low = table.Column<decimal>(type: "numeric", nullable: false),
                    Vol = table.Column<decimal>(type: "numeric", nullable: false),
                    Buy = table.Column<decimal>(type: "numeric", nullable: false),
                    Sell = table.Column<decimal>(type: "numeric", nullable: false),
                    Date = table.Column<long>(type: "bigint", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickers", x => x.Symbol);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Tid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Tid);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetNetworks_Asset_Network",
                table: "AssetNetworks",
                columns: new[] { "Asset", "Network" });

            migrationBuilder.CreateIndex(
                name: "IX_Candles_Symbol_Resolution_Timestamp",
                table: "Candles",
                columns: new[] { "Symbol", "Resolution", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetFees");

            migrationBuilder.DropTable(
                name: "AssetNetworks");

            migrationBuilder.DropTable(
                name: "Candles");

            migrationBuilder.DropTable(
                name: "OrderBooks");

            migrationBuilder.DropTable(
                name: "Symbols");

            migrationBuilder.DropTable(
                name: "Tickers");

            migrationBuilder.DropTable(
                name: "Trades");
        }
    }
}
