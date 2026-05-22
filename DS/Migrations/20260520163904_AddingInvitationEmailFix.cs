using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DS.Migrations
{
    /// <inheritdoc />
    public partial class AddingInvitationEmailFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Used",
                table: "Invitations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Used",
                table: "Invitations");
        }
    }
}
