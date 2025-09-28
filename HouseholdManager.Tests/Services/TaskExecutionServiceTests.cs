using HouseholdManager.Models;
using HouseholdManager.Models.Enums;
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
    public class TaskExecutionServiceTests
    {
        private Mock<IExecutionRepository> _mockExecutionRepository;
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IHouseholdService> _mockHouseholdService;
        private Mock<IFileUploadService> _mockFileUploadService;
        private Mock<ILogger<TaskExecutionService>> _mockLogger;
        private TaskExecutionService _taskExecutionService;

        [SetUp]
        public void Setup()
        {
            _mockExecutionRepository = new Mock<IExecutionRepository>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockHouseholdService = new Mock<IHouseholdService>();
            _mockFileUploadService = new Mock<IFileUploadService>();
            _mockLogger = new Mock<ILogger<TaskExecutionService>>();

            _taskExecutionService = new TaskExecutionService(
                _mockExecutionRepository.Object,
                _mockTaskRepository.Object,
                _mockHouseholdService.Object,
                _mockFileUploadService.Object,
                _mockLogger.Object);
        }

        #region Complete Task Operations

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_CompletesTask_WhenValidRequest()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var notes = "Task completed successfully";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Type = TaskType.OneTime,
                Title = "Test Task"
            };

            var execution = new TaskExecution
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Notes = notes,
                CompletedAt = DateTime.UtcNow,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockExecutionRepository.Setup(r => r.CreateExecutionAsync(taskId, userId, notes, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            // Act
            var result = await _taskExecutionService.CompleteTaskAsync(taskId, userId, notes);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TaskId, Is.EqualTo(taskId));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.Notes, Is.EqualTo(notes));

            _mockExecutionRepository.Verify(r => r.CreateExecutionAsync(taskId, userId, notes, null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_ThrowsException_WhenTaskNotFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = "user123";

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _taskExecutionService.CompleteTaskAsync(taskId, userId));

            Assert.That(exception.Message, Is.EqualTo("Task not found"));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_ThrowsException_WhenRegularTaskAlreadyCompletedThisWeek()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Type = TaskType.Regular,
                Title = "Weekly Task"
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockExecutionRepository.Setup(r => r.IsTaskCompletedThisWeekAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _taskExecutionService.CompleteTaskAsync(taskId, userId));

            Assert.That(exception.Message, Is.EqualTo("This task has already been completed this week"));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_UploadsPhoto_WhenPhotoProvided()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var photoPath = "/uploads/execution-photo.jpg";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Type = TaskType.OneTime
            };

            var mockPhoto = new Mock<IFormFile>();
            var execution = new TaskExecution
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                PhotoPath = photoPath
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockFileUploadService.Setup(s => s.UploadExecutionPhotoAsync(mockPhoto.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photoPath);
            _mockExecutionRepository.Setup(r => r.CreateExecutionAsync(taskId, userId, null, photoPath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            // Act
            var result = await _taskExecutionService.CompleteTaskAsync(taskId, userId, null, mockPhoto.Object);

            // Assert
            Assert.That(result.PhotoPath, Is.EqualTo(photoPath));
            _mockFileUploadService.Verify(s => s.UploadExecutionPhotoAsync(mockPhoto.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region CRUD Operations

        [Test]
        [Category("TaskExecutionService")]
        [Category("UpdateExecution")]
        public async Task UpdateExecutionAsync_UpdatesExecution_WhenValidRequest()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var updatedNotes = "Updated notes";

            var existingExecution = new TaskExecution
            {
                Id = executionId,
                UserId = userId,
                HouseholdId = householdId,
                Notes = "Original notes"
            };

            var updatedExecution = new TaskExecution
            {
                Id = executionId,
                Notes = updatedNotes
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingExecution);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHouseholdService.Setup(s => s.IsUserOwnerAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // User is not owner but is the creator
            _mockExecutionRepository.Setup(r => r.UpdateAsync(existingExecution, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _taskExecutionService.UpdateExecutionAsync(updatedExecution, userId);

            // Assert
            Assert.That(existingExecution.Notes, Is.EqualTo(updatedNotes));
            _mockExecutionRepository.Verify(r => r.UpdateAsync(existingExecution, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("DeleteExecution")]
        public async Task DeleteExecutionAsync_DeletesExecution_WhenUserIsCreator()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";

            var execution = new TaskExecution
            {
                Id = executionId,
                UserId = userId,
                HouseholdId = householdId,
                PhotoPath = "/uploads/photo.jpg"
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);
            _mockHouseholdService.Setup(s => s.IsUserOwnerAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockFileUploadService.Setup(s => s.DeleteFileAsync(execution.PhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockExecutionRepository.Setup(r => r.DeleteAsync(execution, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _taskExecutionService.DeleteExecutionAsync(executionId, userId);

            // Assert
            _mockFileUploadService.Verify(s => s.DeleteFileAsync(execution.PhotoPath, It.IsAny<CancellationToken>()), Times.Once);
            _mockExecutionRepository.Verify(r => r.DeleteAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("DeleteExecution")]
        public async Task DeleteExecutionAsync_ThrowsException_WhenUserNotAuthorized()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var creatorUserId = "creator123";
            var requestingUserId = "other_user";

            var execution = new TaskExecution
            {
                Id = executionId,
                UserId = creatorUserId,
                HouseholdId = householdId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);
            _mockHouseholdService.Setup(s => s.IsUserOwnerAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
                async () => await _taskExecutionService.DeleteExecutionAsync(executionId, requestingUserId));

            Assert.That(exception.Message, Is.EqualTo("You can only delete your own executions or be a household owner"));
        }

        #endregion

        #region Query Operations

        [Test]
        [Category("TaskExecutionService")]
        [Category("GetExecutions")]
        public async Task GetTaskExecutionsAsync_ReturnsExecutions_WhenTaskExists()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var executions = new List<TaskExecution>
            {
                new TaskExecution { Id = Guid.NewGuid(), TaskId = taskId, UserId = "user1" },
                new TaskExecution { Id = Guid.NewGuid(), TaskId = taskId, UserId = "user2" }
            };

            _mockExecutionRepository.Setup(r => r.GetByTaskIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _taskExecutionService.GetTaskExecutionsAsync(taskId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.All(e => e.TaskId == taskId), Is.True);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("GetExecutions")]
        public async Task GetWeeklyExecutionsAsync_ReturnsThisWeekExecutions_WhenNoDateSpecified()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var thisWeekExecutions = new List<TaskExecution>
            {
                new TaskExecution { Id = Guid.NewGuid(), HouseholdId = householdId, CompletedAt = DateTime.UtcNow }
            };

            _mockExecutionRepository.Setup(r => r.GetThisWeekAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(thisWeekExecutions);

            // Act
            var result = await _taskExecutionService.GetWeeklyExecutionsAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            _mockExecutionRepository.Verify(r => r.GetThisWeekAsync(householdId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("GetExecutions")]
        public async Task GetWeeklyExecutionsAsync_ReturnsSpecificWeekExecutions_WhenDateSpecified()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var weekStarting = new DateTime(2024, 1, 1); // Monday
            var weekEnd = weekStarting.AddDays(7);
            var weekExecutions = new List<TaskExecution>
            {
                new TaskExecution { Id = Guid.NewGuid(), HouseholdId = householdId, CompletedAt = weekStarting.AddDays(2) }
            };

            _mockExecutionRepository.Setup(r => r.GetByDateRangeAsync(householdId, weekStarting, weekEnd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(weekExecutions);

            // Act
            var result = await _taskExecutionService.GetWeeklyExecutionsAsync(householdId, weekStarting);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            _mockExecutionRepository.Verify(r => r.GetByDateRangeAsync(householdId, weekStarting, weekEnd, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Photo Management

        [Test]
        [Category("TaskExecutionService")]
        [Category("PhotoManagement")]
        public async Task UploadExecutionPhotoAsync_UploadsAndUpdatesPhoto_WhenValidRequest()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var oldPhotoPath = "/uploads/old-photo.jpg";
            var newPhotoPath = "/uploads/new-photo.jpg";

            var execution = new TaskExecution
            {
                Id = executionId,
                UserId = userId,
                HouseholdId = householdId,
                PhotoPath = oldPhotoPath
            };

            var mockPhoto = new Mock<IFormFile>();

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHouseholdService.Setup(s => s.IsUserOwnerAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockFileUploadService.Setup(s => s.DeleteFileAsync(oldPhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockFileUploadService.Setup(s => s.UploadExecutionPhotoAsync(mockPhoto.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newPhotoPath);
            _mockExecutionRepository.Setup(r => r.UpdateAsync(execution, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _taskExecutionService.UploadExecutionPhotoAsync(executionId, mockPhoto.Object, userId);

            // Assert
            Assert.That(result, Is.EqualTo(newPhotoPath));
            Assert.That(execution.PhotoPath, Is.EqualTo(newPhotoPath));

            _mockFileUploadService.Verify(s => s.DeleteFileAsync(oldPhotoPath, It.IsAny<CancellationToken>()), Times.Once);
            _mockFileUploadService.Verify(s => s.UploadExecutionPhotoAsync(mockPhoto.Object, It.IsAny<CancellationToken>()), Times.Once);
            _mockExecutionRepository.Verify(r => r.UpdateAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("PhotoManagement")]
        public async Task DeleteExecutionPhotoAsync_DeletesPhoto_WhenPhotoExists()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";
            var photoPath = "/uploads/photo.jpg";

            var execution = new TaskExecution
            {
                Id = executionId,
                UserId = userId,
                HouseholdId = householdId,
                PhotoPath = photoPath
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHouseholdService.Setup(s => s.IsUserOwnerAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _mockFileUploadService.Setup(s => s.DeleteFileAsync(photoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockExecutionRepository.Setup(r => r.UpdateAsync(execution, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _taskExecutionService.DeleteExecutionPhotoAsync(executionId, userId);

            // Assert
            Assert.That(execution.PhotoPath, Is.Null);
            _mockFileUploadService.Verify(s => s.DeleteFileAsync(photoPath, It.IsAny<CancellationToken>()), Times.Once);
            _mockExecutionRepository.Verify(r => r.UpdateAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Status Checking

        [Test]
        [Category("TaskExecutionService")]
        [Category("StatusChecking")]
        public async Task IsTaskCompletedThisWeekAsync_ReturnsTrue_WhenTaskCompletedThisWeek()
        {
            // Arrange
            var taskId = Guid.NewGuid();

            _mockExecutionRepository.Setup(r => r.IsTaskCompletedThisWeekAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _taskExecutionService.IsTaskCompletedThisWeekAsync(taskId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("StatusChecking")]
        public async Task GetLatestExecutionForTaskAsync_ReturnsLatestExecution_WhenExecutionsExist()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var latestExecution = new TaskExecution
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                CompletedAt = DateTime.UtcNow
            };

            _mockExecutionRepository.Setup(r => r.GetLatestExecutionForTaskAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(latestExecution);

            // Act
            var result = await _taskExecutionService.GetLatestExecutionForTaskAsync(taskId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TaskId, Is.EqualTo(taskId));
        }

        #endregion

        #region Validation

        [Test]
        [Category("TaskExecutionService")]
        [Category("ValidateExecutionAccess")]
        public async Task ValidateExecutionAccessAsync_DoesNotThrow_WhenUserIsCreator()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "user123";

            var execution = new TaskExecution
            {
                Id = executionId,
                UserId = userId,
                HouseholdId = householdId
            };

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHouseholdService.Setup(s => s.IsUserOwnerAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _taskExecutionService.ValidateExecutionAccessAsync(executionId, userId));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("ValidateExecutionAccess")]
        public async Task ValidateExecutionAccessAsync_ThrowsException_WhenExecutionNotFound()
        {
            // Arrange
            var executionId = Guid.NewGuid();
            var userId = "user123";

            _mockExecutionRepository.Setup(r => r.GetByIdAsync(executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskExecution)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _taskExecutionService.ValidateExecutionAccessAsync(executionId, userId));

            Assert.That(exception.Message, Is.EqualTo("Execution not found"));
        }

        #endregion
    }
}
