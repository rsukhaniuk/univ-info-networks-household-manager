using HouseholdManager.Data;
using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HouseholdManager.Repositories.Implementations
{
    /// <summary>
    /// Implementation of task repository with EF Core
    /// </summary>
    public class TaskRepository : EfRepository<HouseholdTask>, ITaskRepository
    {
        public TaskRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        // Basic queries with relations
        public async Task<HouseholdTask?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Room)
                .Include(t => t.AssignedUser)
                .Include(t => t.Household)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Room)
                .Include(t => t.AssignedUser)
                .Where(t => t.HouseholdId == householdId)
                .OrderBy(t => t.Room.Name)
                .ThenBy(t => t.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetActiveByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Room)
                .Include(t => t.AssignedUser)
                .Where(t => t.HouseholdId == householdId && t.IsActive)
                .OrderBy(t => t.Room.Name)
                .ThenBy(t => t.Title)
                .ToListAsync(cancellationToken);
        }

        // Basic filtering
        public async Task<IReadOnlyList<HouseholdTask>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.AssignedUser)
                .Where(t => t.RoomId == roomId && t.IsActive)
                .OrderBy(t => t.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetByAssignedUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Room)
                .Include(t => t.Household)
                .Where(t => t.AssignedUserId == userId && t.IsActive)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetUnassignedTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Room)
                .Where(t => t.HouseholdId == householdId && t.AssignedUserId == null && t.IsActive)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.Room.Name)
                .ToListAsync(cancellationToken);
        }

        // Assignment operations
        public async Task AssignTaskAsync(Guid taskId, string userId, CancellationToken cancellationToken = default)
        {
            var task = await GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException($"Task with ID {taskId} not found");

            task.AssignedUserId = userId;
            await UpdateAsync(task, cancellationToken);
        }

        public async Task UnassignTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var task = await GetByIdAsync(taskId, cancellationToken);
            if (task == null)
                throw new InvalidOperationException($"Task with ID {taskId} not found");

            task.AssignedUserId = null;
            await UpdateAsync(task, cancellationToken);
        }

        // Bulk assignment operation
        public async Task BulkAssignTasksAsync(Dictionary<Guid, string> taskAssignments, CancellationToken cancellationToken = default)
        {
            foreach (var assignment in taskAssignments)
            {
                var task = await GetByIdAsync(assignment.Key, cancellationToken);
                if (task != null)
                {
                    task.AssignedUserId = assignment.Value;
                    _dbContext.Entry(task).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Task type specific queries
        public async Task<IReadOnlyList<HouseholdTask>> GetRegularTasksByWeekdayAsync(Guid householdId, DayOfWeek weekday, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(t => t.Room)
                .Include(t => t.AssignedUser)
                .Where(t => t.HouseholdId == householdId &&
                           t.Type == TaskType.Regular &&
                           t.ScheduledWeekday == weekday &&
                           t.IsActive)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.Room.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdTask>> GetOverdueTasksAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _dbSet
                .Include(t => t.Room)
                .Include(t => t.AssignedUser)
                .Where(t => t.HouseholdId == householdId &&
                           t.Type == TaskType.OneTime &&
                           t.DueDate < now &&
                           t.IsActive)
                .OrderBy(t => t.DueDate)
                .ToListAsync(cancellationToken);
        }
    }
}
