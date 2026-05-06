using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlaMonitor.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRelationAndFixAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Name",
                table: "Tenants");
        }
    }
}
