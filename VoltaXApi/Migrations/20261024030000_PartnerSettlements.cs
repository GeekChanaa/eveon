using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Partner commission terms (Partners.CommissionRate as a fraction, Partners.CommissionPerKwh excl. VAT, both 0 for
/// existing partners) and monthly settlements: PartnerSettlements (one per partner and month, totals, terms applied,
/// payout status) and PartnerSettlementLines (one per session or refund, unique source key so nothing is settled
/// twice). Written by hand: column types are left to the provider (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261024030000_PartnerSettlements")]
public partial class PartnerSettlements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "CommissionRate", table: "Partners", precision: 5, scale: 4, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<decimal>(name: "CommissionPerKwh", table: "Partners", precision: 18, scale: 4, nullable: false, defaultValue: 0m);

        migrationBuilder.CreateTable(
            name: "PartnerSettlements",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                PartnerID = table.Column<int>(nullable: false),
                Year = table.Column<int>(nullable: false),
                Month = table.Column<int>(nullable: false),
                PeriodStart = table.Column<DateTime>(nullable: false),
                PeriodEnd = table.Column<DateTime>(nullable: false),
                SessionCount = table.Column<int>(nullable: false),
                EnergyKwh = table.Column<decimal>(precision: 18, scale: 3, nullable: false),
                GrossAmount = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                NetExclVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                RevenueVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                CommissionExclVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                CommissionVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                NetPayout = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                CommissionRate = table.Column<decimal>(precision: 5, scale: 4, nullable: false),
                CommissionPerKwh = table.Column<decimal>(precision: 18, scale: 4, nullable: false),
                VatRate = table.Column<decimal>(precision: 5, scale: 4, nullable: false),
                Status = table.Column<string>(maxLength: 16, nullable: false),
                PaidAt = table.Column<DateTime>(nullable: true),
                PaymentReference = table.Column<string>(maxLength: 128, nullable: true),
                PaidByUserID = table.Column<int>(nullable: true),
                GeneratedAt = table.Column<DateTime>(nullable: false),
                GeneratedByUserID = table.Column<int>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_PartnerSettlements", s => s.ID));
        migrationBuilder.CreateIndex(name: "IX_PartnerSettlements_PartnerID_Year_Month", table: "PartnerSettlements",
            columns: new[] { "PartnerID", "Year", "Month" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_PartnerSettlements_Year_Month", table: "PartnerSettlements", columns: new[] { "Year", "Month" });

        migrationBuilder.CreateTable(
            name: "PartnerSettlementLines",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                SettlementID = table.Column<int>(nullable: false),
                SourceKey = table.Column<string>(maxLength: 64, nullable: false),
                ChargingSessionID = table.Column<int>(nullable: true),
                RefundID = table.Column<int>(nullable: true),
                Date = table.Column<DateTime>(nullable: false),
                Station = table.Column<string>(maxLength: 128, nullable: true),
                ChargePoint = table.Column<string>(maxLength: 64, nullable: true),
                Kwh = table.Column<decimal>(precision: 18, scale: 3, nullable: false),
                Gross = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                NetExclVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                Vat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                CommissionExclVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                CommissionVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                NetPayout = table.Column<decimal>(precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PartnerSettlementLines", l => l.ID);
                table.ForeignKey(
                    name: "FK_PartnerSettlementLines_PartnerSettlements_SettlementID",
                    column: l => l.SettlementID,
                    principalTable: "PartnerSettlements",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_PartnerSettlementLines_SourceKey", table: "PartnerSettlementLines", column: "SourceKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_PartnerSettlementLines_ChargingSessionID", table: "PartnerSettlementLines", column: "ChargingSessionID");
        migrationBuilder.CreateIndex(name: "IX_PartnerSettlementLines_SettlementID", table: "PartnerSettlementLines", column: "SettlementID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PartnerSettlementLines");
        migrationBuilder.DropTable(name: "PartnerSettlements");
        migrationBuilder.DropColumn(name: "CommissionRate", table: "Partners");
        migrationBuilder.DropColumn(name: "CommissionPerKwh", table: "Partners");
    }
}
