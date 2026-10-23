using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// OcppPendingTransactions.ConnectorID: an OCPP 2.0.1 transaction started on plug-in, before any token
/// (TxStartPoint EVConnected), waits for its authorization with the connector its Started event named, as later
/// events may omit the EVSE. Nullable, no foreign key (the row lives for minutes); works on SQL Server and MySQL /
/// MariaDB.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261023160000_PendingTransactionConnector")]
public partial class PendingTransactionConnector : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ConnectorID", table: "OcppPendingTransactions", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ConnectorID", table: "OcppPendingTransactions");
    }
}
