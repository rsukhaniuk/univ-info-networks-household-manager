using HouseholdManager.Data;
using HouseholdManager.Models;
using HouseholdManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HouseholdManager.Repositories.Implementations
{
    /// <summary>
    /// Implementation of room repository with EF Core
    /// </summary>
    public class RoomRepository : EfRepository<Room>, IRoomRepository
    {
        public RoomRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        // Basic household-scoped operations
        public async Task<IReadOnlyList<Room>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.HouseholdId == householdId)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<Room?> GetByIdWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(r => r.Tasks.Where(t => t.IsActive))
                    .ThenInclude(t => t.AssignedUser)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        // Validation
        public async Task<bool> ExistsInHouseholdAsync(Guid roomId, Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(r => r.Id == roomId && r.HouseholdId == householdId, cancellationToken);
        }

        public async Task<bool> IsNameUniqueInHouseholdAsync(string name, Guid householdId, Guid? excludeRoomId = null, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(r => r.HouseholdId == householdId && r.Name.ToLower() == name.ToLower());

            if (excludeRoomId.HasValue)
            {
                query = query.Where(r => r.Id != excludeRoomId.Value);
            }

            return !await query.AnyAsync(cancellationToken);
        }
    }

}
