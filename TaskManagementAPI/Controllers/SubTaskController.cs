using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagementAPI.Data;
using TaskManagementAPI.DTOs;
using TaskManagementAPI.Entities;
using TaskEntity = TaskManagementAPI.Entities.Task;
using TaskStatusEnum = TaskManagementAPI.Entities.TaskStatus;

namespace TaskManagementAPI.Controllers
{
    [ApiController]
    [Route("api/Tasks/{taskId}/[controller]")]
    [Authorize]
    public class SubTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SubTasksController(AppDbContext context)
        {
            _context = context;
        }

        // Helper method to update parent task status based on subtask completion
        private async System.Threading.Tasks.Task UpdateParentTaskStatus(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return;

            var subTasks = await _context.SubTasks.Where(st => st.TaskId == taskId).ToListAsync();

            if (subTasks.Count == 0) return;

            var allCompleted = subTasks.All(st => st.IsCompleted);
            var anyCompleted = subTasks.Any(st => st.IsCompleted);

            if (allCompleted)
            {
                // All subtasks completed - mark parent as Completed
                task.Status = TaskStatusEnum.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.UpdatedAt = DateTime.UtcNow;
            }
            else if (task.Status == TaskStatusEnum.Completed)
            {
                // Was completed but now has incomplete subtasks - revert to InProgress
                task.Status = TaskStatusEnum.InProgress;
                task.CompletedAt = null;
                task.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        // GET: api/Tasks/{taskId}/SubTasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubTaskDto>>> GetSubTasks(int taskId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verify task belongs to user and get task details
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            var subTasks = await _context.SubTasks
                .Where(st => st.TaskId == taskId)
                .OrderBy(st => st.DisplayOrder)
                .Select(st => new SubTaskDto
                {
                    Id = st.Id,
                    Title = st.Title,
                    Description = st.Description,
                    IsCompleted = st.IsCompleted,
                    DisplayOrder = st.DisplayOrder,
                    CreatedAt = st.CreatedAt,
                    UpdatedAt = st.UpdatedAt,
                    TaskId = st.TaskId,
                    ParentTask = new ParentTaskDto
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Description = task.Description,
                        Status = task.Status.ToString(),
                        StartDate = task.StartDate,
                        EndDate = task.EndDate
                    }
                })
                .ToListAsync();

            return Ok(subTasks);
        }

        // GET: api/Tasks/{taskId}/SubTasks/{subTaskId}
        [HttpGet("{subTaskId}")]
        public async Task<ActionResult<SubTaskDto>> GetSubTask(int taskId, int subTaskId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verify task belongs to user and get task details
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            var subTask = await _context.SubTasks
                .Where(st => st.Id == subTaskId && st.TaskId == taskId)
                .Select(st => new SubTaskDto
                {
                    Id = st.Id,
                    Title = st.Title,
                    Description = st.Description,
                    IsCompleted = st.IsCompleted,
                    DisplayOrder = st.DisplayOrder,
                    CreatedAt = st.CreatedAt,
                    UpdatedAt = st.UpdatedAt,
                    TaskId = st.TaskId,
                    ParentTask = new ParentTaskDto
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Description = task.Description,
                        Status = task.Status.ToString(),
                        StartDate = task.StartDate,
                        EndDate = task.EndDate
                    }
                })
                .FirstOrDefaultAsync();

            if (subTask == null)
            {
                return NotFound(new { message = "SubTask not found" });
            }

            return Ok(subTask);
        }

        // POST: api/Tasks/{taskId}/SubTasks
        [HttpPost]
        public async Task<ActionResult<SubTaskDto>> CreateSubTask(int taskId, [FromBody] SubTaskCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verify task belongs to user and get task details
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            // Get the next display order
            var maxDisplayOrder = await _context.SubTasks
                .Where(st => st.TaskId == taskId)
                .MaxAsync(st => (int?)st.DisplayOrder) ?? 0;

            var subTask = new SubTask
            {
                TaskId = taskId,
                Title = dto.Title,
                Description = dto.Description,
                IsCompleted = dto.IsCompleted ?? false,
                DisplayOrder = dto.DisplayOrder ?? (maxDisplayOrder + 1),
                CreatedAt = DateTime.UtcNow
            };

            _context.SubTasks.Add(subTask);
            await _context.SaveChangesAsync();

            // Update parent task status based on all subtasks completion
            await UpdateParentTaskStatus(taskId);

            var result = new SubTaskDto
            {
                Id = subTask.Id,
                Title = subTask.Title,
                Description = subTask.Description,
                IsCompleted = subTask.IsCompleted,
                DisplayOrder = subTask.DisplayOrder,
                CreatedAt = subTask.CreatedAt,
                UpdatedAt = subTask.UpdatedAt,
                TaskId = subTask.TaskId,
                ParentTask = new ParentTaskDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status.ToString(),
                    StartDate = task.StartDate,
                    EndDate = task.EndDate
                }
            };

            return CreatedAtAction(nameof(GetSubTask), new { taskId, subTaskId = subTask.Id }, result);
        }

        // PUT: api/Tasks/{taskId}/SubTasks/{subTaskId}
        [HttpPut("{subTaskId}")]
        public async Task<ActionResult<SubTaskDto>> UpdateSubTask(int taskId, int subTaskId, [FromBody] SubTaskUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verify task belongs to user and get task details
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            var subTask = await _context.SubTasks.FirstOrDefaultAsync(st => st.Id == subTaskId && st.TaskId == taskId);
            if (subTask == null)
            {
                return NotFound(new { message = "SubTask not found" });
            }

            subTask.Title = dto.Title;
            subTask.Description = dto.Description;
            subTask.IsCompleted = dto.IsCompleted;
            subTask.UpdatedAt = DateTime.UtcNow;

            if (dto.DisplayOrder.HasValue)
            {
                subTask.DisplayOrder = dto.DisplayOrder.Value;
            }

            await _context.SaveChangesAsync();

            // Update parent task status based on all subtasks completion
            await UpdateParentTaskStatus(taskId);

            var result = new SubTaskDto
            {
                Id = subTask.Id,
                Title = subTask.Title,
                Description = subTask.Description,
                IsCompleted = subTask.IsCompleted,
                DisplayOrder = subTask.DisplayOrder,
                CreatedAt = subTask.CreatedAt,
                UpdatedAt = subTask.UpdatedAt,
                TaskId = subTask.TaskId,
                ParentTask = new ParentTaskDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status.ToString(),
                    StartDate = task.StartDate,
                    EndDate = task.EndDate
                }
            };

            return Ok(result);
        }

        // PATCH: api/Tasks/{taskId}/SubTasks/{subTaskId}/toggle
        [HttpPatch("{subTaskId}/toggle")]
        public async Task<ActionResult<SubTaskDto>> ToggleSubTask(int taskId, int subTaskId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verify task belongs to user and get task details
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            var subTask = await _context.SubTasks.FirstOrDefaultAsync(st => st.Id == subTaskId && st.TaskId == taskId);
            if (subTask == null)
            {
                return NotFound(new { message = "SubTask not found" });
            }

            subTask.IsCompleted = !subTask.IsCompleted;
            subTask.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Update parent task status based on all subtasks completion
            await UpdateParentTaskStatus(taskId);

            var result = new SubTaskDto
            {
                Id = subTask.Id,
                Title = subTask.Title,
                Description = subTask.Description,
                IsCompleted = subTask.IsCompleted,
                DisplayOrder = subTask.DisplayOrder,
                CreatedAt = subTask.CreatedAt,
                UpdatedAt = subTask.UpdatedAt,
                TaskId = subTask.TaskId,
                ParentTask = new ParentTaskDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status.ToString(),
                    StartDate = task.StartDate,
                    EndDate = task.EndDate
                }
            };

            return Ok(result);
        }

        // DELETE: api/Tasks/{taskId}/SubTasks/{subTaskId}
        [HttpDelete("{subTaskId}")]
        public async Task<IActionResult> DeleteSubTask(int taskId, int subTaskId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Verify task belongs to user
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
            if (task == null)
            {
                return NotFound(new { message = "Task not found" });
            }

            var subTask = await _context.SubTasks.FirstOrDefaultAsync(st => st.Id == subTaskId && st.TaskId == taskId);
            if (subTask == null)
            {
                return NotFound(new { message = "SubTask not found" });
            }

            _context.SubTasks.Remove(subTask);
            await _context.SaveChangesAsync();

            // Update parent task status based on remaining subtasks
            await UpdateParentTaskStatus(taskId);

            return Ok(new { message = "SubTask deleted successfully" });
        }
    }
}