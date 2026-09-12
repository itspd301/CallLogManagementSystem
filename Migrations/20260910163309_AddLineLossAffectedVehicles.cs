using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallLogManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddLineLossAffectedVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineLossAffectedVehicles",
                table: "CallLogs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineLossAffectedVehicles",
                table: "CallLogs");
        }
    }
}
