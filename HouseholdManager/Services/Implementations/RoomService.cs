using HouseholdManager.Models;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Interfaces;

namespace HouseholdManager.Services.Implementations
{
    /// <summary>
    /// Implementation of room service with business logic
    /// </summary>
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IHouseholdService _householdService;
        private readonly ILogger<RoomService> _logger;

        public RoomService(
            IRoomRepository roomRepository,
            IHouseholdService householdService,
            ILogger<RoomService> logger)
        {
            _roomRepository = roomRepository;
            _householdService = householdService;
            _logger = logger;
        }

        // Basic CRUD operations
        public async Task<Room> CreateRoomAsync(Guid householdId, string name, string? description, int priority, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await _householdService.ValidateOwnerAccessAsync(householdId, requestingUserId, cancellationToken);

            if (!await IsNameUniqueInHouseholdAsync(name, householdId, null, cancellationToken))
                throw new InvalidOperationException("Room name must be unique within the household");

            var room = new Room
            {
                HouseholdId = householdId,
                Name = name,
                Description = description,
                Priority = priority,
                CreatedAt = DateTime.UtcNow
            };

            var createdRoom = await _roomRepository.AddAsync(room, cancellationToken);
            _logger.LogInformation("Created room {RoomId} in household {HouseholdId}", createdRoom.Id, householdId);

            return createdRoom;
        }

        public async Task<Room?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _roomRepository.GetByIdAsync(id, cancellationToken);
        }

        public async Task<Room?> GetRoomWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _roomRepository.GetByIdWithTasksAsync(id, cancellationToken);
        }

        public async Task<IReadOnlyList<Room>> GetHouseholdRoomsAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            return await _roomRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
        }

        public async Task UpdateRoomAsync(Room room, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateRoomOwnerAccessAsync(room.Id, requestingUserId, cancellationToken);

            if (!await IsNameUniqueInHouseholdAsync(room.Name, room.HouseholdId, room.Id, cancellationToken))
                throw new InvalidOperationException("Room name must be unique within the household");

            await _roomRepository.UpdateAsync(room, cancellationToken);
            _logger.LogInformation("Updated room {RoomId}", room.Id);
        }

        public async Task DeleteRoomAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateRoomOwnerAccessAsync(id, requestingUserId, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(id, cancellationToken);
            if (room == null)
                throw new InvalidOperationException("Room not found");

            await _roomRepository.DeleteByIdAsync(id, cancellationToken);
            _logger.LogInformation("Deleted room {RoomId}", id);
        }

        //// Photo management
        //public async Task<string> UploadRoomPhotoAsync(Guid roomId, IFormFile photo, string requestingUserId, CancellationToken cancellationToken = default)
        //{
            
        //}

        //public async Task DeleteRoomPhotoAsync(Guid roomId, string requestingUserId, CancellationToken cancellationToken = default)
        //{
            
        //}

        // Validation
        public async Task<bool> IsNameUniqueInHouseholdAsync(string name, Guid householdId, Guid? excludeRoomId = null, CancellationToken cancellationToken = default)
        {
            return await _roomRepository.IsNameUniqueInHouseholdAsync(name, householdId, excludeRoomId, cancellationToken);
        }

        public async Task ValidateRoomAccessAsync(Guid roomId, string userId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                throw new InvalidOperationException("Room not found");

            await _householdService.ValidateUserAccessAsync(room.HouseholdId, userId, cancellationToken);
        }

        public async Task ValidateRoomOwnerAccessAsync(Guid roomId, string userId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                throw new InvalidOperationException("Room not found");

            await _householdService.ValidateOwnerAccessAsync(room.HouseholdId, userId, cancellationToken);
        }
    }
}
