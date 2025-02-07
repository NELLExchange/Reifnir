using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nellebot.Data.Migrations.Migrations
{
    public partial class AddMutuallyExclusiveFlagToUserRoleGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MutuallyExclusive",
                table: "UserRoleGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MutuallyExclusive",
                table: "UserRoleGroups");
        }
    }
}
