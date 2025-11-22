using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Infrastructure.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Знайди шлях до API-проєкту (де зберігається appsettings.json)
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../HouseholdManager.Api");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var databaseProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use DatabaseProvider from configuration (same logic as DependencyInjection.cs)
            if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                // PostgreSQL (SQL Server migrations excluded from compilation via .csproj)
                optionsBuilder.UseNpgsql(connectionString, b =>
                {
                    b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    b.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
            }
            else
            {
                // SQL Server (to use: remove exclusion in .csproj and exclude PostgreSQL migrations)
                optionsBuilder.UseSqlServer(connectionString, b =>
                {
                    b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    b.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                });
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
