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
        Task<Room> CreateRoomAsync(Guid householdId, string name, string? description, int priority, string requestingUserId, CancellationToken cancellationToken = default);
        Task<Room?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Room?> GetRoomWithTasksAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Room>> GetHouseholdRoomsAsync(Guid householdId, CancellationToken cancellationToken = default);
        Task UpdateRoomAsync(Room room, string requestingUserId, CancellationToken cancellationToken = default);
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
