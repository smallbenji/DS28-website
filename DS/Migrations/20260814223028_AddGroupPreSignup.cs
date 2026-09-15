using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DS.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupPreSignup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupPreSignups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    Beaver = table.Column<int>(type: "integer", nullable: false),
                    Wolf = table.Column<int>(type: "integer", nullable: false),
                    Junior = table.Column<int>(type: "integer", nullable: false),
                    Trop = table.Column<int>(type: "integer", nullable: false),
                    Senior = table.Column<int>(type: "integer", nullable: false),
                    Rover = table.Column<int>(type: "integer", nullable: false),
                    Leader = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupPreSignups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupPreSignups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupPreSignups_GroupId",
                table: "GroupPreSignups",
                column: "GroupId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupPreSignups");
        }
    }
}
