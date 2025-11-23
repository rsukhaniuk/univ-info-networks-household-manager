using HouseholdManager.Application.DTOs.Calendar;
using HouseholdManager.Application.Interfaces.Repositories;
using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HouseholdManager.Application.Services
{
    /// <summary>
    /// Service for exporting household tasks to iCalendar format
    /// </summary>
    public class CalendarExportService : ICalendarExportService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IExecutionRepository _executionRepository;
        private readonly IHouseholdService _householdService;
        private readonly ICalendarGenerator _calendarGenerator;
        private readonly ICalendarTokenService _calendarTokenService;
        private readonly ILogger<CalendarExportService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CalendarExportService(
            ITaskRepository taskRepository,
            IExecutionRepository executionRepository,
            IHouseholdService householdService,
            ICalendarGenerator calendarGenerator,
            ICalendarTokenService calendarTokenService,
            ILogger<CalendarExportService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _taskRepository = taskRepository;
            _executionRepository = executionRepository;
            _householdService = householdService;
            _calendarGenerator = calendarGenerator;
            _calendarTokenService = calendarTokenService;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> ExportHouseholdTasksAsync(
            Guid householdId,
            string userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "User {UserId} exporting calendar for household {HouseholdId}",
                userId,
                householdId);

            // Validate user access
            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // Get household details
            var household = await _householdService.GetHouseholdAsync(householdId, cancellationToken);
            if (household == null)
            {
                throw new NotFoundException("Household", householdId);
            }

            // Get active tasks for household
            var allTasks = await _taskRepository.GetActiveByHouseholdIdAsync(householdId, cancellationToken);

            // Filter tasks based on user role:
            // Owner sees all tasks
            // Member sees only tasks assigned to them
            var isOwner = await _householdService.IsUserOwnerAsync(householdId, userId, cancellationToken);
            var tasks = isOwner
                ? allTasks
                : allTasks.Where(t => t.AssignedUserId == userId).ToList();

            _logger.LogInformation(
                "User {UserId} is {Role} - showing {TaskCount} of {TotalCount} tasks",
                userId,
                isOwner ? "Owner" : "Member",
                tasks.Count(),
                allTasks.Count);

            // Get execution history for tasks
            var taskIds = tasks.Select(t => t.Id).ToList();
            var executionsByTask = new Dictionary<Guid, IEnumerable<TaskExecution>>();

            foreach (var taskId in taskIds)
            {
                var executions = await _executionRepository.GetByTaskIdAsync(taskId, cancellationToken);
                executionsByTask[taskId] = executions;
            }

            // Convert to calendar events
            var calendarEvents = new List<Ical.Net.CalendarComponents.CalendarEvent>();
            foreach (var task in tasks)
            {
                executionsByTask.TryGetValue(task.Id, out var executions);
                var calendarEvent = _calendarGenerator.ConvertTaskToEvent(task, executions);
                calendarEvents.Add(calendarEvent);
            }

            // Generate iCalendar
            var calendarName = $"{household.Name} - Household Tasks";
            var description = $"Tasks and chores for {household.Name}";
            var icalContent = _calendarGenerator.GenerateCalendar(calendarEvents, calendarName, description);

            _logger.LogInformation(
                "Successfully exported {EventCount} calendar events for household {HouseholdId}",
                calendarEvents.Count,
                householdId);

            return icalContent;
        }

        public async Task<string> ExportTaskAsync(Guid taskId, string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "User {UserId} exporting single task {TaskId}",
                userId,
                taskId);

            // Get task with relations
            var task = await _taskRepository.GetByIdWithRelationsAsync(taskId, cancellationToken);
            if (task == null)
            {
                throw new NotFoundException("Task", taskId);
            }

            // Validate user access to household
            await _householdService.ValidateUserAccessAsync(task.HouseholdId, userId, cancellationToken);

            // Get execution history
            var executions = await _executionRepository.GetByTaskIdAsync(taskId, cancellationToken);

            // Convert to calendar event
            var calendarEvent = _calendarGenerator.ConvertTaskToEvent(task, executions);

            // Generate iCalendar
            var calendarName = $"Task: {task.Title}";
            var icalContent = _calendarGenerator.GenerateCalendar(
                new[] { calendarEvent },
                calendarName);

            _logger.LogInformation(
                "Successfully exported task {TaskId}",
                taskId);

            return icalContent;
        }

        public async Task<CalendarSubscriptionDto> GetSubscriptionUrlAsync(Guid householdId, string userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "User {UserId} requesting subscription URL for household {HouseholdId}",
                userId,
                householdId);

            // Validate user access
            await _householdService.ValidateUserAccessAsync(householdId, userId, cancellationToken);

            // Get household details
            var household = await _householdService.GetHouseholdAsync(householdId, cancellationToken);
            if (household == null)
            {
                throw new NotFoundException("Household", householdId);
            }

            // Generate calendar subscription token
            var token = await _calendarTokenService.GenerateTokenAsync(householdId, userId, cancellationToken);

            // Generate subscription URL with token parameter
            var request = _httpContextAccessor.HttpContext?.Request;
            // Use HTTPS in production (X-Forwarded-Proto header from load balancer, or force HTTPS for non-localhost)
            var scheme = request?.Headers["X-Forwarded-Proto"].FirstOrDefault()
                ?? (request?.Host.Host == "localhost" ? request?.Scheme : "https");
            var baseUrl = $"{scheme}://{request?.Host}{request?.PathBase}";
            var subscriptionUrl = $"{baseUrl}/api/households/{householdId}/calendar/feed.ics?token={token}";

            var subscriptionDto = new CalendarSubscriptionDto
            {
                SubscriptionUrl = subscriptionUrl,
                HouseholdId = householdId,
                CalendarName = $"{household.Name} - Household Tasks",
                Instructions = BuildInstructions(subscriptionUrl),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Generated subscription URL for household {HouseholdId}: {SubscriptionUrl}",
                householdId,
                subscriptionUrl);

            return subscriptionDto;
        }

        private string BuildInstructions(string subscriptionUrl)
        {
            return $@"Subscribe to this calendar in your preferred application:

                    **Google Calendar:**
                    1. Open Google Calendar
                    2. Click '+' next to 'Other calendars'
                    3. Select 'From URL'
                    4. Paste this URL: {subscriptionUrl}
                    5. Click 'Add calendar'

                    **Outlook:**
                    1. Open Outlook Calendar
                    2. Go to 'Add calendar' → 'Subscribe from web'
                    3. Paste this URL: {subscriptionUrl}
                    4. Click 'Import'

                    **Apple Calendar:**
                    1. Open Calendar app
                    2. Go to File → New Calendar Subscription
                    3. Paste this URL: {subscriptionUrl}
                    4. Click 'Subscribe'

                    The calendar will automatically update with changes to your household tasks.";
        }
    }
}
