using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManagerId",
                table: "LeaveRequests",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_ManagerId",
                table: "LeaveRequests",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ManagerId",
                table: "LeaveRequests",
                column: "ManagerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_AspNetUsers_ManagerId",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_ManagerId",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "LeaveRequests");
        }
    }
}
