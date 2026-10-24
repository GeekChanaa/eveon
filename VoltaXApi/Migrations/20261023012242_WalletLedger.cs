using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Wallet ledger: one immutable WalletTransactions row per balance change of a charging card (unique Reference
/// makes a replayed event a no-op), and Cards.BalanceVersion, the optimistic concurrency token of the balance.
/// Every card that already has a balance gets an "Opening balance" movement so the movements of a card add up
/// to its balance from the start. Written by hand: column types are left to the provider (SQL Server or
/// MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261023012242_WalletLedger")]
public partial class WalletLedger : Migration
{
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "BalanceVersion", table: "Cards", nullable: false, defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "WalletTransactions",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                CardID = table.Column<int>(nullable: false),
                Type = table.Column<string>(maxLength: 32, nullable: false),
                Amount = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                BalanceAfter = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                ChargingSessionID = table.Column<int>(nullable: true),
                OrderID = table.Column<int>(nullable: true),
                Reference = table.Column<string>(maxLength: 100, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                CreatedByUserID = table.Column<int>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WalletTransactions", w => w.ID);
                table.ForeignKey(
                    name: "FK_WalletTransactions_Cards_CardID",
                    column: w => w.CardID,
                    principalTable: "Cards",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_WalletTransactions_CardID_CreatedAt", table: "WalletTransactions", columns: new[] { "CardID", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_WalletTransactions_ChargingSessionID", table: "WalletTransactions", column: "ChargingSessionID");
        migrationBuilder.CreateIndex(name: "IX_WalletTransactions_OrderID", table: "WalletTransactions", column: "OrderID");
        migrationBuilder.CreateIndex(name: "IX_WalletTransactions_Reference", table: "WalletTransactions", column: "Reference", unique: true);

        if (migrationBuilder.ActiveProvider == SqlServerProvider)
            migrationBuilder.Sql(@"
INSERT INTO WalletTransactions (CardID, Type, Amount, BalanceAfter, Reference, Description, CreatedAt)
SELECT ID, 'Adjustment', Balance, Balance, CONCAT('opening:', ID), 'Opening balance', SYSUTCDATETIME()
FROM Cards WHERE Balance <> 0;");
        else
            migrationBuilder.Sql(@"
INSERT INTO `WalletTransactions` (`CardID`, `Type`, `Amount`, `BalanceAfter`, `Reference`, `Description`, `CreatedAt`)
SELECT `ID`, 'Adjustment', `Balance`, `Balance`, CONCAT('opening:', `ID`), 'Opening balance', UTC_TIMESTAMP(6)
FROM `Cards` WHERE `Balance` <> 0;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "WalletTransactions");
        migrationBuilder.DropColumn(name: "BalanceVersion", table: "Cards");
    }
}
