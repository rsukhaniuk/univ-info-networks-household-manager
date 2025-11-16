using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Implementations;
using Microsoft.AspNetCore.Identity;
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
    public class HouseholdServiceTests
    {
        private Mock<IHouseholdRepository> _mockHouseholdRepository;
        private Mock<IHouseholdMemberRepository> _mockMemberRepository;
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<ILogger<HouseholdService>> _mockLogger;
        private HouseholdService _householdService;

        [SetUp]
        public void Setup()
        {
            _mockHouseholdRepository = new Mock<IHouseholdRepository>();
            _mockMemberRepository = new Mock<IHouseholdMemberRepository>();
            _mockLogger = new Mock<ILogger<HouseholdService>>();

            // Mock UserManager - потрібен IUserStore
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object,
                null, null, null, null, null, null, null, null);

            _householdService = new HouseholdService(
                _mockHouseholdRepository.Object,
                _mockMemberRepository.Object,
                _mockUserManager.Object,
                _mockLogger.Object);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("CreateHousehold")]
        public async Task CreateHouseholdAsync_CreatesHouseholdWithOwner_WhenValidData()
        {
            // Arrange
            var name = "Test Household";
            var description = "Test Description";
            var ownerId = "user123";

            var createdHousehold = new Household
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                InviteCode = Guid.NewGuid()
            };

            var createdMember = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = createdHousehold.Id,
                UserId = ownerId,
                Role = HouseholdRole.Owner,
                JoinedAt = DateTime.UtcNow
            };

            _mockHouseholdRepository.Setup(r => r.AddAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdHousehold);
            _mockMemberRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdMember);

            // Act
            var result = await _householdService.CreateHouseholdAsync(name, description, ownerId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo(name));
            Assert.That(result.Description, Is.EqualTo(description));

            _mockHouseholdRepository.Verify(r => r.AddAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMemberRepository.Verify(r => r.AddAsync(It.Is<HouseholdMember>(m =>
                m.HouseholdId == createdHousehold.Id &&
                m.UserId == ownerId &&
                m.Role == HouseholdRole.Owner), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("GetHousehold")]
        public async Task GetHouseholdAsync_ReturnsHousehold_WhenExists()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var household = new Household
            {
                Id = householdId,
                Name = "Test Household",
                Description = "Test Description"
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
                .ReturnsAsync((Household)null);

            // Act
            var result = await _householdService.GetHouseholdAsync(householdId);

            // Assert
            Assert.That(result, Is.Null);
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
                InviteCode = inviteCode
            };

            var createdMember = new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = household.Id,
                UserId = userId,
                Role = HouseholdRole.Member,
                JoinedAt = DateTime.UtcNow
            };

            _mockHouseholdRepository.Setup(r => r.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(household.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockMemberRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdMember>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdMember);

            // Act
            var result = await _householdService.JoinHouseholdAsync(inviteCode, userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.HouseholdId, Is.EqualTo(household.Id));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.Role, Is.EqualTo(HouseholdRole.Member));

            _mockMemberRepository.Verify(r => r.AddAsync(It.Is<HouseholdMember>(m =>
                m.HouseholdId == household.Id &&
                m.UserId == userId &&
                m.Role == HouseholdRole.Member), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("JoinHousehold")]
        public async Task JoinHouseholdAsync_ThrowsException_WhenInvalidInviteCode()
        {
            // Arrange
            var inviteCode = Guid.NewGuid();
            var userId = "user123";

            _mockHouseholdRepository.Setup(r => r.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Household)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _householdService.JoinHouseholdAsync(inviteCode, userId));

            Assert.That(exception.Message, Is.EqualTo("Invalid invite code"));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("JoinHousehold")]
        public async Task JoinHouseholdAsync_ThrowsException_WhenUserAlreadyMember()
        {
            // Arrange
            var inviteCode = Guid.NewGuid();
            var userId = "user123";
            var household = new Household
            {
                Id = Guid.NewGuid(),
                Name = "Test Household",
                InviteCode = inviteCode
            };

            _mockHouseholdRepository.Setup(r => r.GetByInviteCodeAsync(inviteCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);
            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(household.Id, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _householdService.JoinHouseholdAsync(inviteCode, userId));

            Assert.That(exception.Message, Is.EqualTo("User is already a member of this household"));
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
        public async Task LeaveHouseholdAsync_ThrowsException_WhenLastOwner()
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
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _householdService.LeaveHouseholdAsync(householdId, userId));

            Assert.That(exception.Message, Contains.Substring("Cannot leave household as the last owner"));
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
        public async Task DeleteHouseholdAsync_ThrowsException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "user123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _householdService.DeleteHouseholdAsync(householdId, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("User is not an owner of this household"));
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

            _mockHouseholdRepository
                .Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _mockMemberRepository
                .Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);

            _mockHouseholdRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Household>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var updated = new Household
            {
                Id = householdId,
                Name = "Updated Name",
                Description = "Updated Description"
            };

            // Act
            await _householdService.UpdateHouseholdAsync(updated);

            // Assert
            _mockHouseholdRepository.Verify(r => r.UpdateAsync(
                It.Is<Household>(h =>
                    h.Id == householdId &&
                    h.Name == "Updated Name" &&
                    h.Description == "Updated Description"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("RegenerateInviteCode")]
        public async Task RegenerateInviteCodeAsync_GeneratesNewCode_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";
            var household = new Household
            {
                Id = householdId,
                Name = "Test Household",
                InviteCode = Guid.NewGuid()
            };

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);
            _mockHouseholdRepository.Setup(r => r.GetByIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);
            _mockHouseholdRepository.Setup(r => r.IsInviteCodeUniqueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockHouseholdRepository.Setup(r => r.UpdateAsync(household, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var originalInviteCode = household.InviteCode;

            // Act
            var result = await _householdService.RegenerateInviteCodeAsync(householdId, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.EqualTo(originalInviteCode));
            _mockHouseholdRepository.Verify(r => r.UpdateAsync(household, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateAccess")]
        public async Task ValidateOwnerAccessAsync_DoesNotThrow_WhenUserIsOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "owner123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Owner);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _householdService.ValidateOwnerAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateAccess")]
        public async Task ValidateOwnerAccessAsync_ThrowsException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockMemberRepository.Setup(r => r.GetUserRoleAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(HouseholdRole.Member);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _householdService.ValidateOwnerAccessAsync(householdId, userId));

            Assert.That(exception.Message, Is.EqualTo("User is not an owner of this household"));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateAccess")]
        public async Task ValidateUserAccessAsync_DoesNotThrow_WhenUserIsMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _householdService.ValidateUserAccessAsync(householdId, userId));
        }

        [Test]
        [Category("HouseholdService")]
        [Category("ValidateAccess")]
        public async Task ValidateUserAccessAsync_ThrowsException_WhenUserNotMember()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var userId = "user123";

            _mockMemberRepository.Setup(r => r.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _householdService.ValidateUserAccessAsync(householdId, userId));

            Assert.That(exception.Message, Is.EqualTo("User is not a member of this household"));
        }
    }
}
