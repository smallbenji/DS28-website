using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DS.Migrations
{
    /// <inheritdoc />
    public partial class activity_teams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivityTeamId",
                table: "Activities",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ActivityTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTeams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTeamMemberships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserID = table.Column<string>(type: "text", nullable: true),
                    ActivityTeamId = table.Column<int>(type: "integer", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTeamMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityTeamMemberships_ActivityTeams_ActivityTeamId",
                        column: x => x.ActivityTeamId,
                        principalTable: "ActivityTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActivityTeamId",
                table: "Activities",
                column: "ActivityTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTeamMemberships_ActivityTeamId",
                table: "ActivityTeamMemberships",
                column: "ActivityTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTeamMemberships_UserID_ActivityTeamId",
                table: "ActivityTeamMemberships",
                columns: new[] { "UserID", "ActivityTeamId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_ActivityTeams_ActivityTeamId",
                table: "Activities",
                column: "ActivityTeamId",
                principalTable: "ActivityTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_ActivityTeams_ActivityTeamId",
                table: "Activities");

            migrationBuilder.DropTable(
                name: "ActivityTeamMemberships");

            migrationBuilder.DropTable(
                name: "ActivityTeams");

            migrationBuilder.DropIndex(
                name: "IX_Activities_ActivityTeamId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ActivityTeamId",
                table: "Activities");
        }
    }
}
