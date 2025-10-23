using AutoMapper;
using HouseholdManager.Application.DTOs.Room;
using HouseholdManager.Application.DTOs.Task;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Application.Mapping;
using HouseholdManager.Application.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseholdManager.Application.Tests.Services
{
    [TestFixture]
    public class HouseholdTaskServiceTests
    {
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IHouseholdService> _mockHouseholdService;
        private Mock<IRoomService> _mockRoomService;
        private Mock<IHouseholdMemberService> _mockHouseholdMemberService;
        private Mock<ITaskAssignmentService> _mockTaskAssignmentService;
        private Mock<ILogger<HouseholdTaskService>> _mockLogger;
        private IMapper _mapper;
        private HouseholdTaskService _householdTaskService;

        [SetUp]
        public void Setup()
        {
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockHouseholdService = new Mock<IHouseholdService>();
            _mockRoomService = new Mock<IRoomService>();
            _mockHouseholdMemberService = new Mock<IHouseholdMemberService>();
            _mockTaskAssignmentService = new Mock<ITaskAssignmentService>();
            _mockLogger = new Mock<ILogger<HouseholdTaskService>>();

            var loggerFactory = LoggerFactory.Create(b => { });

            var config = new MapperConfiguration(cfg => {
                cfg.AddMaps(new[] {
                    typeof(TaskProfile).Assembly,
                    typeof(RoomProfile).Assembly,
                    typeof(ExecutionProfile).Assembly
                });
            }, loggerFactory);

            _mapper = config.CreateMapper();

            _householdTaskService = new HouseholdTaskService(
                _mockTaskRepository.Object,
                _mockHouseholdService.Object,
                _mockRoomService.Object,
                _mockHouseholdMemberService.Object,
                _mockTaskAssignmentService.Object,
                _mapper,
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

            var request = new UpsertTaskRequest
            {
                Title = "Clean Kitchen",
                Description = "Deep clean the kitchen",
                Type = TaskType.Regular,
                Priority = TaskPriority.High,
                EstimatedMinutes = 60,
                ScheduledWeekday = DayOfWeek.Monday,
                HouseholdId = householdId,
                RoomId = roomId,
                IsActive = true
            };

            var roomDto = new RoomDto
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen"
            };

            var createdTask = new HouseholdTask
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Type = request.Type,
                Priority = request.Priority,
                EstimatedMinutes = request.EstimatedMinutes,
                ScheduledWeekday = request.ScheduledWeekday,
                HouseholdId = householdId,
                RoomId = roomId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Room = new Room
                {
                    Id = roomId,
                    Name = "Kitchen",
                    HouseholdId = householdId
                }
            };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.ValidateRoomAccessAsync(roomId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.GetRoomAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(roomDto);
            _mockTaskRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdTask>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTask);
            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTask);

            // Act
            var result = await _householdTaskService.CreateTaskAsync(request, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Title, Is.EqualTo("Clean Kitchen"));
            Assert.That(result.IsActive, Is.True);
            Assert.That(result.Priority, Is.EqualTo(TaskPriority.High));
            Assert.That(result.Type, Is.EqualTo(TaskType.Regular));

            _mockTaskRepository.Verify(r => r.AddAsync(It.Is<HouseholdTask>(t =>
                t.Title == "Clean Kitchen" &&
                t.HouseholdId == householdId &&
                t.RoomId == roomId &&
                t.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("CreateTask")]
        public async Task CreateTaskAsync_ThrowsValidationException_WhenRoomNotInHousehold()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var otherHouseholdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var request = new UpsertTaskRequest
            {
                Title = "Clean Kitchen",
                HouseholdId = householdId,
                RoomId = roomId,
                Type = TaskType.Regular,
                Priority = TaskPriority.Medium,
                EstimatedMinutes = 30
            };

            var roomDto = new RoomDto
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
                .ReturnsAsync(roomDto);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(
                async () => await _householdTaskService.CreateTaskAsync(request, requestingUserId));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception.Errors.ContainsKey("HouseholdId"), Is.True);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("CreateTask")]
        public async Task CreateTaskAsync_ThrowsUnauthorizedException_WhenUserNotOwner()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var roomId = Guid.NewGuid();
            var requestingUserId = "member123";

            var request = new UpsertTaskRequest
            {
                Title = "Clean Kitchen",
                HouseholdId = householdId,
                RoomId = roomId,
                Type = TaskType.Regular,
                Priority = TaskPriority.Medium,
                EstimatedMinutes = 30
            };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("You are not authorized to perform this action"));

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _householdTaskService.CreateTaskAsync(request, requestingUserId));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetTask")]
        public async Task GetTaskAsync_ReturnsTaskDto_WhenTaskExists()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var task = new HouseholdTask
            {
                Id = taskId,
                Title = "Clean Kitchen",
                Type = TaskType.Regular,
                Priority = TaskPriority.High,
                EstimatedMinutes = 60,
                Room = new Room { Id = Guid.NewGuid(), Name = "Kitchen" },
                AssignedUser = new ApplicationUser { Id = "user123", FirstName = "John", LastName = "Doe" }
            };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            // Act
            var result = await _householdTaskService.GetTaskAsync(taskId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(taskId));
            Assert.That(result.Title, Is.EqualTo("Clean Kitchen"));
            Assert.That(result.RoomName, Is.EqualTo("Kitchen"));
            Assert.That(result.AssignedUserName, Is.EqualTo("John Doe"));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetTask")]
        public async Task GetTaskAsync_ReturnsNull_WhenTaskNotFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask)null);

            // Act
            var result = await _householdTaskService.GetTaskAsync(taskId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetTaskWithRelations")]
        public async Task GetTaskWithRelationsAsync_ReturnsTaskDetailsDto_WhenTaskExists()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Title = "Clean Kitchen",
                Type = TaskType.Regular,
                Priority = TaskPriority.High,
                EstimatedMinutes = 60,
                Room = new Room { Id = Guid.NewGuid(), Name = "Kitchen", HouseholdId = householdId },
                Executions = new List<TaskExecution>()
            };

            var members = new List<HouseholdManager.Application.DTOs.Household.HouseholdMemberDto>
            {
                new HouseholdManager.Application.DTOs.Household.HouseholdMemberDto
                {
                    UserId = "user123",
                    UserName = "John Doe",
                    Email = "john@test.com"
                }
            };

            var taskCounts = new Dictionary<string, int> { { "user123", 5 } };

            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdMemberService.Setup(s => s.GetHouseholdMembersAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);
            _mockHouseholdMemberService.Setup(s => s.GetMemberTaskCountsAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(taskCounts);

            // Act
            var result = await _householdTaskService.GetTaskWithRelationsAsync(taskId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Task.Id, Is.EqualTo(taskId));
            Assert.That(result.Room.Name, Is.EqualTo("Kitchen"));
            Assert.That(result.AvailableAssignees.Count, Is.EqualTo(1));
            Assert.That(result.AvailableAssignees.First().CurrentTaskCount, Is.EqualTo(5));
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

            var request = new UpsertTaskRequest
            {
                Title = "Updated Task",
                Description = "Updated description",
                HouseholdId = householdId,
                RoomId = roomId,
                Type = TaskType.Regular,
                Priority = TaskPriority.Low,
                EstimatedMinutes = 45
            };

            var existingTask = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Title = "Old Task",
                Room = new Room { Id = roomId, Name = "Kitchen", HouseholdId = householdId }
            };

            var roomDto = new RoomDto
            {
                Id = roomId,
                HouseholdId = householdId,
                Name = "Kitchen"
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTask);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.GetRoomAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(roomDto);
            _mockTaskRepository.Setup(r => r.UpdateAsync(It.IsAny<HouseholdTask>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTask);

            // Act
            var result = await _householdTaskService.UpdateTaskAsync(taskId, request, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            _mockTaskRepository.Verify(r => r.UpdateAsync(It.IsAny<HouseholdTask>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("UpdateTask")]
        public async Task UpdateTaskAsync_ThrowsNotFoundException_WhenTaskNotFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var request = new UpsertTaskRequest
            {
                HouseholdId = householdId,
                RoomId = Guid.NewGuid(),
                Title = "Updated Task",
                Type = TaskType.Regular,
                Priority = TaskPriority.Medium,
                EstimatedMinutes = 30
            };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<NotFoundException>(
                async () => await _householdTaskService.UpdateTaskAsync(taskId, request, requestingUserId));

            Assert.That(exception.Message, Does.Contain("Task"));
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

        #region Task Filtering

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetHouseholdTasks")]
        public async Task GetHouseholdTasksAsync_ReturnsAllTasks_WhenTasksExist()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var tasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    Title = "Task 1",
                    IsActive = true,
                    Room = new Room { Name = "Kitchen" }
                },
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    Title = "Task 2",
                    IsActive = false,
                    Room = new Room { Name = "Bathroom" }
                }
            };

            _mockTaskRepository.Setup(r => r.GetByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tasks);

            // Act
            var result = await _householdTaskService.GetHouseholdTasksAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetActiveHouseholdTasks")]
        public async Task GetActiveHouseholdTasksAsync_ReturnsOnlyActiveTasks_WhenTasksExist()
        {
            // Arrange
            var householdId = Guid.NewGuid();
            var activeTasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    Title = "Active Task",
                    IsActive = true,
                    Room = new Room { Name = "Kitchen" }
                }
            };

            _mockTaskRepository.Setup(r => r.GetActiveByHouseholdIdAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeTasks);

            // Act
            var result = await _householdTaskService.GetActiveHouseholdTasksAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().IsActive, Is.True);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetRoomTasks")]
        public async Task GetRoomTasksAsync_ReturnsRoomTasks_WhenTasksExist()
        {
            // Arrange
            var roomId = Guid.NewGuid();
            var roomTasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    RoomId = roomId,
                    Title = "Room Task",
                    Room = new Room { Id = roomId, Name = "Kitchen" }
                }
            };

            _mockTaskRepository.Setup(r => r.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(roomTasks);

            // Act
            var result = await _householdTaskService.GetRoomTasksAsync(roomId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().RoomId, Is.EqualTo(roomId));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("GetUserTasks")]
        public async Task GetUserTasksAsync_ReturnsUserTasks_WhenTasksExist()
        {
            // Arrange
            var userId = "user123";
            var userTasks = new List<HouseholdTask>
            {
                new HouseholdTask
                {
                    Id = Guid.NewGuid(),
                    AssignedUserId = userId,
                    Title = "User Task",
                    Room = new Room { Name = "Kitchen" },
                    AssignedUser = new ApplicationUser { Id = userId, FirstName = "John", LastName = "Doe" }
                }
            };

            _mockTaskRepository.Setup(r => r.GetByAssignedUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userTasks);

            // Act
            var result = await _householdTaskService.GetUserTasksAsync(userId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().AssignedUserId, Is.EqualTo(userId));
        }

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
                    Title = "Overdue Task",
                    Room = new Room { Name = "Kitchen" }
                }
            };

            _mockTaskRepository.Setup(r => r.GetOverdueTasksAsync(householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueTasks);

            // Act
            var result = await _householdTaskService.GetOverdueTasksAsync(householdId);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().IsOverdue, Is.True);
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
                    Title = "Monday Task",
                    Room = new Room { Name = "Kitchen" }
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

            var request = new AssignTaskRequest { UserId = userId };

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                Room = new Room { Name = "Kitchen" }
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.AssignTaskAsync(taskId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            // Act
            var result = await _householdTaskService.AssignTaskAsync(taskId, request, requestingUserId);

            // Assert
            Assert.That(result, Is.Not.Null);
            _mockTaskRepository.Verify(r => r.AssignTaskAsync(taskId, userId, It.IsAny<CancellationToken>()), Times.Once);
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
                HouseholdId = householdId,
                AssignedUserId = "user123"
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

        [Test]
        [Category("HouseholdTaskService")]
        [Category("AutoAssignTasks")]
        public async Task AutoAssignTasksAsync_DelegatesToAssignmentService_WhenValidRequest()
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
        [Category("GetSuggestedAssignee")]
        public async Task GetSuggestedAssigneeAsync_ReturnsSuggestedUser_WhenUserFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";
            var suggestedUserId = "user123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateUserAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskAssignmentService.Setup(s => s.GetSuggestedAssigneeAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(suggestedUserId);

            // Act
            var result = await _householdTaskService.GetSuggestedAssigneeAsync(taskId, requestingUserId);

            // Assert
            Assert.That(result, Is.EqualTo(suggestedUserId));
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
        public async Task ValidateTaskAccessAsync_ThrowsNotFoundException_WhenTaskNotFound()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = "user123";

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<NotFoundException>(
                async () => await _householdTaskService.ValidateTaskAccessAsync(taskId, userId));

            Assert.That(exception.Message, Does.Contain("Task"));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("ValidateTaskOwnerAccess")]
        public async Task ValidateTaskOwnerAccessAsync_DoesNotThrow_WhenUserIsOwner()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "owner123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _householdTaskService.ValidateTaskOwnerAccessAsync(taskId, userId));
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("ValidateTaskOwnerAccess")]
        public async Task ValidateTaskOwnerAccessAsync_ThrowsUnauthorizedException_WhenUserNotOwner()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var userId = "member123";

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("You are not authorized to perform this action"));

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _householdTaskService.ValidateTaskOwnerAccessAsync(taskId, userId));
        }

        #endregion

        #region Edge Cases

        [Test]
        [Category("HouseholdTaskService")]
        [Category("CreateTask")]
        public async Task CreateTaskAsync_SetsCreatedAtAndIsActive_WhenCalled()
        {
            // Arrange
            var request = new UpsertTaskRequest
            {
                Title = "Test Task",
                HouseholdId = Guid.NewGuid(),
                RoomId = Guid.NewGuid(),
                Type = TaskType.Regular,
                Priority = TaskPriority.Medium,
                EstimatedMinutes = 30
            };

            var roomDto = new RoomDto { Id = request.RoomId, HouseholdId = request.HouseholdId };
            var createdTask = new HouseholdTask { Id = Guid.NewGuid(), Room = new Room { Name = "Test" } };

            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.ValidateRoomAccessAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockRoomService.Setup(s => s.GetRoomAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(roomDto);
            _mockTaskRepository.Setup(r => r.AddAsync(It.IsAny<HouseholdTask>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTask);
            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTask);

            // Act
            await _householdTaskService.CreateTaskAsync(request, "owner123");

            // Assert
            _mockTaskRepository.Verify(r => r.AddAsync(
                It.Is<HouseholdTask>(t =>
                    t.CreatedAt != default &&
                    t.IsActive == true),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        [Category("HouseholdTaskService")]
        [Category("AssignTask")]
        public async Task AssignTaskAsync_UnassignsTask_WhenUserIdIsNull()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var householdId = Guid.NewGuid();
            var requestingUserId = "owner123";

            var request = new AssignTaskRequest { UserId = null };

            var task = new HouseholdTask
            {
                Id = taskId,
                HouseholdId = householdId,
                AssignedUserId = "oldUser",
                Room = new Room { Name = "Kitchen" }
            };

            _mockTaskRepository.Setup(r => r.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);
            _mockHouseholdService.Setup(s => s.ValidateOwnerAccessAsync(householdId, requestingUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.UnassignTaskAsync(taskId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockTaskRepository.Setup(r => r.GetByIdWithRelationsAsync(taskId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            // Act
            await _householdTaskService.AssignTaskAsync(taskId, request, requestingUserId);

            // Assert
            _mockTaskRepository.Verify(r => r.UnassignTaskAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
            _mockTaskRepository.Verify(r => r.AssignTaskAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}
