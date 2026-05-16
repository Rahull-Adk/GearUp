using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GearUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImageProcessingStatusToCarImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "CarImages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalFilePath",
                table: "CarImages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CarImages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "CarImages");

            migrationBuilder.DropColumn(
                name: "LocalFilePath",
                table: "CarImages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CarImages");
        }
    }
}
