using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Online payments: orders keep their provider, provider reference, payment time, failure reason and the
/// operator of a manual top-up; PaymentEvents records each verified provider notification once (unique per
/// provider and event id); users get their provider customer id (saved cards) and saved cards a default flag.
/// Written by hand: column types are left to the provider (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261023020956_OnlinePayments")]
public partial class OnlinePayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Provider", table: "Orders", maxLength: 32, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ProviderReference", table: "Orders", maxLength: 128, nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "PaidAt", table: "Orders", nullable: true);
        migrationBuilder.AddColumn<string>(name: "FailureReason", table: "Orders", maxLength: 256, nullable: true);
        migrationBuilder.AddColumn<int>(name: "CreatedByUserID", table: "Orders", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Note", table: "Orders", maxLength: 256, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_Provider_ProviderReference", table: "Orders", columns: new[] { "Provider", "ProviderReference" });

        migrationBuilder.AddColumn<string>(name: "PaymentCustomerId", table: "Users", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "IsDefault", table: "DebitCards", nullable: false, defaultValue: false);

        migrationBuilder.CreateTable(
            name: "PaymentEvents",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Provider = table.Column<string>(maxLength: 32, nullable: false),
                EventId = table.Column<string>(maxLength: 128, nullable: false),
                Type = table.Column<string>(maxLength: 64, nullable: true),
                OrderID = table.Column<int>(nullable: true),
                Result = table.Column<string>(maxLength: 256, nullable: true),
                ReceivedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PaymentEvents", e => e.ID));
        migrationBuilder.CreateIndex(name: "IX_PaymentEvents_Provider_EventId", table: "PaymentEvents", columns: new[] { "Provider", "EventId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_PaymentEvents_OrderID", table: "PaymentEvents", column: "OrderID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PaymentEvents");
        migrationBuilder.DropIndex(name: "IX_Orders_Provider_ProviderReference", table: "Orders");
        foreach (var column in new[] { "Provider", "ProviderReference", "PaidAt", "FailureReason", "CreatedByUserID", "Note" })
            migrationBuilder.DropColumn(name: column, table: "Orders");
        migrationBuilder.DropColumn(name: "PaymentCustomerId", table: "Users");
        migrationBuilder.DropColumn(name: "IsDefault", table: "DebitCards");
    }
}
