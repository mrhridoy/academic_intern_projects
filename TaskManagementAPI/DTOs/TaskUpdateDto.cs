using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskManagementAPI.Entities;

namespace TaskManagementAPI.DTOs;

public class TaskUpdateDto
{
    [StringLength(200, MinimumLength = 1)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Entities.TaskStatus? Status { get; set; }
}