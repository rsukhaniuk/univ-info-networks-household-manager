using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using HouseholdManager.Infrastructure.Data;

namespace HouseholdManager.Infrastructure.Repositories
{
    /// <summary>
    /// Implementation of household repository with EF Core
    /// </summary>
    public class HouseholdRepository : EfRepository<Household>, IHouseholdRepository
    {
        public HouseholdRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<Household>> GetAllWithMembersAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(h => h.Members)
                    .ThenInclude(m => m.User)
                .Include(h => h.Rooms) // Add rooms for completeness
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        // Essential queries with relations
        public async Task<Household?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(h => h.Members)
                    .ThenInclude(m => m.User)
                .Include(h => h.Rooms) // Add rooms to the query
                .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Household>> GetUserHouseholdsAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(h => h.Members)
                .Where(h => h.Members.Any(m => m.UserId == userId))
                .ToListAsync(cancellationToken);
        }

        // Invite code operations
        public async Task<Household?> GetByInviteCodeAsync(Guid inviteCode, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(h => h.Members)
                .FirstOrDefaultAsync(h => h.InviteCode == inviteCode, cancellationToken);
        }

        public async Task<bool> IsInviteCodeUniqueAsync(Guid inviteCode, CancellationToken cancellationToken = default)
        {
            return !await _dbSet.AnyAsync(h => h.InviteCode == inviteCode, cancellationToken);
        }

        // Member management
        public async Task<HouseholdMember> AddMemberAsync(Guid householdId, string userId, HouseholdRole role, CancellationToken cancellationToken = default)
        {
            // Check if user is already a member
            if (await IsUserMemberAsync(householdId, userId, cancellationToken))
                throw new InvalidOperationException("User is already a member of this household");

            var member = new HouseholdMember
            {
                HouseholdId = householdId,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };

            await _dbContext.HouseholdMembers.AddAsync(member, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return member;
        }

        public async Task RemoveMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            var member = await _dbContext.HouseholdMembers
                .FirstOrDefaultAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId, cancellationToken);

            if (member == null)
                throw new InvalidOperationException("User is not a member of this household");

            // Check if this is the last owner
            var ownerCount = await _dbContext.HouseholdMembers
                .CountAsync(hm => hm.HouseholdId == householdId && hm.Role == HouseholdRole.Owner, cancellationToken);

            if (member.Role == HouseholdRole.Owner && ownerCount <= 1)
                throw new InvalidOperationException("Cannot remove the last owner of the household");

            _dbContext.HouseholdMembers.Remove(member);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> IsUserMemberAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.HouseholdMembers
                .AnyAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId, cancellationToken);
        }

        public async Task<HouseholdRole?> GetUserRoleAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            var member = await _dbContext.HouseholdMembers
                .FirstOrDefaultAsync(hm => hm.HouseholdId == householdId && hm.UserId == userId, cancellationToken);

            return member?.Role;
        }
    }
}
