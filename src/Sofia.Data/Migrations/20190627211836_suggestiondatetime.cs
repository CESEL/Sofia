using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sophia.Data.Migrations
{
    public partial class suggestiondatetime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SuggestionDateTime",
                table: "Candidate",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuggestionDateTime",
                table: "Candidate");
        }
    }
}
