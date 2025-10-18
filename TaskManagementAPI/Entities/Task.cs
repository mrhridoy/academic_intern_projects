namespace TaskManagementAPI.Entities;

public class Task
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? DisplayOrder { get; set; }

    public string? AttachmentFileName { get; set; }
    public string? AttachmentFilePath { get; set; }
    public string? AttachmentContentType { get; set; }
    public long? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUploadedAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();
}

public enum TaskStatus
{
    NotStarted,
    Pending,
    InProgress,
    OnHold,
    Cancelled,
    Completed,
    Archived
}