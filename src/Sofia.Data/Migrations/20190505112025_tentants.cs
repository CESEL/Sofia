using Microsoft.EntityFrameworkCore.Migrations;

namespace Sofia.Data.Migrations
{
    public partial class tentants : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SubscriptionId",
                table: "FileHistories",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionId",
                table: "Contributions",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_FileHistories_SubscriptionId",
                table: "FileHistories",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_SubscriptionId",
                table: "Contributions",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contributions_Subscriptions_SubscriptionId",
                table: "Contributions",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FileHistories_Subscriptions_SubscriptionId",
                table: "FileHistories",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contributions_Subscriptions_SubscriptionId",
                table: "Contributions");

            migrationBuilder.DropForeignKey(
                name: "FK_FileHistories_Subscriptions_SubscriptionId",
                table: "FileHistories");

            migrationBuilder.DropIndex(
                name: "IX_FileHistories_SubscriptionId",
                table: "FileHistories");

            migrationBuilder.DropIndex(
                name: "IX_Contributions_SubscriptionId",
                table: "Contributions");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "FileHistories");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Contributions");
        }
    }
}
