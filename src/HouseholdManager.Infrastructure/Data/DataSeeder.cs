using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Infrastructure.Data
{
    /// <summary>
    /// Seeds initial data into the database
    /// </summary>
    public class DataSeeder
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            ApplicationDbContext dbcontext,
            IConfiguration configuration,
            ILogger<DataSeeder> logger)
        {
            _dbContext = dbcontext;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Seeds all required data
        /// </summary>
        public async Task SeedAsync()
        {
            try
            {
                // Ensure database is created
                await _dbContext.Database.EnsureCreatedAsync();

                // Seed admin user if configured
                await SeedAdminUserAsync();

                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        /// <summary>
        /// Seeds system administrator user from configuration
        /// </summary>
        private async Task SeedAdminUserAsync()
        {
            var adminEmail = _configuration["AdminUser:Email"];
            var adminAuth0Id = _configuration["AdminUser:Auth0Id"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminAuth0Id))
            {
                _logger.LogWarning("Admin user configuration is missing. Skipping admin user seeding.");
                return;
            }

            // Check if admin user already exists
            var existingAdmin = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == adminEmail || u.Id == adminAuth0Id);

            if (existingAdmin != null)
            {
                _logger.LogInformation("Admin user already exists. Skipping seeding.");
                return;
            }

            // Create admin user
            var adminUser = new ApplicationUser
            {
                Id = adminAuth0Id,
                Email = adminEmail,
                FirstName = _configuration["AdminUser:FirstName"] ?? "System",
                LastName = _configuration["AdminUser:LastName"] ?? "Administrator",
                Role = SystemRole.SystemAdmin,
                CreatedAt = DateTime.UtcNow,
                ProfilePictureUrl = null,
                CurrentHouseholdId = null
            };

            _dbContext.Users.Add(adminUser);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Admin user seeded successfully: {Email}", adminEmail);
        }
    }
}
