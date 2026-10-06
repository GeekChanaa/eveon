using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Charger PKI and OCPP security profiles 2/3. Hand written for the same reason as SecurityHardening
/// (the snapshot is MySQL-shaped while the application runs on SQL Server).
///
///  - ChargePoints.SecurityProfile (default 1 = Basic auth, current behaviour). Chargers that only had a
///    pinned client certificate thumbprint (no password) get profile 3, which is how they authenticated before.
///  - ChargerCertificates: client certificates issued by the charger CA from SignCertificate CSRs.
///  - InstalledCertificateRecords: last GetInstalledCertificateIds report per charger.
///  - PkiCertificateAuthorities: the auto-created development CA (private key encrypted with Data Protection).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261004140000_ChargerPki")]
public partial class ChargerPki : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "SecurityProfile", table: "ChargePoints", nullable: false, defaultValue: 1);

        if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            migrationBuilder.Sql(@"
UPDATE ChargePoints SET SecurityProfile = 3
WHERE (Password IS NULL OR Password = '') AND ClientCertThumb IS NOT NULL AND LTRIM(RTRIM(ClientCertThumb)) <> '';");
        }

        migrationBuilder.CreateTable(
            name: "ChargerCertificates",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<int>(nullable: false),
                CertificateType = table.Column<string>(maxLength: 32, nullable: false),
                SerialNumber = table.Column<string>(maxLength: 64, nullable: false),
                Subject = table.Column<string>(maxLength: 512, nullable: false),
                ThumbprintSha256 = table.Column<string>(maxLength: 64, nullable: false),
                NotBefore = table.Column<DateTime>(nullable: false),
                NotAfter = table.Column<DateTime>(nullable: false),
                CertificatePem = table.Column<string>(nullable: false),
                Status = table.Column<string>(maxLength: 16, nullable: false),
                StatusReason = table.Column<string>(maxLength: 512, nullable: true),
                IssuedAt = table.Column<DateTime>(nullable: false),
                StatusChangedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChargerCertificates", c => c.ID);
                table.ForeignKey(
                    name: "FK_ChargerCertificates_ChargePoints_ChargePointID",
                    column: c => c.ChargePointID,
                    principalTable: "ChargePoints",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(name: "IX_ChargerCertificates_ChargePointID_CertificateType_Status", table: "ChargerCertificates",
            columns: new[] { "ChargePointID", "CertificateType", "Status" });
        migrationBuilder.CreateIndex(name: "IX_ChargerCertificates_ThumbprintSha256", table: "ChargerCertificates", column: "ThumbprintSha256");
        migrationBuilder.CreateIndex(name: "IX_ChargerCertificates_Status_NotAfter", table: "ChargerCertificates", columns: new[] { "Status", "NotAfter" });

        migrationBuilder.CreateTable(
            name: "InstalledCertificateRecords",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChargePointID = table.Column<int>(nullable: false),
                CertificateType = table.Column<string>(maxLength: 32, nullable: false),
                HashAlgorithm = table.Column<string>(maxLength: 8, nullable: false),
                IssuerNameHash = table.Column<string>(maxLength: 128, nullable: false),
                IssuerKeyHash = table.Column<string>(maxLength: 128, nullable: false),
                SerialNumber = table.Column<string>(maxLength: 40, nullable: false),
                ChildCertificatesJson = table.Column<string>(nullable: true),
                ReportedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InstalledCertificateRecords", r => r.ID);
                table.ForeignKey(
                    name: "FK_InstalledCertificateRecords_ChargePoints_ChargePointID",
                    column: r => r.ChargePointID,
                    principalTable: "ChargePoints",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_InstalledCertificateRecords_ChargePointID", table: "InstalledCertificateRecords", column: "ChargePointID");

        migrationBuilder.CreateTable(
            name: "PkiCertificateAuthorities",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Name = table.Column<string>(maxLength: 64, nullable: false),
                CertificatePem = table.Column<string>(nullable: false),
                PrivateKeyProtected = table.Column<string>(nullable: false),
                NotAfter = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PkiCertificateAuthorities", a => a.ID));

        migrationBuilder.CreateIndex(name: "IX_PkiCertificateAuthorities_Name", table: "PkiCertificateAuthorities", column: "Name", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PkiCertificateAuthorities");
        migrationBuilder.DropTable(name: "InstalledCertificateRecords");
        migrationBuilder.DropTable(name: "ChargerCertificates");
        migrationBuilder.DropColumn(name: "SecurityProfile", table: "ChargePoints");
    }
}
