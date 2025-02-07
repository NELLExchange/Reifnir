using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nellebot.Data.Migrations.Migrations
{
    public partial class AddUserRoleGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE public."UserRoles" set "GroupNumber" = null; """);

            migrationBuilder.RenameColumn(
                name: "GroupNumber",
                table: "UserRoles",
                newName: "GroupId");

            migrationBuilder.CreateTable(
                name: "UserRoleGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_GroupId",
                table: "UserRoles",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_UserRoleGroups_GroupId",
                table: "UserRoles",
                column: "GroupId",
                principalTable: "UserRoleGroups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_UserRoleGroups_GroupId",
                table: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserRoleGroups");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_GroupId",
                table: "UserRoles");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "UserRoles",
                newName: "GroupNumber");
        }
    }
}
