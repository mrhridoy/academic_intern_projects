using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.DTOs;
using TaskStatus = TaskManagementAPI.Entities.TaskStatus;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public TasksController(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TaskDto>>> GetTasks([FromQuery] TaskSearchDto searchDto)
    {
        var userId = GetUserId();

        var query = _context.Tasks
            .Include(t => t.User)
            .Include(t => t.SubTasks)
            .Where(t => t.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchDto.SearchTerm))
        {
            var searchTerm = searchDto.SearchTerm.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchTerm) ||
                                    t.Description.ToLower().Contains(searchTerm));
        }

        if (searchDto.Status.HasValue)
            query = query.Where(t => t.Status == searchDto.Status.Value);

        query = searchDto.SortBy switch
        {
            SortByOption.Id => searchDto.SortOrder == SortOrderOption.Asc
                ? query.OrderBy(t => t.Id)
                : query.OrderByDescending(t => t.Id),

            SortByOption.Title => searchDto.SortOrder == SortOrderOption.Asc
                ? query.OrderBy(t => t.Title)
                : query.OrderByDescending(t => t.Title),

            SortByOption.StartDate => searchDto.SortOrder == SortOrderOption.Asc
                ? query.OrderBy(t => t.StartDate)
                : query.OrderByDescending(t => t.StartDate),

            SortByOption.EndDate => searchDto.SortOrder == SortOrderOption.Asc
                ? query.OrderBy(t => t.EndDate)
                : query.OrderByDescending(t => t.EndDate),

            SortByOption.Status => searchDto.SortOrder == SortOrderOption.Asc
                ? query.OrderBy(t => t.Status)
                : query.OrderByDescending(t => t.Status),

            SortByOption.CreatedAt => searchDto.SortOrder == SortOrderOption.Asc
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt),

            _ => searchDto.SortOrder == SortOrderOption.Asc
                ? query.OrderBy(t => t.DisplayOrder ?? int.MaxValue).ThenByDescending(t => t.CreatedAt)
                : query.OrderByDescending(t => t.DisplayOrder ?? int.MinValue).ThenByDescending(t => t.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        if (searchDto.PageSize < 1) searchDto.PageSize = 10;
        if (searchDto.PageSize > 100) searchDto.PageSize = 100;
        if (searchDto.PageNumber < 1) searchDto.PageNumber = 1;

        var tasks = await query
            .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = t.Status,
                CompletedAt = t.CompletedAt,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                DisplayOrder = t.DisplayOrder,
                UserId = t.UserId,
                Username = t.User.Username,
                AttachmentFileName = t.AttachmentFileName,
                AttachmentFilePath = t.AttachmentFilePath,
                AttachmentContentType = t.AttachmentContentType,
                AttachmentFileSize = t.AttachmentFileSize,
                AttachmentUploadedAt = t.AttachmentUploadedAt,
                SubTasks = t.SubTasks.OrderBy(s => s.DisplayOrder).Select(s => new SubTaskDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Description = s.Description,
                    IsCompleted = s.IsCompleted,
                    DisplayOrder = s.DisplayOrder,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList(),
                SubTaskCount = t.SubTasks.Count,
                CompletedSubTaskCount = t.SubTasks.Count(s => s.IsCompleted),
                CompletionPercentage = t.SubTasks.Any() ? (int)((double)t.SubTasks.Count(s => s.IsCompleted) / t.SubTasks.Count * 100) : 0
            })
            .ToListAsync();

        var result = new PagedResultDto<TaskDto>
        {
            Items = tasks,
            TotalCount = totalCount,
            PageNumber = searchDto.PageNumber,
            PageSize = searchDto.PageSize
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskDto>> GetTask(int id)
    {
        var userId = GetUserId();

        var task = await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        var taskDto = new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            Status = task.Status,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DisplayOrder = task.DisplayOrder,
            UserId = task.UserId,
            Username = task.User.Username,
            AttachmentFileName = task.AttachmentFileName,
            AttachmentFilePath = task.AttachmentFilePath,
            AttachmentContentType = task.AttachmentContentType,
            AttachmentFileSize = task.AttachmentFileSize,
            AttachmentUploadedAt = task.AttachmentUploadedAt,
            SubTasks = task.SubTasks.OrderBy(s => s.DisplayOrder).Select(s => new SubTaskDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                IsCompleted = s.IsCompleted,
                DisplayOrder = s.DisplayOrder,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            SubTaskCount = task.SubTasks.Count,
            CompletedSubTaskCount = task.SubTasks.Count(s => s.IsCompleted),
            CompletionPercentage = task.SubTasks.Any() ? (int)((double)task.SubTasks.Count(s => s.IsCompleted) / task.SubTasks.Count * 100) : 0
        };

        return Ok(taskDto);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask(TaskCreateDto dto)
    {
        var userId = GetUserId();

        var task = new Entities.Task
        {
            Title = dto.Title,
            Description = dto.Description,
            StartDate = dto.StartDate ?? DateTime.UtcNow,
            EndDate = dto.EndDate ?? DateTime.UtcNow.AddDays(7),
            Status = dto.Status,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        task = await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.SubTasks)
            .FirstAsync(t => t.Id == task.Id);

        var taskDto = new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            Status = task.Status,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DisplayOrder = task.DisplayOrder,
            UserId = task.UserId,
            Username = task.User.Username,
            SubTasks = new List<SubTaskDto>(),
            SubTaskCount = 0,
            CompletedSubTaskCount = 0,
            CompletionPercentage = 0
        };

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, taskDto);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskDto>> UpdateTask(int id, TaskUpdateDto dto)
    {
        var userId = GetUserId();

        var task = await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        if (dto.Title != null)
            task.Title = dto.Title;

        if (dto.Description != null)
            task.Description = dto.Description;

        if (dto.StartDate.HasValue)
            task.StartDate = dto.StartDate.Value;

        if (dto.EndDate.HasValue)
            task.EndDate = dto.EndDate.Value;

        if (dto.Status.HasValue)
        {
            task.Status = dto.Status.Value;

            if (dto.Status.Value == TaskStatus.Completed && task.CompletedAt == null)
                task.CompletedAt = DateTime.UtcNow;
            else if (dto.Status.Value != TaskStatus.Completed)
                task.CompletedAt = null;
        }

        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var taskDto = new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            Status = task.Status,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DisplayOrder = task.DisplayOrder,
            UserId = task.UserId,
            Username = task.User.Username,
            AttachmentFileName = task.AttachmentFileName,
            AttachmentFilePath = task.AttachmentFilePath,
            AttachmentContentType = task.AttachmentContentType,
            AttachmentFileSize = task.AttachmentFileSize,
            AttachmentUploadedAt = task.AttachmentUploadedAt,
            SubTasks = task.SubTasks.OrderBy(s => s.DisplayOrder).Select(s => new SubTaskDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                IsCompleted = s.IsCompleted,
                DisplayOrder = s.DisplayOrder,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            SubTaskCount = task.SubTasks.Count,
            CompletedSubTaskCount = task.SubTasks.Count(s => s.IsCompleted),
            CompletionPercentage = task.SubTasks.Any() ? (int)((double)task.SubTasks.Count(s => s.IsCompleted) / task.SubTasks.Count * 100) : 0
        };

        return Ok(taskDto);
    }

    [HttpPost("{id}/attachment")]
    public async Task<ActionResult<TaskDto>> UploadAttachment(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var userId = GetUserId();

        var task = await _context.Tasks
            .Include(t => t.User)
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = "File type not allowed. Only images and documents." });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size exceeds 10MB limit" });

        var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRootPath, "uploads", "tasks");

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        if (!string.IsNullOrEmpty(task.AttachmentFilePath))
        {
            var oldFilePath = Path.Combine(webRootPath, task.AttachmentFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        var relativeFilePath = $"/uploads/tasks/{uniqueFileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        task.AttachmentFileName = file.FileName;
        task.AttachmentFilePath = relativeFilePath;
        task.AttachmentFileSize = file.Length;
        task.AttachmentContentType = file.ContentType;
        task.AttachmentUploadedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var taskDto = new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            Status = task.Status,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            DisplayOrder = task.DisplayOrder,
            UserId = task.UserId,
            Username = task.User.Username,
            AttachmentFileName = task.AttachmentFileName,
            AttachmentFilePath = task.AttachmentFilePath,
            AttachmentContentType = task.AttachmentContentType,
            AttachmentFileSize = task.AttachmentFileSize,
            AttachmentUploadedAt = task.AttachmentUploadedAt,
            SubTasks = task.SubTasks.OrderBy(s => s.DisplayOrder).Select(s => new SubTaskDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                IsCompleted = s.IsCompleted,
                DisplayOrder = s.DisplayOrder,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            SubTaskCount = task.SubTasks.Count,
            CompletedSubTaskCount = task.SubTasks.Count(s => s.IsCompleted),
            CompletionPercentage = task.SubTasks.Any() ? (int)((double)task.SubTasks.Count(s => s.IsCompleted) / task.SubTasks.Count * 100) : 0
        };

        return Ok(taskDto);
    }

    [HttpGet("{id}/attachment/view")]
    public async Task<IActionResult> ViewAttachment(int id)
    {
        var userId = GetUserId();

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        if (string.IsNullOrEmpty(task.AttachmentFilePath))
            return NotFound(new { message = "No attachment found" });

        var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRootPath, task.AttachmentFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(physicalPath))
            return NotFound(new { message = "File not found on server" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return File(fileBytes, task.AttachmentContentType ?? "application/octet-stream");
    }

    [HttpGet("{id}/attachment/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var userId = GetUserId();

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        if (string.IsNullOrEmpty(task.AttachmentFilePath))
            return NotFound(new { message = "No attachment found" });

        var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRootPath, task.AttachmentFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(physicalPath))
            return NotFound(new { message = "File not found on server" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return File(fileBytes, task.AttachmentContentType ?? "application/octet-stream", task.AttachmentFileName ?? "download");
    }

    [HttpDelete("{id}/attachment")]
    public async Task<ActionResult> DeleteAttachment(int id)
    {
        var userId = GetUserId();

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        if (string.IsNullOrEmpty(task.AttachmentFilePath))
            return NotFound(new { message = "No attachment found" });

        var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var physicalPath = Path.Combine(webRootPath, task.AttachmentFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }

        task.AttachmentFileName = null;
        task.AttachmentFilePath = null;
        task.AttachmentContentType = null;
        task.AttachmentFileSize = null;
        task.AttachmentUploadedAt = null;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<ActionResult> ReorderTasks([FromBody] List<int> taskIds)
    {
        var userId = GetUserId();

        for (int i = 0; i < taskIds.Count; i++)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskIds[i] && t.UserId == userId);
            if (task != null)
            {
                task.DisplayOrder = i + 1;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Tasks reordered successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTask(int id)
    {
        var userId = GetUserId();

        var task = await _context.Tasks
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        if (!string.IsNullOrEmpty(task.AttachmentFilePath))
        {
            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var physicalPath = Path.Combine(webRootPath, task.AttachmentFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
