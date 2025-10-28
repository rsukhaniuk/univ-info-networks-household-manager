using AutoMapper;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Mapping;
using HouseholdManager.Application.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace HouseholdManager.Application.Tests.Services
{
    [TestFixture]
    public class HouseholdServiceTests
    {
        private Mock<IHouseholdRepository> _mockHouseholdRepository;
        private Mock<IHouseholdMemberRepository> _mockMemberRepository;
        private Mock<ILogger<HouseholdService>> _mockLogger;
        private Mock<IUserRepository> _mockUserRepository;
        private IMapper _mapper; // Реальний AutoMapper
        private HouseholdService _householdService;

        [SetUp]
        public void Setup()
        {
            _mockHouseholdRepository = new Mock<IHouseholdRepository>();
            _mockMemberRepository = new Mock<IHouseholdMemberRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<HouseholdService>>();


            var loggerFactory = LoggerFactory.Create(b => { });

            var config = new MapperConfiguration(
                cfg => { cfg.AddMaps(typeof(HouseholdProfile).Assembly); },
                loggerFactory
            );

            _mapper = config.CreateMapper();

            _householdService = new HouseholdService(
                _mockHouseholdRepository.Object,
                _mockMemberRepository.Object,
                _mockUserRepository.Object,
                _mapper,
                _mockLogger.Object);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("CreateHousehold")]
        public async Task CreateHouseholdAsync_CreatesHouseholdWithOwner_WhenValidData()
        {
            // Arrange
            var request = new UpsertHouseholdRequest
            {
                Name = "Test Household",
                Description = "Test Description"
            };
            var ownerId = "user123";

            var createdHousehold = new Household
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                InviteCode = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            _mockHouseholdRepository.Setup(r => r.AddAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdHousehold);

            _mockMemberRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = createdHousehold.Id,
                    UserId = ownerId,
                    Role = HouseholdRole.Owner
                });

            // Act
            var result = await _householdService.CreateHouseholdAsync(request, ownerId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(request.Name));
            Assert.That(result.Description, Is.EqualTo(request.Description));

            _mockHouseholdRepository.Verify(r => r.AddAsync(
                It.Is<Household>(h => h.Name == request.Name),
                It.IsAny<CancellationToken>()), Times.Once);

            _mockMemberRepository.Verify(r => r.AddAsync(
                It.Is<HouseholdMember>(m =>
                    m.HouseholdId == createdHousehold.Id &&
                    m.UserId == ownerId &&
                    m.Role == HouseholdRole.Owner),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("GetHousehold")]
        public async Task GetHouseholdAsync_ReturnsDto_WhenExists()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var household = new Household
            {
                Id = householdId,
                Name = "Test Household",
                Description = "Test Description",
                InviteCode = Guid.NewGuid(),
                Members = new List<HouseholdMember>(),
                Rooms = new List<Room>(),
                Tasks = new List<HouseholdTask>()
            };

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            // Act
            var result = await _householdService.GetHouseholdAsync(householdId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(householdId));
            Assert.That(result.Name, Is.EqualTo("Test Household"));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("GetHousehold")]
        public async Task GetHouseholdAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Household?)null);

            // Act
            var result = await _householdService.GetHouseholdAsync(householdId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("GetHouseholdDetails")]
        public async Task GetHouseholdWithMembersAsync_ReturnsDetailsDto_WithMappedData()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId1 = "user1";
            var userId2 = "user2";

            var household = new Household
            {
                Id = householdId,
                Name = "Test Household",
                InviteCode = Guid.NewGuid(),
                Members = new List<HouseholdMember>
                {
                    new HouseholdMember
                    {
                        UserId = userId1,
                        HouseholdId = householdId,
                        Role = HouseholdRole.Owner,
                        User = new ApplicationUser
                        {
                            Id = userId1,
                            Email = "user1@test.com",
                            FirstName = "John",
                            LastName = "Doe"
                        }
                    },
                    new HouseholdMember
                    {
                        UserId = userId2,
                        HouseholdId = householdId,
                        Role = HouseholdRole.Member,
                        User = new ApplicationUser
                        {
                            Id = userId2,
                            Email = "user2@test.com",
                            FirstName = "Jane",
                            LastName = "Smith"
                        }
                    }
                },
                Rooms = new List<Room>
                {
                    new Room { Id = Guid.NewGuid(), Name = "Kitchen", HouseholdId = householdId, Tasks = new List<HouseholdTask>() },
                    new Room { Id = Guid.NewGuid(), Name = "Bathroom", HouseholdId = householdId, Tasks = new List<HouseholdTask>() }
                },
                Tasks = new List<HouseholdTask>
                {
                    new HouseholdTask
                    {
                        Id = Guid.NewGuid(),
                        Title = "Task 1",
                        IsActive = true,
                        AssignedUserId = userId1,
                        HouseholdId = householdId,
                        Room = new Room { Name = "Kitchen" }
                    },
                    new HouseholdTask
                    {
                        Id = Guid.NewGuid(),
                        Title = "Task 2",
                        IsActive = true,
                        AssignedUserId = userId2,
                        HouseholdId = householdId,
                        Room = new Room { Name = "Bathroom" }
                    },
                    new HouseholdTask
                    {
                        Id = Guid.NewGuid(),
                        Title = "Inactive Task",
                        IsActive = false,
                        HouseholdId = householdId,
                        Room = new Room { Name = "Kitchen" }
                    }
                }
            };

            _mockHouseholdRepository.Setup(r => r.GetByIdWithMembersAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            // Act
            var result = await _householdService.GetHouseholdWithMembersAsync(householdId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Household.Name, Is.EqualTo("Test Household"));
            Assert.That(result.Members.Count, Is.EqualTo(2));
            Assert.That(result.Rooms.Count, Is.EqualTo(2));
            Assert.That(result.ActiveTasks.Count, Is.EqualTo(2)); // Тільки активні

            // Перевірка TaskCountsByUser
            Assert.That(result.TaskCountsByUser[userId1], Is.EqualTo(1));
            Assert.That(result.TaskCountsByUser[userId2], Is.EqualTo(1));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("JoinHousehold")]
        public async Task JoinHouseholdAsync_AddsUserAsMember_WhenValidInviteCode()
        {
            // Arrange
            var inviteCode = Guid.NewGuid();
            var userId = "user123";
            var household = new Household
            {
                Id = Guid.NewGuid(),
                Name = "Test Household",
                InviteCode = inviteCode,
                Members = new List<HouseholdMember>(),
                Rooms = new List<Room>(),
                Tasks = new List<HouseholdTask>()
            };

            _mockHouseholdRepository.Setup(r => r.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(household.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HouseholdMember
                {
                    HouseholdId = household.Id,
                    UserId = userId,
                    Role = HouseholdRole.Member
                });

            var request = new JoinHouseholdRequest { InviteCode = inviteCode };

            // Act
            var result = await _householdService.JoinHouseholdAsync(request, userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(household.Id));

            _mockMemberRepository.Verify(r => r.AddAsync(
                It.Is<HouseholdMember>(m =>
                    m.HouseholdId == household.Id &&
                    m.UserId == userId &&
                    m.Role == HouseholdRole.Member),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("JoinHousehold")]
        public void JoinHouseholdAsync_ThrowsNotFoundException_WhenInvalidInviteCode()
        {
            // Arrange
            var inviteCode = Guid.NewGuid();
            var userId = "user123";

            _mockHouseholdRepository.Setup(r => r.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Household?)null);

            var request = new JoinHouseholdRequest { InviteCode = inviteCode };

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(
                async () => await _householdService.JoinHouseholdAsync(request, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("JoinHousehold")]
        public void JoinHouseholdAsync_ThrowsValidationException_WhenUserAlreadyMember()
        {
            // Arrange
            var inviteCode = Guid.NewGuid();
            var userId = "user123";
            var household = new Household
            {
                Id = Guid.NewGuid(),
                InviteCode = inviteCode
            };

            _mockHouseholdRepository.Setup(r => r.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(household.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var request = new JoinHouseholdRequest { InviteCode = inviteCode };

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(
                async () => await _householdService.JoinHouseholdAsync(request, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("UpdateHousehold")]
        public async Task UpdateHouseholdAsync_UpdatesHousehold_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";
            var existing = new Household
            {
                Id = householdId,
                Name = "Old Name",
                Description = "Old Description",
                InviteCode = Guid.NewGuid()
            };

            var request = new UpsertHouseholdRequest
            {
                Id = householdId,
                Name = "Updated Name",
                Description = "Updated Description"
            };

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);

            _mockHouseholdRepository.Setup(r => r.UpdateAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _householdService.UpdateHouseholdAsync(householdId, request, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Updated Name"));
            Assert.That(result.Description, Is.EqualTo("Updated Description"));

            _mockHouseholdRepository.Verify(r => r.UpdateAsync(
                It.Is<Household>(h =>
                    h.Id == householdId &&
                    h.Name == "Updated Name" &&
                    h.Description == "Updated Description"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("DeleteHousehold")]
        public async Task DeleteHouseholdAsync_DeletesHousehold_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockHouseholdRepository.Setup(r => r.DeleteByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdService.DeleteHouseholdAsync(householdId, requestingUserId);

            // Assert
            _mockHouseholdRepository.Verify(r => r.DeleteByIdAsync(householdId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("DeleteHousehold")]
        public void DeleteHouseholdAsync_ThrowsUnauthorizedException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _householdService.DeleteHouseholdAsync(householdId, requestingUserId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("LeaveHousehold")]
        public async Task LeaveHouseholdAsync_RemovesMember_WhenNotLastOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Member
            };

            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.DeleteAsync(member, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdService.LeaveHouseholdAsync(householdId, userId);

            // Assert
            _mockMemberRepository.Verify(r => r.DeleteAsync(member, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("LeaveHousehold")]
        public void LeaveHouseholdAsync_ThrowsValidationException_WhenLastOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Owner
            };

            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.GetOwnerCountAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(
                async () => await _householdService.LeaveHouseholdAsync(householdId, userId));

            Assert.That(exception.Message, Does.Contain("last owner"));
        }
    }
}