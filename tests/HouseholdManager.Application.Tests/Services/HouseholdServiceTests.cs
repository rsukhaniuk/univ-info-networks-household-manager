using AutoMapper;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
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
        private Mock<IRoomRepository> _mockRoomRepository;
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IExecutionRepository> _mockExecutionRepository;
        private Mock<IFileUploadService> _mockFileUploadService;
        private IMapper _mapper; // Реальний AutoMapper
        private HouseholdService _householdService;

        [SetUp]
        public void Setup()
        {
            _mockHouseholdRepository = new Mock<IHouseholdRepository>();
            _mockMemberRepository = new Mock<IHouseholdMemberRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockRoomRepository = new Mock<IRoomRepository>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockExecutionRepository = new Mock<IExecutionRepository>();
            _mockFileUploadService = new Mock<IFileUploadService>();
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
                _mockRoomRepository.Object,
                _mockTaskRepository.Object,
                _mockExecutionRepository.Object,
                _mockFileUploadService.Object,
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

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId, Name = "Test Household" });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockTaskRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());
            _mockRoomRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Room>());
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

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId, Name = "Test Household" });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenException>(
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

        [Test]
        [Category("HouseholdService")]
        [Category("GetUserHouseholds")]
        public async Task GetUserHouseholdsAsync_ReturnsUserHouseholdsWithRoles()
        {
            // Arrange
            var userId = "user123";
            var household1 = new Household
            {
                Id = Guid.NewGuid(),
                Name = "Household 1",
                InviteCode = Guid.NewGuid(),
                Members = new List<HouseholdMember>(),
                Rooms = new List<Room>(),
                Tasks = new List<HouseholdTask>()
            };
            var household2 = new Household
            {
                Id = Guid.NewGuid(),
                Name = "Household 2",
                InviteCode = Guid.NewGuid(),
                Members = new List<HouseholdMember>(),
                Rooms = new List<Room>(),
                Tasks = new List<HouseholdTask>()
            };

            _mockHouseholdRepository.Setup(r => r.GetUserHouseholdsAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Household> { household1, household2 });
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(household1.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(household2.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act
            var result = await _householdService.GetUserHouseholdsAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("Household 1"));
            Assert.That(result[0].Role, Is.EqualTo(HouseholdRole.Owner));
            Assert.That(result[1].Name, Is.EqualTo("Household 2"));
            Assert.That(result[1].Role, Is.EqualTo(HouseholdRole.Member));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("RegenerateInviteCode")]
        public async Task RegenerateInviteCodeAsync_GeneratesNewCode_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";
            var oldInviteCode = Guid.NewGuid();
            var household = new Household
            {
                Id = householdId,
                Name = "Test Household",
                InviteCode = oldInviteCode,
                InviteCodeExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockHouseholdRepository.Setup(r => r.IsInviteCodeUniqueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockHouseholdRepository.Setup(r => r.UpdateAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _householdService.RegenerateInviteCodeAsync(householdId, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.InviteCode, Is.Not.EqualTo(oldInviteCode));
            Assert.That(result.InviteCode, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.InviteCodeExpiresAt, Is.Not.Null);
            Assert.That(result.InviteCodeExpiresAt, Is.GreaterThan(DateTime.UtcNow));

            _mockHouseholdRepository.Verify(r => r.UpdateAsync(
                It.Is<Household>(h => h.InviteCode != oldInviteCode),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("RegenerateInviteCode")]
        public void RegenerateInviteCodeAsync_ThrowsForbiddenException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "member123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId, Name = "Test" });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenException>(
                async () => await _householdService.RegenerateInviteCodeAsync(householdId, requestingUserId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("AddMember")]
        public async Task AddMemberAsync_AddsNewMember_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var newUserId = "newuser123";
            var requestingUserId = "owner123";
            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = newUserId,
                Role = HouseholdRole.Member,
                User = new ApplicationUser
                {
                    Id = newUserId,
                    Email = "newuser@test.com",
                    FirstName = "New",
                    LastName = "User"
                }
            };

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, newUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, newUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            // Act
            var result = await _householdService.AddMemberAsync(householdId, newUserId, HouseholdRole.Member, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserId, Is.EqualTo(newUserId));
            Assert.That(result.Role, Is.EqualTo("Member")); // Role is string in HouseholdMemberDto

            _mockMemberRepository.Verify(r => r.AddAsync(
                It.Is<HouseholdMember>(m =>
                    m.HouseholdId == householdId &&
                    m.UserId == newUserId &&
                    m.Role == HouseholdRole.Member),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("AddMember")]
        public void AddMemberAsync_ThrowsValidationException_WhenUserAlreadyMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "existinguser123";
            var requestingUserId = "owner123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            Assert.ThrowsAsync<ValidationException>(
                async () => await _householdService.AddMemberAsync(householdId, userId, HouseholdRole.Member, requestingUserId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("RemoveMember")]
        public async Task RemoveMemberAsync_RemovesMember_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var memberUserId = "member123";
            var requestingUserId = "owner123";
            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = memberUserId,
                Role = HouseholdRole.Member
            };

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, memberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.DeleteAsync(member, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdService.RemoveMemberAsync(householdId, memberUserId, requestingUserId);

            // Assert
            _mockMemberRepository.Verify(r => r.DeleteAsync(member, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("RemoveMember")]
        public void RemoveMemberAsync_ThrowsValidationException_WhenRemovingLastOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var ownerUserId = "owner123";
            var requestingUserId = "owner123";
            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = ownerUserId,
                Role = HouseholdRole.Owner
            };

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, ownerUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.GetOwnerCountAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(
                async () => await _householdService.RemoveMemberAsync(householdId, ownerUserId, requestingUserId));

            Assert.That(exception.Message, Does.Contain("last owner"));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateUserAccess")]
        public async Task ValidateUserAccessAsync_DoesNotThrow_WhenUserIsMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "member123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _householdService.ValidateUserAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateUserAccess")]
        public void ValidateUserAccessAsync_ThrowsForbiddenException_WhenUserNotMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "nonmember123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenException>(
                async () => await _householdService.ValidateUserAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateUserAccess")]
        public void ValidateUserAccessAsync_ThrowsNotFoundException_WhenHouseholdNotFound()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Household?)null);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(
                async () => await _householdService.ValidateUserAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateOwnerAccess")]
        public async Task ValidateOwnerAccessAsync_DoesNotThrow_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "owner123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _householdService.ValidateOwnerAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateOwnerAccess")]
        public void ValidateOwnerAccessAsync_ThrowsForbiddenException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "member123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenException>(
                async () => await _householdService.ValidateOwnerAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateUserAccess")]
        public async Task ValidateUserAccessAsync_AllowsSystemAdmin_EvenWhenNotMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var adminUserId = "admin123";

            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Household { Id = householdId });
            _mockUserRepository.Setup(r => r.IsSystemAdminAsync(adminUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _householdService.ValidateUserAccessAsync(householdId, adminUserId));

            // Verify that member check was not called because SystemAdmin was detected
            _mockMemberRepository.Verify(r => r.IsUserMemberAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}