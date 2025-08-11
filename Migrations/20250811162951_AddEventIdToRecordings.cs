using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoraUFF_API.Migrations
{
    /// <inheritdoc />
    public partial class AddEventIdToRecordings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventId",
                table: "Recordings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Recordings");
        }
    }
}
