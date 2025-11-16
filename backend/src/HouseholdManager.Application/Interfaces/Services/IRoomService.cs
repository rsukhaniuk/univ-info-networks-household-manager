using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Service interface for room business logic
    /// </summary>
    public interface IRoomService
    {
        // Basic CRUD operations
        Task<RoomDto> CreateRoomAsync(UpsertRoomRequest request, string requestingUserId, CancellationToken cancellationToken = default);
        Task<RoomDto?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default);
        Task<RoomWithTasksDto?> GetRoomWithTasksAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<RoomDto>> GetHouseholdRoomsAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task<RoomDto> UpdateRoomAsync(Guid id, UpsertRoomRequest request, string requestingUserId, CancellationToken cancellationToken = default);
        Task DeleteRoomAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default);

        // Photo management
        Task<string> UploadRoomPhotoAsync(Guid roomId, IFormFile photo, string requestingUserId, CancellationToken cancellationToken = default);
        Task DeleteRoomPhotoAsync(Guid roomId, string requestingUserId, CancellationToken cancellationToken = default);

        // Validation
        Task<bool> IsNameUniqueInHouseholdAsync(string name, Guid householdId, Guid? excludeRoomId = null, CancellationToken cancellationToken = default);
        Task ValidateRoomAccessAsync(Guid roomId, string userId, CancellationToken cancellationToken = default);
        Task ValidateRoomOwnerAccessAsync(Guid roomId, string userId, CancellationToken cancellationToken = default);
    }
}
