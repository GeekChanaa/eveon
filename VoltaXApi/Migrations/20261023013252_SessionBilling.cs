using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Charging sessions keep the full tariff they started with (price per kWh, flat fee, VAT rate join the
/// existing per-minute prices) and the amounts billed when they end (energy, time, flat fee, idle, total) with
/// the card balance before and after the debit. All nullable: sessions billed before this keep their
/// time-based amounts. Column types are left to the provider (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261023013252_SessionBilling")]
public partial class SessionBilling : Migration
{
    private static readonly (string Column, int Precision, int Scale)[] DecimalColumns =
    {
        ("PricePerKWh", 18, 4),
        ("FlatFee", 18, 4),
        ("VatRate", 5, 4),
        ("EnergyAmount", 18, 2),
        ("TimeAmount", 18, 2),
        ("FlatFeeAmount", 18, 2),
        ("IdleAmount", 18, 2),
        ("TotalAmount", 18, 2),
        ("BalanceBefore", 18, 2),
        ("BalanceAfter", 18, 2),
    };

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (column, precision, scale) in DecimalColumns)
            migrationBuilder.AddColumn<decimal>(name: column, table: "ChargingSessions", precision: precision, scale: scale, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "BilledAt", table: "ChargingSessions", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var (column, _, _) in DecimalColumns)
            migrationBuilder.DropColumn(name: column, table: "ChargingSessions");
        migrationBuilder.DropColumn(name: "BilledAt", table: "ChargingSessions");
    }
}
