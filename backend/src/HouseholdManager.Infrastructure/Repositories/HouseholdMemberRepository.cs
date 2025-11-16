using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using HouseholdManager.Infrastructure.Data;

namespace HouseholdManager.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of household member repository with EF Core
    /// </summary>
    public class HouseholdMemberRepository : EfRepository<HouseholdMember>, IHouseholdMemberRepository
    {
        public HouseholdMemberRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        // Member queries
        public async Task<IReadOnlyList<HouseholdMember>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(hm => hm.User)
                .Where(hm => hm.HouseholdId == householdId)
                .OrderBy(hm => hm.Role)
                .ThenBy(hm => hm.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdMember>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(hm => hm.Household)
                .Where(hm => hm.UserId == userId)
                .OrderBy(hm => hm.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<HouseholdMember?> GetMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(hm => hm.User)
                .Include(hm => hm.Household)
                .FirstOrDefaultAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId, cancellationToken);
        }

        // Role-based queries
        public async Task<IReadOnlyList<HouseholdMember>> GetMembersByRoleAsync(Guid householdId, HouseholdRole role, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(hm => hm.User)
                .Where(hm => hm.HouseholdId == householdId && hm.Role == role)
                .OrderBy(hm => hm.JoinedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<HouseholdMember>> GetOwnersAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await GetMembersByRoleAsync(householdId, HouseholdRole.Owner, cancellationToken);
        }

        // Role management
        public async Task UpdateRoleAsync(Guid householdId, string userId, HouseholdRole newRole, CancellationToken cancellationToken = default)
        {
            var member = await GetMemberAsync(householdId, userId, cancellationToken);
            if (member == null)
                throw new InvalidOperationException("User is not a member of this household");

            // If demoting from owner, check if there will be at least one owner left
            if (member.Role == HouseholdRole.Owner && newRole != HouseholdRole.Owner)
            {
                var ownerCount = await GetOwnerCountAsync(householdId, cancellationToken);
                if (ownerCount <= 1)
                    throw new InvalidOperationException("Cannot demote the last owner of the household");
            }

            member.Role = newRole;
            await UpdateAsync(member, cancellationToken);
        }

        public async Task<bool> IsUserMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId, cancellationToken);
        }

        public async Task<HouseholdRole?> GetUserRoleAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            var member = await _dbSet
                .FirstOrDefaultAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId, cancellationToken);

            return member?.Role;
        }

        // Statistics
        public async Task<int> GetMemberCountAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(hm => hm.HouseholdId == householdId, cancellationToken);
        }

        public async Task<int> GetOwnerCountAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .CountAsync(hm => hm.HouseholdId == householdId && hm.Role == HouseholdRole.Owner, cancellationToken);
        }
    }
}
