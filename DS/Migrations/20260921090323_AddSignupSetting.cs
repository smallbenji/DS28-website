using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DS.Migrations
{
    /// <inheritdoc />
    public partial class AddSignupSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSignupOpen",
                table: "RegistrationSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "RegistrationSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsSignupOpen",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSignupOpen",
                table: "RegistrationSettings");
        }
    }
}
