using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkOps.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paddocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paddocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Breaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Cause = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Breaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Breaches_Fences_FenceId",
                        column: x => x.FenceId,
                        principalTable: "Fences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Breaches_FenceId",
                table: "Breaches",
                column: "FenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Breaches");

            migrationBuilder.DropTable(
                name: "Paddocks");

            migrationBuilder.DropTable(
                name: "Fences");
        }
    }
}
