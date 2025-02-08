using Microsoft.EntityFrameworkCore.Migrations;

namespace Nellebot.Data.Migrations.Migrations
{
    public partial class AddAwardChannelId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AwardChannelId",
                table: "AwardMessages",
                type: "numeric(20,0)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwardChannelId",
                table: "AwardMessages");
        }
    }
}
