using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace HouseholdManager.Infrastructure.Services
{
    /// <summary>
    /// Implementation of calendar token service for managing subscription tokens
    /// </summary>
    public class CalendarTokenService : ICalendarTokenService
    {
        private readonly ApplicationDbContext _context;

        public CalendarTokenService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateTokenAsync(
            Guid householdId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            // Check if token already exists for this household/user
            var existingToken = await _context.CalendarSubscriptionTokens
                .FirstOrDefaultAsync(
                    t => t.HouseholdId == householdId && t.UserId == userId && t.IsActive,
                    cancellationToken);

            if (existingToken != null)
            {
                // Return existing token
                return existingToken.Token;
            }

            // Generate new cryptographically secure token
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")  // Make URL-safe
                .Replace("/", "_")  // Make URL-safe
                .Replace("=", "");  // Remove padding

            var subscriptionToken = new CalendarSubscriptionToken
            {
                HouseholdId = householdId,
                UserId = userId,
                Token = token,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = null,  // Never expires by default
                IsActive = true,
                LastAccessedAt = null
            };

            await _context.CalendarSubscriptionTokens.AddAsync(subscriptionToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return token;
        }

        public async Task<(Guid householdId, string userId)?> ValidateTokenAsync(
            string? token,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var subscriptionToken = await _context.CalendarSubscriptionTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.Token == token && t.IsActive,
                    cancellationToken);

            if (subscriptionToken == null)
                return null;

            // Check if token is expired
            if (subscriptionToken.ExpiresAt.HasValue && subscriptionToken.ExpiresAt.Value < DateTime.UtcNow)
                return null;

            // Update LastAccessedAt timestamp (fire and forget, don't await)
            _ = Task.Run(async () =>
            {
                try
                {
                    var tokenToUpdate = await _context.CalendarSubscriptionTokens
                        .FirstOrDefaultAsync(t => t.Id == subscriptionToken.Id, cancellationToken);

                    if (tokenToUpdate != null)
                    {
                        tokenToUpdate.LastAccessedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }
                catch
                {
                    // Ignore errors in background update
                }
            }, cancellationToken);

            return (subscriptionToken.HouseholdId, subscriptionToken.UserId);
        }

        public async Task<bool> RevokeTokenAsync(
            Guid householdId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var token = await _context.CalendarSubscriptionTokens
                .FirstOrDefaultAsync(
                    t => t.HouseholdId == householdId && t.UserId == userId && t.IsActive,
                    cancellationToken);

            if (token == null)
                return false;

            token.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
