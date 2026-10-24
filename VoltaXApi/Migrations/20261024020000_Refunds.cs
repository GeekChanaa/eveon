using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Refunds of top-ups and charging sessions decided by operators: amount, reason, method (provider, wallet or
/// outside the platform), status and provider reference, with a unique idempotency key so a request sent twice
/// refunds once. Written by hand: column types are left to the provider (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261024020000_Refunds")]
public partial class Refunds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Refunds",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Target = table.Column<string>(maxLength: 16, nullable: false),
                OrderID = table.Column<int>(nullable: true),
                ChargingSessionID = table.Column<int>(nullable: true),
                CardID = table.Column<int>(nullable: false),
                UserID = table.Column<int>(nullable: true),
                Amount = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                Reason = table.Column<string>(maxLength: 256, nullable: false),
                Method = table.Column<string>(maxLength: 16, nullable: false),
                Status = table.Column<string>(maxLength: 16, nullable: false),
                Provider = table.Column<string>(maxLength: 32, nullable: true),
                ProviderReference = table.Column<string>(maxLength: 128, nullable: true),
                ProviderStatus = table.Column<string>(maxLength: 32, nullable: true),
                FailureReason = table.Column<string>(maxLength: 256, nullable: true),
                IdempotencyKey = table.Column<string>(maxLength: 64, nullable: false),
                RequestedByUserID = table.Column<int>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                CompletedAt = table.Column<DateTime>(nullable: true),
                CreditNoteID = table.Column<int>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Refunds", r => r.ID));
        migrationBuilder.CreateIndex(name: "IX_Refunds_IdempotencyKey", table: "Refunds", column: "IdempotencyKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Refunds_OrderID", table: "Refunds", column: "OrderID");
        migrationBuilder.CreateIndex(name: "IX_Refunds_ChargingSessionID", table: "Refunds", column: "ChargingSessionID");
        migrationBuilder.CreateIndex(name: "IX_Refunds_CreatedAt", table: "Refunds", column: "CreatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Refunds");
    }
}
