using AutoMapper;
using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for TaskExecution entity mappings
    /// </summary>
    public class ExecutionProfile : Profile
    {
        public ExecutionProfile()
        {
            // Entity → DTO mappings

            // TaskExecution → ExecutionDto
            CreateMap<TaskExecution, ExecutionDto>()
                .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => src.Task.Title))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.Task.Room.Name))
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.PhotoPath) ? $"/uploads/{src.PhotoPath}" : null))
                .ForMember(dest => dest.TimeAgo, opt => opt.MapFrom(src => src.TimeAgo))
                .ForMember(dest => dest.IsThisWeek, opt => opt.MapFrom(src => src.IsThisWeek));

            // Request → Entity mappings

            // CompleteTaskRequest → TaskExecution (for Create)
            CreateMap<CompleteTaskRequest, TaskExecution>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be generated
                .ForMember(dest => dest.TaskId, opt => opt.MapFrom(src => src.TaskId))
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // Set by service from current user
                .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src =>
                    src.CompletedAt ?? DateTime.UtcNow))
                .ForMember(dest => dest.WeekStarting, opt => opt.MapFrom(src =>
                    TaskExecution.GetWeekStarting(src.CompletedAt ?? DateTime.UtcNow)))
                .ForMember(dest => dest.HouseholdId, opt => opt.Ignore()) // Denormalized - set by service
                .ForMember(dest => dest.RoomId, opt => opt.Ignore()) // Denormalized - set by service
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Household, opt => opt.Ignore())
                .ForMember(dest => dest.Room, opt => opt.Ignore());

            // UpdateExecutionRequest → TaskExecution (for Update - only Notes and PhotoPath)
            CreateMap<UpdateExecutionRequest, TaskExecution>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TaskId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.WeekStarting, opt => opt.Ignore())
                .ForMember(dest => dest.HouseholdId, opt => opt.Ignore())
                .ForMember(dest => dest.RoomId, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Household, opt => opt.Ignore())
                .ForMember(dest => dest.Room, opt => opt.Ignore());
        }
    }
}
