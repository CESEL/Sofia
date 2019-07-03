using Microsoft.EntityFrameworkCore.Migrations;

namespace Sofia.Data.Migrations
{
    public partial class ContributorGithubLogin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionEvent_Subscriptions_SubscriptionId",
                table: "SubscriptionEvent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionEvent",
                table: "SubscriptionEvent");

            migrationBuilder.RenameTable(
                name: "SubscriptionEvent",
                newName: "SubscriptionEvents");

            migrationBuilder.RenameColumn(
                name: "ContributorGithubId",
                table: "Contributions",
                newName: "ContributorGithubLogin");

            migrationBuilder.RenameIndex(
                name: "IX_SubscriptionEvent_SubscriptionId",
                table: "SubscriptionEvents",
                newName: "IX_SubscriptionEvents_SubscriptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionEvents",
                table: "SubscriptionEvents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionEvents_Subscriptions_SubscriptionId",
                table: "SubscriptionEvents",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionEvents_Subscriptions_SubscriptionId",
                table: "SubscriptionEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionEvents",
                table: "SubscriptionEvents");

            migrationBuilder.RenameTable(
                name: "SubscriptionEvents",
                newName: "SubscriptionEvent");

            migrationBuilder.RenameColumn(
                name: "ContributorGithubLogin",
                table: "Contributions",
                newName: "ContributorGithubId");

            migrationBuilder.RenameIndex(
                name: "IX_SubscriptionEvents_SubscriptionId",
                table: "SubscriptionEvent",
                newName: "IX_SubscriptionEvent_SubscriptionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionEvent",
                table: "SubscriptionEvent",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionEvent_Subscriptions_SubscriptionId",
                table: "SubscriptionEvent",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
