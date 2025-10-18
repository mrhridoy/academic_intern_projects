using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskManagementAPI.Entities;

namespace TaskManagementAPI.DTOs;

public class TaskCreateDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Entities.TaskStatus Status { get; set; } = Entities.TaskStatus.NotStarted;
}