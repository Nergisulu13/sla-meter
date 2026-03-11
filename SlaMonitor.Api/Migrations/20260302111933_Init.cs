using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlaMonitor.Api.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Downtimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Customers = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Downtimes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Downtimes");
        }
    }
}
