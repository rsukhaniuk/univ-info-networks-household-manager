using HouseholdManager.Models.Entities;
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

            builder.Entity<HouseholdTask>()
            .HasOne(t => t.Room)
            .WithMany(r => r.Tasks)
            .HasForeignKey(t => t.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

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
        }
    }
}
