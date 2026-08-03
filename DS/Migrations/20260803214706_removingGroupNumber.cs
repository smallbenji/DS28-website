using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DS.Migrations
{
    /// <inheritdoc />
    public partial class removingGroupNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupNumber",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupNumber",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }
    }
}
