using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlaMonitor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantToDowntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Downtimes",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Downtimes");
        }
    }
}
