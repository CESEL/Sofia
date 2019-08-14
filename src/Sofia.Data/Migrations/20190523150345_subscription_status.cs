using Microsoft.EntityFrameworkCore.Migrations;

namespace Sophia.Data.Migrations
{
    public partial class subscription_status : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OauthToken",
                table: "Subscriptions");

            migrationBuilder.AddColumn<long>(
                name: "InstallationId",
                table: "Subscriptions",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "IssueNumber",
                table: "Subscriptions",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "RepositoryId",
                table: "Subscriptions",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "Subscriptions",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallationId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "IssueNumber",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "RepositoryId",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Subscriptions");

            migrationBuilder.AddColumn<string>(
                name: "OauthToken",
                table: "Subscriptions",
                nullable: true);
        }
    }
}
