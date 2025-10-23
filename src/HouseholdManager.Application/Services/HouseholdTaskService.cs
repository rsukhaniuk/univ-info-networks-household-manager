using AutoMapper;
using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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
        private readonly IHouseholdMemberService _householdMemberService;
        private readonly ITaskAssignmentService _taskAssignmentService;
        private readonly ILogger<HouseholdTaskService> _logger;
        private readonly IMapper _mapper;

        public HouseholdTaskService(
            ITaskRepository taskRepository,
            IHouseholdService householdService,
            IRoomService roomService,
            IHouseholdMemberService householdMemberService,
            ITaskAssignmentService taskAssignmentService,
            IMapper mapper,
            ILogger<HouseholdTaskService> logger)
        {
            _taskRepository = taskRepository;
            _householdService = householdService;
            _roomService = roomService;
            _householdMemberService = householdMemberService;
            _taskAssignmentService = taskAssignmentService;
            _mapper = mapper;
            _logger = logger;
        }

        // Basic CRUD operations
        public async Task<TaskDto> CreateTaskAsync(
            UpsertTaskRequest request,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            await _householdService.ValidateOwnerAccessAsync(request.HouseholdId, requestingUserId, cancellationToken);
            await _roomService.ValidateRoomAccessAsync(request.RoomId, requestingUserId, cancellationToken);

            // Validate room belongs to household
            var room = await _roomService.GetRoomAsync(request.RoomId, cancellationToken);
            if (room?.HouseholdId != request.HouseholdId)
                throw new ValidationException("HouseholdId", "Room does not belong to the specified household");


            var task = _mapper.Map<HouseholdTask>(request);
            task.CreatedAt = DateTime.UtcNow;
            task.IsActive = true;

            var createdTask = await _taskRepository.AddAsync(task, cancellationToken);

            _logger.LogInformation("Created task {TaskId} in household {HouseholdId}",
                createdTask.Id, request.HouseholdId);

            // Load navigation properties for DTO mapping
            createdTask = await _taskRepository.GetByIdWithRelationsAsync(createdTask.Id, cancellationToken);

            return _mapper.Map<TaskDto>(createdTask);
        }

        public async Task<TaskDto?> GetTaskAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdWithRelationsAsync(id, cancellationToken);
            return task == null ? null : _mapper.Map<TaskDto>(task);
        }

        public async Task<TaskDetailsDto?> GetTaskWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdWithRelationsAsync(id, cancellationToken);
            if (task == null) return null;

            var dto = _mapper.Map<TaskDetailsDto>(task);

            var members = await _householdMemberService.GetHouseholdMembersAsync(task.HouseholdId, cancellationToken);
            dto.AvailableAssignees = members.Select(m => new TaskAssigneeDto
            {
                UserId = m.UserId,
                UserName = m.UserName ?? "Unknown",
                Email = m.Email,
                CurrentTaskCount = 0
            }).ToList();

            var counts = await _householdMemberService.GetMemberTaskCountsAsync(task.HouseholdId, cancellationToken);

            foreach (var assignee in dto.AvailableAssignees)
            {
                assignee.CurrentTaskCount = counts.TryGetValue(assignee.UserId, out var c) ? c : 0;
            }

            //dto.Permissions.IsOwner = false; 
            return dto;
        }

        public async Task<IReadOnlyList<TaskDto>> GetHouseholdTasksAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _taskRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<TaskDto>>(tasks);
        }

        public async Task<IReadOnlyList<TaskDto>> GetActiveHouseholdTasksAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<TaskDto>>(tasks);
        }

        public async Task<TaskDto> UpdateTaskAsync(
            Guid id,
            UpsertTaskRequest request,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(id, requestingUserId, cancellationToken);

            var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", id);

            // Validate room belongs to household if room changed
            var room = await _roomService.GetRoomAsync(request.RoomId, cancellationToken);
            if (room?.HouseholdId != request.HouseholdId)
                throw new ValidationException("HouseholdId", "Room does not belong to the specified household");

            // Update properties from request
            _mapper.Map(request, task);

            await _taskRepository.UpdateAsync(task, cancellationToken);

            _logger.LogInformation("Updated task {TaskId}", task.Id);

            // Reload with relations for DTO
            task = await _taskRepository.GetByIdWithRelationsAsync(id, cancellationToken);

            return _mapper.Map<TaskDto>(task);
        }

        public async Task DeleteTaskAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(id, requestingUserId, cancellationToken);

            await _taskRepository.DeleteByIdAsync(id, cancellationToken);
            _logger.LogInformation("Deleted task {TaskId}", id);
        }

        // Task filtering
        public async Task<IReadOnlyList<TaskDto>> GetRoomTasksAsync(
            Guid roomId,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _taskRepository.GetByRoomIdAsync(roomId, cancellationToken);
            return _mapper.Map<IReadOnlyList<TaskDto>>(tasks);
        }

        public async Task<IReadOnlyList<TaskDto>> GetUserTasksAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _taskRepository.GetByAssignedUserIdAsync(userId, cancellationToken);
            return _mapper.Map<IReadOnlyList<TaskDto>>(tasks);
        }

        public async Task<IReadOnlyList<TaskDto>> GetOverdueTasksAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _taskRepository.GetOverdueTasksAsync(householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<TaskDto>>(tasks);
        }

        public async Task<IReadOnlyList<TaskDto>> GetTasksForWeekdayAsync(
            Guid householdId,
            DayOfWeek weekday,
            CancellationToken cancellationToken = default)
        {
            var tasks = await _taskRepository.GetRegularTasksByWeekdayAsync(householdId, weekday, cancellationToken);
            return _mapper.Map<IReadOnlyList<TaskDto>>(tasks);
        }

        // Assignment operations - delegate to TaskAssignmentService
        public async Task<TaskDto> AssignTaskAsync(
            Guid taskId,
            AssignTaskRequest request,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(taskId, requestingUserId, cancellationToken);

            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Validate user is member of household (if assigning)
            if (request.UserId != null)
            {
                await _householdService.ValidateUserAccessAsync(task.HouseholdId, request.UserId, cancellationToken);
            }

            // Use repository for assignment
            if (request.UserId != null)
            {
                await _taskRepository.AssignTaskAsync(taskId, request.UserId, cancellationToken);
                _logger.LogInformation("Manually assigned task {TaskId} to user {UserId}", taskId, request.UserId);
            }
            else
            {
                await _taskRepository.UnassignTaskAsync(taskId, cancellationToken);
                _logger.LogInformation("Unassigned task {TaskId}", taskId);
            }

            // Reload with relations
            task = await _taskRepository.GetByIdWithRelationsAsync(taskId, cancellationToken);

            return _mapper.Map<TaskDto>(task);
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
                throw new ValidationException("Assignee", "No suitable assignee found for this task");

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
                throw new NotFoundException("Task", taskId);

            task.IsActive = true;
            await _taskRepository.UpdateAsync(task, cancellationToken);
            _logger.LogInformation("Activated task {TaskId}", taskId);
        }

        public async Task DeactivateTaskAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateTaskOwnerAccessAsync(taskId, requestingUserId, cancellationToken);

            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", taskId);

            task.IsActive = false;
            await _taskRepository.UpdateAsync(task, cancellationToken);
            _logger.LogInformation("Deactivated task {TaskId}", taskId);
        }

        // Validation
        public async Task ValidateTaskAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", taskId);

            await _householdService.ValidateUserAccessAsync(task.HouseholdId, userId, cancellationToken);
        }

        public async Task ValidateTaskOwnerAccessAsync(Guid taskId, string userId, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", taskId);

            await _householdService.ValidateOwnerAccessAsync(task.HouseholdId, userId, cancellationToken);
        }
    }
}
