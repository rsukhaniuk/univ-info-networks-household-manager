using AutoMapper;
using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for HouseholdTask entity mappings
    /// </summary>
    public class TaskProfile : Profile
    {
        public TaskProfile()
        {
            // Entity → DTO mappings

            // HouseholdTask → TaskDto
            CreateMap<HouseholdTask, TaskDto>()
                .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room.Name))
                .ForMember(dest => dest.FormattedEstimatedTime, opt => opt.MapFrom(src => src.FormattedEstimatedTime))
                .ForMember(dest => dest.AssignedUserName, opt => opt.MapFrom(src =>
                    src.AssignedUser != null ? src.AssignedUser.FullName : null))
                .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue))
                .ForMember(dest => dest.IsCompletedThisWeek, opt => opt.Ignore()); // Calculated by service

            // HouseholdTask → TaskDetailsDto (complex mapping with nested data)
            CreateMap<HouseholdTask, TaskDetailsDto>()
                .ForMember(dest => dest.Task, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Room))
                .ForMember(dest => dest.RecentExecutions, opt => opt.MapFrom(src =>
                    src.Executions.OrderByDescending(e => e.CompletedAt).Take(10)))
                .ForMember(dest => dest.AvailableAssignees, opt => opt.Ignore()) // Loaded separately by service
                .ForMember(dest => dest.Permissions, opt => opt.Ignore()) // Set by controller/service
                .ForMember(dest => dest.Stats, opt => opt.MapFrom(src => MapTaskStats(src)));

            // HouseholdTask → TaskCalendarItemDto (for calendar view)
            CreateMap<HouseholdTask, TaskCalendarItemDto>()
                .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Room.Name))
                .ForMember(dest => dest.AssignedUserName, opt => opt.MapFrom(src =>
                    src.AssignedUser != null ? src.AssignedUser.FullName : null))
                .ForMember(dest => dest.IsCompleted, opt => opt.Ignore()) // Calculated by service
                .ForMember(dest => dest.CompletedAt, opt => opt.Ignore()) // Calculated by service
                .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue));

            // TaskExecution → ExecutionDto (for nested executions)
            CreateMap<TaskExecution, ExecutionDto>()
                .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => src.Task.Title))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Task.Room.Name))
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.PhotoPath) ? $"/{src.PhotoPath}" : null))
                .ForMember(dest => dest.TimeAgo, opt => opt.MapFrom(src => src.TimeAgo))
                .ForMember(dest => dest.IsThisWeek, opt => opt.MapFrom(src => src.IsThisWeek));

            // Room → RoomDto (for nested room in TaskDetailsDto)
            CreateMap<Room, RoomDto>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.PhotoPath) ? $"/{src.PhotoPath}" : null))
                .ForMember(dest => dest.ActiveTaskCount, opt => opt.MapFrom(src => src.Tasks.Count(t => t.IsActive)));

            // ApplicationUser → TaskAssigneeDto (for task assignment dropdown)
            CreateMap<ApplicationUser, TaskAssigneeDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.CurrentTaskCount, opt => opt.Ignore()); // Calculated by service

            // Request → Entity mappings

            // UpsertTaskRequest → HouseholdTask (for Create)
            CreateMap<UpsertTaskRequest, HouseholdTask>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be set by service or generated
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Household, opt => opt.Ignore())
                .ForMember(dest => dest.Room, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedUser, opt => opt.Ignore())
                .ForMember(dest => dest.Executions, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

            // UpsertTaskRequest → HouseholdTask (for Update)
            CreateMap<UpsertTaskRequest, HouseholdTask>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be set by service
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.Household, opt => opt.Ignore())
                .ForMember(dest => dest.Room, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedUser, opt => opt.Ignore())
                .ForMember(dest => dest.Executions, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));
        }

        /// <summary>
        /// Safe mapping for TaskStatsDto with null checks
        /// </summary>
        private static TaskStatsDto MapTaskStats(HouseholdTask task)
        {
            var now = DateTime.UtcNow;
            var weekStart = TaskExecution.GetWeekStarting(now);

            var allExecutions = task.Executions.ToList();
            var thisWeekExecutions = allExecutions.Where(e => e.WeekStarting == weekStart).ToList();
            var thisMonthExecutions = allExecutions.Where(e => e.CompletedAt >= now.AddMonths(-1)).ToList();
            var lastExecution = allExecutions.OrderByDescending(e => e.CompletedAt).FirstOrDefault();

            return new TaskStatsDto
            {
                TotalExecutions = allExecutions.Count,
                ExecutionsThisWeek = thisWeekExecutions.Count,
                ExecutionsThisMonth = thisMonthExecutions.Count,
                LastCompleted = lastExecution?.CompletedAt, // Safe null check
                LastCompletedBy = lastExecution?.User.FullName, // Safe null check
                AverageCompletionTime = allExecutions.Any()
                    ? (int?)allExecutions.Average(e => e.Task.EstimatedMinutes)
                    : task.EstimatedMinutes // Fallback to estimated time if no executions
            };
        }
    }
}
