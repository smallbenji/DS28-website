using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DS.Migrations
{
    /// <inheritdoc />
    public partial class userIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityTeamMemberships_UserID_ActivityTeamId",
                table: "ActivityTeamMemberships");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "ActivityTeamMemberships",
                newName: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTeamMemberships_UserId",
                table: "ActivityTeamMemberships",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityTeamMemberships_AspNetUsers_UserId",
                table: "ActivityTeamMemberships",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityTeamMemberships_AspNetUsers_UserId",
                table: "ActivityTeamMemberships");

            migrationBuilder.DropIndex(
                name: "IX_ActivityTeamMemberships_UserId",
                table: "ActivityTeamMemberships");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ActivityTeamMemberships",
                newName: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTeamMemberships_UserID_ActivityTeamId",
                table: "ActivityTeamMemberships",
                columns: new[] { "UserID", "ActivityTeamId" },
                unique: true);
        }
    }
}
