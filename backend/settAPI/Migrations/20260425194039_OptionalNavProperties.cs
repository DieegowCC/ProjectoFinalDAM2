using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace settAPI.Migrations
{
    /// <inheritdoc />
    public partial class OptionalNavProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activity_periods_work_session_WorkSessionid",
                table: "activity_periods");

            migrationBuilder.DropForeignKey(
                name: "FK_app_activity_work_session_WorkSessionid",
                table: "app_activity");

            migrationBuilder.DropForeignKey(
                name: "FK_work_session_workers_Workerid",
                table: "work_session");

            migrationBuilder.AlterColumn<int>(
                name: "Workerid",
                table: "work_session",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "WorkSessionid",
                table: "app_activity",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "WorkSessionid",
                table: "activity_periods",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_activity_periods_work_session_WorkSessionid",
                table: "activity_periods",
                column: "WorkSessionid",
                principalTable: "work_session",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_app_activity_work_session_WorkSessionid",
                table: "app_activity",
                column: "WorkSessionid",
                principalTable: "work_session",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_work_session_workers_Workerid",
                table: "work_session",
                column: "Workerid",
                principalTable: "workers",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_activity_periods_work_session_WorkSessionid",
                table: "activity_periods");

            migrationBuilder.DropForeignKey(
                name: "FK_app_activity_work_session_WorkSessionid",
                table: "app_activity");

            migrationBuilder.DropForeignKey(
                name: "FK_work_session_workers_Workerid",
                table: "work_session");

            migrationBuilder.AlterColumn<int>(
                name: "Workerid",
                table: "work_session",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkSessionid",
                table: "app_activity",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkSessionid",
                table: "activity_periods",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_activity_periods_work_session_WorkSessionid",
                table: "activity_periods",
                column: "WorkSessionid",
                principalTable: "work_session",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_app_activity_work_session_WorkSessionid",
                table: "app_activity",
                column: "WorkSessionid",
                principalTable: "work_session",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_work_session_workers_Workerid",
                table: "work_session",
                column: "Workerid",
                principalTable: "workers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
