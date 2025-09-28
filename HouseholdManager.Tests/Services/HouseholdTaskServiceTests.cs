using HouseholdManager.Models;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Implementations;
using HouseholdManager.Services.Interfaces;
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
    public class HouseholdTaskServiceTests
    {
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IHouseholdService> _mockHouseholdService;
        private Mock<IRoomService> _mockRoomService;
        private Mock<ITaskAssignmentService> _mockTaskAssignmentService;
        private Mock<ILogger<HouseholdTaskService>> _mockLogger;
        private HouseholdTaskService _householdTaskService;

        [SetUp]
        public void Setup()
        {
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockHouseholdService = new Mock<IHouseholdService>();
            _mockRoomService = new Mock<IRoomService>();
            _mockTaskAssignmentService = new Mock<ITaskAssignmentService>();
            _mockLogger = new Mock<ILogger<HouseholdTaskService>>();

            _householdTaskService = new HouseholdTaskService(
                _mockTaskRepository.Object,
                _mockHouseholdService.Object,
                _mockRoomService.Object,
                _mockTaskAssignmentService.Object,
                _mockLogger.Object);
        }

        #region CRUD Operations

        [Test]
        [Category("HouseholdTaskService")]
        [Category("CreateTask")]
        public async Task CreateTaskAsync_CreatesTask_WhenValidData()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var task = new HouseholdTask
            {
                Title = "Clean Kitchen",
                Description = "Deep clean the kitchen",
                Type = TaskType.Regular,
                Priority = TaskPriority.High,
                EstimatedMinutes = 60,
                ScheduledWeekday = DayOfWeek.Monday,
                HouseholdId = householdId,
                RoomId = roomId
            };

            var room = new Room
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen"
            };

            var createdTask = new HouseholdTask
            {
                Id = Guid.NewGuid(),
                Title = task.Title,
                HouseholdId = householdId,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.ValidateRoomAccessAsync(roomId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.GetRoomAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);
            _mockTaskRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdTask>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTask);

            // Act
            var result = await _householdTaskService.CreateTaskAsync(task, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Title, Is.EqualTo("Clean Kitchen"));
            Assert.That(result.IsActive, Is.True);

            _mockTaskRepository.Verify(r => r.AddAsync(It.Is<HouseholdTask>(t =>
                t.Title == "Clean Kitchen" &&
                t.HouseholdId == householdId &&
                t.RoomId == roomId &&
                t.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("CreateTask")]
        public async Task CreateTaskAsync_ThrowsException_WhenRoomNotInHousehold()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var otherHouseholdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var task = new HouseholdTask
            {
                Title = "Clean Kitchen",
                HouseholdId = householdId,
                RoomId = roomId
            };

            var room = new Room
            {
                Id = roomId,
                HouseholdId = otherHouseholdId, // Different household
                Name = "Kitchen"
            };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.ValidateRoomAccessAsync(roomId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.GetRoomAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _householdTaskService.CreateTaskAsync(task, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("Room does not belong to the specified household"));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("UpdateTask")]
        public async Task UpdateTaskAsync_UpdatesTask_WhenValidData()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var task = new HouseholdTask
            {
                Id = taskId,
                Title = "Updated Task",
                HouseholdId = householdId,
                RoomId = roomId
            };

            var room = new Room
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen"
            };

            var existingTask = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTask);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.GetRoomAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(room);
            _mockTaskRepository.Setup(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdTaskService.UpdateTaskAsync(task, requestingUserId);

            // Assert
            _mockTaskRepository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("DeleteTask")]
        public async Task DeleteTaskAsync_DeletesTask_WhenUserIsOwner()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var existingTask = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTask);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.DeleteByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdTaskService.DeleteTaskAsync(taskId, requestingUserId);

            // Assert
            _mockTaskRepository.Verify(r => r.DeleteByIdAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Assignment Operations

        [Test]
        [Category("HouseholdTaskService")]
        [Category("AssignTask")]
        public async Task AssignTaskAsync_AssignsTask_WhenValidRequest()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var requestingUserId = "owner123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.AssignTaskAsync(taskId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdTaskService.AssignTaskAsync(taskId, userId, requestingUserId);

            // Assert
            _mockTaskRepository.Verify(r => r.AssignTaskAsync(taskId, userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("AutoAssignTasks")]
        public async Task AutoAssignTasksAsync_CallsAssignmentService_WhenValidRequest()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";
            var assignments = new Dictionary<Guid, string>
            {
                { Guid.NewGuid(), "user1" },
                { Guid.NewGuid(), "user2" }
            };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskAssignmentService.Setup(s => s.AutoAssignAllTasksAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(assignments);

            // Act
            await _householdTaskService.AutoAssignTasksAsync(householdId, requestingUserId);

            // Assert
            _mockTaskAssignmentService.Verify(s => s.AutoAssignAllTasksAsync(householdId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("UnassignTask")]
        public async Task UnassignTaskAsync_UnassignsTask_WhenValidRequest()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.UnassignTaskAsync(taskId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdTaskService.UnassignTaskAsync(taskId, requestingUserId);

            // Assert
            _mockTaskRepository.Verify(r => r.UnassignTaskAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Task Status Operations

        [Test]
        [Category("HouseholdTaskService")]
        [Category("ActivateTask")]
        public async Task ActivateTaskAsync_ActivatesTask_WhenValidRequest()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                IsActive = false
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdTaskService.ActivateTaskAsync(taskId, requestingUserId);

            // Assert
            Assert.That(task.IsActive, Is.True);
            _mockTaskRepository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("DeactivateTask")]
        public async Task DeactivateTaskAsync_DeactivatesTask_WhenValidRequest()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                IsActive = true
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _householdTaskService.DeactivateTaskAsync(taskId, requestingUserId);

            // Assert
            Assert.That(task.IsActive, Is.False);
            _mockTaskRepository.Verify(r => r.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Complex Filtering

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetOverdueTasks")]
        public async Task GetOverdueTasksAsync_ReturnsOverdueTasks_WhenTasksOverdue()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var overdueTasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    Type = TaskType.OneTime,
                    DueDate = DateTime.UtcNow.AddDays(-1),
                    Title = "Overdue Task"
                }
            };

            _mockTaskRepository.Setup(r => r.GetOverdueTasksAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueTasks);

            // Act
            var result = await _householdTaskService.GetOverdueTasksAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().Title, Is.EqualTo("Overdue Task"));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetTasksForWeekday")]
        public async Task GetTasksForWeekdayAsync_ReturnsWeekdayTasks_WhenTasksExist()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var weekday = DayOfWeek.Monday;
            var weekdayTasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    Type = TaskType.Regular,
                    ScheduledWeekday = weekday,
                    Title = "Monday Task"
                }
            };

            _mockTaskRepository.Setup(r => r.GetRegularTasksByWeekdayAsync(householdId, weekday, It.IsAny<CancellationToken>()))
                .ReturnsAsync(weekdayTasks);

            // Act
            var result = await _householdTaskService.GetTasksForWeekdayAsync(householdId, weekday);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().ScheduledWeekday, Is.EqualTo(weekday));
        }

        #endregion

        #region Validation

        [Test]
        [Category("HouseholdTaskService")]
        [Category("ValidateTaskAccess")]
        public async Task ValidateTaskAccessAsync_DoesNotThrow_WhenUserHasAccess()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _householdTaskService.ValidateTaskAccessAsync(taskId, userId));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("ValidateTaskAccess")]
        public async Task ValidateTaskAccessAsync_ThrowsException_WhenTaskNotFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = "user123";

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _householdTaskService.ValidateTaskAccessAsync(taskId, userId));

            Assert.That(exception.Message, Is.EqualTo("Task not found"));
        }

        #endregion
    }
}
