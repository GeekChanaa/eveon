using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Indexes for the hot lookups and the statistics windows (see docs/performance.md): cards by number, transactions
/// by the charger's id and by start, a driver's current session, a connector's sessions by start, sessions and
/// orders by date, and a charger's message log by time. Cards.CardNumber, Transactions.Uid and
/// MessageLogs.ChargePointId get a length so they can be index keys; the ALTER fails (nothing is truncated) if a
/// stored value is longer. The single-column indexes on ChargingSessions.ConnectorID / UserID are replaced by the
/// composite ones that start with the same column (they still serve the foreign keys).
/// Written by hand: column types are left to the provider (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261026010000_QueryIndexes")]
public partial class QueryIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(name: "CardNumber", table: "Cards", maxLength: 64, nullable: false,
            oldClrType: typeof(string), oldNullable: false);
        migrationBuilder.AlterColumn<string>(name: "Uid", table: "Transactions", maxLength: 64, nullable: true,
            oldClrType: typeof(string), oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "ChargePointId", table: "MessageLogs", maxLength: 450, nullable: true,
            oldClrType: typeof(string), oldNullable: true);

        migrationBuilder.CreateIndex(name: "IX_Cards_CardNumber", table: "Cards", column: "CardNumber");
        migrationBuilder.CreateIndex(name: "IX_Transactions_Uid", table: "Transactions", column: "Uid");
        migrationBuilder.CreateIndex(name: "IX_Transactions_StartTime", table: "Transactions", column: "StartTime");
        migrationBuilder.CreateIndex(name: "IX_Orders_RechargeDate", table: "Orders", column: "RechargeDate");
        migrationBuilder.CreateIndex(name: "IX_MessageLogs_ChargePointId_LogTime", table: "MessageLogs", columns: new[] { "ChargePointId", "LogTime" });
        migrationBuilder.CreateIndex(name: "IX_ChargingSessions_StartDate", table: "ChargingSessions", column: "StartDate");
        migrationBuilder.CreateIndex(name: "IX_ChargingSessions_UserID_EndDate", table: "ChargingSessions", columns: new[] { "UserID", "EndDate" });
        migrationBuilder.CreateIndex(name: "IX_ChargingSessions_ConnectorID_StartDate", table: "ChargingSessions", columns: new[] { "ConnectorID", "StartDate" });

        // After the composite indexes exist: MySQL / MariaDB keep an index on every foreign key column.
        migrationBuilder.DropIndex(name: "IX_ChargingSessions_UserID", table: "ChargingSessions");
        migrationBuilder.DropIndex(name: "IX_ChargingSessions_ConnectorID", table: "ChargingSessions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(name: "IX_ChargingSessions_UserID", table: "ChargingSessions", column: "UserID");
        migrationBuilder.CreateIndex(name: "IX_ChargingSessions_ConnectorID", table: "ChargingSessions", column: "ConnectorID");

        migrationBuilder.DropIndex(name: "IX_ChargingSessions_ConnectorID_StartDate", table: "ChargingSessions");
        migrationBuilder.DropIndex(name: "IX_ChargingSessions_UserID_EndDate", table: "ChargingSessions");
        migrationBuilder.DropIndex(name: "IX_ChargingSessions_StartDate", table: "ChargingSessions");
        migrationBuilder.DropIndex(name: "IX_MessageLogs_ChargePointId_LogTime", table: "MessageLogs");
        migrationBuilder.DropIndex(name: "IX_Orders_RechargeDate", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Transactions_StartTime", table: "Transactions");
        migrationBuilder.DropIndex(name: "IX_Transactions_Uid", table: "Transactions");
        migrationBuilder.DropIndex(name: "IX_Cards_CardNumber", table: "Cards");

        migrationBuilder.AlterColumn<string>(name: "ChargePointId", table: "MessageLogs", nullable: true,
            oldClrType: typeof(string), oldMaxLength: 450, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "Uid", table: "Transactions", nullable: true,
            oldClrType: typeof(string), oldMaxLength: 64, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "CardNumber", table: "Cards", nullable: false,
            oldClrType: typeof(string), oldMaxLength: 64, oldNullable: false);
    }
}
