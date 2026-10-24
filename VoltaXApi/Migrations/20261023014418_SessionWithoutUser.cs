using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// ChargingSessions.UserID becomes nullable: a session started with a card that belongs to nobody (fleet or
/// unassigned card) has no user, instead of being attributed to user 1. The foreign key no longer cascades:
/// deleting a user must not delete charging history (users are soft deleted anyway). The generic operations
/// work on SQL Server and MySQL / MariaDB (the UserID index survives a nullability change on both).
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20261023014418_SessionWithoutUser")]
public partial class SessionWithoutUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_ChargingSessions_Users_UserID", table: "ChargingSessions");
        migrationBuilder.AlterColumn<int>(name: "UserID", table: "ChargingSessions", nullable: true,
            oldClrType: typeof(int));
        migrationBuilder.AddForeignKey(name: "FK_ChargingSessions_Users_UserID", table: "ChargingSessions", column: "UserID",
            principalTable: "Users", principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_ChargingSessions_Users_UserID", table: "ChargingSessions");
        migrationBuilder.AlterColumn<int>(name: "UserID", table: "ChargingSessions", nullable: false, defaultValue: 0,
            oldClrType: typeof(int), oldNullable: true);
        migrationBuilder.AddForeignKey(name: "FK_ChargingSessions_Users_UserID", table: "ChargingSessions", column: "UserID",
            principalTable: "Users", principalColumn: "ID", onDelete: ReferentialAction.Cascade);
    }
}
