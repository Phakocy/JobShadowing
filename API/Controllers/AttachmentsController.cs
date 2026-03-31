using System.Security.Claims;
using JobShadowing.Application.DTOs;
using JobShadowing.Application.Interfaces;
using JobShadowing.Domain.Entities;
using JobShadowing.Domain.Exceptions;
using JobShadowing.Infrastructure.Data;
using JobShadowing.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace JobShadowing.API.Controllers
{
    [Route("api/tasks/{taskId}/attachments")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    [Tags("Attachments")]
    public class AttachmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IProjectService _projectService;
        private readonly FileStorageSettings _fileSettings;
        private readonly ILogger<AttachmentsController> _logger;

        public AttachmentsController(
            AppDbContext context,
            IProjectService projectService,
            IOptions<FileStorageSettings> fileSettings,
            ILogger<AttachmentsController> logger)
        {
            _context = context;
            _projectService = projectService;
            _fileSettings = fileSettings.Value;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new ForbiddenException("Invalid user token");
            }
            return userId;
        }

        // Get all attachments for a task
        /// <param name="taskId">Task ID</param>
        // <returns>List of attachments</returns>
        [HttpGet]
        [SwaggerOperation(Summary = "Get task attachments", Description = "Returns all attachments for the specified task")]
        [ProducesResponseType(typeof(List<TaskAttachmentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<TaskAttachmentDto>>> GetAttachments(int taskId)
        {
            var userId = GetCurrentUserId();
            await ValidateTaskAccessAsync(taskId, userId);

            var attachments = await _context.TaskAttachments
                .Include(a => a.UploadedBy)
                .Where(a => a.TaskId == taskId)
                .Select(a => new TaskAttachmentDto
                {
                    Id = a.Id,
                    TaskId = a.TaskId,
                    FileName = a.FileName,
                    ContentType = a.ContentType,
                    FileSize = a.FileSize,
                    UploadedAt = a.UploadedAt,
                    UploadedByUserId = a.UploadedByUserId,
                    UploadedByName = a.UploadedBy.FullName,
                    DownloadUrl = $"/api/tasks/{taskId}/attachments/{a.Id}/download"
                })
                .ToListAsync();

            return Ok(attachments);
        }

        /// Upload a file attachment to a task
        /// <param name="taskId">Task ID</param>
        /// <param name="file">File to upload</param>
        /// <returns>Created attachment information</returns>
        [HttpPost]
        [SwaggerOperation(Summary = "Upload attachment", Description = "Uploads a file attachment to the specified task")]
        [ProducesResponseType(typeof(TaskAttachmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<TaskAttachmentDto>> UploadAttachment(int taskId, IFormFile file)
        {
            var userId = GetCurrentUserId();
            await ValidateTaskAccessAsync(taskId, userId);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = 400,
                    Message = "No file provided"
                });
            }

            if (file.Length > _fileSettings.MaxFileSize)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = 400,
                    Message = $"File size exceeds maximum allowed size of {_fileSettings.MaxFileSize / (1024 * 1024)}MB"
                });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_fileSettings.AllowedExtensions.Contains(extension))
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = 400,
                    Message = $"File type '{extension}' is not allowed"
                });
            }

            // Generate unique filename
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var storagePath = Path.Combine(_fileSettings.StoragePath, taskId.ToString());

            // Ensure directory exists
            Directory.CreateDirectory(storagePath);

            var filePath = Path.Combine(storagePath, storedFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new TaskAttachment
            {
                TaskId = taskId,
                FileName = file.FileName,
                StoredFileName = filePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = userId
            };

            _context.TaskAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            var dto = new TaskAttachmentDto
            {
                Id = attachment.Id,
                TaskId = attachment.TaskId,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                UploadedAt = attachment.UploadedAt,
                UploadedByUserId = attachment.UploadedByUserId,
                UploadedByName = user?.FullName ?? "Unknown",
                DownloadUrl = $"/api/tasks/{taskId}/attachments/{attachment.Id}/download"
            };

            _logger.LogInformation("User {UserId} uploaded attachment {AttachmentId} to task {TaskId}", userId, attachment.Id, taskId);

            return CreatedAtAction(nameof(GetAttachments), new { taskId }, dto);
        }

        /// Download an attachment
        /// <param name="taskId">Task ID</param>
        /// <param name="attachmentId">Attachment ID</param>
        /// <returns>File download</returns>
        [HttpGet("{attachmentId}/download")]
        [SwaggerOperation(Summary = "Download attachment", Description = "Downloads the specified attachment file")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DownloadAttachment(int taskId, int attachmentId)
        {
            var userId = GetCurrentUserId();
            await ValidateTaskAccessAsync(taskId, userId);

            var attachment = await _context.TaskAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == taskId);

            if (attachment == null)
            {
                throw new NotFoundException("Attachment", attachmentId);
            }

            if (!System.IO.File.Exists(attachment.StoredFileName))
            {
                throw new NotFoundException("Attachment file not found on server");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(attachment.StoredFileName);

            return File(fileBytes, attachment.ContentType, attachment.FileName);
        }

        /// Delete an attachment
        /// <param name="taskId">Task ID</param>
        /// <param name="attachmentId">Attachment ID</param>
        [HttpDelete("{attachmentId}")]
        [SwaggerOperation(Summary = "Delete attachment", Description = "Deletes the specified attachment")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteAttachment(int taskId, int attachmentId)
        {
            var userId = GetCurrentUserId();
            await ValidateTaskAccessAsync(taskId, userId);

            var attachment = await _context.TaskAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == taskId);

            if (attachment == null)
            {
                throw new NotFoundException("Attachment", attachmentId);
            }

            // Only uploader or task owner can delete
            var task = await _context.Tasks.FindAsync(taskId);
            if (attachment.UploadedByUserId != userId && task?.UserId != userId)
            {
                throw new ForbiddenException("You do not have permission to delete this attachment");
            }

            // Delete file from storage
            if (System.IO.File.Exists(attachment.StoredFileName))
            {
                System.IO.File.Delete(attachment.StoredFileName);
            }

            _context.TaskAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} deleted attachment {AttachmentId} from task {TaskId}", userId, attachmentId, taskId);

            return NoContent();
        }

        private async Task ValidateTaskAccessAsync(int taskId, int userId)
        {
            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null)
            {
                throw new NotFoundException("Task", taskId);
            }

            // User owns the task
            if (task.UserId == userId)
            {
                return;
            }

            // Check project access for team tasks
            if (task.ProjectId.HasValue)
            {
                var canAccess = await _projectService.CanUserAccessProjectAsync(userId, task.ProjectId.Value);
                if (canAccess)
                {
                    return;
                }
            }

            throw new ForbiddenException("You do not have access to this task");
        }
    }
}
