using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VoiceCaptureService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangedRecordingMetadataColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecordingMetadata_SampleRate",
                table: "RecordingSessions",
                newName: "SampleRate");

            migrationBuilder.RenameColumn(
                name: "RecordingMetadata_MimeType",
                table: "RecordingSessions",
                newName: "MimeType");

            migrationBuilder.RenameColumn(
                name: "RecordingMetadata_ChannelCount",
                table: "RecordingSessions",
                newName: "ChannelCount");

            migrationBuilder.RenameColumn(
                name: "RecordingMetadata_BitsPerSample",
                table: "RecordingSessions",
                newName: "BitsPerSample");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SampleRate",
                table: "RecordingSessions",
                newName: "RecordingMetadata_SampleRate");

            migrationBuilder.RenameColumn(
                name: "MimeType",
                table: "RecordingSessions",
                newName: "RecordingMetadata_MimeType");

            migrationBuilder.RenameColumn(
                name: "ChannelCount",
                table: "RecordingSessions",
                newName: "RecordingMetadata_ChannelCount");

            migrationBuilder.RenameColumn(
                name: "BitsPerSample",
                table: "RecordingSessions",
                newName: "RecordingMetadata_BitsPerSample");
        }
    }
}
