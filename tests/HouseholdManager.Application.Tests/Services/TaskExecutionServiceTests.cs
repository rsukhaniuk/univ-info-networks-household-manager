using AutoMapper;
using HouseholdManager.Application.DTOs.Execution;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Tests.Services
{
    /// <summary>
    /// Unit tests for TaskExecutionService with AutoMapper and DTOs
    /// </summary>
    [TestFixture]
    public class TaskExecutionServiceTests
    {
        private Mock<IExecutionRepository> _mockExecutionRepository;
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IHouseholdService> _mockHouseholdService;
        private Mock<IFileUploadService> _mockFileUploadService;
        private Mock<ILogger<TaskExecutionService>> _mockLogger;
        private IMapper _mapper;
        private TaskExecutionService _service;

        // Test data
        private Guid _householdId;
        private Guid _roomId;
        private Guid _taskId;
        private Guid _executionId;
        private string _userId;
        private string _ownerUserId;

        [SetUp]
        public void Setup()
        {
            // Initialize test data
            _householdId = Guid.NewGuid();
            _roomId = Guid.NewGuid();
            _taskId = Guid.NewGuid();
            _executionId = Guid.NewGuid();
            _userId = "auth0|user123";
            _ownerUserId = "auth0|owner456";

            // Setup mocks
            _mockExecutionRepository = new Mock<IExecutionRepository>();
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockHouseholdService = new Mock<IHouseholdService>();
            _mockFileUploadService = new Mock<IFileUploadService>();
            _mockLogger = new Mock<ILogger<TaskExecutionService>>();

            var loggerFactory = LoggerFactory.Create(b => { });

            var config = new MapperConfiguration(cfg => {
                cfg.AddMaps(new[] {
                    typeof(ExecutionProfile).Assembly,
                    typeof(TaskProfile).Assembly
                });
            }, loggerFactory);

            _mapper = config.CreateMapper();

            // Create service instance
            _service = new TaskExecutionService(
                _mockExecutionRepository.Object,
                _mockTaskRepository.Object,
                _mockHouseholdService.Object,
                _mockFileUploadService.Object,
                _mapper,
                _mockLogger.Object
            );
        }

        #region Helper Methods

        private HouseholdTask CreateTestTask(TaskType type = TaskType.Regular, bool isActive = true)
        {
            return new HouseholdTask
            {
                Id = _taskId,
                HouseholdId = _householdId,
                RoomId = _roomId,
                Title = "Test Task",
                Description = "Test task description",
                Type = type,
                Priority = TaskPriority.Medium,
                EstimatedMinutes = 30,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                RecurrenceRule = type == TaskType.Regular ? "FREQ=WEEKLY;BYDAY=MO" : null,
                DueDate = type == TaskType.OneTime ? DateTime.UtcNow.AddDays(7) : null,
                Household = new Household { Id = _householdId, Name = "Test Household" },
                Room = new Room { Id = _roomId, Name = "Living Room", HouseholdId = _householdId },
                AssignedUser = null,
                Executions = new List<TaskExecution>()
            };
        }

        private TaskExecution CreateTestExecution(HouseholdTask task, string userId)
        {
            return new TaskExecution
            {
                Id = _executionId,
                TaskId = task.Id,
                UserId = userId,
                CompletedAt = DateTime.UtcNow,
                Notes = "Test execution",
                PhotoPath = null,
                WeekStarting = TaskExecution.GetWeekStarting(DateTime.UtcNow),
                HouseholdId = task.HouseholdId,
                RoomId = task.RoomId,
                Task = task,
                User = new ApplicationUser
                {
                    Id = userId,
                    Email = "user@test.com",
                    FirstName = "Test",
                    LastName = "User"
                }
            };
        }

        private Mock<IFormFile> CreateMockFormFile(string fileName = "test.jpg")
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(1024);
            return fileMock;
        }

        #endregion

        #region CompleteTaskAsync Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_WithValidRegularTask_ReturnsExecutionDto()
        {
            // Arrange
            var task = CreateTestTask(TaskType.Regular);
            var request = new CompleteTaskRequest
            {
                TaskId = _taskId,
                Notes = "Cleaned the living room"
            };

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.IsTaskCompletedInPeriodAsync(_taskId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var execution = CreateTestExecution(task, _userId);
            execution.Notes = request.Notes;

            _mockExecutionRepository
                .Setup(x => x.CreateExecutionAsync(_taskId, _userId, request.Notes, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockExecutionRepository
                .Setup(x => x.GetByIdWithRelationsAsync(execution.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            // Act
            var result = await _service.CompleteTaskAsync(request, _userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TaskId, Is.EqualTo(_taskId));
            Assert.That(result.UserId, Is.EqualTo(_userId));
            Assert.That(result.TaskTitle, Is.EqualTo("Test Task"));
            Assert.That(result.UserName, Is.EqualTo("Test User"));
            Assert.That(result.RoomName, Is.EqualTo("Living Room"));
            Assert.That(result.Notes, Is.EqualTo("Cleaned the living room"));

            _mockTaskRepository.Verify(x => x.UpdateAsync(It.IsAny<HouseholdTask>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_WithOneTimeTask_DeactivatesTask()
        {
            // Arrange
            var task = CreateTestTask(TaskType.OneTime);
            var request = new CompleteTaskRequest
            {
                TaskId = _taskId,
                Notes = "Task completed"
            };

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var execution = CreateTestExecution(task, _userId);
            execution.Notes = request.Notes;

            _mockExecutionRepository
                .Setup(x => x.CreateExecutionAsync(_taskId, _userId, request.Notes, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockExecutionRepository
                .Setup(x => x.GetByIdWithRelationsAsync(execution.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockTaskRepository
                .Setup(x => x.UpdateAsync(It.IsAny<HouseholdTask>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CompleteTaskAsync(request, _userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(task.IsActive, Is.False);
            _mockTaskRepository.Verify(x => x.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_WithPhoto_UploadsPhotoAndReturnsPhotoUrl()
        {
            // Arrange
            var task = CreateTestTask(TaskType.Regular);
            var request = new CompleteTaskRequest
            {
                TaskId = _taskId,
                Notes = "Task with photo"
            };
            var photoMock = CreateMockFormFile("photo.jpg");
            var uploadedPhotoPath = "executions/test-photo.jpg";

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.IsTaskCompletedInPeriodAsync(_taskId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockFileUploadService
                .Setup(x => x.UploadExecutionPhotoAsync(photoMock.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadedPhotoPath);

            var execution = CreateTestExecution(task, _userId);
            execution.Notes = request.Notes;
            execution.PhotoPath = uploadedPhotoPath;

            _mockExecutionRepository
                .Setup(x => x.CreateExecutionAsync(_taskId, _userId, request.Notes, uploadedPhotoPath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockExecutionRepository
                .Setup(x => x.GetByIdWithRelationsAsync(execution.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            // Act
            var result = await _service.CompleteTaskAsync(request, _userId, photoMock.Object);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PhotoPath, Is.EqualTo(uploadedPhotoPath));
            Assert.That(result.PhotoUrl, Is.EqualTo($"/{uploadedPhotoPath}"));
            Assert.That(result.HasPhoto, Is.True);

            _mockFileUploadService.Verify(x => x.UploadExecutionPhotoAsync(photoMock.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_WhenTaskNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var request = new CompleteTaskRequest { TaskId = _taskId };

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask?)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<NotFoundException>(
                async () => await _service.CompleteTaskAsync(request, _userId));

            Assert.That(exception.Message, Does.Contain("Task"));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_WhenUserHasNoAccess_ThrowsUnauthorizedException()
        {
            // Arrange
            var task = CreateTestTask();
            var request = new CompleteTaskRequest { TaskId = _taskId };

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("No access"));

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _service.CompleteTaskAsync(request, _userId));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("CompleteTask")]
        public async Task CompleteTaskAsync_WhenRegularTaskAlreadyCompletedThisWeek_ThrowsValidationException()
        {
            // Arrange
            var task = CreateTestTask(TaskType.Regular);
            var request = new CompleteTaskRequest { TaskId = _taskId };

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.IsTaskCompletedInPeriodAsync(_taskId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(
                async () => await _service.CompleteTaskAsync(request, _userId));

            Assert.That(exception.Message, Does.Contain("already been completed"));
        }

        #endregion

        #region GetExecutionAsync Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("GetExecution")]
        public async Task GetExecutionAsync_WhenExists_ReturnsExecutionDto()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);

            _mockExecutionRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            // Act
            var result = await _service.GetExecutionAsync(_executionId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(_executionId));
            Assert.That(result.TaskTitle, Is.EqualTo("Test Task"));
            Assert.That(result.UserName, Is.EqualTo("Test User"));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("GetExecution")]
        public async Task GetExecutionAsync_WhenNotExists_ReturnsNull()
        {
            // Arrange
            _mockExecutionRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskExecution?)null);

            // Act
            var result = await _service.GetExecutionAsync(_executionId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region UpdateExecutionAsync Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("UpdateExecution")]
        public async Task UpdateExecutionAsync_WithValidData_UpdatesAndReturnsDto()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);
            var updateRequest = new UpdateExecutionRequest
            {
                Notes = "Updated notes",
                PhotoPath = "new-photo.jpg"
            };

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockExecutionRepository
                .Setup(x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            // Act
            var result = await _service.UpdateExecutionAsync(_executionId, updateRequest, _userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(execution.Notes, Is.EqualTo("Updated notes"));
            Assert.That(execution.PhotoPath, Is.EqualTo("new-photo.jpg"));

            _mockExecutionRepository.Verify(x => x.UpdateAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("UpdateExecution")]
        public async Task UpdateExecutionAsync_WhenNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var updateRequest = new UpdateExecutionRequest { Notes = "Updated" };

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TaskExecution?)null);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(
                async () => await _service.UpdateExecutionAsync(_executionId, updateRequest, _userId));
        }

        #endregion

        #region DeleteExecutionAsync Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("DeleteExecution")]
        public async Task DeleteExecutionAsync_ByOwner_DeletesExecution()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _ownerUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockExecutionRepository
                .Setup(x => x.DeleteAsync(execution, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteExecutionAsync(_executionId, _ownerUserId);

            // Assert
            _mockExecutionRepository.Verify(x => x.DeleteAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("DeleteExecution")]
        public async Task DeleteExecutionAsync_WithPhoto_DeletesPhotoAndExecution()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);
            execution.PhotoPath = "executions/photo.jpg";

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockFileUploadService
                .Setup(x => x.DeleteFileAsync(execution.PhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.DeleteAsync(execution, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteExecutionAsync(_executionId, _userId);

            // Assert
            _mockFileUploadService.Verify(x => x.DeleteFileAsync(execution.PhotoPath, It.IsAny<CancellationToken>()), Times.Once);
            _mockExecutionRepository.Verify(x => x.DeleteAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("DeleteExecution")]
        public async Task DeleteExecutionAsync_WhenNotOwnerOrCreator_ThrowsUnauthorizedException()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);
            var unauthorizedUserId = "auth0|other999";

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, unauthorizedUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ForbiddenException>(
                async () => await _service.DeleteExecutionAsync(_executionId, unauthorizedUserId));

            Assert.That(exception.Message, Does.Contain("only delete your own executions"));
        }

        #endregion

        #region Query Operations Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("QueryOperations")]
        public async Task GetTaskExecutionsAsync_ReturnsExecutionDtoList()
        {
            // Arrange
            var task = CreateTestTask();
            var executions = new List<TaskExecution>
            {
                CreateTestExecution(task, _userId),
                CreateTestExecution(task, _ownerUserId)
            };

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _service.GetTaskExecutionsAsync(_taskId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Is.All.InstanceOf<ExecutionDto>());
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("QueryOperations")]
        public async Task GetHouseholdExecutionsAsync_ReturnsExecutionDtoList()
        {
            // Arrange
            var task = CreateTestTask();
            var executions = new List<TaskExecution>
            {
                CreateTestExecution(task, _userId)
            };

            _mockExecutionRepository
                .Setup(x => x.GetByHouseholdIdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _service.GetHouseholdExecutionsAsync(_householdId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result, Is.All.InstanceOf<ExecutionDto>());
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("QueryOperations")]
        public async Task GetWeeklyExecutionsAsync_WithWeekStarting_ReturnsFilteredExecutions()
        {
            // Arrange
            var task = CreateTestTask();
            var weekStarting = TaskExecution.GetWeekStarting(DateTime.UtcNow);
            var weekEnd = weekStarting.AddDays(7);
            var executions = new List<TaskExecution>
            {
                CreateTestExecution(task, _userId)
            };

            _mockExecutionRepository
                .Setup(x => x.GetByDateRangeAsync(_householdId, weekStarting, weekEnd, It.IsAny<CancellationToken>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _service.GetWeeklyExecutionsAsync(_householdId, weekStarting);

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            _mockExecutionRepository.Verify(x => x.GetByDateRangeAsync(_householdId, weekStarting, weekEnd, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("QueryOperations")]
        public async Task GetWeeklyExecutionsAsync_WithoutWeekStarting_ReturnsThisWeekExecutions()
        {
            // Arrange
            var task = CreateTestTask();
            var executions = new List<TaskExecution>
            {
                CreateTestExecution(task, _userId)
            };

            _mockExecutionRepository
                .Setup(x => x.GetThisWeekAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(executions);

            // Act
            var result = await _service.GetWeeklyExecutionsAsync(_householdId);

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            _mockExecutionRepository.Verify(x => x.GetThisWeekAsync(_householdId, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Photo Management Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("PhotoManagement")]
        public async Task UploadExecutionPhotoAsync_WithValidPhoto_UploadsAndReturnsPath()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);
            var photoMock = CreateMockFormFile("new-photo.jpg");
            var uploadedPath = "executions/new-photo.jpg";

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockFileUploadService
                .Setup(x => x.UploadExecutionPhotoAsync(photoMock.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadedPath);

            _mockExecutionRepository
                .Setup(x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UploadExecutionPhotoAsync(_executionId, photoMock.Object, _userId);

            // Assert
            Assert.That(result, Is.EqualTo(uploadedPath));
            Assert.That(execution.PhotoPath, Is.EqualTo(uploadedPath));

            _mockFileUploadService.Verify(x => x.UploadExecutionPhotoAsync(photoMock.Object, It.IsAny<CancellationToken>()), Times.Once);
            _mockExecutionRepository.Verify(x => x.UpdateAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("PhotoManagement")]
        public async Task UploadExecutionPhotoAsync_ReplacingExistingPhoto_DeletesOldPhotoFirst()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);
            execution.PhotoPath = "executions/old-photo.jpg";

            var photoMock = CreateMockFormFile("new-photo.jpg");
            var newPhotoPath = "executions/new-photo.jpg";

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockFileUploadService
                .Setup(x => x.DeleteFileAsync(execution.PhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockFileUploadService
                .Setup(x => x.UploadExecutionPhotoAsync(photoMock.Object, It.IsAny<CancellationToken>()))
                .ReturnsAsync(newPhotoPath);

            _mockExecutionRepository
                .Setup(x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.UploadExecutionPhotoAsync(_executionId, photoMock.Object, _userId);

            // Assert
            Assert.That(result, Is.EqualTo(newPhotoPath));
            _mockFileUploadService.Verify(x => x.DeleteFileAsync("executions/old-photo.jpg", It.IsAny<CancellationToken>()), Times.Once);
            _mockFileUploadService.Verify(x => x.UploadExecutionPhotoAsync(photoMock.Object, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("PhotoManagement")]
        public async Task DeleteExecutionPhotoAsync_WithExistingPhoto_DeletesPhoto()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);
            execution.PhotoPath = "executions/photo.jpg";

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockFileUploadService
                .Setup(x => x.DeleteFileAsync(execution.PhotoPath, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteExecutionPhotoAsync(_executionId, _userId);

            // Assert
            Assert.That(execution.PhotoPath, Is.Null);
            _mockFileUploadService.Verify(x => x.DeleteFileAsync("executions/photo.jpg", It.IsAny<CancellationToken>()), Times.Once);
            _mockExecutionRepository.Verify(x => x.UpdateAsync(execution, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Status Checking Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("StatusChecking")]
        public async Task IsTaskCompletedThisWeekAsync_WhenCompleted_ReturnsTrue()
        {
            // Arrange
            _mockExecutionRepository
                .Setup(x => x.IsTaskCompletedThisWeekAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.IsTaskCompletedThisWeekAsync(_taskId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("StatusChecking")]
        public async Task GetLatestExecutionForTaskAsync_WhenExists_ReturnsDto()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);

            _mockExecutionRepository
                .Setup(x => x.GetLatestExecutionForTaskAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            // Act
            var result = await _service.GetLatestExecutionForTaskAsync(_taskId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.TaskId, Is.EqualTo(_taskId));
        }

        #endregion

        #region Validation Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("Validation")]
        public async Task ValidateExecutionAccessAsync_WithValidAccess_DoesNotThrow()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _service.ValidateExecutionAccessAsync(_executionId, _userId));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("Validation")]
        public async Task ValidateExecutionAccessAsync_WhenNotCreatorOrOwner_ThrowsUnauthorizedException()
        {
            // Arrange
            var task = CreateTestTask();
            var execution = CreateTestExecution(task, _userId);
            var unauthorizedUserId = "auth0|other999";

            _mockExecutionRepository
                .Setup(x => x.GetByIdAsync(_executionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(execution);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, unauthorizedUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, unauthorizedUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ForbiddenException>(
                async () => await _service.ValidateExecutionAccessAsync(_executionId, unauthorizedUserId));

            Assert.That(exception.Message, Does.Contain("only access your own executions"));
        }

        #endregion

        #region InvalidateExecutionInCurrentPeriod Tests

        [Test]
        [Category("TaskExecutionService")]
        [Category("InvalidateExecution")]
        public async Task InvalidateExecutionInCurrentPeriodAsync_WithWeeklyTask_InvalidatesExecutionsThisWeek()
        {
            // Arrange
            var task = CreateTestTask();
            task.Type = Domain.Enums.TaskType.Regular;
            task.RecurrenceRule = "FREQ=WEEKLY;BYDAY=MO,WE,FR";

            // Create executions: one this week (should be invalidated), one last week (should remain)
            var thisWeekExecution = CreateTestExecution(task, _userId);
            thisWeekExecution.CompletedAt = DateTime.UtcNow.Date.AddDays(-1); // Yesterday
            thisWeekExecution.IsCountedForCompletion = true;

            var lastWeekExecution = CreateTestExecution(task, _userId);
            lastWeekExecution.Id = Guid.NewGuid();
            lastWeekExecution.CompletedAt = DateTime.UtcNow.Date.AddDays(-8); // Last week
            lastWeekExecution.IsCountedForCompletion = true;

            _mockTaskRepository
                .Setup(x => x.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateOwnerAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution> { thisWeekExecution, lastWeekExecution });

            _mockExecutionRepository
                .Setup(x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.InvalidateExecutionInCurrentPeriodAsync(_taskId, _userId);

            // Assert
            _mockExecutionRepository.Verify(
                x => x.UpdateAsync(
                    It.Is<TaskExecution>(e =>
                        e.Id == thisWeekExecution.Id &&
                        e.IsCountedForCompletion == false),
                    It.IsAny<CancellationToken>()),
                Times.Once,
                "Should invalidate this week's execution");

            _mockExecutionRepository.Verify(
                x => x.UpdateAsync(
                    It.Is<TaskExecution>(e => e.Id == lastWeekExecution.Id),
                    It.IsAny<CancellationToken>()),
                Times.Never,
                "Should NOT invalidate last week's execution");
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("InvalidateExecution")]
        public async Task InvalidateExecutionInCurrentPeriodAsync_WithDailyTask_InvalidatesExecutionsToday()
        {
            // Arrange
            var task = CreateTestTask();
            task.Type = Domain.Enums.TaskType.Regular;
            task.RecurrenceRule = "FREQ=DAILY";

            var todayExecution = CreateTestExecution(task, _userId);
            todayExecution.CompletedAt = DateTime.UtcNow.Date.AddHours(10); // Today at 10am
            todayExecution.IsCountedForCompletion = true;

            var yesterdayExecution = CreateTestExecution(task, _userId);
            yesterdayExecution.Id = Guid.NewGuid();
            yesterdayExecution.CompletedAt = DateTime.UtcNow.Date.AddDays(-1).AddHours(10); // Yesterday
            yesterdayExecution.IsCountedForCompletion = true;

            _mockTaskRepository
                .Setup(x => x.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateOwnerAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution> { todayExecution, yesterdayExecution });

            _mockExecutionRepository
                .Setup(x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.InvalidateExecutionInCurrentPeriodAsync(_taskId, _userId);

            // Assert
            _mockExecutionRepository.Verify(
                x => x.UpdateAsync(
                    It.Is<TaskExecution>(e =>
                        e.Id == todayExecution.Id &&
                        e.IsCountedForCompletion == false),
                    It.IsAny<CancellationToken>()),
                Times.Once,
                "Should invalidate today's execution");

            _mockExecutionRepository.Verify(
                x => x.UpdateAsync(
                    It.Is<TaskExecution>(e => e.Id == yesterdayExecution.Id),
                    It.IsAny<CancellationToken>()),
                Times.Never,
                "Should NOT invalidate yesterday's execution");
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("InvalidateExecution")]
        public void InvalidateExecutionInCurrentPeriodAsync_WithOneTimeTask_ThrowsValidationException()
        {
            // Arrange
            var task = CreateTestTask();
            task.Type = Domain.Enums.TaskType.OneTime;

            _mockTaskRepository
                .Setup(x => x.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateOwnerAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(
                async () => await _service.InvalidateExecutionInCurrentPeriodAsync(_taskId, _userId));

            Assert.That(exception.Message, Does.Contain("Only Regular tasks can be reset"));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("InvalidateExecution")]
        public void InvalidateExecutionInCurrentPeriodAsync_WithNoExecutionsInPeriod_ThrowsValidationException()
        {
            // Arrange
            var task = CreateTestTask();
            task.Type = Domain.Enums.TaskType.Regular;
            task.RecurrenceRule = "FREQ=WEEKLY";

            // No executions this week, only old execution
            var oldExecution = CreateTestExecution(task, _userId);
            oldExecution.CompletedAt = DateTime.UtcNow.Date.AddDays(-30); // 30 days ago
            oldExecution.IsCountedForCompletion = true;

            _mockTaskRepository
                .Setup(x => x.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateOwnerAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution> { oldExecution });

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(
                async () => await _service.InvalidateExecutionInCurrentPeriodAsync(_taskId, _userId));

            Assert.That(exception.Message, Does.Contain("no counted execution"));
            Assert.That(exception.Message, Does.Contain("to invalidate"));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("InvalidateExecution")]
        public void InvalidateExecutionInCurrentPeriodAsync_WhenTaskNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _mockTaskRepository
                .Setup(x => x.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask?)null);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(
                async () => await _service.InvalidateExecutionInCurrentPeriodAsync(_taskId, _userId));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("InvalidateExecution")]
        public void InvalidateExecutionInCurrentPeriodAsync_WhenUserNotOwner_ThrowsUnauthorizedException()
        {
            // Arrange
            var task = CreateTestTask();
            task.Type = Domain.Enums.TaskType.Regular;

            _mockTaskRepository
                .Setup(x => x.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateOwnerAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ForbiddenException("User is not an owner"));

            // Act & Assert
            Assert.ThrowsAsync<ForbiddenException>(
                async () => await _service.InvalidateExecutionInCurrentPeriodAsync(_taskId, _userId));
        }

        [Test]
        [Category("TaskExecutionService")]
        [Category("InvalidateExecution")]
        public async Task InvalidateExecutionInCurrentPeriodAsync_IgnoresAlreadyInvalidatedExecutions()
        {
            // Arrange
            var task = CreateTestTask();
            task.Type = Domain.Enums.TaskType.Regular;
            task.RecurrenceRule = "FREQ=WEEKLY";

            // One counted execution (should be invalidated)
            var countedExecution = CreateTestExecution(task, _userId);
            countedExecution.CompletedAt = DateTime.UtcNow.Date.AddDays(-1);
            countedExecution.IsCountedForCompletion = true;

            // One already invalidated execution (should be ignored)
            var invalidatedExecution = CreateTestExecution(task, _userId);
            invalidatedExecution.Id = Guid.NewGuid();
            invalidatedExecution.CompletedAt = DateTime.UtcNow.Date.AddDays(-2);
            invalidatedExecution.IsCountedForCompletion = false;

            _mockTaskRepository
                .Setup(x => x.GetByIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateOwnerAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution> { countedExecution, invalidatedExecution });

            _mockExecutionRepository
                .Setup(x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.InvalidateExecutionInCurrentPeriodAsync(_taskId, _userId);

            // Assert
            _mockExecutionRepository.Verify(
                x => x.UpdateAsync(It.IsAny<TaskExecution>(), It.IsAny<CancellationToken>()),
                Times.Once,
                "Should only update the counted execution, not the already invalidated one");
        }

        #endregion
    }
}
