using HouseholdManager.Models;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Implementations;
using HouseholdManager.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Tests.Services
{
    [TestFixture]
    public class RoomServiceTests
    {
        private Mock<IRoomRepository> _mockRoomRepository;
        private Mock<IHouseholdService> _mockHouseholdService;
        private Mock<ILogger<RoomService>> _mockLogger;
        private RoomService _roomService;

        [SetUp]
        public void Setup()
        {
            _mockRoomRepository = new Mock<IRoomRepository>();
            _mockHouseholdService = new Mock<IHouseholdService>();
            _mockLogger = new Mock<ILogger<RoomService>>();

            _roomService = new RoomService(
                _mockRoomRepository.Object,
                _mockHouseholdService.Object,
                _mockLogger.Object);
        }

        [Test]
        [Category("RoomService")]
        [Category("CreateRoom")]
        public async Task CreateRoomAsync_CreatesRoom_WhenValidData()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var name = "Living Room";
            var description = "Main living area";
            var priority = 7;
            var requestingUserId = "owner123";

            var createdRoom = new Room
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                Name = name,
                Description = description,
                Priority = priority,
                CreatedAt = DateTime.UtcNow
            };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomRepository.Setup(r => r.IsNameUniqueInHouseholdAsync(name, householdId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockRoomRepository.Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdRoom);

            // Act
            var result = await _roomService.CreateRoomAsync(householdId, name, description, priority, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(name));
            Assert.That(result.Description, Is.EqualTo(description));
            Assert.That(result.Priority, Is.EqualTo(priority));
            Assert.That(result.HouseholdId, Is.EqualTo(householdId));

            _mockHouseholdService.Verify(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()), Times.Once);
            _mockRoomRepository.Verify(r => r.AddAsync(It.Is<Room>(room =>
                room.Name == name &&
                room.HouseholdId == householdId &&
                room.Priority == priority), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("RoomService")]
        [Category("CreateRoom")]
        public async Task CreateRoomAsync_ThrowsException_WhenNameNotUnique()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var name = "Living Room";
            var requestingUserId = "owner123";

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomRepository.Setup(r => r.IsNameUniqueInHouseholdAsync(name, householdId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _roomService.CreateRoomAsync(householdId, name, "desc", 5, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("Room name must be unique within the household"));
        }

        [Test]
        [Category("RoomService")]
        [Category("CreateRoom")]
        public async Task CreateRoomAsync_ThrowsException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("User is not an owner of this household"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _roomService.CreateRoomAsync(householdId, "Room", "desc", 5, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("User is not an owner of this household"));
        }

        [Test]
        [Category("RoomService")]
        [Category("GetRoom")]
        public async Task GetRoomAsync_ReturnsRoom_WhenExists()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var room = new Room
            {
                Id = roomId,
                Name = "Kitchen",
                Description = "Cooking area",
                Priority = 8
            };

            _mockRoomRepository.Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            // Act
            var result = await _roomService.GetRoomAsync(roomId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(roomId));
            Assert.That(result.Name, Is.EqualTo("Kitchen"));
        }

        [Test]
        [Category("RoomService")]
        [Category("GetRoom")]
        public async Task GetRoomAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            _mockRoomRepository.Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Room)null);

            // Act
            var result = await _roomService.GetRoomAsync(roomId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        [Category("RoomService")]
        [Category("UpdateRoom")]
        public async Task UpdateRoomAsync_UpdatesRoom_WhenValidData()
        {
            // Arrange
            var room = new Room
            {
                Id = Guid.NewGuid(),
                HouseholdId = Guid.NewGuid(),
                Name = "Updated Kitchen",
                Description = "Updated cooking area",
                Priority = 9
            };
            var requestingUserId = "owner123";

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(room.HouseholdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomRepository.Setup(r => r.IsNameUniqueInHouseholdAsync(room.Name, room.HouseholdId, room.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockRoomRepository.Setup(r => r.UpdateAsync(room, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _roomService.UpdateRoomAsync(room, requestingUserId);

            // Assert
            _mockRoomRepository.Verify(r => r.UpdateAsync(room, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("RoomService")]
        [Category("UpdateRoom")]
        public async Task UpdateRoomAsync_ThrowsException_WhenNameNotUnique()
        {
            // Arrange
            var room = new Room
            {
                Id = Guid.NewGuid(),
                HouseholdId = Guid.NewGuid(),
                Name = "Duplicate Name",
                Priority = 5
            };
            var requestingUserId = "owner123";

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(room.HouseholdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomRepository.Setup(r => r.IsNameUniqueInHouseholdAsync(room.Name, room.HouseholdId, room.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _roomService.UpdateRoomAsync(room, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("Room name must be unique within the household"));
        }


        [Test]
        [Category("RoomService")]
        [Category("DeleteRoom")]
        public async Task DeleteRoomAsync_ThrowsException_WhenRoomNotFound()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var requestingUserId = "owner123";

            _mockRoomRepository.Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Room)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _roomService.DeleteRoomAsync(roomId, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("Room not found"));
        }

        [Test]
        [Category("RoomService")]
        [Category("Validation")]
        public async Task IsNameUniqueInHouseholdAsync_ReturnsTrue_WhenNameIsUnique()
        {
            // Arrange
            var name = "Unique Room";
            var householdId = Guid.NewGuid();

            _mockRoomRepository.Setup(r => r.IsNameUniqueInHouseholdAsync(name, householdId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _roomService.IsNameUniqueInHouseholdAsync(name, householdId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        [Category("RoomService")]
        [Category("Validation")]
        public async Task IsNameUniqueInHouseholdAsync_ReturnsFalse_WhenNameExists()
        {
            // Arrange
            var name = "Existing Room";
            var householdId = Guid.NewGuid();

            _mockRoomRepository.Setup(r => r.IsNameUniqueInHouseholdAsync(name, householdId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _roomService.IsNameUniqueInHouseholdAsync(name, householdId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        [Category("RoomService")]
        [Category("Validation")]
        public async Task ValidateRoomAccessAsync_DoesNotThrow_WhenUserHasAccess()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var userId = "user123";
            var room = new Room
            {
                Id = roomId,
                HouseholdId = Guid.NewGuid(),
                Name = "Living Room"
            };

            _mockRoomRepository.Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(room.HouseholdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _roomService.ValidateRoomAccessAsync(roomId, userId));
        }

        [Test]
        [Category("RoomService")]
        [Category("Validation")]
        public async Task ValidateRoomAccessAsync_ThrowsException_WhenRoomNotFound()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var userId = "user123";

            _mockRoomRepository.Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Room)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _roomService.ValidateRoomAccessAsync(roomId, userId));

            Assert.That(exception.Message, Is.EqualTo("Room not found"));
        }
    }
}
