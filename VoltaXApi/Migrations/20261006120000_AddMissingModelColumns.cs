using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Columns that are in the model but that no migration created:
///  - Users.ResetPasswordCode / ResetPasswordCodeExpiresAt (013a880): SecurityHardening adds them on
///    SQL Server only, so a MySQL / MariaDB database built from the migrations lacks them.
///  - ChargePoints.QrValue: never added by a migration on either provider.
/// Written by hand (the snapshot targets SQL Server) and idempotent, since some databases got these
/// columns by hand.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261006120000_AddMissingModelColumns")]
public partial class AddMissingModelColumns : Migration
{
    private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider == SqlServerProvider)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'ResetPasswordCode') IS NULL ALTER TABLE Users ADD ResetPasswordCode nvarchar(max) NULL;
IF COL_LENGTH('Users', 'ResetPasswordCodeExpiresAt') IS NULL ALTER TABLE Users ADD ResetPasswordCodeExpiresAt datetime2 NULL;
IF COL_LENGTH('ChargePoints', 'QrValue') IS NULL ALTER TABLE ChargePoints ADD QrValue nvarchar(max) NULL;");
        }
        else
        {
            // ADD COLUMN IF NOT EXISTS is MariaDB syntax (Hostinger runs MariaDB).
            migrationBuilder.Sql(@"
ALTER TABLE `Users`
  ADD COLUMN IF NOT EXISTS `ResetPasswordCode` longtext CHARACTER SET utf8mb4 NULL,
  ADD COLUMN IF NOT EXISTS `ResetPasswordCodeExpiresAt` datetime(6) NULL;
ALTER TABLE `ChargePoints`
  ADD COLUMN IF NOT EXISTS `QrValue` longtext CHARACTER SET utf8mb4 NULL;");
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The columns stay: some databases had them before this migration and the model uses them.
    }
}
