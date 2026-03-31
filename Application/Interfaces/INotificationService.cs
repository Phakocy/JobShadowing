namespace JobShadowing.Application.Interfaces
{
    public interface INotificationService
    {
        // Notify a specific user
        Task NotifyUserAsync(int userId, string eventType, object data);

        // Notify all members of a team
        Task NotifyTeamAsync(int teamId, string eventType, object data);

        // Notify all members of a project
        Task NotifyProjectAsync(int projectId, string eventType, object data);

        // Notify when a task is created
        Task NotifyTaskCreatedAsync(int projectId, int taskId, string taskTitle, int createdByUserId);

        // Notify when a task status changes
        Task NotifyTaskStatusChangedAsync(int projectId, int taskId, string taskTitle, string newStatus);

        // Notify when a member is added to a team
        Task NotifyMemberAddedAsync(int teamId, int userId, string userName);

        // Notify when a member is removed from a team
        Task NotifyMemberRemovedAsync(int teamId, int userId, string userName);
    }
}
