using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sofia.Data.Migrations
{
    public partial class remove_lastrefresh : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRefreshDateTime",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Subscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "Branch",
                table: "Subscriptions",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanningStatus",
                table: "Subscriptions",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Subscriptions_RepositoryId_Branch",
                table: "Subscriptions",
                columns: new[] { "RepositoryId", "Branch" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Subscriptions_RepositoryId_Branch",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "ScanningStatus",
                table: "Subscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "Branch",
                table: "Subscriptions",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRefreshDateTime",
                table: "Subscriptions",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "Subscriptions",
                nullable: false,
                defaultValue: "");
        }
    }
}
