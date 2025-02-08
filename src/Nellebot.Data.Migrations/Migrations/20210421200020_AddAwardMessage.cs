using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Nellebot.Data.Migrations.Migrations
{
    public partial class AddAwardMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AwardMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AwardedMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    AwardCount = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AwardMessages_AwardedMessageId",
                table: "AwardMessages",
                column: "AwardedMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AwardMessages_OriginalMessageId",
                table: "AwardMessages",
                column: "OriginalMessageId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AwardMessages");
        }
    }
}
