using HouseholdManager.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HouseholdManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for domain models
        public DbSet<Household> Households { get; set; }
        public DbSet<HouseholdMember> HouseholdMembers { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<HouseholdTask> HouseholdTasks { get; set; }
        public DbSet<TaskExecution> TaskExecutions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Only configurations that CAN'T be done with Data Annotations

            // Unique composite constraint - user can only join household once
            builder.Entity<HouseholdMember>()
                .HasIndex(e => new { e.UserId, e.HouseholdId })
                .IsUnique();

            // Unique invite code constraint
            builder.Entity<Household>()
                .HasIndex(e => e.InviteCode)
                .IsUnique();

            // Enum string conversion for better DB readability
            builder.Entity<ApplicationUser>()
                .Property(e => e.Role)
                .HasConversion<string>();

            builder.Entity<HouseholdMember>()
                .Property(e => e.Role)
                .HasConversion<string>();

            builder.Entity<HouseholdTask>()
                .Property(e => e.Type)
                .HasConversion<string>();

            builder.Entity<HouseholdTask>()
                .Property(e => e.Priority)
                .HasConversion<string>();

            builder.Entity<HouseholdTask>()
                .Property(e => e.ScheduledWeekday)
                .HasConversion<string>();

            // Custom cascade behavior
            builder.Entity<HouseholdTask>()
                .HasOne(e => e.AssignedUser)
                .WithMany(e => e.AssignedTasks)
                .OnDelete(DeleteBehavior.SetNull); // Unassign when user deleted

            builder.Entity<TaskExecution>()
                .HasOne(e => e.User)
                .WithMany(e => e.TaskExecutions)
                .OnDelete(DeleteBehavior.Restrict); // Keep history when user deleted

            // Performance indexes for common queries
            builder.Entity<HouseholdTask>()
                .HasIndex(e => new { e.HouseholdId, e.IsActive });

            builder.Entity<TaskExecution>()
                .HasIndex(e => new { e.UserId, e.WeekStarting });
        }
    }
}
