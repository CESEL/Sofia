using Microsoft.EntityFrameworkCore.Migrations;

namespace Sophia.Data.Migrations
{
    public partial class PullRequestSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubscriptionId",
                table: "PullRequests",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_SubscriptionId",
                table: "PullRequests",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PullRequests_Subscriptions_SubscriptionId",
                table: "PullRequests",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PullRequests_Subscriptions_SubscriptionId",
                table: "PullRequests");

            migrationBuilder.DropIndex(
                name: "IX_PullRequests_SubscriptionId",
                table: "PullRequests");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "PullRequests");
        }
    }
}
