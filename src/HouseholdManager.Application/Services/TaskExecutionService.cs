using AutoMapper;
using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Services
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
        private readonly IMapper _mapper;
        private readonly ILogger<TaskExecutionService> _logger;

        public TaskExecutionService(
            IExecutionRepository executionRepository,
            ITaskRepository taskRepository,
            IHouseholdService householdService,
            IFileUploadService fileUploadService,
            IMapper mapper,
            ILogger<TaskExecutionService> logger)
        {
            _executionRepository = executionRepository;
            _taskRepository = taskRepository;
            _householdService = householdService;
            _fileUploadService = fileUploadService;
            _mapper = mapper;
            _logger = logger;
        }

        // Execution operations
        public async Task<ExecutionDto> CompleteTaskAsync(
            CompleteTaskRequest request,
            string userId,
            IFormFile? photo = null,
            CancellationToken cancellationToken = default)
        {
            // Validate task access
            var task = await _taskRepository.GetByIdWithRelationsAsync(request.TaskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", request.TaskId);

            // Validate user has access to household
            await _householdService.ValidateUserAccessAsync(task.HouseholdId, userId, cancellationToken);

            // Check if task is already completed this week (for regular tasks)
            // IsTaskCompletedThisWeekAsync now only counts executions with IsCountedForCompletion = true
            if (task.Type == Domain.Enums.TaskType.Regular)
            {
                var isCompletedThisWeek = await _executionRepository.IsTaskCompletedThisWeekAsync(request.TaskId, cancellationToken);
                if (isCompletedThisWeek)
                    throw new ValidationException("TaskId", "This task has already been completed this week");
            }

            // Upload photo if provided
            string? photoPath = request.PhotoPath;
            if (photo != null)
            {
                photoPath = await _fileUploadService.UploadExecutionPhotoAsync(photo, cancellationToken);
            }

            // Create execution with denormalized fields
            // IsCountedForCompletion defaults to true
            var execution = await _executionRepository.CreateExecutionAsync(
                request.TaskId,
                userId,
                request.Notes,
                photoPath,
                cancellationToken);

            // Deactivate one-time tasks
            if (task.Type == Domain.Enums.TaskType.OneTime)
            {
                task.IsActive = false;
                await _taskRepository.UpdateAsync(task, cancellationToken);
            }

            _logger.LogInformation("Completed task {TaskId} by user {UserId}", request.TaskId, userId);

            // Load with relations for DTO
            execution = await _executionRepository.GetByIdWithRelationsAsync(execution.Id, cancellationToken);

            return _mapper.Map<ExecutionDto>(execution);
        }

        public async Task<ExecutionDto?> GetExecutionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var execution = await _executionRepository.GetByIdWithRelationsAsync(id, cancellationToken);
            return execution == null ? null : _mapper.Map<ExecutionDto>(execution);
        }

        public async Task<ExecutionDto?> GetExecutionWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var execution = await _executionRepository.GetByIdWithRelationsAsync(id, cancellationToken);
            return execution == null ? null : _mapper.Map<ExecutionDto>(execution);
        }

        public async Task<ExecutionDto> UpdateExecutionAsync(
             Guid id,
             UpdateExecutionRequest request,
             string requestingUserId,
             CancellationToken cancellationToken = default)
        {
            await ValidateExecutionAccessAsync(id, requestingUserId, cancellationToken);

            var execution = await _executionRepository.GetByIdAsync(id, cancellationToken);
            if (execution == null)
                throw new NotFoundException("Execution", id);

            // Only update allowed fields
            execution.Notes = request.Notes ?? execution.Notes;
            execution.PhotoPath = request.PhotoPath ?? execution.PhotoPath;

            await _executionRepository.UpdateAsync(execution, cancellationToken);

            _logger.LogInformation("Updated execution {ExecutionId}", execution.Id);

            // Reload with relations
            execution = await _executionRepository.GetByIdWithRelationsAsync(id, cancellationToken);

            return _mapper.Map<ExecutionDto>(execution);
        }

        public async Task DeleteExecutionAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            var execution = await _executionRepository.GetByIdAsync(id, cancellationToken);
            if (execution == null)
                throw new NotFoundException("Execution", id);

            // Only allow deletion by the user who completed it or household owner
            var isOwner = await _householdService.IsUserOwnerAsync(execution.HouseholdId, requestingUserId, cancellationToken);
            if (execution.UserId != requestingUserId && !isOwner)
                throw new ForbiddenException("You can only delete your own executions or must be a household owner");

            // Delete photo if exists
            if (!string.IsNullOrEmpty(execution.PhotoPath))
            {
                await _fileUploadService.DeleteFileAsync(execution.PhotoPath, cancellationToken);
            }

            await _executionRepository.DeleteAsync(execution, cancellationToken);
            _logger.LogInformation("Deleted execution {ExecutionId}", id);
        }

        // Query operations
        public async Task<IReadOnlyList<ExecutionDto>> GetTaskExecutionsAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var executions = await _executionRepository.GetByTaskIdAsync(taskId, cancellationToken);
            return _mapper.Map<IReadOnlyList<ExecutionDto>>(executions);
        }

        public async Task<IReadOnlyList<ExecutionDto>> GetHouseholdExecutionsAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var executions = await _executionRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<ExecutionDto>>(executions);
        }

        public async Task<IReadOnlyList<ExecutionDto>> GetUserExecutionsAsync(
            string userId,
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var executions = await _executionRepository.GetUserExecutionsThisWeekAsync(userId, householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<ExecutionDto>>(executions);
        }

        public async Task<IReadOnlyList<ExecutionDto>> GetWeeklyExecutionsAsync(
            Guid householdId,
            DateTime? weekStarting = null,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TaskExecution> executions;

            if (weekStarting.HasValue)
            {
                var weekEnd = weekStarting.Value.AddDays(7);
                executions = await _executionRepository.GetByDateRangeAsync(
                    householdId, weekStarting.Value, weekEnd, cancellationToken);
            }
            else
            {
                executions = await _executionRepository.GetThisWeekAsync(householdId, cancellationToken);
            }

            return _mapper.Map<IReadOnlyList<ExecutionDto>>(executions);
        }

        // Status checking
        public async Task<bool> IsTaskCompletedThisWeekAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await _executionRepository.IsTaskCompletedThisWeekAsync(taskId, cancellationToken);
        }

        public async Task<ExecutionDto?> GetLatestExecutionForTaskAsync(
            Guid taskId,
            CancellationToken cancellationToken = default)
        {
            var execution = await _executionRepository.GetLatestExecutionForTaskAsync(taskId, cancellationToken);
            return execution == null ? null : _mapper.Map<ExecutionDto>(execution);
        }

        // Photo management
        public async Task<string> UploadExecutionPhotoAsync(Guid executionId, IFormFile photo, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateExecutionAccessAsync(executionId, requestingUserId, cancellationToken);

            var execution = await _executionRepository.GetByIdAsync(executionId, cancellationToken);
            if (execution == null)
                throw new NotFoundException("Execution", executionId);

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
                throw new NotFoundException("Execution", executionId);

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
                throw new NotFoundException("Execution", executionId);

            // Check if user has access to household
            await _householdService.ValidateUserAccessAsync(execution.HouseholdId, userId, cancellationToken);

            // For editing/deleting, user must be the creator or household owner
            var isOwner = await _householdService.IsUserOwnerAsync(execution.HouseholdId, userId, cancellationToken);
            if (execution.UserId != userId && !isOwner)
                throw new ForbiddenException("You can only access your own executions or must be a household owner");
        }

        // Reset operations
        /// <summary>
        /// Invalidates (uncounts) this week's execution for a Regular task.
        /// Sets IsCountedForCompletion = false, preserving history but allowing recompletion.
        /// </summary>
        public async Task InvalidateExecutionThisWeekAsync(Guid taskId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            // Get task
            var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", taskId);

            // Validate user is household owner
            await _householdService.ValidateOwnerAccessAsync(task.HouseholdId, requestingUserId, cancellationToken);

            // Validate task is Regular type
            if (task.Type != Domain.Enums.TaskType.Regular)
                throw new ValidationException("TaskId", "Only Regular tasks can be reset. OneTime tasks cannot be reset.");

            // Get this week's executions
            // NULL is treated as true (counted), so we filter for NULL or true
            var weekStart = TaskExecution.GetWeekStarting(DateTime.UtcNow);
            var executions = await _executionRepository.GetByTaskIdAsync(taskId, cancellationToken);
            var thisWeekExecutions = executions
                .Where(e => e.WeekStarting == weekStart && 
                           (e.IsCountedForCompletion == null || e.IsCountedForCompletion == true))
                .ToList();

            if (!thisWeekExecutions.Any())
            {
                _logger.LogInformation("No counted execution found for task {TaskId} this week, nothing to invalidate", taskId);
                throw new ValidationException("TaskId", "This task has no counted execution this week to invalidate.");
            }

            // Invalidate all this week's executions (set IsCountedForCompletion = false)
            foreach (var execution in thisWeekExecutions)
            {
                execution.IsCountedForCompletion = false;
                await _executionRepository.UpdateAsync(execution, cancellationToken);
            }

            _logger.LogInformation(
                "Owner {UserId} invalidated {Count} execution(s) for task {TaskId} this week (history preserved, task can be recompleted)",
                requestingUserId, thisWeekExecutions.Count, taskId);
        }
    }
}
