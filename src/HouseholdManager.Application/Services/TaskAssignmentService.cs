using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
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
                throw new InvalidOperationException($"Task with ID {taskId} not found");

            var suggestedUserId = await GetSuggestedAssigneeAsync(taskId, cancellationToken);
            if (suggestedUserId == null)
                throw new InvalidOperationException("No available users to assign the task");

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

            // Sort members by current workload (ascending)
            var sortedMembers = memberUserIds
                .OrderBy(userId => workloadStats.GetValueOrDefault(userId, 0))
                .ToList();

            var memberIndex = 0;
            foreach (var task in unassignedTasks.OrderBy(t => t.Priority).ThenBy(t => t.CreatedAt))
            {
                var assignedUserId = sortedMembers[memberIndex % sortedMembers.Count];
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

            // Sort members by current workload (ascending)
            var sortedMembers = memberUserIds
                .OrderBy(userId => workloadStats.GetValueOrDefault(userId, 0))
                .ToList();

            var memberIndex = 0;
            foreach (var task in unassignedTasks.OrderBy(t => t.Priority).ThenBy(t => t.CreatedAt))
            {
                var assignedUserId = sortedMembers[memberIndex % sortedMembers.Count];
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

            var daysOfWeek = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            foreach (var day in daysOfWeek)
            {
                var dayTasks = await _taskRepository.GetRegularTasksByWeekdayAsync(householdId, day, cancellationToken);
                var unassignedDayTasks = dayTasks.Where(t => t.AssignedUserId == null).ToList();

                if (!unassignedDayTasks.Any())
                    continue;

                // Sort members by current workload for this day
                var sortedMembers = memberUserIds
                    .OrderBy(userId => workloadStats.GetValueOrDefault(userId, 0))
                    .ToList();

                var memberIndex = 0;
                foreach (var task in unassignedDayTasks.OrderBy(t => t.Priority))
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
                throw new InvalidOperationException($"Task with ID {taskId} not found");

            var activeMembers = await _memberRepository.GetByHouseholdIdAsync(task.HouseholdId, cancellationToken);
            var memberUserIds = activeMembers.Select(m => m.UserId).ToList();

            if (!memberUserIds.Any())
                throw new InvalidOperationException("No active members in household");

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
    }
}
