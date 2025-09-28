using HouseholdManager.Models;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Interfaces;

namespace HouseholdManager.Services.Implementations
{
    /// <summary>
    /// Implementation of task execution service with business logic
    /// </summary>
    public class TaskExecutionService : ITaskExecutionService
    {
        private readonly IExecutionRepository _executionRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly IHouseholdService _householdService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<TaskExecutionService> _logger;

        public TaskExecutionService(
            IExecutionRepository executionRepository,
            ITaskRepository taskRepository,
            IHouseholdService householdService,
            IFileUploadService fileUploadService,
            ILogger<TaskExecutionService> logger)
        {
            _executionRepository = executionRepository;
            _taskRepository = taskRepository;
            _householdService = householdService;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // Execution operations
        public async Task<TaskExecution> CompleteTaskAsync(Guid taskId, string userId, string? notes = null, IFormFile? photo = null, CancellationToken cancellationToken = default)
        {
            // Validate task access
            var task = await _taskRepository.GetByIdWithRelationsAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            // Validate user has access to household
            await _householdService.ValidateUserAccessAsync(task.HouseholdId, userId, cancellationToken);

            // Check if task is already completed this week (for regular tasks)
            if (task.Type == Models.Enums.TaskType.Regular)
            {
                var isCompletedThisWeek = await _executionRepository.IsTaskCompletedThisWeekAsync(taskId, cancellationToken);
                if (isCompletedThisWeek)
                    throw new InvalidOperationException("This task has already been completed this week");
            }

            // Upload photo if provided
            string? photoPath = null;
            if (photo != null)
            {
                photoPath = await _fileUploadService.UploadExecutionPhotoAsync(photo, cancellationToken);
            }

            // Create execution with denormalized fields
            var execution = await _executionRepository.CreateExecutionAsync(taskId, userId, notes, photoPath, cancellationToken);

            _logger.LogInformation("Completed task {TaskId} by user {UserId}", taskId, userId);
            return execution;
        }

        public async Task<TaskExecution?> GetExecutionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<TaskExecution?> GetExecutionWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.GetByIdWithRelationsAsync(id, cancellationToken);
        }

        public async Task UpdateExecutionAsync(TaskExecution execution, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateExecutionAccessAsync(execution.Id, requestingUserId, cancellationToken);

            // Only allow updating notes and photo, not core data
            var existingExecution = await _executionRepository.GetByIdAsync(execution.Id, cancellationToken);
            if (existingExecution == null)
                throw new InvalidOperationException("Execution not found");

            // Only update allowed fields
            existingExecution.Notes = execution.Notes;
            // PhotoPath update is handled separately

            await _executionRepository.UpdateAsync(existingExecution, cancellationToken);
            _logger.LogInformation("Updated execution {ExecutionId}", execution.Id);
        }

        public async Task DeleteExecutionAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            var execution = await _executionRepository.GetByIdAsync(id, cancellationToken);
            if (execution == null)
                throw new InvalidOperationException("Execution not found");

            // Only allow deletion by the user who completed it or household owner
            var isOwner = await _householdService.IsUserOwnerAsync(execution.HouseholdId, requestingUserId, cancellationToken);
            if (execution.UserId != requestingUserId && !isOwner)
                throw new UnauthorizedAccessException("You can only delete your own executions or be a household owner");

            // Delete photo if exists
            if (!string.IsNullOrEmpty(execution.PhotoPath))
            {
                await _fileUploadService.DeleteFileAsync(execution.PhotoPath, cancellationToken);
            }

            await _executionRepository.DeleteAsync(execution, cancellationToken);
            _logger.LogInformation("Deleted execution {ExecutionId}", id);
        }

        // Query operations
        public async Task<IReadOnlyList<TaskExecution>> GetTaskExecutionsAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.GetByTaskIdAsync(taskId, cancellationToken);
        }

        public async Task<IReadOnlyList<TaskExecution>> GetHouseholdExecutionsAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
        }

        public async Task<IReadOnlyList<TaskExecution>> GetUserExecutionsAsync(string userId, Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.GetUserExecutionsThisWeekAsync(userId, householdId, cancellationToken);
        }

        public async Task<IReadOnlyList<TaskExecution>> GetWeeklyExecutionsAsync(Guid householdId, DateTime? weekStarting = null, CancellationToken cancellationToken = default)
        {
            if (weekStarting.HasValue)
            {
                var weekEnd = weekStarting.Value.AddDays(7);
                return await _executionRepository.GetByDateRangeAsync(householdId, weekStarting.Value, weekEnd, cancellationToken);
            }
            else
            {
                return await _executionRepository.GetThisWeekAsync(householdId, cancellationToken);
            }
        }

        // Status checking
        public async Task<bool> IsTaskCompletedThisWeekAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.IsTaskCompletedThisWeekAsync(taskId, cancellationToken);
        }

        public async Task<TaskExecution?> GetLatestExecutionForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.GetLatestExecutionForTaskAsync(taskId, cancellationToken);
        }

        // Photo management
        public async Task<string> UploadExecutionPhotoAsync(Guid executionId, IFormFile photo, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateExecutionAccessAsync(executionId, requestingUserId, cancellationToken);

            var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
            if (execution == null)
                throw new InvalidOperationException("Execution not found");

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(execution.PhotoPath))
            {
                await _fileUploadService.DeleteFileAsync(execution.PhotoPath, cancellationToken);
            }

            // Upload new photo
            var photoPath = await _fileUploadService.UploadExecutionPhotoAsync(photo, cancellationToken);

            // Update execution
            execution.PhotoPath = photoPath;
            await _executionRepository.UpdateAsync(execution, cancellationToken);

            _logger.LogInformation("Uploaded photo for execution {ExecutionId}: {PhotoPath}", executionId, photoPath);
            return photoPath;
        }

        public async Task DeleteExecutionPhotoAsync(Guid executionId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateExecutionAccessAsync(executionId, requestingUserId, cancellationToken);

            var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
            if (execution == null)
                throw new InvalidOperationException("Execution not found");

            if (!string.IsNullOrEmpty(execution.PhotoPath))
            {
                await _fileUploadService.DeleteFileAsync(execution.PhotoPath, cancellationToken);
                execution.PhotoPath = null;
                await _executionRepository.UpdateAsync(execution, cancellationToken);

                _logger.LogInformation("Deleted photo for execution {ExecutionId}", executionId);
            }
        }

        // Validation
        public async Task ValidateExecutionAccessAsync(Guid executionId, string userId, CancellationToken cancellationToken = default)
        {
            var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
            if (execution == null)
                throw new InvalidOperationException("Execution not found");

            // Check if user has access to household
            await _householdService.ValidateUserAccessAsync(execution.HouseholdId, userId, cancellationToken);

            // For editing/deleting, user must be the creator or household owner
            var isOwner = await _householdService.IsUserOwnerAsync(execution.HouseholdId, userId, cancellationToken);
            if (execution.UserId != userId && !isOwner)
                throw new UnauthorizedAccessException("You can only access your own executions or be a household owner");
        }
    }
}
