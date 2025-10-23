using AutoMapper;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for Room entity mappings
    /// </summary>
    public class RoomProfile : Profile
    {
        public RoomProfile()
        {
            // Entity → DTO mappings

            // Room → RoomDto
            CreateMap<Room, RoomDto>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.PhotoPath) ? $"/uploads/{src.PhotoPath}" : null))
                .ForMember(dest => dest.ActiveTaskCount, opt => opt.MapFrom(src => src.Tasks.Count(t => t.IsActive)));

            // Room → RoomWithTasksDto (complex mapping with nested data)
            CreateMap<Room, RoomWithTasksDto>()
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.ActiveTasks, opt => opt.MapFrom(src =>
                    src.Tasks.Where(t => t.IsActive)))
                .ForMember(dest => dest.RecentExecutions, opt => opt.Ignore()) // Loaded separately by service
                .ForMember(dest => dest.IsOwner, opt => opt.Ignore()) // Set by controller/service
                .ForMember(dest => dest.Stats, opt => opt.MapFrom(src => MapRoomStats(src)));


            // Request → Entity mappings

            // UpsertRoomRequest → Room (for Create)
            CreateMap<UpsertRoomRequest, Room>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be set by service or generated
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Household, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore());

            // UpsertRoomRequest → Room (for Update)
            CreateMap<UpsertRoomRequest, Room>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Don't update creation date
                .ForMember(dest => dest.Household, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore());
        }
        /// <summary>
        /// Safe mapping for RoomStatsDto with null checks
        /// </summary>
        private static RoomStatsDto MapRoomStats(Room room)
        {
            var allExecutions = room.Tasks
                .SelectMany(t => t.Executions)
                .ToList();

            var weekStart = TaskExecution.GetWeekStarting(DateTime.UtcNow);
            var thisWeekExecutions = allExecutions
                .Where(e => e.WeekStarting == weekStart)
                .ToList();

            return new RoomStatsDto
            {
                TotalTasks = room.Tasks.Count,
                ActiveTasks = room.Tasks.Count(t => t.IsActive),
                OverdueTasks = room.Tasks.Count(t => t.IsOverdue),
                CompletedThisWeek = thisWeekExecutions.Count,
                AverageCompletionTime = allExecutions.Any()
                    ? (int?)allExecutions.Average(e => e.Task.EstimatedMinutes)
                    : null,
                LastActivity = allExecutions
                    .OrderByDescending(e => e.CompletedAt)
                    .FirstOrDefault()?.CompletedAt // Safe null check
            };
        }
    }
}
