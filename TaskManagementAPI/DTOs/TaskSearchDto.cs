using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using TaskManagementAPI.Entities;

namespace TaskManagementAPI.DTOs;

public class TaskSearchDto
{
    public string? SearchTerm { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Entities.TaskStatus? Status { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortByOption SortBy { get; set; } = SortByOption.CreatedAt;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortOrderOption SortOrder { get; set; } = SortOrderOption.Desc;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortByOption
{
    Id,
    Title,
    StartDate,
    EndDate,
    Status,
    CreatedAt
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortOrderOption
{
    Asc,
    Desc
}