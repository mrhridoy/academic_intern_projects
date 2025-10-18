
using TaskStatus = TaskManagementAPI.Entities.TaskStatus;

namespace TaskManagementAPI.DTOs;

public class TaskDto
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
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;

    public string? AttachmentFileName { get; set; }
    public string? AttachmentFilePath { get; set; }
    public string? AttachmentContentType { get; set; }
    public long? AttachmentFileSize { get; set; }
    public DateTime? AttachmentUploadedAt { get; set; }
    public bool HasAttachment => !string.IsNullOrEmpty(AttachmentFilePath);

    public List<SubTaskDto> SubTasks { get; set; } = [];
    public int SubTaskCount { get; set; }
    public int CompletedSubTaskCount { get; set; }
    public int CompletionPercentage { get; set; }
}
