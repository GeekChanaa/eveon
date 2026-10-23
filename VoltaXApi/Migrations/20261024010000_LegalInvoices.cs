using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Legal invoices: Invoices (one row per issued invoice or credit note, unique number and unique source
/// reference), InvoiceFiles (the PDF rendered at issue time) and InvoiceSequences (last number per series and
/// year; the increment commits with the invoice, so numbers have no gaps). Written by hand: column types are left
/// to the provider (SQL Server or MySQL / MariaDB).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261024010000_LegalInvoices")]
public partial class LegalInvoices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InvoiceSequences",
            columns: table => new
            {
                Series = table.Column<string>(maxLength: 8, nullable: false),
                Year = table.Column<int>(nullable: false),
                LastNumber = table.Column<int>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_InvoiceSequences", s => new { s.Series, s.Year }));

        migrationBuilder.CreateTable(
            name: "Invoices",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Kind = table.Column<string>(maxLength: 16, nullable: false),
                Number = table.Column<string>(maxLength: 32, nullable: false),
                Series = table.Column<string>(maxLength: 8, nullable: false),
                Year = table.Column<int>(nullable: false),
                Sequence = table.Column<int>(nullable: false),
                SourceReference = table.Column<string>(maxLength: 64, nullable: false),
                IssuedAt = table.Column<DateTime>(nullable: false),
                ChargingSessionID = table.Column<int>(nullable: true),
                OrderID = table.Column<int>(nullable: true),
                RefundID = table.Column<int>(nullable: true),
                CreditedInvoiceID = table.Column<int>(nullable: true),
                UserID = table.Column<int>(nullable: true),
                CardID = table.Column<int>(nullable: true),
                CustomerName = table.Column<string>(maxLength: 200, nullable: false),
                CustomerEmail = table.Column<string>(maxLength: 256, nullable: true),
                Language = table.Column<string>(maxLength: 8, nullable: false),
                Currency = table.Column<string>(maxLength: 3, nullable: false),
                VatRate = table.Column<decimal>(precision: 5, scale: 4, nullable: false),
                AmountExclVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                VatAmount = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                AmountInclVat = table.Column<decimal>(precision: 18, scale: 2, nullable: false),
                Description = table.Column<string>(maxLength: 256, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Invoices", i => i.ID));
        migrationBuilder.CreateIndex(name: "IX_Invoices_Number", table: "Invoices", column: "Number", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Invoices_SourceReference", table: "Invoices", column: "SourceReference", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Invoices_ChargingSessionID", table: "Invoices", column: "ChargingSessionID");
        migrationBuilder.CreateIndex(name: "IX_Invoices_OrderID", table: "Invoices", column: "OrderID");
        migrationBuilder.CreateIndex(name: "IX_Invoices_UserID_IssuedAt", table: "Invoices", columns: new[] { "UserID", "IssuedAt" });
        migrationBuilder.CreateIndex(name: "IX_Invoices_IssuedAt", table: "Invoices", column: "IssuedAt");

        migrationBuilder.CreateTable(
            name: "InvoiceFiles",
            columns: table => new
            {
                InvoiceID = table.Column<int>(nullable: false),
                Content = table.Column<byte[]>(nullable: false),
                ContentType = table.Column<string>(maxLength: 64, nullable: false),
                FileName = table.Column<string>(maxLength: 128, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InvoiceFiles", f => f.InvoiceID);
                table.ForeignKey(
                    name: "FK_InvoiceFiles_Invoices_InvoiceID",
                    column: f => f.InvoiceID,
                    principalTable: "Invoices",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InvoiceFiles");
        migrationBuilder.DropTable(name: "Invoices");
        migrationBuilder.DropTable(name: "InvoiceSequences");
    }
}
