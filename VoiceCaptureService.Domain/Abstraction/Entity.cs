namespace VoiceCaptureService.Domain.Abstraction;

public abstract class Entity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Set default to current UTC time
    public Guid? CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public Guid? LastModifiedBy { get; set; }
    public int? RecordStatus { get; set; } = 1; // Active by default
}
