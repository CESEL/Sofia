using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sophia.Data.Migrations
{
    public partial class EnumToString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FileHistoryType",
                table: "FileHistories",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AlterColumn<string>(
                name: "ContributionType",
                table: "Contributions",
                nullable: false,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateTime",
                table: "Contributions",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateTime",
                table: "Contributions");

            migrationBuilder.AlterColumn<int>(
                name: "FileHistoryType",
                table: "FileHistories",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<int>(
                name: "ContributionType",
                table: "Contributions",
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}
