using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Implementations;
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
    public class HouseholdMemberServiceTests
    {
        private Mock<IHouseholdMemberRepository> _mockMemberRepository;
        private Mock<IHouseholdRepository> _mockHouseholdRepository;
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<ILogger<HouseholdMemberService>> _mockLogger;
        private HouseholdMemberService _memberService;

        [SetUp]
        public void Setup()
        {
            _mockMemberRepository = new Mock<IHouseholdMemberRepository>();
            _mockHouseholdRepository = new Mock<IHouseholdRepository>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockLogger = new Mock<ILogger<HouseholdMemberService>>();

            _memberService = new HouseholdMemberService(
                _mockMemberRepository.Object,
                _mockHouseholdRepository.Object,
                _mockTaskRepository.Object,
                _mockLogger.Object);
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("GetMembers")]
        public async Task GetHouseholdMembersAsync_ReturnsMembers_WhenMembersExist()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var members = new List<HouseholdMember>
            {
                new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    UserId = "user1",
                    Role = HouseholdRole.Owner,
                    JoinedAt = DateTime.UtcNow.AddDays(-30),
                    User = new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe" }
                },
                new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    UserId = "user2",
                    Role = HouseholdRole.Member,
                    JoinedAt = DateTime.UtcNow.AddDays(-15),
                    User = new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith" }
                }
            };

            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            // Act
            var result = await _memberService.GetHouseholdMembersAsync(householdId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().Role, Is.EqualTo(HouseholdRole.Owner));
            Assert.That(result.Last().Role, Is.EqualTo(HouseholdRole.Member));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("GetMembers")]
        public async Task GetMembersByRoleAsync_ReturnsOnlyMembersWithSpecificRole()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var owners = new List<HouseholdMember>
            {
                new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    UserId = "owner1",
                    Role = HouseholdRole.Owner,
                    User = new ApplicationUser { Id = "owner1", FirstName = "Owner", LastName = "One" }
                }
            };

            _mockMemberRepository.Setup(r => r.GetMembersByRoleAsync(householdId, HouseholdRole.Owner, It.IsAny<CancellationToken>()))
                .ReturnsAsync(owners);

            // Act
            var result = await _memberService.GetMembersByRoleAsync(householdId, HouseholdRole.Owner);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().Role, Is.EqualTo(HouseholdRole.Owner));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("UpdateRole")]
        public async Task UpdateMemberRoleAsync_UpdatesRole_WhenValidRequest()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var newRole = HouseholdRole.Owner;
            var requestingUserId = "owner123";

            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Member
            };

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.UpdateRoleAsync(householdId, userId, newRole, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _memberService.UpdateMemberRoleAsync(householdId, userId, newRole, requestingUserId);

            // Assert
            _mockMemberRepository.Verify(r => r.UpdateRoleAsync(householdId, userId, newRole, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("UpdateRole")]
        public async Task UpdateMemberRoleAsync_ThrowsException_WhenRequestingUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var newRole = HouseholdRole.Owner;
            var requestingUserId = "member123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _memberService.UpdateMemberRoleAsync(householdId, userId, newRole, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("User is not an owner of this household"));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("UpdateRole")]
        public async Task UpdateMemberRoleAsync_ThrowsException_WhenDemotingLastOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "owner123";
            var newRole = HouseholdRole.Member;
            var requestingUserId = "owner123"; // Same user trying to demote themselves

            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Owner
            };

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.GetOwnerCountAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _memberService.UpdateMemberRoleAsync(householdId, userId, newRole, requestingUserId));

            Assert.That(exception.Message, Contains.Substring("Cannot demote yourself as the last owner"));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("UpdateRole")]
        public async Task UpdateMemberRoleAsync_ThrowsException_WhenDemotingLastOwnerByAnotherOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "owner1";
            var newRole = HouseholdRole.Member;
            var requestingUserId = "owner2";

            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Owner
            };

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.GetOwnerCountAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _memberService.UpdateMemberRoleAsync(householdId, userId, newRole, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("Cannot demote the last owner of the household"));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("PromoteUser")]
        public async Task PromoteToOwnerAsync_PromotesUser_WhenValidRequest()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var requestingUserId = "owner123";

            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Member
            };

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.UpdateRoleAsync(householdId, userId, HouseholdRole.Owner, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _memberService.PromoteToOwnerAsync(householdId, userId, requestingUserId);

            // Assert
            _mockMemberRepository.Verify(r => r.UpdateRoleAsync(householdId, userId, HouseholdRole.Owner, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("DemoteUser")]
        public async Task DemoteFromOwnerAsync_DemotesUser_WhenNotLastOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "owner1";
            var requestingUserId = "owner2";

            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Owner
            };

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mockMemberRepository.Setup(r => r.GetOwnerCountAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(2); // More than one owner
            _mockMemberRepository.Setup(r => r.UpdateRoleAsync(householdId, userId, HouseholdRole.Member, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _memberService.DemoteFromOwnerAsync(householdId, userId, requestingUserId);

            // Assert
            _mockMemberRepository.Verify(r => r.UpdateRoleAsync(householdId, userId, HouseholdRole.Member, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("Statistics")]
        public async Task GetMemberTaskCountsAsync_ReturnsTaskCounts_ForAllMembers()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var members = new List<HouseholdMember>
            {
                new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    UserId = "user1",
                    Role = HouseholdRole.Owner
                },
                new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    UserId = "user2",
                    Role = HouseholdRole.Member
                }
            };

            var activeTasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    AssignedUserId = "user1",
                    IsActive = true
                },
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    AssignedUserId = "user1",
                    IsActive = true
                },
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    AssignedUserId = "user2",
                    IsActive = true
                }
            };

            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeTasks);

            // Act
            var result = await _memberService.GetMemberTaskCountsAsync(householdId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result["user1"], Is.EqualTo(2));
            Assert.That(result["user2"], Is.EqualTo(1));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("Statistics")]
        public async Task GetMemberCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var memberCount = 5;

            _mockMemberRepository.Setup(r => r.GetMemberCountAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memberCount);

            // Act
            var result = await _memberService.GetMemberCountAsync(householdId);

            // Assert
            Assert.That(result, Is.EqualTo(memberCount));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("Statistics")]
        public async Task GetOwnerCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var ownerCount = 2;

            _mockMemberRepository.Setup(r => r.GetOwnerCountAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ownerCount);

            // Act
            var result = await _memberService.GetOwnerCountAsync(householdId);

            // Assert
            Assert.That(result, Is.EqualTo(ownerCount));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("Validation")]
        public async Task ValidateMemberAccessAsync_DoesNotThrow_WhenUserIsMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _memberService.ValidateMemberAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("Validation")]
        public async Task ValidateMemberAccessAsync_ThrowsException_WhenUserNotMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _memberService.ValidateMemberAccessAsync(householdId, userId));

            Assert.That(exception.Message, Is.EqualTo("User is not a member of this household"));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("Validation")]
        public async Task ValidateOwnerAccessAsync_DoesNotThrow_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "owner123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _memberService.ValidateOwnerAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("Validation")]
        public async Task ValidateOwnerAccessAsync_ThrowsException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _memberService.ValidateOwnerAccessAsync(householdId, userId));

            Assert.That(exception.Message, Is.EqualTo("User is not an owner of this household"));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("GetMembers")]
        public async Task GetUserMembershipsAsync_ReturnsUserMemberships()
        {
            // Arrange
            var userId = "user123";
            var memberships = new List<HouseholdMember>
            {
                new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = Guid.NewGuid(),
                    UserId = userId,
                    Role = HouseholdRole.Owner,
                    Household = new Household { Id = Guid.NewGuid(), Name = "Home 1" }
                },
                new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = Guid.NewGuid(),
                    UserId = userId,
                    Role = HouseholdRole.Member,
                    Household = new Household { Id = Guid.NewGuid(), Name = "Home 2" }
                }
            };

            _mockMemberRepository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memberships);

            // Act
            var result = await _memberService.GetUserMembershipsAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First().Role, Is.EqualTo(HouseholdRole.Owner));
            Assert.That(result.Last().Role, Is.EqualTo(HouseholdRole.Member));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("GetMembers")]
        public async Task GetMemberAsync_ReturnsMember_WhenExists()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var member = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = userId,
                Role = HouseholdRole.Member,
                User = new ApplicationUser { Id = userId, FirstName = "John", LastName = "Doe" },
                Household = new Household { Id = householdId, Name = "Test Home" }
            };

            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            // Act
            var result = await _memberService.GetMemberAsync(householdId, userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.HouseholdId, Is.EqualTo(householdId));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.Role, Is.EqualTo(HouseholdRole.Member));
        }

        [Test]
        [Category("HouseholdMemberService")]
        [Category("GetMembers")]
        public async Task GetMemberAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockMemberRepository.Setup(r => r.GetMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdMember)null);

            // Act
            var result = await _memberService.GetMemberAsync(householdId, userId);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}
