using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceCaptureService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "recording");

            migrationBuilder.RenameTable(
                name: "RecordingSessions",
                newName: "RecordingSessions",
                newSchema: "recording");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "RecordingSessions",
                schema: "recording",
                newName: "RecordingSessions");
        }
    }
}
