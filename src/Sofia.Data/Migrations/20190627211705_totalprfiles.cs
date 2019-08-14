using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sophia.Data.Migrations
{
    public partial class totalprfiles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PullRequests_Subscriptions_SubscriptionId",
                table: "PullRequests");

            migrationBuilder.AlterColumn<long>(
                name: "SubscriptionId",
                table: "PullRequests",
                nullable: false,
                oldClrType: typeof(long),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfFiles",
                table: "PullRequests",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "CanonicalName",
                table: "Contributors",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                table: "Contributions",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Candidate",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    SubscriptionId = table.Column<long>(nullable: false),
                    PullRequestNumber = table.Column<int>(nullable: false),
                    Rank = table.Column<int>(nullable: false),
                    GitHubLogin = table.Column<string>(nullable: true),
                    RecommenderType = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidate_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_Number",
                table: "PullRequests",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_Contributors_CanonicalName",
                table: "Contributors",
                column: "CanonicalName");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_ActivityId",
                table: "Contributions",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidate_PullRequestNumber",
                table: "Candidate",
                column: "PullRequestNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Candidate_SubscriptionId",
                table: "Candidate",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PullRequests_Subscriptions_SubscriptionId",
                table: "PullRequests",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PullRequests_Subscriptions_SubscriptionId",
                table: "PullRequests");

            migrationBuilder.DropTable(
                name: "Candidate");

            migrationBuilder.DropIndex(
                name: "IX_PullRequests_Number",
                table: "PullRequests");

            migrationBuilder.DropIndex(
                name: "IX_Contributors_CanonicalName",
                table: "Contributors");

            migrationBuilder.DropIndex(
                name: "IX_Contributions_ActivityId",
                table: "Contributions");

            migrationBuilder.DropColumn(
                name: "NumberOfFiles",
                table: "PullRequests");

            migrationBuilder.AlterColumn<long>(
                name: "SubscriptionId",
                table: "PullRequests",
                nullable: true,
                oldClrType: typeof(long));

            migrationBuilder.AlterColumn<string>(
                name: "CanonicalName",
                table: "Contributors",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                table: "Contributions",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PullRequests_Subscriptions_SubscriptionId",
                table: "PullRequests",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
