using AutoMapper;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Application.Mapping;
using HouseholdManager.Application.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace HouseholdManager.Application.Tests.Services
{
    [TestFixture]
    public class RoomServiceTests
    {
        private Mock<IRoomRepository> _mockRoomRepository;
        private Mock<IHouseholdService> _mockHouseholdService;
        private Mock<IFileUploadService> _mockFileUploadService;
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IExecutionRepository> _mockExecutionRepository;
        private Mock<ILogger<RoomService>> _mockLogger;
        private IMapper _mapper;
        private RoomService _roomService;

        [SetUp]
        public void Setup()
        {
            _mockRoomRepository = new Mock<IRoomRepository>();
            _mockHouseholdService = new Mock<IHouseholdService>();
            _mockFileUploadService = new Mock<IFileUploadService>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockExecutionRepository = new Mock<IExecutionRepository>();
            _mockLogger = new Mock<ILogger<RoomService>>();

            var loggerFactory = LoggerFactory.Create(b => { });

            var config = new MapperConfiguration(
                cfg => { cfg.AddMaps(typeof(RoomProfile).Assembly); },
                loggerFactory
            );

            _mapper = config.CreateMapper();

            _roomService = new RoomService(
                _mockRoomRepository.Object,
                _mockHouseholdService.Object,
                _mockFileUploadService.Object,
                _mockTaskRepository.Object,
                _mockExecutionRepository.Object,
                _mapper,
                _mockLogger.Object);
        }

        #region CreateRoomAsync Tests

        [Test]
        [Category("RoomService")]
        [Category("CreateRoom")]
        public async Task CreateRoomAsync_CreatesRoom_WhenValidData()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            var request = new UpsertRoomRequest
            {
                HouseholdId = householdId,
                Name = "Kitchen",
                Description = "Main kitchen",
                Priority = 8
            };

            var createdRoom = new Room
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                Name = "Kitchen",
                Description = "Main kitchen",
                Priority = 8,
                CreatedAt = DateTime.UtcNow
            };

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockRoomRepository
                .Setup(r => r.IsNameUniqueInHouseholdAsync(request.Name, householdId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockRoomRepository
                .Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdRoom);

            // Act
            var result = await _roomService.CreateRoomAsync(request, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Kitchen"));
            Assert.That(result.HouseholdId, Is.EqualTo(householdId));
            Assert.That(result.Priority, Is.EqualTo(8));

            _mockHouseholdService.Verify(s =>
                s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()),
                Times.Once);
            _mockRoomRepository.Verify(r =>
                r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        [Category("RoomService")]
        [Category("CreateRoom")]
        public async Task CreateRoomAsync_ThrowsUnauthorizedException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            var request = new UpsertRoomRequest
            {
                HouseholdId = householdId,
                Name = "Kitchen"
            };

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("User is not an owner of this household"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _roomService.CreateRoomAsync(request, requestingUserId));

            Assert.That(exception.Message, Contains.Substring("not an owner"));
        }

        [Test]
        [Category("RoomService")]
        [Category("CreateRoom")]
        public async Task CreateRoomAsync_ThrowsValidationException_WhenNameNotUnique()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            var request = new UpsertRoomRequest
            {
                HouseholdId = householdId,
                Name = "Kitchen"
            };

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockRoomRepository
                .Setup(r => r.IsNameUniqueInHouseholdAsync(request.Name, householdId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(
                async () => await _roomService.CreateRoomAsync(request, requestingUserId));

            Assert.That(exception.Message, Contains.Substring("unique"));
        }

        #endregion

        #region GetRoomAsync Tests

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
                HouseholdId = Guid.NewGuid(),
                Priority = 5,
                Tasks = new List<HouseholdTask>
                {
                    new() { IsActive = true },
                    new() { IsActive = true },
                    new() { IsActive = false }
                }
            };

            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            // Act
            var result = await _roomService.GetRoomAsync(roomId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(roomId));
            Assert.That(result.Name, Is.EqualTo("Kitchen"));
            Assert.That(result.ActiveTaskCount, Is.EqualTo(2));
        }

        [Test]
        [Category("RoomService")]
        [Category("GetRoom")]
        public async Task GetRoomAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Room?)null);

            // Act
            var result = await _roomService.GetRoomAsync(roomId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region GetRoomWithTasksAsync Tests

        [Test]
        [Category("RoomService")]
        [Category("GetRoomWithTasks")]
        public async Task GetRoomWithTasksAsync_ReturnsRoomWithStats_WhenExists()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var weekStart = TaskExecution.GetWeekStarting(DateTime.UtcNow);

            var room = new Room
            {
                Id = roomId,
                Name = "Kitchen",
                HouseholdId = Guid.NewGuid(),
                Priority = 5,
                Tasks = new List<HouseholdTask>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Title = "Clean dishes",
                        IsActive = true,
                        EstimatedMinutes = 30,
                        Executions = new List<TaskExecution>
                        {
                            new()
                            {
                                CompletedAt = DateTime.UtcNow.AddHours(-2),
                                WeekStarting = weekStart,
                                Task = new HouseholdTask { EstimatedMinutes = 30 }
                            }
                        },
                        Room = new Room { Name = "Kitchen" }
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Title = "Mop floor",
                        IsActive = true,
                        EstimatedMinutes = 20,
                        Executions = new List<TaskExecution>(),
                        Room = new Room { Name = "Kitchen" }
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Title = "Old task",
                        IsActive = false,
                        EstimatedMinutes = 15,
                        Executions = new List<TaskExecution>(),
                        Room = new Room { Name = "Kitchen" }
                    }
                }
            };

            _mockRoomRepository
                .Setup(r => r.GetByIdWithTasksAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            // Act
            var result = await _roomService.GetRoomWithTasksAsync(roomId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Room.Name, Is.EqualTo("Kitchen"));
            Assert.That(result.ActiveTasks.Count, Is.EqualTo(2));
            Assert.That(result.Stats.TotalTasks, Is.EqualTo(3));
            Assert.That(result.Stats.ActiveTasks, Is.EqualTo(2));
            Assert.That(result.Stats.CompletedThisWeek, Is.EqualTo(1));
            Assert.That(result.Stats.LastActivity, Is.Not.Null);
        }

        [Test]
        [Category("RoomService")]
        [Category("GetRoomWithTasks")]
        public async Task GetRoomWithTasksAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            _mockRoomRepository
                .Setup(r => r.GetByIdWithTasksAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Room?)null);

            // Act
            var result = await _roomService.GetRoomWithTasksAsync(roomId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region UpdateRoomAsync Tests

        [Test]
        [Category("RoomService")]
        [Category("UpdateRoom")]
        public async Task UpdateRoomAsync_UpdatesRoom_WhenValidData()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            var existingRoom = new Room
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen",
                Description = "Old description",
                Priority = 5
            };

            var request = new UpsertRoomRequest
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Updated Kitchen",
                Description = "New description",
                Priority = 8
            };

            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingRoom);

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockRoomRepository
                .Setup(r => r.IsNameUniqueInHouseholdAsync(request.Name, householdId, roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockRoomRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _roomService.UpdateRoomAsync(roomId, request, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Updated Kitchen"));
            Assert.That(result.Description, Is.EqualTo("New description"));
            Assert.That(result.Priority, Is.EqualTo(8));

            _mockRoomRepository.Verify(r =>
                r.UpdateAsync(It.Is<Room>(room =>
                    room.Name == "Updated Kitchen" &&
                    room.Description == "New description" &&
                    room.Priority == 8),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        [Category("RoomService")]
        [Category("UpdateRoom")]
        public async Task UpdateRoomAsync_ThrowsNotFoundException_WhenRoomNotFound()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var requestingUserId = "user123";

            var request = new UpsertRoomRequest
            {
                Id = roomId,
                HouseholdId = Guid.NewGuid(),
                Name = "Kitchen"
            };

            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Room?)null);

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(It.IsAny<Guid>(), requestingUserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotFoundException("Room", roomId));

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(
                async () => await _roomService.UpdateRoomAsync(roomId, request, requestingUserId));
        }

        #endregion

        #region DeleteRoomAsync Tests

        [Test]
        [Category("RoomService")]
        [Category("DeleteRoom")]
        public async Task DeleteRoomAsync_DeletesRoom_WhenUserIsOwner()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var room = new Room
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen",
                PhotoPath = "uploads/rooms/photo.jpg"
            };

            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockTaskRepository
                .Setup(r => r.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());

            _mockFileUploadService
                .Setup(f => f.DeleteFileAsync(room.PhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockRoomRepository
                .Setup(r => r.DeleteByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _roomService.DeleteRoomAsync(roomId, requestingUserId);

            // Assert
            _mockFileUploadService.Verify(f =>
                f.DeleteFileAsync(room.PhotoPath, It.IsAny<CancellationToken>()),
                Times.Once);
            _mockRoomRepository.Verify(r =>
                r.DeleteByIdAsync(roomId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        [Category("RoomService")]
        [Category("DeleteRoom")]
        public async Task DeleteRoomAsync_ThrowsUnauthorizedException_WhenUserNotOwner()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            var room = new Room
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen"
            };

            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("User is not an owner of this household"));

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _roomService.DeleteRoomAsync(roomId, requestingUserId));
        }

        #endregion

        #region Photo Management Tests

        [Test]
        [Category("RoomService")]
        [Category("PhotoManagement")]
        public async Task UploadRoomPhotoAsync_UploadsPhoto_WhenUserIsOwner()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";
            var photoPath = "uploads/rooms/new-photo.jpg";
            var oldPhotoPath = "uploads/rooms/old-photo.jpg";

            var room = new Room
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen",
                PhotoPath = oldPhotoPath
            };

            var mockPhoto = new Mock<IFormFile>();

            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockFileUploadService
                .Setup(f => f.DeleteFileAsync(room.PhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockFileUploadService
                .Setup(f => f.UploadRoomPhotoAsync(mockPhoto.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photoPath);

            _mockRoomRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _roomService.UploadRoomPhotoAsync(roomId, mockPhoto.Object, requestingUserId);

            // Assert
            Assert.That(result, Is.EqualTo(photoPath));
            _mockFileUploadService.Verify(f =>
                f.DeleteFileAsync(oldPhotoPath, It.IsAny<CancellationToken>()),
                Times.Once);
            _mockFileUploadService.Verify(f =>
                f.UploadRoomPhotoAsync(mockPhoto.Object, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        [Category("RoomService")]
        [Category("PhotoManagement")]
        public async Task DeleteRoomPhotoAsync_DeletesPhoto_WhenExists()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";
            var originalPhotoPath = "uploads/rooms/photo.jpg";

            var room = new Room
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen",
                PhotoPath =  originalPhotoPath
            };

            _mockRoomRepository
                .Setup(r => r.GetByIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            _mockHouseholdService
                .Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockFileUploadService
                .Setup(f => f.DeleteFileAsync(room.PhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockRoomRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _roomService.DeleteRoomPhotoAsync(roomId, requestingUserId);

            _mockFileUploadService.Verify(f =>
                f.DeleteFileAsync(originalPhotoPath, It.IsAny<CancellationToken>()),
                Times.Once);
            _mockRoomRepository.Verify(r =>
                r.UpdateAsync(It.Is<Room>(r => r.PhotoPath == null), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Validation Tests

        [Test]
        [Category("RoomService")]
        [Category("Validation")]
        public async Task IsNameUniqueInHouseholdAsync_ReturnsTrue_WhenNameIsUnique()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var name = "Kitchen";

            _mockRoomRepository
                .Setup(r => r.IsNameUniqueInHouseholdAsync(name, householdId, null, It.IsAny<CancellationToken>()))
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
            var householdId = Guid.NewGuid();
            var name = "Kitchen";

            _mockRoomRepository
                .Setup(r => r.IsNameUniqueInHouseholdAsync(name, householdId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _roomService.IsNameUniqueInHouseholdAsync(name, householdId);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion
    }
}