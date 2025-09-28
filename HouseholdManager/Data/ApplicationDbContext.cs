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

            // Configure HouseholdTask relationships explicitly
            builder.Entity<HouseholdTask>()
                .HasOne(ht => ht.Household)
                .WithMany(h => h.Tasks)
                .HasForeignKey(ht => ht.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<HouseholdTask>()
                .HasOne(ht => ht.Room)
                .WithMany(r => r.Tasks)
                .HasForeignKey(ht => ht.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<HouseholdTask>()
                .HasOne(ht => ht.AssignedUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(ht => ht.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure denormalized relationships in TaskExecution to avoid conflicts
            builder.Entity<TaskExecution>()
                .HasOne(te => te.Household)
                .WithMany()
                .HasForeignKey(te => te.HouseholdId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<TaskExecution>()
                .HasOne(te => te.Room)
                .WithMany()
                .HasForeignKey(te => te.RoomId)
                .OnDelete(DeleteBehavior.NoAction);

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
        }
    }
}
