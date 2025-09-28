using HouseholdManager.Models;
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
    public class TaskAssignmentServiceTests
    {
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IHouseholdMemberRepository> _mockMemberRepository;
        private Mock<ILogger<TaskAssignmentService>> _mockLogger;
        private TaskAssignmentService _taskAssignmentService;

        [SetUp]
        public void Setup()
        {
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockMemberRepository = new Mock<IHouseholdMemberRepository>();
            _mockLogger = new Mock<ILogger<TaskAssignmentService>>();

            _taskAssignmentService = new TaskAssignmentService(
                _mockTaskRepository.Object,
                _mockMemberRepository.Object,
                _mockLogger.Object);
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("AssignTask")]
        public async Task AssignTaskAsync_AssignsTaskToSuggestedUser_WhenTaskExists()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var suggestedUserId = "user123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Title = "Test Task"
            };

            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = suggestedUserId, HouseholdId = householdId }
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());
            _mockTaskRepository.Setup(r => r.AssignTaskAsync(taskId, suggestedUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _taskAssignmentService.AssignTaskAsync(taskId);

            // Assert
            Assert.That(result, Is.EqualTo(suggestedUserId));
            _mockTaskRepository.Verify(r => r.AssignTaskAsync(taskId, suggestedUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("AssignTask")]
        public async Task AssignTaskAsync_ThrowsException_WhenTaskNotFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _taskAssignmentService.AssignTaskAsync(taskId));

            Assert.That(exception.Message, Is.EqualTo($"Task with ID {taskId} not found"));
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("AutoAssignWeeklyTasks")]
        public async Task AutoAssignWeeklyTasksAsync_AssignsTasksByWeekday_WhenTasksExist()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var user1Id = "user1";

            var mondayTasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    ScheduledWeekday = DayOfWeek.Monday,
                    Priority = TaskPriority.High,
                    AssignedUserId = null
                }
            };

            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = user1Id, HouseholdId = householdId }
            };

            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());

            _mockTaskRepository.Setup(r => r.GetRegularTasksByWeekdayAsync(householdId, It.IsAny<DayOfWeek>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());

            // Окремо для понеділка
            _mockTaskRepository.Setup(r => r.GetRegularTasksByWeekdayAsync(householdId, DayOfWeek.Monday, It.IsAny<CancellationToken>()))
                .ReturnsAsync(mondayTasks);

            _mockTaskRepository.Setup(r => r.BulkAssignTasksAsync(It.IsAny<Dictionary<Guid, string>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _taskAssignmentService.AutoAssignWeeklyTasksAsync(householdId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.Values.First(), Is.EqualTo(user1Id));
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("ReassignTaskToNextUser")]
        public async Task ReassignTaskToNextUserAsync_AssignsToNextUser_WhenMultipleMembers()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var currentUserId = "user1";
            var nextUserId = "user2";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                AssignedUserId = currentUserId
            };

            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = currentUserId, HouseholdId = householdId },
                new HouseholdMember { UserId = nextUserId, HouseholdId = householdId },
                new HouseholdMember { UserId = "user3", HouseholdId = householdId }
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.AssignTaskAsync(taskId, nextUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _taskAssignmentService.ReassignTaskToNextUserAsync(taskId);

            // Assert
            Assert.That(result, Is.EqualTo(nextUserId));
            _mockTaskRepository.Verify(r => r.AssignTaskAsync(taskId, nextUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("ReassignTaskToNextUser")]
        public async Task ReassignTaskToNextUserAsync_AssignsToSameUser_WhenOnlyOneMember()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var singleUserId = "user1";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                AssignedUserId = singleUserId
            };

            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = singleUserId, HouseholdId = householdId }
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.AssignTaskAsync(taskId, singleUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _taskAssignmentService.ReassignTaskToNextUserAsync(taskId);

            // Assert
            Assert.That(result, Is.EqualTo(singleUserId));
            _mockTaskRepository.Verify(r => r.AssignTaskAsync(taskId, singleUserId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("ReassignTaskToNextUser")]
        public async Task ReassignTaskToNextUserAsync_ThrowsException_WhenNoMembers()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdMember>());

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _taskAssignmentService.ReassignTaskToNextUserAsync(taskId));

            Assert.That(exception.Message, Is.EqualTo("No active members in household"));
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("GetSuggestedAssignee")]
        public async Task GetSuggestedAssigneeAsync_ReturnsUserWithLeastWorkload_WhenMultipleMembers()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var user1Id = "user1"; // Will have 2 tasks
            var user2Id = "user2"; // Will have 1 task (should be suggested)
            var user3Id = "user3"; // Will have 3 tasks

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = user1Id, HouseholdId = householdId },
                new HouseholdMember { UserId = user2Id, HouseholdId = householdId },
                new HouseholdMember { UserId = user3Id, HouseholdId = householdId }
            };

            var activeTasks = new List<HouseholdTask>
            {
                new HouseholdTask { AssignedUserId = user1Id, HouseholdId = householdId },
                new HouseholdTask { AssignedUserId = user1Id, HouseholdId = householdId },
                new HouseholdTask { AssignedUserId = user2Id, HouseholdId = householdId },
                new HouseholdTask { AssignedUserId = user3Id, HouseholdId = householdId },
                new HouseholdTask { AssignedUserId = user3Id, HouseholdId = householdId },
                new HouseholdTask { AssignedUserId = user3Id, HouseholdId = householdId }
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeTasks);

            // Act
            var result = await _taskAssignmentService.GetSuggestedAssigneeAsync(taskId);

            // Assert
            Assert.That(result, Is.EqualTo(user2Id)); // User with least workload (1 task)
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("GetSuggestedAssignee")]
        public async Task GetSuggestedAssigneeAsync_ReturnsNull_WhenTaskNotFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask)null);

            // Act
            var result = await _taskAssignmentService.GetSuggestedAssigneeAsync(taskId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("GetWorkloadStats")]
        public async Task GetWorkloadStatsAsync_ReturnsCorrectWorkloadDistribution_WhenTasksAssigned()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var user1Id = "user1";
            var user2Id = "user2";
            var user3Id = "user3";

            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = user1Id, HouseholdId = householdId },
                new HouseholdMember { UserId = user2Id, HouseholdId = householdId },
                new HouseholdMember { UserId = user3Id, HouseholdId = householdId }
            };

            var activeTasks = new List<HouseholdTask>
            {
                new HouseholdTask { AssignedUserId = user1Id, HouseholdId = householdId, IsActive = true },
                new HouseholdTask { AssignedUserId = user1Id, HouseholdId = householdId, IsActive = true },
                new HouseholdTask { AssignedUserId = user2Id, HouseholdId = householdId, IsActive = true },
                new HouseholdTask { AssignedUserId = null, HouseholdId = householdId, IsActive = true } // Unassigned task
            };

            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeTasks);

            // Act
            var result = await _taskAssignmentService.GetWorkloadStatsAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result[user1Id], Is.EqualTo(2));
            Assert.That(result[user2Id], Is.EqualTo(1));
            Assert.That(result[user3Id], Is.EqualTo(0));
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("GetWorkloadStats")]
        public async Task GetWorkloadStatsAsync_ReturnsEmptyDictionary_WhenNoMembers()
        {
            // Arrange
            var householdId = Guid.NewGuid();

            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdMember>());
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());

            // Act
            var result = await _taskAssignmentService.GetWorkloadStatsAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("GetWorkloadStats")]
        public async Task GetWorkloadStatsAsync_IgnoresUnassignedTasks_WhenCalculatingWorkload()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var user1Id = "user1";

            var members = new List<HouseholdMember>
            {
                new HouseholdMember { UserId = user1Id, HouseholdId = householdId }
            };

            var activeTasks = new List<HouseholdTask>
            {
                new HouseholdTask { AssignedUserId = user1Id, HouseholdId = householdId, IsActive = true },
                new HouseholdTask { AssignedUserId = null, HouseholdId = householdId, IsActive = true }, // Unassigned
                new HouseholdTask { AssignedUserId = string.Empty, HouseholdId = householdId, IsActive = true } // Empty string
            };

            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeTasks);

            // Act
            var result = await _taskAssignmentService.GetWorkloadStatsAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[user1Id], Is.EqualTo(1)); // Only one assigned task counted
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("AssignTask")]
        public async Task AssignTaskAsync_ThrowsException_WhenNoMembersAvailable()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Title = "Test Task"
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdMember>());

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _taskAssignmentService.AssignTaskAsync(taskId));

            Assert.That(exception.Message, Is.EqualTo("No available users to assign the task"));
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("AutoAssignAllTasks")]
        public async Task AutoAssignAllTasksAsync_AssignsTasksEvenly_WhenMultipleMembersAndTasks()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var user1Id = "user1";
            var user2Id = "user2";

            var unassignedTasks = new List<HouseholdTask>
        {
            new HouseholdTask { Id = Guid.NewGuid(), HouseholdId = householdId, Priority = TaskPriority.High },
            new HouseholdTask { Id = Guid.NewGuid(), HouseholdId = householdId, Priority = TaskPriority.Medium },
            new HouseholdTask { Id = Guid.NewGuid(), HouseholdId = householdId, Priority = TaskPriority.Low }
        };

            var members = new List<HouseholdMember>
        {
            new HouseholdMember { UserId = user1Id, HouseholdId = householdId },
            new HouseholdMember { UserId = user2Id, HouseholdId = householdId }
        };

            _mockTaskRepository.Setup(r => r.GetUnassignedTasksAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(unassignedTasks);
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());
            _mockTaskRepository.Setup(r => r.BulkAssignTasksAsync(It.IsAny<Dictionary<Guid, string>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _taskAssignmentService.AutoAssignAllTasksAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.Values.Distinct().Count(), Is.EqualTo(2)); // Tasks distributed between 2 users

            _mockTaskRepository.Verify(r => r.BulkAssignTasksAsync(
                It.Is<Dictionary<Guid, string>>(dict => dict.Count == 3),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskAssignmentService")]
        [Category("AutoAssignAllTasks")]
        public async Task AutoAssignAllTasksAsync_ReturnsEmptyDictionary_WhenNoMembers()
        {
            // Arrange
            var householdId = Guid.NewGuid();

            _mockTaskRepository.Setup(r => r.GetUnassignedTasksAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdTask>());
            _mockMemberRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<HouseholdMember>());

            // Act
            var result = await _taskAssignmentService.AutoAssignAllTasksAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
            _mockTaskRepository.Verify(r => r.BulkAssignTasksAsync(It.IsAny<Dictionary<Guid, string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
