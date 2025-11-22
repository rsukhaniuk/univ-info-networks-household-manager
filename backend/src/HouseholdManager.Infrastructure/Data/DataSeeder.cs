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
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DataSeeder> logger)
        {
            _context = context;
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
                // Apply pending migrations (creates __EFMigrationsHistory table)
                await _context.Database.MigrateAsync();

                // Seed admin user if configured
                await SeedAdminUserAsync();

                // Optionally seed test data in development
                if (_configuration.GetValue<bool>("Seeding:EnableTestData"))
                {
                    await SeedTestDataAsync();
                }

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
            var adminAuth0Id = _configuration["AdminUser:Auth0Id"];
            var adminEmail = _configuration["AdminUser:Email"];

            if (string.IsNullOrEmpty(adminAuth0Id))
            {
                _logger.LogWarning("AdminUser:Auth0Id is not configured. Skipping admin user seeding.");
                return;
            }

            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("AdminUser:Email is not configured. Skipping admin user seeding.");
                return;
            }

            // Check if admin user already exists by Auth0 ID
            var existingAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == adminAuth0Id);

            if (existingAdmin != null)
            {
                _logger.LogInformation(
                    "Admin user already exists: {Email} (Auth0 ID: {Auth0Id})",
                    existingAdmin.Email,
                    existingAdmin.Id);
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

            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "System administrator created successfully: {Email} (Auth0 ID: {Auth0Id})",
                adminEmail,
                adminAuth0Id);
        }

        /// <summary>
        /// Seeds test data for development environment
        /// </summary>
        private async Task SeedTestDataAsync()
        {
            // Only seed if database is empty
            if (await _context.Households.AnyAsync())
            {
                _logger.LogInformation("Test data already exists. Skipping test data seeding.");
                return;
            }

            _logger.LogInformation("Seeding test data for development...");

            // Get admin user
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == SystemRole.SystemAdmin);
            if (admin == null)
            {
                _logger.LogWarning("No admin user found. Cannot seed test data.");
                return;
            }

            // Create test household
            var household = new Household
            {
                Id = Guid.NewGuid(),
                Name = "Test Family Home",
                Description = "A sample household for testing",
                InviteCode = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            await _context.Households.AddAsync(household);

            // Add admin as household owner
            var householdMember = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                HouseholdId = household.Id,
                Role = HouseholdRole.Owner,
                JoinedAt = DateTime.UtcNow
            };

            await _context.HouseholdMembers.AddAsync(householdMember);

            // Create test rooms
            var kitchen = new Room
            {
                Id = Guid.NewGuid(),
                Name = "Kitchen",
                Description = "Cooking and dining area",
                HouseholdId = household.Id,
                Priority = 8,
                CreatedAt = DateTime.UtcNow
            };

            var bathroom = new Room
            {
                Id = Guid.NewGuid(),
                Name = "Bathroom",
                Description = "Main bathroom",
                HouseholdId = household.Id,
                Priority = 7,
                CreatedAt = DateTime.UtcNow
            };

            var livingRoom = new Room
            {
                Id = Guid.NewGuid(),
                Name = "Living Room",
                Description = "Family gathering space",
                HouseholdId = household.Id,
                Priority = 6,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Rooms.AddRangeAsync(kitchen, bathroom, livingRoom);

            // Create test tasks
            var tasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    Title = "Wash dishes",
                    Description = "Clean all dirty dishes after meals",
                    Type = TaskType.Regular,
                    Priority = TaskPriority.High,
                    EstimatedMinutes = 20,
                    RecurrenceRule = "FREQ=WEEKLY;BYDAY=MO",
                    HouseholdId = household.Id,
                    RoomId = kitchen.Id,
                    AssignedUserId = admin.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    Title = "Clean bathroom",
                    Description = "Deep clean bathroom surfaces",
                    Type = TaskType.Regular,
                    Priority = TaskPriority.Medium,
                    EstimatedMinutes = 45,
                    RecurrenceRule = "FREQ=WEEKLY;BYDAY=SA",
                    HouseholdId = household.Id,
                    RoomId = bathroom.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    Title = "Vacuum living room",
                    Description = "Vacuum carpets and under furniture",
                    Type = TaskType.Regular,
                    Priority = TaskPriority.Medium,
                    EstimatedMinutes = 30,
                    RecurrenceRule = "FREQ=WEEKLY;BYDAY=WE",
                    HouseholdId = household.Id,
                    RoomId = livingRoom.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await _context.HouseholdTasks.AddRangeAsync(tasks);

            // Save all test data
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Test data seeded: 1 household, 3 rooms, {TaskCount} tasks",
                tasks.Count);
        }
    }
}
