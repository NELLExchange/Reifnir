using Microsoft.EntityFrameworkCore.Migrations;

namespace Nellebot.Data.Migrations.Migrations
{
    public partial class AddOriginalChannelIdToAwardMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OriginalChannelId",
                table: "AwardMessages",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalChannelId",
                table: "AwardMessages");
        }
    }
}
