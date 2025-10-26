using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for user management with Auth0 integration
    /// </summary>
    public class UserRepository : EfRepository<ApplicationUser>, IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // Basic queries

        public async Task<ApplicationUser?> GetByIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.HouseholdMemberships)
                    .ThenInclude(hm => hm.Household)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.HouseholdMemberships)
                    .ThenInclude(hm => hm.Household)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationUser>> GetAllUsersAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.HouseholdMemberships)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationUser>> GetHouseholdUsersAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Where(u => u.HouseholdMemberships.Any(hm => hm.HouseholdId == householdId))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ApplicationUser>> SearchUsersAsync(
            string searchTerm,
            CancellationToken cancellationToken = default)
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.Users
                .Where(u =>
                    u.Email.ToLower().Contains(lowerSearchTerm) ||
                    (u.FirstName != null && u.FirstName.ToLower().Contains(lowerSearchTerm)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(lowerSearchTerm)))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync(cancellationToken);
        }

        // User management (Upsert for Auth0 sync)

        public async Task<ApplicationUser> UpsertUserAsync(
    string userId,
    string email,
    string? firstName = null,
    string? lastName = null,
    string? profilePictureUrl = null,
    CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                // Create new user
                user = new ApplicationUser
                {
                    Id = userId,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    ProfilePictureUrl = profilePictureUrl,
                    CreatedAt = DateTime.UtcNow,
                    Role = SystemRole.User
                };
                await _context.Users.AddAsync(user, cancellationToken);
            }
            else
            {
                // Update existing user ONLY if data changed
                bool hasChanges = false;

                if (user.Email != email)
                {
                    user.Email = email;
                    hasChanges = true;
                }

                if (user.FirstName != firstName)
                {
                    user.FirstName = firstName;
                    hasChanges = true;
                }

                if (user.LastName != lastName)
                {
                    user.LastName = lastName;
                    hasChanges = true;
                }

                if (user.ProfilePictureUrl != profilePictureUrl)
                {
                    user.ProfilePictureUrl = profilePictureUrl;
                    hasChanges = true;
                }

                // Only call Update if something actually changed
                if (hasChanges)
                {
                    _context.Users.Update(user);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task UpdateProfileAsync(
            string userId,
            string? firstName,
            string? lastName,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User with ID '{userId}' not found");

            user.FirstName = firstName;
            user.LastName = lastName;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SetCurrentHouseholdAsync(
            string userId,
            Guid? householdId,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User with ID '{userId}' not found");

            user.CurrentHouseholdId = householdId;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // System role management

        public async Task SetSystemRoleAsync(
            string userId,
            SystemRole role,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new KeyNotFoundException($"User with ID '{userId}' not found");

            user.Role = role;

            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Validation helpers

        public async Task<bool> ExistsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == userId, cancellationToken);
        }

        public async Task<bool> IsSystemAdminAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == userId && u.Role == SystemRole.SystemAdmin,
                    cancellationToken);
        }
    }
}
