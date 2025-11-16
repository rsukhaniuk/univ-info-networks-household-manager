using HouseholdManager.Models.Entities;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Tests.Services
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<UserManager<ApplicationUser>> _mockUserManager;
        private Mock<IHouseholdMemberRepository> _mockHouseholdMemberRepository;
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IExecutionRepository> _mockExecutionRepository;
        private Mock<IUserStore<ApplicationUser>> _mockUserStore;
        private Mock<ILogger<UserService>> _mockLogger;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            _mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                _mockUserStore.Object, null, null, null, null, null, null, null, null);
            _mockHouseholdMemberRepository = new Mock<IHouseholdMemberRepository>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockExecutionRepository = new Mock<IExecutionRepository>();
            _mockLogger = new Mock<ILogger<UserService>>();

            _userService = new UserService(
                _mockUserManager.Object,
                _mockHouseholdMemberRepository.Object,
                _mockTaskRepository.Object,
                _mockExecutionRepository.Object,
                _mockLogger.Object);
        }

        #region Profile Management Operations

        [Test]
        [Category("UserService")]
        [Category("ProfileManagement")]
        public async Task UpdateUserProfileAsync_UpdatesProfile_WhenValidData()
        {
            // Arrange
            var userId = "user123";
            var firstName = "John";
            var lastName = "Doe";
            var email = "john.doe@example.com";

            var user = new ApplicationUser
            {
                Id = userId,
                FirstName = "Old",
                LastName = "Name",
                Email = "old@example.com",
                UserName = "old@example.com"
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(um => um.SetEmailAsync(user, email))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.SetUserNameAsync(user, email))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, firstName, lastName, email);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(user.FirstName, Is.EqualTo(firstName));
            Assert.That(user.LastName, Is.EqualTo(lastName));

            _mockUserManager.Verify(um => um.SetEmailAsync(user, email), Times.Once);
            _mockUserManager.Verify(um => um.SetUserNameAsync(user, email), Times.Once);
            _mockUserManager.Verify(um => um.UpdateAsync(user), Times.Once);
        }

        [Test]
        [Category("UserService")]
        [Category("ProfileManagement")]
        public async Task UpdateUserProfileAsync_ReturnsFailure_WhenUserNotFound()
        {
            // Arrange
            var userId = "nonexistent";

            _mockUserManager.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, "John", "Doe", "john@example.com");

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Is.EqualTo("User not found"));
        }

        [Test]
        [Category("UserService")]
        [Category("ProfileManagement")]
        public async Task ChangePasswordAsync_ChangesPassword_WhenValidData()
        {
            // Arrange
            var userId = "user123";
            var currentPassword = "OldPassword123!";
            var newPassword = "NewPassword123!";

            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockUserManager.Setup(um => um.ChangePasswordAsync(user, currentPassword, newPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(um => um.ChangePasswordAsync(user, currentPassword, newPassword), Times.Once);
        }

        [Test]
        [Category("UserService")]
        [Category("ProfileManagement")]
        public async Task SetCurrentHouseholdAsync_SetsHousehold_WhenUserIsMember()
        {
            // Arrange
            var userId = "user123";
            var householdId = Guid.NewGuid();

            var user = new ApplicationUser { Id = userId };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(user);
            _mockHouseholdMemberRepository.Setup(repo => repo.IsUserMemberAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUserManager.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            await _userService.SetCurrentHouseholdAsync(userId, householdId);

            // Assert
            Assert.That(user.CurrentHouseholdId, Is.EqualTo(householdId));
            _mockUserManager.Verify(um => um.UpdateAsync(user), Times.Once);
        }

        #endregion

        #region User Creation and Management Operations

        [Test]
        [Category("UserService")]
        [Category("UserManagement")]
        public async Task CreateUserAsync_CreatesUser_WhenRequestingUserIsSystemAdmin()
        {
            // Arrange
            var requestingUserId = "admin123";
            var password = "Password123!";
            var newUser = new ApplicationUser
            {
                Id = "newuser123",
                Email = "newuser@example.com",
                UserName = "newuser@example.com"
            };

            var adminUser = new ApplicationUser
            {
                Id = requestingUserId,
                Role = SystemRole.SystemAdmin
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(requestingUserId))
                .ReturnsAsync(adminUser);
            _mockUserManager.Setup(um => um.CreateAsync(newUser, password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.CreateUserAsync(newUser, password, requestingUserId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(newUser.CreatedAt, Is.Not.EqualTo(default(DateTime)));
            _mockUserManager.Verify(um => um.CreateAsync(newUser, password), Times.Once);
        }

        [Test]
        [Category("UserService")]
        [Category("UserManagement")]
        public async Task CreateUserAsync_ThrowsException_WhenRequestingUserIsNotSystemAdmin()
        {
            // Arrange
            var requestingUserId = "user123";
            var password = "Password123!";
            var newUser = new ApplicationUser { Id = "newuser123" };

            var regularUser = new ApplicationUser
            {
                Id = requestingUserId,
                Role = SystemRole.User
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(requestingUserId))
                .ReturnsAsync(regularUser);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _userService.CreateUserAsync(newUser, password, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("System administrator access required"));
        }

        [Test]
        [Category("UserService")]
        [Category("UserManagement")]
        public async Task DeleteUserAsync_DeletesUser_WhenValidRequest()
        {
            // Arrange
            var requestingUserId = "admin123";
            var userToDeleteId = "user123";

            var adminUser = new ApplicationUser
            {
                Id = requestingUserId,
                Role = SystemRole.SystemAdmin
            };

            var userToDelete = new ApplicationUser { Id = userToDeleteId };

            _mockUserManager.Setup(um => um.FindByIdAsync(requestingUserId))
                .ReturnsAsync(adminUser);
            _mockUserManager.Setup(um => um.FindByIdAsync(userToDeleteId))
                .ReturnsAsync(userToDelete);
            _mockUserManager.Setup(um => um.DeleteAsync(userToDelete))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.DeleteUserAsync(userToDeleteId, requestingUserId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            _mockUserManager.Verify(um => um.DeleteAsync(userToDelete), Times.Once);
        }

        [Test]
        [Category("UserService")]
        [Category("UserManagement")]
        public async Task DeleteUserAsync_ReturnsFailure_WhenAttemptingSelfDeletion()
        {
            // Arrange
            var requestingUserId = "admin123";

            var adminUser = new ApplicationUser
            {
                Id = requestingUserId,
                Role = SystemRole.SystemAdmin
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(requestingUserId))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _userService.DeleteUserAsync(requestingUserId, requestingUserId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Is.EqualTo("Cannot delete your own account"));
        }

        #endregion

        #region Role Management Operations

        [Test]
        [Category("UserService")]
        [Category("RoleManagement")]
        public async Task SetSystemRoleAsync_UpdatesRole_WhenValidRequest()
        {
            // Arrange
            var requestingUserId = "admin123";
            var targetUserId = "user123";
            var newRole = SystemRole.SystemAdmin;

            var adminUser = new ApplicationUser
            {
                Id = requestingUserId,
                Role = SystemRole.SystemAdmin
            };

            var targetUser = new ApplicationUser
            {
                Id = targetUserId,
                Role = SystemRole.User
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(requestingUserId))
                .ReturnsAsync(adminUser);
            _mockUserManager.Setup(um => um.FindByIdAsync(targetUserId))
                .ReturnsAsync(targetUser);
            _mockUserManager.Setup(um => um.UpdateAsync(targetUser))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.SetSystemRoleAsync(targetUserId, newRole, requestingUserId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(targetUser.Role, Is.EqualTo(newRole));
            _mockUserManager.Verify(um => um.UpdateAsync(targetUser), Times.Once);
        }

        [Test]
        [Category("UserService")]
        [Category("RoleManagement")]
        public async Task SetSystemRoleAsync_ReturnsFailure_WhenAttemptingToChangeOwnRole()
        {
            // Arrange
            var requestingUserId = "admin123";
            var newRole = SystemRole.User;

            var adminUser = new ApplicationUser
            {
                Id = requestingUserId,
                Role = SystemRole.SystemAdmin
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(requestingUserId))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _userService.SetSystemRoleAsync(requestingUserId, newRole, requestingUserId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.First().Description, Is.EqualTo("Cannot change your own system role"));
        }

        #endregion

        #region Search and Listing Operations

        [Test]
        [Category("UserService")]
        [Category("UserSearch")]
        public async Task SearchUsersAsync_ReturnsMatchingUsers_WhenSearchTermProvided()
        {
            // Arrange
            var searchTerm = "john";
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", FirstName = "John", LastName = "Doe", Email = "john.doe@example.com" },
                new ApplicationUser { Id = "2", FirstName = "Jane", LastName = "Johnson", Email = "jane.johnson@example.com" },
                new ApplicationUser { Id = "3", FirstName = "Bob", LastName = "Smith", Email = "bob.smith@example.com" }
            };

            var mockUsers = users.BuildMock();

            _mockUserManager
                .SetupGet(um => um.Users)
                .Returns(mockUsers);

            // Act
            var result = await _userService.SearchUsersAsync(searchTerm);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2)); // John Doe and Jane Johnson
            Assert.That(result.Any(u => u.FirstName == "John"), Is.True);
            Assert.That(result.Any(u => u.LastName == "Johnson"), Is.True);
        }

        [Test]
        [Category("UserService")]
        [Category("UserSearch")]
        public async Task GetHouseholdUsersAsync_ReturnsHouseholdMembers_WhenMembersExist()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = "user1", HouseholdId = householdId },
                new HouseholdMember { UserId = "user2", HouseholdId = householdId }
            };

            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "user1", FirstName = "John", LastName = "Doe" },
                new ApplicationUser { Id = "user2", FirstName = "Jane", LastName = "Smith" },
                new ApplicationUser { Id = "user3", FirstName = "Bob",  LastName = "Johnson" }
            };

            var mockUsers = users.BuildMock(); 

            _mockHouseholdMemberRepository
                .Setup(repo => repo.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            _mockUserManager
                .Setup(um => um.Users)
                .Returns(mockUsers); 

            // Act
            var result = await _userService.GetHouseholdUsersAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.Any(u => u.Id == "user1"), Is.True);
            Assert.That(result.Any(u => u.Id == "user2"), Is.True);
            Assert.That(result.Any(u => u.Id == "user3"), Is.False);
        }

        #endregion

        #region Statistics and Dashboard Operations

        [Test]
        [Category("UserService")]
        [Category("Statistics")]
        public async Task GetUserDashboardStatsAsync_ReturnsStats_WhenUserHasMemberships()
        {
            // Arrange
            var userId = "user123";
            var householdId1 = Guid.NewGuid();
            var householdId2 = Guid.NewGuid();

            var memberships = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = userId, HouseholdId = householdId1, Role = HouseholdRole.Owner },
                new HouseholdMember { UserId = userId, HouseholdId = householdId2, Role = HouseholdRole.Member }
            };

            var userTasks = new List<HouseholdTask>
            {
                new HouseholdTask { Id = Guid.NewGuid(), AssignedUserId = userId },
                new HouseholdTask { Id = Guid.NewGuid(), AssignedUserId = userId }
            };

            var executions = new List<TaskExecution>
            {
                new TaskExecution { Id = Guid.NewGuid(), UserId = userId, CompletedAt = DateTime.UtcNow.AddHours(-1) }
            };

            _mockHouseholdMemberRepository.Setup(repo => repo.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memberships);
            _mockTaskRepository.Setup(repo => repo.GetByAssignedUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userTasks);
            _mockExecutionRepository.Setup(repo => repo.GetUserExecutionsThisWeekAsync(userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _userService.GetUserDashboardStatsAsync(userId);

            // Assert
            Assert.That(result.TotalHouseholds, Is.EqualTo(2));
            Assert.That(result.OwnedHouseholds, Is.EqualTo(1));
            Assert.That(result.ActiveTasks, Is.EqualTo(2));
            Assert.That(result.CompletedTasksThisWeek, Is.EqualTo(2)); // 1 execution per household
            Assert.That(result.LastActivity, Is.Not.Null);
        }

        [Test]
        [Category("UserService")]
        [Category("Statistics")]
        public async Task GetUserActivitySummaryAsync_ReturnsFormattedSummary_WhenStatsExist()
        {
            // Arrange
            var userId = "user123";
            var memberships = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = userId, HouseholdId = Guid.NewGuid(), Role = HouseholdRole.Owner }
            };

            _mockHouseholdMemberRepository.Setup(repo => repo.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memberships);
            _mockTaskRepository.Setup(repo => repo.GetByAssignedUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());
            _mockExecutionRepository.Setup(repo => repo.GetUserExecutionsThisWeekAsync(userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution>());

            // Act
            var result = await _userService.GetUserActivitySummaryAsync(userId);

            // Assert
            Assert.That(result.ContainsKey("totalHouseholds"), Is.True);
            Assert.That(result.ContainsKey("ownedHouseholds"), Is.True);
            Assert.That(result.ContainsKey("activeTasks"), Is.True);
            Assert.That(result.ContainsKey("completedThisWeek"), Is.True);
            Assert.That(result.ContainsKey("lastActivity"), Is.True);
            Assert.That(result["lastActivity"], Is.EqualTo("Never"));
        }

        #endregion

        #region Validation Operations

        [Test]
        [Category("UserService")]
        [Category("Validation")]
        public async Task ValidateUserAccessAsync_DoesNotThrow_WhenUserAccessesOwnProfile()
        {
            // Arrange
            var userId = "user123";

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _userService.ValidateUserAccessAsync(userId, userId));
        }

        [Test]
        [Category("UserService")]
        [Category("Validation")]
        public async Task ValidateUserAccessAsync_DoesNotThrow_WhenSystemAdminAccessesAnyProfile()
        {
            // Arrange
            var userId = "user123";
            var adminUserId = "admin123";

            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                Role = SystemRole.SystemAdmin
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(adminUserId))
                .ReturnsAsync(adminUser);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _userService.ValidateUserAccessAsync(userId, adminUserId));
        }

        [Test]
        [Category("UserService")]
        [Category("Validation")]
        public async Task ValidateUserAccessAsync_ThrowsException_WhenRegularUserAccessesOtherProfile()
        {
            // Arrange
            var userId = "user123";
            var requestingUserId = "user456";

            var requestingUser = new ApplicationUser
            {
                Id = requestingUserId,
                Role = SystemRole.User
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(requestingUserId))
                .ReturnsAsync(requestingUser);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _userService.ValidateUserAccessAsync(userId, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("Access denied"));
        }

        [Test]
        [Category("UserService")]
        [Category("Validation")]
        public async Task IsSystemAdminAsync_ReturnsTrue_WhenUserIsSystemAdmin()
        {
            // Arrange
            var userId = "admin123";
            var adminUser = new ApplicationUser
            {
                Id = userId,
                Role = SystemRole.SystemAdmin
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(adminUser);

            // Act
            var result = await _userService.IsSystemAdminAsync(userId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        [Category("UserService")]
        [Category("Validation")]
        public async Task IsSystemAdminAsync_ReturnsFalse_WhenUserIsNotSystemAdmin()
        {
            // Arrange
            var userId = "user123";
            var regularUser = new ApplicationUser
            {
                Id = userId,
                Role = SystemRole.User
            };

            _mockUserManager.Setup(um => um.FindByIdAsync(userId))
                .ReturnsAsync(regularUser);

            // Act
            var result = await _userService.IsSystemAdminAsync(userId);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion
    }
}
