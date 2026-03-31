using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace JobShadowing.API.Hubs
{
    // SignalR hub for real-time notifications
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Add user to their personal group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("User {UserId} connected to notifications hub", userId);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation("User {UserId} disconnected from notifications hub", userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Join a team's notification group
        public async Task JoinTeamGroup(int teamId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");
            _logger.LogInformation("Connection {ConnectionId} joined team_{TeamId} group", Context.ConnectionId, teamId);
        }

        // Leave a team's notification group
        public async Task LeaveTeamGroup(int teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team_{teamId}");
            _logger.LogInformation("Connection {ConnectionId} left team_{TeamId} group", Context.ConnectionId, teamId);
        }

        // Join a project's notification group
        public async Task JoinProjectGroup(int projectId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectId}");
            _logger.LogInformation("Connection {ConnectionId} joined project_{ProjectId} group", Context.ConnectionId, projectId);
        }

        // Leave a project's notification group
        public async Task LeaveProjectGroup(int projectId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project_{projectId}");
            _logger.LogInformation("Connection {ConnectionId} left project_{ProjectId} group", Context.ConnectionId, projectId);
        }
    }
}
