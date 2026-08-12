using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DS.Migrations
{
    /// <inheritdoc />
    public partial class addHasEnabledAuthenticatorToUSer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasEnabledAuthenticator",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasEnabledAuthenticator",
                table: "AspNetUsers");
        }
    }
}
