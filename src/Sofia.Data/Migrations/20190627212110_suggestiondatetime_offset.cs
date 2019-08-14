using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sophia.Data.Migrations
{
    public partial class suggestiondatetime_offset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "SuggestionDateTime",
                table: "Candidate",
                nullable: false,
                oldClrType: typeof(DateTime));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SuggestionDateTime",
                table: "Candidate",
                nullable: false,
                oldClrType: typeof(DateTimeOffset));
        }
    }
}
