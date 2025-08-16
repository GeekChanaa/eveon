using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoltaXApi.Migrations
{
    /// <inheritdoc />
    public partial class removed_chargepointid_conenctor_status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectorStatuses_ChargePoints_ChargePointID1",
                table: "ConnectorStatuses");

            migrationBuilder.DropForeignKey(
                name: "FK_ConnectorStatuses_Connectors_ConnectorID",
                table: "ConnectorStatuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConnectorStatuses",
                table: "ConnectorStatuses");

            migrationBuilder.DropIndex(
                name: "IX_ConnectorStatuses_ChargePointID1",
                table: "ConnectorStatuses");

            migrationBuilder.DropColumn(
                name: "ChargePointID1",
                table: "ConnectorStatuses");

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "ConnectorStatuses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AlterColumn<int>(
                name: "ChargePointID",
                table: "ConnectorStatuses",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<int>(
                name: "ConnectorID",
                table: "ConnectorStatuses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConnectorStatuses",
                table: "ConnectorStatuses",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorStatuses_ChargePointID",
                table: "ConnectorStatuses",
                column: "ChargePointID");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorStatuses_ConnectorID",
                table: "ConnectorStatuses",
                column: "ConnectorID");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectorStatuses_ChargePoints_ChargePointID",
                table: "ConnectorStatuses",
                column: "ChargePointID",
                principalTable: "ChargePoints",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectorStatuses_Connectors_ConnectorID",
                table: "ConnectorStatuses",
                column: "ConnectorID",
                principalTable: "Connectors",
                principalColumn: "ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectorStatuses_ChargePoints_ChargePointID",
                table: "ConnectorStatuses");

            migrationBuilder.DropForeignKey(
                name: "FK_ConnectorStatuses_Connectors_ConnectorID",
                table: "ConnectorStatuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConnectorStatuses",
                table: "ConnectorStatuses");

            migrationBuilder.DropIndex(
                name: "IX_ConnectorStatuses_ChargePointID",
                table: "ConnectorStatuses");

            migrationBuilder.DropIndex(
                name: "IX_ConnectorStatuses_ConnectorID",
                table: "ConnectorStatuses");

            migrationBuilder.AlterColumn<int>(
                name: "ConnectorID",
                table: "ConnectorStatuses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChargePointID",
                table: "ConnectorStatuses",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                table: "ConnectorStatuses",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "ChargePointID1",
                table: "ConnectorStatuses",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConnectorStatuses",
                table: "ConnectorStatuses",
                columns: new[] { "ConnectorID", "ChargePointID" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorStatuses_ChargePointID1",
                table: "ConnectorStatuses",
                column: "ChargePointID1");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectorStatuses_ChargePoints_ChargePointID1",
                table: "ConnectorStatuses",
                column: "ChargePointID1",
                principalTable: "ChargePoints",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectorStatuses_Connectors_ConnectorID",
                table: "ConnectorStatuses",
                column: "ConnectorID",
                principalTable: "Connectors",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
