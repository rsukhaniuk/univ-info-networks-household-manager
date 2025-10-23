using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {

            services.AddAutoMapper(cfg =>
            {
                cfg.AddMaps(typeof(DependencyInjection).Assembly);
            });

            // Application Services
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<IHouseholdService, HouseholdService>();
            services.AddScoped<IRoomService, RoomService>();
            services.AddScoped<IHouseholdTaskService, HouseholdTaskService>();
            services.AddScoped<ITaskExecutionService, TaskExecutionService>();
            services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
            services.AddScoped<IHouseholdMemberService, HouseholdMemberService>();

            // UserService temporarily commented out until IUserRepository is implemented
            // services.AddScoped<IUserService, UserService>();

            // TODO: Add FluentValidation validators when ready
            // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // TODO: Add AutoMapper when ready

            return services;
        }
    }
}
