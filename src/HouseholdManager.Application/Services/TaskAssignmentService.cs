using HouseholdManager.Application.Helpers;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// Implementation of task assignment service with round-robin algorithm
    /// </summary>
    public class TaskAssignmentService : ITaskAssignmentService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly ILogger<TaskAssignmentService> _logger;

        public TaskAssignmentService(
            ITaskRepository taskRepository,
            IHouseholdMemberRepository memberRepository,
            ILogger<TaskAssignmentService> logger)
        {
            _taskRepository = taskRepository;
            _memberRepository = memberRepository;
            _logger = logger;
        }

        public async Task<string> AssignTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdWithRelationsAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", taskId);

            var suggestedUserId = await GetSuggestedAssigneeAsync(taskId, cancellationToken);
            if (suggestedUserId == null)
                throw new ValidationException("No available users to assign the task");

            await _taskRepository.AssignTaskAsync(taskId, suggestedUserId, cancellationToken);

            _logger.LogInformation("Assigned task {TaskId} to user {UserId}", taskId, suggestedUserId);
            return suggestedUserId;
        }

        public async Task<Dictionary<Guid, string>> AutoAssignAllTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var unassignedTasks = await _taskRepository.GetUnassignedTasksAsync(householdId, cancellationToken);
            var activeMembers = await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);

            if (!activeMembers.Any())
            {
                _logger.LogWarning("No active members found in household {HouseholdId}", householdId);
                return new Dictionary<Guid, string>();
            }

            var assignments = new Dictionary<Guid, string>();
            var memberUserIds = activeMembers.Select(m => m.UserId).ToList();

            // Get current workload for fair distribution
            var workloadStats = await GetWorkloadStatsAsync(householdId, cancellationToken);

            // Get all active tasks for conflict checking
            var activeTasks = await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);
            // Include unassigned tasks we're about to process as well, so conflicts among new assignments are detected
            var allRelevantTasks = activeTasks.Concat(unassignedTasks).ToList();

            // Sort members by current workload (ascending)
            var sortedMembers = memberUserIds
                .OrderBy(userId => workloadStats.GetValueOrDefault(userId, 0))
                .ToList();

            var memberIndex = 0;
            // Process higher priority tasks first so that in conflict situations they win
            foreach (var task in unassignedTasks
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt))
            {
                string? assignedUserId = null;

                // Try to find a user without time conflict
                for (int i = 0; i < sortedMembers.Count; i++)
                {
                    var candidateUserId = sortedMembers[(memberIndex + i) % sortedMembers.Count];
                    
                    if (!HasTimeConflict(allRelevantTasks, task, candidateUserId, assignments))
                    {
                        assignedUserId = candidateUserId;
                        break;
                    }
                }

                // If no user without conflict found, SKIP assignment for this task (keep unassigned)
                if (assignedUserId == null)
                {
                    _logger.LogWarning("Task {TaskId} skipped auto-assign due to time conflicts with all members", task.Id);
                    memberIndex++;
                    continue;
                }

                assignments[task.Id] = assignedUserId;

                // Update workload counter for next iteration
                workloadStats[assignedUserId] = workloadStats.GetValueOrDefault(assignedUserId, 0) + 1;

                memberIndex++;
            }

            // Bulk assign tasks
            await _taskRepository.BulkAssignTasksAsync(assignments, cancellationToken);

            _logger.LogInformation("Auto-assigned {Count} tasks in household {HouseholdId}",
                assignments.Count, householdId);

            return assignments;
        }

        public async Task<Dictionary<Guid, string>> PreviewAutoAssignAllTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var unassignedTasks = await _taskRepository.GetUnassignedTasksAsync(householdId, cancellationToken);
            var activeMembers = await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);

            if (!activeMembers.Any())
            {
                _logger.LogWarning("No active members found in household {HouseholdId}", householdId);
                return new Dictionary<Guid, string>();
            }

            var assignments = new Dictionary<Guid, string>();
            var memberUserIds = activeMembers.Select(m => m.UserId).ToList();

            // Get current workload for fair distribution
            var workloadStats = await GetWorkloadStatsAsync(householdId, cancellationToken);

            // Get all active tasks for conflict checking
            var activeTasks = await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);
            // Include unassigned tasks we're about to process as well, so conflicts among new assignments are detected
            var allRelevantTasks = activeTasks.Concat(unassignedTasks).ToList();

            // Sort members by current workload (ascending)
            var sortedMembers = memberUserIds
                .OrderBy(userId => workloadStats.GetValueOrDefault(userId, 0))
                .ToList();

            var memberIndex = 0;
            // Process higher priority tasks first so preview reflects that they win in conflicts
            foreach (var task in unassignedTasks
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt))
            {
                string? assignedUserId = null;

                // Try to find a user without time conflict
                for (int i = 0; i < sortedMembers.Count; i++)
                {
                    var candidateUserId = sortedMembers[(memberIndex + i) % sortedMembers.Count];
                    
                    if (!HasTimeConflict(allRelevantTasks, task, candidateUserId, assignments))
                    {
                        assignedUserId = candidateUserId;
                        break;
                    }
                }

                // If no user without conflict found, mark as unassigned in preview (omit from result)
                if (assignedUserId == null)
                {
                    _logger.LogWarning("Task {TaskId} would be skipped in preview due to time conflicts with all members", task.Id);
                    memberIndex++;
                    continue;
                }

                assignments[task.Id] = assignedUserId;

                // Update workload counter for next iteration
                workloadStats[assignedUserId] = workloadStats.GetValueOrDefault(assignedUserId, 0) + 1;

                memberIndex++;
            }

            // DO NOT save to database - this is preview only
            _logger.LogInformation("Previewed auto-assignment of {Count} tasks in household {HouseholdId}",
                assignments.Count, householdId);

            return assignments;
        }

        public async Task<Dictionary<Guid, string>> AutoAssignWeeklyTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var assignments = new Dictionary<Guid, string>();
            var activeMembers = await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);

            if (!activeMembers.Any())
            {
                _logger.LogWarning("No active members found in household {HouseholdId}", householdId);
                return assignments;
            }

            var memberUserIds = activeMembers.Select(m => m.UserId).ToList();
            var workloadStats = await GetWorkloadStatsAsync(householdId, cancellationToken);

            // NOTE: Auto-assignment only works for Regular tasks with FREQ=WEEKLY recurrence rules.
            // Tasks with other recurrence patterns (DAILY, MONTHLY, etc.) cannot be auto-assigned by weekday.
            var allTasks = await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);
            var regularTasks = allTasks.Where(t => t.Type == TaskType.Regular && t.AssignedUserId == null).ToList();

            // Group tasks by weekday using RecurrenceRule (only weekly tasks with BYDAY)
            var tasksByWeekday = RruleHelper.GroupTasksByWeekday(regularTasks);

            foreach (var (day, dayTasks) in tasksByWeekday)
            {
                if (!dayTasks.Any())
                    continue;

                // Sort members by current workload for this day
                var sortedMembers = memberUserIds
                    .OrderBy(userId => workloadStats.GetValueOrDefault(userId, 0))
                    .ToList();

                var memberIndex = 0;
                foreach (var task in dayTasks.OrderBy(t => t.Priority))
                {
                    var assignedUserId = sortedMembers[memberIndex % sortedMembers.Count];
                    assignments[task.Id] = assignedUserId;

                    // Update workload counter
                    workloadStats[assignedUserId] = workloadStats.GetValueOrDefault(assignedUserId, 0) + 1;
                    memberIndex++;
                }
            }

            // Bulk assign tasks
            if (assignments.Any())
            {
                await _taskRepository.BulkAssignTasksAsync(assignments, cancellationToken);
                _logger.LogInformation("Auto-assigned {Count} weekly tasks in household {HouseholdId}",
                    assignments.Count, householdId);
            }

            return assignments;
        }

        public async Task<string> ReassignTaskToNextUserAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdWithRelationsAsync(taskId, cancellationToken);
            if (task == null)
                throw new NotFoundException("Task", taskId);

            var activeMembers = await _memberRepository.GetByHouseholdIdAsync(task.HouseholdId, cancellationToken);
            var memberUserIds = activeMembers.Select(m => m.UserId).ToList();

            if (!memberUserIds.Any())
                throw new ValidationException("No active members in household");

            if (memberUserIds.Count == 1)
            {
                // Only one member, assign to them
                var singleUserId = memberUserIds.First();
                await _taskRepository.AssignTaskAsync(taskId, singleUserId, cancellationToken);
                return singleUserId;
            }

            // Find current user index and move to next
            var currentIndex = task.AssignedUserId != null
                ? memberUserIds.IndexOf(task.AssignedUserId)
                : -1;

            var nextIndex = (currentIndex + 1) % memberUserIds.Count;
            var nextUserId = memberUserIds[nextIndex];

            await _taskRepository.AssignTaskAsync(taskId, nextUserId, cancellationToken);

            _logger.LogInformation("Reassigned task {TaskId} from {OldUser} to {NewUser}",
                taskId, task.AssignedUserId, nextUserId);

            return nextUserId;
        }

        public async Task<string?> GetSuggestedAssigneeAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdWithRelationsAsync(taskId, cancellationToken);
            if (task == null)
                return null;

            var activeMembers = await _memberRepository.GetByHouseholdIdAsync(task.HouseholdId, cancellationToken);
            var memberUserIds = activeMembers.Select(m => m.UserId).ToList();

            if (!memberUserIds.Any())
                return null;

            // Get current workload and suggest user with least tasks
            var workloadStats = await GetWorkloadStatsAsync(task.HouseholdId, cancellationToken);

            return memberUserIds
                .OrderBy(userId => workloadStats.GetValueOrDefault(userId, 0))
                .First();
        }

        public async Task<Dictionary<string, int>> GetWorkloadStatsAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var activeMembers = await _memberRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
            var memberUserIds = activeMembers.Select(m => m.UserId).ToList();

            // Initialize all members with 0 tasks
            var workloadStats = memberUserIds.ToDictionary(userId => userId, _ => 0);

            // Get active tasks assigned to users
            var activeTasks = await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);

            foreach (var task in activeTasks.Where(t => !string.IsNullOrEmpty(t.AssignedUserId)))
            {
                if (workloadStats.ContainsKey(task.AssignedUserId!))
                {
                    workloadStats[task.AssignedUserId!]++;
                }
            }

            return workloadStats;
        }

        /// <summary>
        /// Check if assigning a task to a user would create a time conflict
        /// Only applies to OneTime tasks with DueDate
        /// <summary>
        /// Check if assigning a task to a user would create a time conflict
        /// Only applies to OneTime tasks with DueDate
        /// </summary>
        private bool HasTimeConflict(
            IReadOnlyList<HouseholdTask> existingTasks,
            HouseholdTask newTask,
            string userId,
            Dictionary<Guid, string> pendingAssignments)
        {
            // Only check conflicts for OneTime tasks with DueDate
            if (newTask.Type != TaskType.OneTime || !newTask.DueDate.HasValue)
                return false;

            var newStart = newTask.DueDate.Value;
            var newDuration = Math.Max(1, newTask.EstimatedMinutes); // enforce minimal duration to detect conflicts
            var newEnd = newStart.AddMinutes(newDuration);

            // Check existing assigned tasks
            var userTasks = existingTasks.Where(t =>
                t.AssignedUserId == userId &&
                t.Type == TaskType.OneTime &&
                t.DueDate.HasValue);

            foreach (var existing in userTasks)
            {
                var existingStart = existing.DueDate.Value;
                var existingDuration = Math.Max(1, existing.EstimatedMinutes);
                var existingEnd = existingStart.AddMinutes(existingDuration);

                // Check if time intervals overlap
                if (newStart < existingEnd && newEnd > existingStart)
                {
                    _logger.LogDebug(
                        "Time conflict detected: Task {NewTaskId} ({NewStart}-{NewEnd}) conflicts with {ExistingTaskId} ({ExistingStart}-{ExistingEnd}) for user {UserId}",
                        newTask.Id, newStart, newEnd, existing.Id, existingStart, existingEnd, userId);
                    return true;
                }
            }

            // Check pending assignments from current auto-assign operation
            var pendingTasksForUser = pendingAssignments
                .Where(kv => kv.Value == userId)
                .Select(kv => existingTasks.FirstOrDefault(t => t.Id == kv.Key))
                .Where(t => t != null && t.Type == TaskType.OneTime && t.DueDate.HasValue);

            foreach (var pending in pendingTasksForUser)
            {
                if (pending == null) continue;

                var pendingStart = pending.DueDate!.Value;
                var pendingDuration = Math.Max(1, pending.EstimatedMinutes);
                var pendingEnd = pendingStart.AddMinutes(pendingDuration);

                // Check if time intervals overlap
                if (newStart < pendingEnd && newEnd > pendingStart)
                {
                    _logger.LogDebug(
                        "Time conflict detected with pending assignment: Task {NewTaskId} ({NewStart}-{NewEnd}) conflicts with pending {PendingTaskId} ({PendingStart}-{PendingEnd}) for user {UserId}",
                        newTask.Id, newStart, newEnd, pending.Id, pendingStart, pendingEnd, userId);
                    return true;
                }
            }

            return false;
        }
    }
}
