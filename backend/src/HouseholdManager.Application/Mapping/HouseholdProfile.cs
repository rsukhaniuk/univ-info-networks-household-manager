using AutoMapper;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for Household entity mappings
    /// </summary>
    public class HouseholdProfile : Profile
    {
        public HouseholdProfile()
        {
            // Entity → DTO mappings

            // Household → HouseholdDto
            CreateMap<Household, HouseholdDto>()
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count))
                .ForMember(dest => dest.ActiveTaskCount, opt => opt.MapFrom(src => src.Tasks.Count(t => t.IsActive)))
                .ForMember(dest => dest.RoomCount, opt => opt.MapFrom(src => src.Rooms.Count));

            // Household → HouseholdDetailsDto (complex mapping with nested data)
            CreateMap<Household, HouseholdDetailsDto>()
                .ForMember(dest => dest.Household, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Rooms, opt => opt.MapFrom(src => src.Rooms))
                .ForMember(dest => dest.ActiveTasks, opt => opt.MapFrom(src =>
                    src.Tasks.Where(t => t.IsActive)))
                .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.Members))
                .ForMember(dest => dest.IsOwner, opt => opt.Ignore()) // Set by service
                .ForMember(dest => dest.TaskCountsByUser, opt => opt.MapFrom(src =>
                    src.Members.ToDictionary(
                        m => m.UserId,
                        m => src.Tasks.Count(t => t.IsActive && t.AssignedUserId == m.UserId)
                    )));

            // HouseholdMember → HouseholdMemberDto
            CreateMap<HouseholdMember, HouseholdMemberDto>()
                .ForMember(dest => dest.HouseholdId, opt => opt.MapFrom(src => src.HouseholdId))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => GetUserDisplayName(src.User)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.ActiveTaskCount, opt => opt.Ignore()) // Calculated by service
                .ForMember(dest => dest.CompletedThisWeek, opt => opt.Ignore()); // Calculated by service

            // Request → Entity mappings

            // UpsertHouseholdRequest → Household (for Create)
            CreateMap<UpsertHouseholdRequest, Household>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be set by service or generated
                .ForMember(dest => dest.InviteCode, opt => opt.MapFrom(src => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.Members, opt => opt.Ignore())
                .ForMember(dest => dest.Rooms, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore());

            // UpsertHouseholdRequest → Household (for Update)
            CreateMap<UpsertHouseholdRequest, Household>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id ?? Guid.NewGuid()))
                .ForMember(dest => dest.InviteCode, opt => opt.Ignore()) // Don't update invite code
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Don't update creation date
                .ForMember(dest => dest.Members, opt => opt.Ignore())
                .ForMember(dest => dest.Rooms, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore());
        }

        /// <summary>
        /// Gets user display name with fallback chain: FullName -> FirstName -> LastName -> Email -> UserId
        /// </summary>
        private static string? GetUserDisplayName(ApplicationUser? user)
        {
            if (user == null)
                return null;

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

            return null;
        }
    }
}
