using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using VoltaXApi.Data;

#nullable disable

namespace VoltaXApi.Migrations;

/// <summary>
/// Adds an immutable, provider-neutral audit trail for charging card updates.
/// This migration is hand written because the existing snapshot is MySQL-shaped while
/// the application currently runs on SQL Server; scaffolding would otherwise rewrite
/// unrelated tables as part of a provider switch.
/// </summary>
[DbContext(typeof(VoltaXApiDbContext))]
[Migration("20260920003000_AddCardChangeHistory")]
public partial class AddCardChangeHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CardChangeHistories",
            columns: table => new
            {
                ID = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1")
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                ChangeSetID = table.Column<Guid>(nullable: false),
                CardID = table.Column<int>(nullable: false),
                ChangedByUserID = table.Column<int>(nullable: true),
                ChangedByName = table.Column<string>(maxLength: 200, nullable: false),
                Source = table.Column<string>(maxLength: 50, nullable: false),
                PropertyName = table.Column<string>(maxLength: 100, nullable: false),
                OldValue = table.Column<string>(nullable: true),
                NewValue = table.Column<string>(nullable: true),
                ChangedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CardChangeHistories", history => history.ID);
                table.ForeignKey(
                    name: "FK_CardChangeHistories_Cards_CardID",
                    column: history => history.CardID,
                    principalTable: "Cards",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CardChangeHistories_Users_ChangedByUserID",
                    column: history => history.ChangedByUserID,
                    principalTable: "Users",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CardChangeHistories_CardID_ChangedAtUtc",
            table: "CardChangeHistories",
            columns: new[] { "CardID", "ChangedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CardChangeHistories_ChangedByUserID",
            table: "CardChangeHistories",
            column: "ChangedByUserID");

        migrationBuilder.CreateIndex(
            name: "IX_CardChangeHistories_ChangeSetID",
            table: "CardChangeHistories",
            column: "ChangeSetID");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CardChangeHistories");
    }
}
