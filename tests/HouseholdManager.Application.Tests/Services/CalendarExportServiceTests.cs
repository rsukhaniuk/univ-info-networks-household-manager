using HouseholdManager.Application.DTOs.Calendar;
using HouseholdManager.Application.DTOs.Household;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Application.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Ical.Net.CalendarComponents;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;

namespace HouseholdManager.Application.Tests.Services
{
    /// <summary>
    /// Unit tests for CalendarExportService
    /// Tests calendar export, subscription URL generation, and access control
    /// </summary>
    [TestFixture]
    public class CalendarExportServiceTests
    {
        private Mock<ITaskRepository> _mockTaskRepository;
        private Mock<IExecutionRepository> _mockExecutionRepository;
        private Mock<IHouseholdService> _mockHouseholdService;
        private Mock<ICalendarGenerator> _mockCalendarGenerator;
        private Mock<ICalendarTokenService> _mockCalendarTokenService;
        private Mock<ILogger<CalendarExportService>> _mockLogger;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private CalendarExportService _service;

        // Test data
        private Guid _householdId;
        private Guid _taskId1;
        private Guid _taskId2;
        private Guid _executionId;
        private string _userId;
        private string _ownerUserId;

        [SetUp]
        public void Setup()
        {
            // Initialize test data
            _householdId = Guid.NewGuid();
            _taskId1 = Guid.NewGuid();
            _taskId2 = Guid.NewGuid();
            _executionId = Guid.NewGuid();
            _userId = "auth0|user123";
            _ownerUserId = "auth0|owner456";

            // Setup mocks
            _mockTaskRepository = new Mock<ITaskRepository>();
            _mockExecutionRepository = new Mock<IExecutionRepository>();
            _mockHouseholdService = new Mock<IHouseholdService>();
            _mockCalendarGenerator = new Mock<ICalendarGenerator>();
            _mockCalendarTokenService = new Mock<ICalendarTokenService>();
            _mockLogger = new Mock<ILogger<CalendarExportService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            // Setup HTTP context for subscription URLs
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new HeaderDictionary();

            mockRequest.Setup(r => r.Scheme).Returns("https");
            mockRequest.Setup(r => r.Host).Returns(new HostString("api.example.com"));
            mockRequest.Setup(r => r.PathBase).Returns(new PathString(""));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders);

            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            // Create service instance
            _service = new CalendarExportService(
                _mockTaskRepository.Object,
                _mockExecutionRepository.Object,
                _mockHouseholdService.Object,
                _mockCalendarGenerator.Object,
                _mockCalendarTokenService.Object,
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockHttpContextAccessor.Object
            );
        }

        #region Helper Methods

        private HouseholdTask CreateTestTask(Guid taskId, string title, string? assignedUserId = null)
        {
            return new HouseholdTask
            {
                Id = taskId,
                HouseholdId = _householdId,
                Title = title,
                Description = $"Description for {title}",
                Type = TaskType.Regular,
                Priority = TaskPriority.Medium,
                EstimatedMinutes = 30,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RecurrenceRule = "FREQ=WEEKLY;BYDAY=MO",
                AssignedUserId = assignedUserId
            };
        }

        private TaskExecution CreateTestExecution(Guid taskId, string userId)
        {
            return new TaskExecution
            {
                Id = _executionId,
                TaskId = taskId,
                UserId = userId,
                CompletedAt = DateTime.UtcNow,
                Notes = "Completed successfully",
                HouseholdId = _householdId
            };
        }

        private HouseholdDto CreateTestHouseholdDto()
        {
            return new HouseholdDto
            {
                Id = _householdId,
                Name = "Test Household",
                Description = "Test household description"
            };
        }

        #endregion

        #region ExportHouseholdTasksAsync Tests

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportHouseholdTasks")]
        public async Task ExportHouseholdTasksAsync_AsOwner_ExportsAllTasks()
        {
            // Arrange
            var household = CreateTestHouseholdDto();
            var task1 = CreateTestTask(_taskId1, "Task 1", _userId);
            var task2 = CreateTestTask(_taskId2, "Task 2", _ownerUserId);
            var tasks = new List<HouseholdTask> { task1, task2 };

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _ownerUserId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            _mockTaskRepository
                .Setup(x => x.GetActiveByHouseholdIdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tasks);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _ownerUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution>());

            var calendarEvent1 = new CalendarEvent();
            var calendarEvent2 = new CalendarEvent();
            _mockCalendarGenerator
                .Setup(x => x.ConvertTaskToEvent(task1, It.IsAny<IEnumerable<TaskExecution>>()))
                .Returns(calendarEvent1);
            _mockCalendarGenerator
                .Setup(x => x.ConvertTaskToEvent(task2, It.IsAny<IEnumerable<TaskExecution>>()))
                .Returns(calendarEvent2);

            var expectedIcal = "BEGIN:VCALENDAR...";
            _mockCalendarGenerator
                .Setup(x => x.GenerateCalendar(
                    It.Is<IEnumerable<CalendarEvent>>(events => events.Count() == 2),
                    "Test Household - Household Tasks",
                    "Tasks and chores for Test Household"))
                .Returns(expectedIcal);

            // Act
            var result = await _service.ExportHouseholdTasksAsync(_householdId, _ownerUserId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedIcal));
            _mockCalendarGenerator.Verify(x => x.ConvertTaskToEvent(It.IsAny<HouseholdTask>(), It.IsAny<IEnumerable<TaskExecution>>()), Times.Exactly(2));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportHouseholdTasks")]
        public async Task ExportHouseholdTasksAsync_AsMember_ExportsOnlyAssignedTasks()
        {
            // Arrange
            var household = CreateTestHouseholdDto();
            var task1 = CreateTestTask(_taskId1, "My Task", _userId);
            var task2 = CreateTestTask(_taskId2, "Other Task", _ownerUserId);
            var allTasks = new List<HouseholdTask> { task1, task2 };

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            _mockTaskRepository
                .Setup(x => x.GetActiveByHouseholdIdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(allTasks);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution>());

            var calendarEvent1 = new CalendarEvent();
            _mockCalendarGenerator
                .Setup(x => x.ConvertTaskToEvent(task1, It.IsAny<IEnumerable<TaskExecution>>()))
                .Returns(calendarEvent1);

            var expectedIcal = "BEGIN:VCALENDAR...";
            _mockCalendarGenerator
                .Setup(x => x.GenerateCalendar(
                    It.Is<IEnumerable<CalendarEvent>>(events => events.Count() == 1),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .Returns(expectedIcal);

            // Act
            var result = await _service.ExportHouseholdTasksAsync(_householdId, _userId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedIcal));
            _mockCalendarGenerator.Verify(x => x.ConvertTaskToEvent(task1, It.IsAny<IEnumerable<TaskExecution>>()), Times.Once);
            _mockCalendarGenerator.Verify(x => x.ConvertTaskToEvent(task2, It.IsAny<IEnumerable<TaskExecution>>()), Times.Never);
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportHouseholdTasks")]
        public async Task ExportHouseholdTasksAsync_HouseholdNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdDto?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
                await _service.ExportHouseholdTasksAsync(_householdId, _userId));

            Assert.That(ex.Message, Does.Contain("Household"));
            Assert.That(ex.Message, Does.Contain(_householdId.ToString()));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportHouseholdTasks")]
        public async Task ExportHouseholdTasksAsync_NoAccess_ThrowsUnauthorizedException()
        {
            // Arrange
            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("User does not have access to this household"));

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _service.ExportHouseholdTasksAsync(_householdId, _userId));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportHouseholdTasks")]
        public async Task ExportHouseholdTasksAsync_WithExecutionHistory_IncludesExecutions()
        {
            // Arrange
            var household = CreateTestHouseholdDto();
            var task = CreateTestTask(_taskId1, "Task with history", _userId);
            var execution = CreateTestExecution(_taskId1, _userId);
            var tasks = new List<HouseholdTask> { task };

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            _mockTaskRepository
                .Setup(x => x.GetActiveByHouseholdIdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tasks);

            _mockHouseholdService
                .Setup(x => x.IsUserOwnerAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution> { execution });

            var calendarEvent = new CalendarEvent();
            _mockCalendarGenerator
                .Setup(x => x.ConvertTaskToEvent(task, It.Is<IEnumerable<TaskExecution>>(e => e.Contains(execution))))
                .Returns(calendarEvent);

            _mockCalendarGenerator
                .Setup(x => x.GenerateCalendar(It.IsAny<IEnumerable<CalendarEvent>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("BEGIN:VCALENDAR...");

            // Act
            await _service.ExportHouseholdTasksAsync(_householdId, _userId);

            // Assert
            _mockCalendarGenerator.Verify(
                x => x.ConvertTaskToEvent(task, It.Is<IEnumerable<TaskExecution>>(e => e.Contains(execution))),
                Times.Once);
        }

        #endregion

        #region ExportTaskAsync Tests

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportTask")]
        public async Task ExportTaskAsync_ValidTask_ReturnsICalContent()
        {
            // Arrange
            var task = CreateTestTask(_taskId1, "Single Task");
            var execution = CreateTestExecution(_taskId1, _userId);

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockExecutionRepository
                .Setup(x => x.GetByTaskIdAsync(_taskId1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<TaskExecution> { execution });

            var calendarEvent = new CalendarEvent();
            _mockCalendarGenerator
                .Setup(x => x.ConvertTaskToEvent(task, It.IsAny<IEnumerable<TaskExecution>>()))
                .Returns(calendarEvent);

            var expectedIcal = "BEGIN:VCALENDAR...";
            _mockCalendarGenerator
                .Setup(x => x.GenerateCalendar(
                    It.Is<IEnumerable<CalendarEvent>>(events => events.Count() == 1),
                    "Task: Single Task",
                    null))
                .Returns(expectedIcal);

            // Act
            var result = await _service.ExportTaskAsync(_taskId1, _userId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedIcal));
            _mockCalendarGenerator.Verify(x => x.ConvertTaskToEvent(task, It.IsAny<IEnumerable<TaskExecution>>()), Times.Once);
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportTask")]
        public async Task ExportTaskAsync_TaskNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdTask?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
                await _service.ExportTaskAsync(_taskId1, _userId));

            Assert.That(ex.Message, Does.Contain("Task"));
            Assert.That(ex.Message, Does.Contain(_taskId1.ToString()));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("ExportTask")]
        public async Task ExportTaskAsync_NoAccess_ThrowsUnauthorizedException()
        {
            // Arrange
            var task = CreateTestTask(_taskId1, "Task");

            _mockTaskRepository
                .Setup(x => x.GetByIdWithRelationsAsync(_taskId1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(task);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("User does not have access to this household"));

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _service.ExportTaskAsync(_taskId1, _userId));
        }

        #endregion

        #region GetSubscriptionUrlAsync Tests

        [Test]
        [Category("CalendarExportService")]
        [Category("GetSubscriptionUrl")]
        public async Task GetSubscriptionUrlAsync_ValidRequest_ReturnsSubscriptionDto()
        {
            // Arrange
            var household = CreateTestHouseholdDto();
            var expectedToken = "test-calendar-token-123";

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            _mockCalendarTokenService
                .Setup(x => x.GenerateTokenAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await _service.GetSubscriptionUrlAsync(_householdId, _userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.HouseholdId, Is.EqualTo(_householdId));
            Assert.That(result.CalendarName, Is.EqualTo("Test Household - Household Tasks"));
            Assert.That(result.SubscriptionUrl, Does.Contain($"/api/households/{_householdId}/calendar/feed.ics"));
            Assert.That(result.SubscriptionUrl, Does.Contain($"token={expectedToken}"));
            Assert.That(result.SubscriptionUrl, Does.StartWith("https://"));
            Assert.That(result.Instructions, Is.Not.Empty);
            Assert.That(result.Instructions, Does.Contain("Google Calendar"));
            Assert.That(result.Instructions, Does.Contain("Outlook"));
            Assert.That(result.Instructions, Does.Contain("Apple Calendar"));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("GetSubscriptionUrl")]
        public async Task GetSubscriptionUrlAsync_LocalhostRequest_UsesHttpScheme()
        {
            // Arrange
            var household = CreateTestHouseholdDto();
            var expectedToken = "test-token";

            // Setup localhost HTTP context
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new HeaderDictionary();

            mockRequest.Setup(r => r.Scheme).Returns("http");
            mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5000));
            mockRequest.Setup(r => r.PathBase).Returns(new PathString(""));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders);

            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            _mockCalendarTokenService
                .Setup(x => x.GenerateTokenAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await _service.GetSubscriptionUrlAsync(_householdId, _userId);

            // Assert
            Assert.That(result.SubscriptionUrl, Does.StartWith("http://localhost:5000"));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("GetSubscriptionUrl")]
        public async Task GetSubscriptionUrlAsync_WithXForwardedProto_UsesForwardedScheme()
        {
            // Arrange
            var household = CreateTestHouseholdDto();
            var expectedToken = "test-token";

            // Setup HTTP context with X-Forwarded-Proto header
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockHeaders = new HeaderDictionary
            {
                { "X-Forwarded-Proto", "https" }
            };

            mockRequest.Setup(r => r.Scheme).Returns("http");
            mockRequest.Setup(r => r.Host).Returns(new HostString("api.example.com"));
            mockRequest.Setup(r => r.PathBase).Returns(new PathString(""));
            mockRequest.Setup(r => r.Headers).Returns(mockHeaders);

            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
            _mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);

            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(household);

            _mockCalendarTokenService
                .Setup(x => x.GenerateTokenAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedToken);

            // Act
            var result = await _service.GetSubscriptionUrlAsync(_householdId, _userId);

            // Assert
            Assert.That(result.SubscriptionUrl, Does.StartWith("https://"));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("GetSubscriptionUrl")]
        public async Task GetSubscriptionUrlAsync_HouseholdNotFound_ThrowsNotFoundException()
        {
            // Arrange
            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockHouseholdService
                .Setup(x => x.GetHouseholdAsync(_householdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((HouseholdDto?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<NotFoundException>(async () =>
                await _service.GetSubscriptionUrlAsync(_householdId, _userId));

            Assert.That(ex.Message, Does.Contain("Household"));
        }

        [Test]
        [Category("CalendarExportService")]
        [Category("GetSubscriptionUrl")]
        public async Task GetSubscriptionUrlAsync_NoAccess_ThrowsUnauthorizedException()
        {
            // Arrange
            _mockHouseholdService
                .Setup(x => x.ValidateUserAccessAsync(_householdId, _userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedException("User does not have access"));

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await _service.GetSubscriptionUrlAsync(_householdId, _userId));
        }

        #endregion
    }
}
