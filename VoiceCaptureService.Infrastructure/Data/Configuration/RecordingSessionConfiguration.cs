using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoiceCaptureService.Domain.Recording.Entities;
using VoiceCaptureService.Domain.Recording.ValueObjects;

namespace VoiceCaptureService.Infrastructure.Data.Configuration;

public sealed class RecordingSessionConfiguration
    : IEntityTypeConfiguration<RecordingSession>
{
    public void Configure(EntityTypeBuilder<RecordingSession> builder)
    {
        builder.ToTable("RecordingSessions", "recording");

        builder.HasKey(x => x.RecordingId);

        builder.Property(x => x.RecordingId)
            .HasConversion(
                id => id.Value,
                value => RecordingId.Of(value));

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.StartedAt);

        builder.Property(x => x.StoppedAt);

        builder.Property(x => x.StoragePath)
            .HasMaxLength(500);

        builder.OwnsOne(x => x.RecordingMetadata, metadata =>
        {
            metadata.Property(m => m.SampleRate).HasColumnName("SampleRate");
            metadata.Property(m => m.ChannelCount).HasColumnName("ChannelCount");
            metadata.Property(m => m.BitsPerSample).HasColumnName("BitsPerSample");
            metadata.Property(m => m.MimeType)
                .HasMaxLength(100)
                .HasColumnName("MimeType");
        });

        // Inherited properties
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy);

        builder.Property(x => x.LastModifiedAt);

        builder.Property(x => x.LastModifiedBy);

        builder.Property(x => x.RecordStatus);
    }
}
