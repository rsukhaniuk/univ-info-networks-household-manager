using AutoMapper;
using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Domain.Entities;

namespace HouseholdManager.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for TaskExecution entity mappings
    /// </summary>
    public class ExecutionProfile : Profile
    {
        public ExecutionProfile()
        {
            // Entity > DTO mappings

            // TaskExecution > ExecutionDto
            CreateMap<TaskExecution, ExecutionDto>()
                .ForMember(dest => dest.TaskTitle, opt => opt.MapFrom(src => 
                    src.Task != null ? src.Task.Title : string.Empty))
                .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => 
                    src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => 
                    src.Task != null && src.Task.Room != null ? src.Task.Room.Name : string.Empty))
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.PhotoPath) ? $"/{src.PhotoPath}" : null))
                .ForMember(dest => dest.TimeAgo, opt => opt.MapFrom(src => src.TimeAgo))
                .ForMember(dest => dest.IsThisWeek, opt => opt.MapFrom(src => src.IsThisWeek))
                .AfterMap((src, dest) =>
                {
                    dest.UserName = GetUserDisplayName(src.User);
                });

            // Request > Entity mappings

            // CompleteTaskRequest > TaskExecution (for Create)
            CreateMap<CompleteTaskRequest, TaskExecution>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TaskId, opt => opt.MapFrom(src => src.TaskId))
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src =>
                    src.CompletedAt ?? DateTime.UtcNow))
                .ForMember(dest => dest.WeekStarting, opt => opt.MapFrom(src =>
                    TaskExecution.GetWeekStarting(src.CompletedAt ?? DateTime.UtcNow)))
                .ForMember(dest => dest.HouseholdId, opt => opt.Ignore())
                .ForMember(dest => dest.RoomId, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Household, opt => opt.Ignore())
                .ForMember(dest => dest.Room, opt => opt.Ignore());

            // UpdateExecutionRequest > TaskExecution (for Update)
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

        /// <summary>
        /// Gets user display name with fallback chain: FullName -> FirstName -> LastName -> Email -> UserId
        /// NOTE: Cannot use ApplicationUser.FullName computed property as EF Core doesn't load it
        /// </summary>
        private static string GetUserDisplayName(ApplicationUser? user)
        {
            if (user == null)
                return "Unknown User";

            var firstName = user.FirstName?.Trim();
            var lastName = user.LastName?.Trim();

            // Try full name
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                return $"{firstName} {lastName}";
            
            // Try first name only
            if (!string.IsNullOrEmpty(firstName))
                return firstName;
            
            // Try last name only
            if (!string.IsNullOrEmpty(lastName))
                return lastName;

            // Fallback to email
            var email = user.Email?.Trim();
            if (!string.IsNullOrEmpty(email))
                return email;

            // Last resort: show abbreviated user ID
            if (!string.IsNullOrEmpty(user.Id))
            {
                var shortId = user.Id.Length > 8 ? user.Id.Substring(0, 8) : user.Id;
                return $"User {shortId}...";
            }

            return "Unknown User";
        }
    }
}
