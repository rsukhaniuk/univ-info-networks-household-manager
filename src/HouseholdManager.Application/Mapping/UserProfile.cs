using AutoMapper;
using HouseholdManager.Application.DTOs.User;
using HouseholdManager.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Mapping
{
    /// <summary>
    /// AutoMapper profile for ApplicationUser entity mappings
    /// </summary>
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            // Entity → DTO mappings

            // ApplicationUser → UserDto
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.IsSystemAdmin, opt => opt.MapFrom(src => src.IsSystemAdmin));

            // ApplicationUser → UserProfileDto (complex mapping)
            CreateMap<ApplicationUser, UserProfileDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Stats, opt => opt.Ignore()) // Calculated by service
                .ForMember(dest => dest.Households, opt => opt.Ignore()); // Will be mapped separately

            // HouseholdMember → UserHouseholdDto (for user's household list)
            CreateMap<HouseholdMember, UserHouseholdDto>()
                .ForMember(dest => dest.HouseholdId, opt => opt.MapFrom(src => src.HouseholdId))
                .ForMember(dest => dest.HouseholdName, opt => opt.MapFrom(src => src.Household.Name))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
                .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.JoinedAt))
                .ForMember(dest => dest.ActiveTaskCount, opt => opt.Ignore()) // Calculated by service
                .ForMember(dest => dest.IsCurrent, opt => opt.Ignore()); // Set by service

            // Request → Entity mappings

            // UpdateProfileRequest → ApplicationUser (for Update - only FirstName/LastName)
            CreateMap<UpdateProfileRequest, ApplicationUser>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore()) // Email managed by Auth0
                .ForMember(dest => dest.ProfilePictureUrl, opt => opt.Ignore()) // Profile picture from Auth0
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentHouseholdId, opt => opt.Ignore())
                .ForMember(dest => dest.HouseholdMemberships, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedTasks, opt => opt.Ignore())
                .ForMember(dest => dest.TaskExecutions, opt => opt.Ignore());

            // No mapping for UserDashboardStats - constructed manually in service
        }
    }
}
