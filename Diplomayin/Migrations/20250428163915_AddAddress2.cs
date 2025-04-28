using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diplomayin.Migrations
{
    /// <inheritdoc />
    public partial class AddAddress2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IPAddress",
                table: "Devices",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IPAddress",
                table: "Devices");
        }
    }
}
