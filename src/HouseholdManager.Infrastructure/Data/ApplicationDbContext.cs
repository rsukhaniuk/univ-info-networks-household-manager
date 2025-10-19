using HouseholdManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdManager.Infrastructure.Data
{
    /// <summary>
    /// Application database context (without Identity - using Auth0)
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for domain models
        public DbSet<ApplicationUser> Users { get; set; } = null!;
        public DbSet<Household> Households { get; set; } = null!;
        public DbSet<HouseholdMember> HouseholdMembers { get; set; } = null!;
        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<HouseholdTask> HouseholdTasks { get; set; } = null!;
        public DbSet<TaskExecution> TaskExecutions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // PRIMARY KEYS

            // ApplicationUser - string ID для Auth0
            builder.Entity<ApplicationUser>()
                .HasKey(u => u.Id);

            // INDEXES

            // User email index (unique)
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Unique composite constraint - user can only join household once
            builder.Entity<HouseholdMember>()
                .HasIndex(e => new { e.UserId, e.HouseholdId })
                .IsUnique();

            // Unique invite code constraint
            builder.Entity<Household>()
                .HasIndex(e => e.InviteCode)
                .IsUnique();

            // Performance indexes for queries
            builder.Entity<TaskExecution>()
                .HasIndex(e => new { e.HouseholdId, e.WeekStarting });

            builder.Entity<TaskExecution>()
                .HasIndex(e => new { e.UserId, e.CompletedAt });

            builder.Entity<HouseholdTask>()
                .HasIndex(e => new { e.HouseholdId, e.IsActive });

            // RELATIONSHIPS

            // HouseholdTask -> Room (restrict delete to prevent orphaned tasks)
            builder.Entity<HouseholdTask>()
                .HasOne(t => t.Room)
                .WithMany(r => r.Tasks)
                .HasForeignKey(t => t.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // HouseholdTask -> Household (cascade delete)
            builder.Entity<HouseholdTask>()
                .HasOne(t => t.Household)
                .WithMany(h => h.Tasks)
                .HasForeignKey(t => t.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room -> Household (cascade delete)
            builder.Entity<Room>()
                .HasOne(r => r.Household)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            // HouseholdMember -> Household (cascade delete)
            builder.Entity<HouseholdMember>()
                .HasOne(m => m.Household)
                .WithMany(h => h.Members)
                .HasForeignKey(m => m.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            // HouseholdMember -> User (restrict delete to preserve data integrity)
            builder.Entity<HouseholdMember>()
                .HasOne(m => m.User)
                .WithMany(u => u.HouseholdMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TaskExecution -> Task (restrict delete to preserve history)
            builder.Entity<TaskExecution>()
                .HasOne(e => e.Task)
                .WithMany(t => t.Executions)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // TaskExecution -> User (restrict delete to preserve history)
            builder.Entity<TaskExecution>()
                .HasOne(e => e.User)
                .WithMany(u => u.TaskExecutions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ENUM STRING CONVERSION

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

            // CONCURRENCY TOKENS

            builder.Entity<HouseholdTask>()
                .Property(e => e.RowVersion)
                .IsRowVersion();
        }
    }
}
