using HouseholdManager.Domain.Entities;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// Implementation of household task service with business logic
    /// </summary>
    public class HouseholdTaskService : IHouseholdTaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IHouseholdService _householdService;
        private readonly IRoomService _roomService;
        private readonly ITaskAssignmentService _taskAssignmentService;
        private readonly ILogger<HouseholdTaskService> _logger;

        public HouseholdTaskService(
            ITaskRepository taskRepository,
            IHouseholdService householdService,
            IRoomService roomService,
            ITaskAssignmentService taskAssignmentService,
            ILogger<HouseholdTaskService> logger)
        {
            _taskRepository = taskRepository;
            _householdService = householdService;
            _roomService = roomService;
            _taskAssignmentService = taskAssignmentService;
            _logger = logger;
        }

        // Basic CRUD operations
        public async Task<HouseholdTask> CreateTaskAsync(HouseholdTask task, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await _householdService.ValidateOwnerAccessAsync(task.HouseholdId, requestingUserId, cancellationToken);
            await _roomService.ValidateRoomAccessAsync(task.RoomId, requestingUserId, cancellationToken);

            // Validate room belongs to household
            var room = await _roomService.GetRoomAsync(task.RoomId, cancellationToken);
            if (room?.HouseholdId != task.HouseholdId)
                throw new InvalidOperationException("Room does not belong to the specified household");

            task.CreatedAt = DateTime.UtcNow;
            task.IsActive = true;

            var createdTask = await _taskRepository.AddAsync(task, cancellationToken);
            _logger.LogInformation("Created task {TaskId} in household {HouseholdId}", createdTask.Id, task.HouseholdId);

            return createdTask;
        }

        public async Task<HouseholdTask?> GetTaskAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<HouseholdTask?> GetTaskWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetByIdWithRelationsAsync(id, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetHouseholdTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetActiveHouseholdTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);
        }

        public async Task UpdateTaskAsync(HouseholdTask task, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(task.Id, requestingUserId, cancellationToken);

            // Validate room belongs to household if room changed
            var room = await _roomService.GetRoomAsync(task.RoomId, cancellationToken);
            if (room?.HouseholdId != task.HouseholdId)
                throw new InvalidOperationException("Room does not belong to the specified household");

            await _taskRepository.UpdateAsync(task, cancellationToken);
            _logger.LogInformation("Updated task {TaskId}", task.Id);
        }

        public async Task DeleteTaskAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(id, requestingUserId, cancellationToken);

            await _taskRepository.DeleteByIdAsync(id, cancellationToken);
            _logger.LogInformation("Deleted task {TaskId}", id);
        }

        // Task filtering
        public async Task<IReadOnlyList<HouseholdTask>> GetRoomTasksAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetByRoomIdAsync(roomId, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetUserTasksAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetByAssignedUserIdAsync(userId, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetOverdueTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetOverdueTasksAsync(householdId, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetTasksForWeekdayAsync(Guid householdId, DayOfWeek weekday, CancellationToken cancellationToken = default)
        {
            return await _taskRepository.GetRegularTasksByWeekdayAsync(householdId, weekday, cancellationToken);
        }

        // Assignment operations - delegate to TaskAssignmentService
        public async Task AssignTaskAsync(Guid taskId, string userId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(taskId, requestingUserId, cancellationToken);

            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            // Validate user is member of household
            await _householdService.ValidateUserAccessAsync(task.HouseholdId, userId, cancellationToken);

            // Use repository directly for manual assignment
            await _taskRepository.AssignTaskAsync(taskId, userId, cancellationToken);
            _logger.LogInformation("Manually assigned task {TaskId} to user {UserId}", taskId, userId);
        }

        public async Task UnassignTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(taskId, requestingUserId, cancellationToken);

            await _taskRepository.UnassignTaskAsync(taskId, cancellationToken);
            _logger.LogInformation("Unassigned task {TaskId}", taskId);
        }

        public async Task AutoAssignTasksAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await _householdService.ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            // Delegate to TaskAssignmentService for auto-assignment logic
            var assignments = await _taskAssignmentService.AutoAssignAllTasksAsync(householdId, cancellationToken);
            _logger.LogInformation("Auto-assigned {Count} tasks in household {HouseholdId} by user {UserId}",
                assignments.Count, householdId, requestingUserId);
        }

        // Advanced assignment operations (delegate to TaskAssignmentService)
        public async Task<string> GetSuggestedAssigneeAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskAccessAsync(taskId, requestingUserId, cancellationToken);

            // Delegate to TaskAssignmentService for suggestion algorithm
            var suggestedUserId = await _taskAssignmentService.GetSuggestedAssigneeAsync(taskId, cancellationToken);
            if (suggestedUserId == null)
                throw new InvalidOperationException("No suitable assignee found for this task");

            return suggestedUserId;
        }

        public async Task<string> ReassignTaskToNextUserAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(taskId, requestingUserId, cancellationToken);

            // Delegate to TaskAssignmentService for rotation logic
            var newAssigneeId = await _taskAssignmentService.ReassignTaskToNextUserAsync(taskId, cancellationToken);
            _logger.LogInformation("Reassigned task {TaskId} to next user {UserId} by {RequestingUserId}",
                taskId, newAssigneeId, requestingUserId);

            return newAssigneeId;
        }

        public async Task AutoAssignWeeklyTasksAsync(Guid householdId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await _householdService.ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            // Delegate to TaskAssignmentService for weekly assignment logic
            var assignments = await _taskAssignmentService.AutoAssignWeeklyTasksAsync(householdId, cancellationToken);
            _logger.LogInformation("Auto-assigned {Count} weekly tasks in household {HouseholdId} by user {UserId}",
                assignments.Count, householdId, requestingUserId);
        }

        // Task status operations
        public async Task ActivateTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(taskId, requestingUserId, cancellationToken);

            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            task.IsActive = true;
            await _taskRepository.UpdateAsync(task, cancellationToken);
            _logger.LogInformation("Activated task {TaskId}", taskId);
        }

        public async Task DeactivateTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(taskId, requestingUserId, cancellationToken);

            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            task.IsActive = false;
            await _taskRepository.UpdateAsync(task, cancellationToken);
            _logger.LogInformation("Deactivated task {TaskId}", taskId);
        }

        // Validation
        public async Task ValidateTaskAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            await _householdService.ValidateUserAccessAsync(task.HouseholdId, userId, cancellationToken);
        }

        public async Task ValidateTaskOwnerAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException("Task not found");

            await _householdService.ValidateOwnerAccessAsync(task.HouseholdId, userId, cancellationToken);
        }
    }
}
