using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sofia.Data.Migrations
{
    public partial class pullrequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OldPath",
                table: "FileHistories",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "FileHistories",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PullRequests",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Number = table.Column<long>(nullable: false),
                    PullRequestAnalyzeStatus = table.Column<int>(nullable: false),
                    PullRequestInfo = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequests", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PullRequests");

            migrationBuilder.DropColumn(
                name: "OldPath",
                table: "FileHistories");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "FileHistories");
        }
    }
}
