using HouseholdManager.Application.Interfaces.ExternalServices;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Infrastructure.Configuration;
using HouseholdManager.Infrastructure.Data;
using HouseholdManager.Infrastructure.ExternalServices.Auth0;
using HouseholdManager.Infrastructure.ExternalServices.Calendar;
using HouseholdManager.Infrastructure.Repositories;
using HouseholdManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            // Repositories
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<IHouseholdRepository, HouseholdRepository>();
            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IExecutionRepository, ExecutionRepository>();
            services.AddScoped<IHouseholdMemberRepository, HouseholdMemberRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // HttpClient (required for Auth0ManagementApiClient)
            services.AddHttpClient();

            // External Services - Auth0
            services.Configure<Auth0Settings>(configuration.GetSection("Auth0"));
            services.AddScoped<IAuth0ManagementApiClient, Auth0ManagementApiClient>();

            // External Services - Calendar
            services.AddScoped<ICalendarGenerator, ICalendarGeneratorImpl>();
            services.AddScoped<ICalendarTokenService, CalendarTokenService>();

            // Data Seeder
            services.AddScoped<DataSeeder>();

            return services;
        }

        /// <summary>
        /// Seeds initial data (call this in Program.cs)
        /// </summary>
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            try
            {
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }
    }
}
