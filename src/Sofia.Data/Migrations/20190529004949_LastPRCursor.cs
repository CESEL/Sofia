using Microsoft.EntityFrameworkCore.Migrations;

namespace Sophia.Data.Migrations
{
    public partial class LastPRCursor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastPullRequestGithubCursor",
                table: "Subscriptions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPullRequestGithubCursor",
                table: "Subscriptions");
        }
    }
}
