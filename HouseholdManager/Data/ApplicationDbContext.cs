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
        

    }
}
