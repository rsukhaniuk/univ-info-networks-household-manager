using HouseholdManager.Domain.Entities;
using HouseholdManager.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using HouseholdManager.Infrastructure.Data;

namespace HouseholdManager.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of execution repository with EF Core
    /// </summary>
    public class ExecutionRepository : EfRepository<TaskExecution>, IExecutionRepository
    {
        public ExecutionRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        // Basic queries with relations
        public async Task<TaskExecution?> GetByIdWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(te => te.Task)
                    .ThenInclude(t => t.Room)
                .Include(te => te.User)
                .FirstOrDefaultAsync(te => te.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<TaskExecution>> GetByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(te => te.Task)
                    .ThenInclude(t => t.Room)
                .Include(te => te.User)
                .Where(te => te.TaskId == taskId)
                .OrderByDescending(te => te.CompletedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TaskExecution>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(te => te.Task)
                    .ThenInclude(t => t.Room)
                .Include(te => te.User)
                .Where(te => te.HouseholdId == householdId)
                .OrderByDescending(te => te.CompletedAt)
                .ToListAsync(cancellationToken);
        }

        // Time-based queries
        public async Task<IReadOnlyList<TaskExecution>> GetThisWeekAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            var weekStart = TaskExecution.GetWeekStarting(DateTime.UtcNow);

            return await _dbSet
                .Include(te => te.Task)
                    .ThenInclude(t => t.Room)
                .Include(te => te.User)
                .Where(te => te.HouseholdId == householdId && te.WeekStarting == weekStart)
                .OrderByDescending(te => te.CompletedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<TaskExecution>> GetByDateRangeAsync(Guid householdId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(te => te.Task)
                    .ThenInclude(t => t.Room)
                .Include(te => te.User)
                .Where(te => te.HouseholdId == householdId &&
                           te.CompletedAt >= fromDate &&
                           te.CompletedAt <= toDate)
                .OrderByDescending(te => te.CompletedAt)
                .ToListAsync(cancellationToken);
        }

        // User-specific queries
        public async Task<IReadOnlyList<TaskExecution>> GetUserExecutionsThisWeekAsync(string userId, Guid householdId, CancellationToken cancellationToken = default)
        {
            var weekStart = TaskExecution.GetWeekStarting(DateTime.UtcNow);

            return await _dbSet
                .Include(te => te.Task)
                    .ThenInclude(t => t.Room)
                .Include(te => te.User)
                .Where(te => te.UserId == userId &&
                           te.HouseholdId == householdId &&
                           te.WeekStarting == weekStart)
                .OrderByDescending(te => te.CompletedAt)
                .ToListAsync(cancellationToken);
        }

        // Task completion tracking
        public async Task<bool> IsTaskCompletedThisWeekAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            var weekStart = TaskExecution.GetWeekStarting(DateTime.UtcNow);

            // Only count executions that are marked as counted for completion
            // NULL is treated as true (for backward compatibility)
            return await _dbSet
                .AnyAsync(te => te.TaskId == taskId && 
                              te.WeekStarting == weekStart && 
                              (te.IsCountedForCompletion == null || te.IsCountedForCompletion == true), 
                          cancellationToken);
        }

        public async Task<TaskExecution?> GetLatestExecutionForTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(te => te.Task)
                    .ThenInclude(t => t.Room)
                .Include(te => te.User)
                .Where(te => te.TaskId == taskId)
                .OrderByDescending(te => te.CompletedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Execution creation with denormalized fields
        public async Task<TaskExecution> CreateExecutionAsync(Guid taskId, string userId, string? notes = null, string? photoPath = null, CancellationToken cancellationToken = default)
        {
            // Get task with related entities to populate denormalized fields
            var task = await _dbContext.HouseholdTasks
                .Include(t => t.Room)
                .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

            if (task == null)
                throw new InvalidOperationException($"Task with ID {taskId} not found");

            var completedAt = DateTime.UtcNow;
            var execution = new TaskExecution
            {
                TaskId = taskId,
                UserId = userId,
                CompletedAt = completedAt,
                Notes = notes,
                PhotoPath = photoPath,
                WeekStarting = TaskExecution.GetWeekStarting(completedAt),
                HouseholdId = task.HouseholdId, // Denormalized
                RoomId = task.RoomId            // Denormalized
            };

            return await AddAsync(execution, cancellationToken);
        }
    }
}
