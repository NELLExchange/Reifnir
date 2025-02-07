using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nellebot.Data.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdbokStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdbokArticlesStore",
                columns: table => new
                {
                    Dictionary = table.Column<string>(type: "text", nullable: false),
                    WordClass = table.Column<string>(type: "text", nullable: false),
                    ArticleCount = table.Column<int>(type: "integer", nullable: false),
                    ArticleList = table.Column<IEnumerable<int>>(type: "jsonb", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdbokArticlesStore", x => new { x.Dictionary, x.WordClass });
                });

            migrationBuilder.CreateTable(
                name: "OrdbokConceptStore",
                columns: table => new
                {
                    Dictionary = table.Column<string>(type: "text", nullable: false),
                    Concepts = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdbokConceptStore", x => x.Dictionary);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdbokArticlesStore");

            migrationBuilder.DropTable(
                name: "OrdbokConceptStore");
        }
    }
}
