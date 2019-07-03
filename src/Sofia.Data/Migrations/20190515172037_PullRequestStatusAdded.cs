using Microsoft.EntityFrameworkCore.Migrations;

namespace Sofia.Data.Migrations
{
    public partial class PullRequestStatusAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PullRequestAnalyzeStatus",
                table: "PullRequests",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<string>(
                name: "PullRequestStatus",
                table: "PullRequests",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PullRequestStatus",
                table: "PullRequests");

            migrationBuilder.AlterColumn<int>(
                name: "PullRequestAnalyzeStatus",
                table: "PullRequests",
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}
