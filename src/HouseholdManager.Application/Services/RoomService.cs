using AutoMapper;
using HouseholdManager.Application.DTOs.Execution;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// Implementation of room service with business logic
    /// </summary>
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IHouseholdService _householdService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<RoomService> _logger;
        private readonly IMapper _mapper;

        public RoomService(
            IRoomRepository roomRepository,
            IHouseholdService householdService,
            IFileUploadService fileUploadService,
            IMapper mapper,
            ILogger<RoomService> logger)
        {
            _roomRepository = roomRepository;
            _householdService = householdService;
            _fileUploadService = fileUploadService;
            _mapper = mapper;
            _logger = logger;
        }

        // Basic CRUD operations
        public async Task<RoomDto> CreateRoomAsync(
            UpsertRoomRequest request,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            await _householdService.ValidateOwnerAccessAsync(request.HouseholdId, requestingUserId, cancellationToken);

            if (!await IsNameUniqueInHouseholdAsync(request.Name, request.HouseholdId, null, cancellationToken))
                throw new ValidationException("Name", "Room name must be unique within the household");

            var room = _mapper.Map<Room>(request);
            room.CreatedAt = DateTime.UtcNow;

            var createdRoom = await _roomRepository.AddAsync(room, cancellationToken);

            _logger.LogInformation("Created room {RoomId} in household {HouseholdId}",
                createdRoom.Id, request.HouseholdId);

            return _mapper.Map<RoomDto>(createdRoom);
        }

        public async Task<RoomDto?> GetRoomAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(id, cancellationToken);
            return room == null ? null : _mapper.Map<RoomDto>(room);
        }

        public async Task<RoomWithTasksDto?> GetRoomWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdWithTasksAsync(id, cancellationToken);
            if (room == null) return null;

            var dto = _mapper.Map<RoomWithTasksDto>(room);

            dto.IsOwner = false; // Set by controller based on current user

            return dto;
        }

        public async Task<IReadOnlyList<RoomDto>> GetHouseholdRoomsAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            var rooms = await _roomRepository.GetByHouseholdIdAsync(householdId, cancellationToken);
            return _mapper.Map<IReadOnlyList<RoomDto>>(rooms);
        }

        public async Task<RoomDto> UpdateRoomAsync(
            Guid id,
            UpsertRoomRequest request,
            string requestingUserId,
            CancellationToken cancellationToken = default)
        {
            await ValidateRoomOwnerAccessAsync(id, requestingUserId, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(id, cancellationToken);
            if (room == null)
                throw new NotFoundException("Room", id);

            if (!await IsNameUniqueInHouseholdAsync(request.Name, request.HouseholdId, id, cancellationToken))
                throw new ValidationException("Name", "Room name must be unique within the household");

            // Update properties from request
            room.Name = request.Name;
            room.Description = request.Description;
            room.Priority = request.Priority;
            room.PhotoPath = request.PhotoPath ?? room.PhotoPath;

            await _roomRepository.UpdateAsync(room, cancellationToken);

            _logger.LogInformation("Updated room {RoomId}", room.Id);

            return _mapper.Map<RoomDto>(room);
        }

        public async Task DeleteRoomAsync(Guid id, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateRoomOwnerAccessAsync(id, requestingUserId, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(id, cancellationToken);
            if (room == null)
                throw new NotFoundException("Room", id);

            // Delete room photo if exists
            if (!string.IsNullOrEmpty(room.PhotoPath))
            {
                await _fileUploadService.DeleteFileAsync(room.PhotoPath, cancellationToken);
            }

            await _roomRepository.DeleteByIdAsync(id, cancellationToken);
            _logger.LogInformation("Deleted room {RoomId}", id);
        }

        // Photo management
        public async Task<string> UploadRoomPhotoAsync(Guid roomId, IFormFile photo, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateRoomOwnerAccessAsync(roomId, requestingUserId, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                throw new NotFoundException("Room", roomId);

            // Delete old photo if exists
            if (!string.IsNullOrEmpty(room.PhotoPath))
            {
                await _fileUploadService.DeleteFileAsync(room.PhotoPath, cancellationToken);
            }

            // Upload new photo
            var photoPath = await _fileUploadService.UploadRoomPhotoAsync(photo, cancellationToken);

            // Update room
            room.PhotoPath = photoPath;
            await _roomRepository.UpdateAsync(room, cancellationToken);

            _logger.LogInformation("Uploaded photo for room {RoomId}: {PhotoPath}", roomId, photoPath);
            return photoPath;
        }

        public async Task DeleteRoomPhotoAsync(Guid roomId, string requestingUserId, CancellationToken cancellationToken = default)
        {
            await ValidateRoomOwnerAccessAsync(roomId, requestingUserId, cancellationToken);

            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                throw new NotFoundException("Room", roomId);

            if (!string.IsNullOrEmpty(room.PhotoPath))
            {
                await _fileUploadService.DeleteFileAsync(room.PhotoPath, cancellationToken);
                room.PhotoPath = null;
                await _roomRepository.UpdateAsync(room, cancellationToken);

                _logger.LogInformation("Deleted photo for room {RoomId}", roomId);
            }
        }

        // Validation
        public async Task<bool> IsNameUniqueInHouseholdAsync(string name, Guid householdId, Guid? excludeRoomId = null, CancellationToken cancellationToken = default)
        {
            return await _roomRepository.IsNameUniqueInHouseholdAsync(name, householdId, excludeRoomId, cancellationToken);
        }

        public async Task ValidateRoomAccessAsync(Guid roomId, string userId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                throw new NotFoundException("Room", roomId);

            await _householdService.ValidateUserAccessAsync(room.HouseholdId, userId, cancellationToken);
        }

        public async Task ValidateRoomOwnerAccessAsync(Guid roomId, string userId, CancellationToken cancellationToken = default)
        {
            var room = await _roomRepository.GetByIdAsync(roomId, cancellationToken);
            if (room == null)
                throw new NotFoundException("Room", roomId);

            await _householdService.ValidateOwnerAccessAsync(room.HouseholdId, userId, cancellationToken);
        }
    }
}
