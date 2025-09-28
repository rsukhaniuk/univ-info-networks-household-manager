using HouseholdManager.Models.Entities;

namespace HouseholdManager.Services.Interfaces
{
    /// <summary>
    /// Service interface for task execution business logic
    /// </summary>
    public interface ITaskExecutionService
    {
        // Execution operations
        Task<TaskExecution> CompleteTaskAsync(Guid taskId, string userId, string? notes = null, IFormFile? photo = null, CancellationToken cancellationToken = default);
        Task<TaskExecution?> GetExecutionAsync(Guid id, CancellationToken cancellationToken = default);
        Task<TaskExecution?> GetExecutionWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);
        Task UpdateExecutionAsync(TaskExecution execution, string requestingUserId, CancellationToken cancellationToken = default);
        Task DeleteExecutionAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default);

        // Query operations
        Task<IReadOnlyList<TaskExecution>> GetTaskExecutionsAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaskExecution>> GetHouseholdExecutionsAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaskExecution>> GetUserExecutionsAsync(string userId, Guid householdId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TaskExecution>> GetWeeklyExecutionsAsync(Guid householdId, DateTime? weekStarting = null, CancellationToken cancellationToken = default);

        // Status checking
        Task<bool> IsTaskCompletedThisWeekAsync(Guid taskId, CancellationToken cancellationToken = default);
        Task<TaskExecution?> GetLatestExecutionForTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

        // Photo management
        Task<string> UploadExecutionPhotoAsync(Guid executionId, IFormFile photo, string requestingUserId, CancellationToken cancellationToken = default);
        Task DeleteExecutionPhotoAsync(Guid executionId, string requestingUserId, CancellationToken cancellationToken = default);

        // Validation
        Task ValidateExecutionAccessAsync(Guid executionId, string userId, CancellationToken cancellationToken = default);
    }
}
