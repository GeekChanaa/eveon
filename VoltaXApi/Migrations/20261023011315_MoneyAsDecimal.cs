using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Money stored as decimal instead of double: amounts and balances decimal(18,2), unit prices decimal(18,4),
/// VAT rates decimal(5,4). ALTER COLUMN on SQL Server, MODIFY COLUMN on MySQL / MariaDB (the generic
/// AlterColumn operation produces both); only the old type differs between providers (float vs double).
/// Existing values are converted by the database (rounded to the new scale).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261023011315_MoneyAsDecimal")]
public partial class MoneyAsDecimal : Migration
{
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    private static readonly (string Table, string Column, string Type, bool Nullable)[] Columns =
    {
        ("Cards", "Balance", "decimal(18,2)", false),
        ("Orders", "Amount", "decimal(18,2)", false),
        ("Transactions", "Amount", "decimal(18,2)", false),
        ("AccountDeletionRequests", "WalletBalance", "decimal(18,2)", false),
        ("Connectors", "PricePerKWh", "decimal(18,4)", false),
        ("Connectors", "PricePerMinute", "decimal(18,4)", false),
        ("Connectors", "PricePerIdleMinute", "decimal(18,4)", false),
        ("Connectors", "CostPerKwh", "decimal(18,4)", false),
        ("Connectors", "FlatFee", "decimal(18,4)", false),
        ("ChargingSessions", "PricePerMinute", "decimal(18,4)", true),
        ("ChargingSessions", "PricePerIdleMinute", "decimal(18,4)", true),
        ("ChargingSessions", "CostPerKwh", "decimal(18,4)", true),
        ("OcpiSessions", "PricePerKWh", "decimal(18,4)", false),
        ("OcpiSessions", "PricePerMinute", "decimal(18,4)", false),
        ("OcpiSessions", "PricePerIdleMinute", "decimal(18,4)", false),
        ("OcpiSessions", "FlatFee", "decimal(18,4)", false),
        ("OcpiSessions", "VatRate", "decimal(5,4)", false),
    };

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var doubleType = DoubleType(migrationBuilder);
        foreach (var (table, column, type, nullable) in Columns)
        {
            if (nullable)
                migrationBuilder.AlterColumn<decimal>(name: column, table: table, type: type, nullable: true,
                    oldClrType: typeof(double), oldType: doubleType, oldNullable: true);
            else
                migrationBuilder.AlterColumn<decimal>(name: column, table: table, type: type, nullable: false,
                    oldClrType: typeof(double), oldType: doubleType);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        var doubleType = DoubleType(migrationBuilder);
        foreach (var (table, column, type, nullable) in Columns)
        {
            if (nullable)
                migrationBuilder.AlterColumn<double>(name: column, table: table, type: doubleType, nullable: true,
                    oldClrType: typeof(decimal), oldType: type, oldNullable: true);
            else
                migrationBuilder.AlterColumn<double>(name: column, table: table, type: doubleType, nullable: false,
                    oldClrType: typeof(decimal), oldType: type);
        }
    }

    private static string DoubleType(MigrationBuilder migrationBuilder) =>
        migrationBuilder.ActiveProvider == SqlServerProvider ? "float" : "double";
}
