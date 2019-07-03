using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sofia.Data.Migrations
{
    public partial class lastrefresh : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionEventType",
                table: "SubscriptionEvent");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRefreshDateTime",
                table: "Subscriptions",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionStatus",
                table: "SubscriptionEvent",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRefreshDateTime",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "SubscriptionEvent");

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionEventType",
                table: "SubscriptionEvent",
                nullable: false,
                defaultValue: 0);
        }
    }
}
