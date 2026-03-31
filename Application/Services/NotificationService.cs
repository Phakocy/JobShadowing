using JobShadowing.API.Hubs;
using JobShadowing.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace JobShadowing.Application.Services
{
    // Service for sending real-time notifications via SignalR
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyUserAsync(int userId, string eventType, object data)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{userId}")
                    .SendAsync(eventType, data);
                _logger.LogDebug("Sent {EventType} notification to user {UserId}", eventType, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            }
        }

        public async Task NotifyTeamAsync(int teamId, string eventType, object data)
        {
            try
            {
                await _hubContext.Clients.Group($"team_{teamId}")
                    .SendAsync(eventType, data);
                _logger.LogDebug("Sent {EventType} notification to team {TeamId}", eventType, teamId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to team {TeamId}", teamId);
            }
        }

        public async Task NotifyProjectAsync(int projectId, string eventType, object data)
        {
            try
            {
                await _hubContext.Clients.Group($"project_{projectId}")
                    .SendAsync(eventType, data);
                _logger.LogDebug("Sent {EventType} notification to project {ProjectId}", eventType, projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to project {ProjectId}", projectId);
            }
        }

        public async Task NotifyTaskCreatedAsync(int projectId, int taskId, string taskTitle, int createdByUserId)
        {
            var data = new
            {
                TaskId = taskId,
                TaskTitle = taskTitle,
                ProjectId = projectId,
                CreatedByUserId = createdByUserId,
                Timestamp = DateTime.UtcNow
            };

            await NotifyProjectAsync(projectId, "TaskCreated", data);
        }

        public async Task NotifyTaskStatusChangedAsync(int projectId, int taskId, string taskTitle, string newStatus)
        {
            var data = new
            {
                TaskId = taskId,
                TaskTitle = taskTitle,
                ProjectId = projectId,
                NewStatus = newStatus,
                Timestamp = DateTime.UtcNow
            };

            await NotifyProjectAsync(projectId, "TaskStatusChanged", data);
        }

        public async Task NotifyMemberAddedAsync(int teamId, int userId, string userName)
        {
            var data = new
            {
                TeamId = teamId,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            };

            await NotifyTeamAsync(teamId, "MemberAdded", data);
        }

        public async Task NotifyMemberRemovedAsync(int teamId, int userId, string userName)
        {
            var data = new
            {
                TeamId = teamId,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            };

            await NotifyTeamAsync(teamId, "MemberRemoved", data);
        }
    }
}
