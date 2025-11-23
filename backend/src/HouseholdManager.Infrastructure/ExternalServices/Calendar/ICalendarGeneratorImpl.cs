using HouseholdManager.Application.Interfaces.Services;
using HouseholdManager.Domain.Entities;
using HouseholdManager.Domain.Enums;
using HouseholdManager.Domain.Exceptions;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace HouseholdManager.Infrastructure.ExternalServices.Calendar
{
    /// <summary>
    /// Implementation of iCalendar generation using Ical.Net library
    /// </summary>
    public class ICalendarGeneratorImpl : ICalendarGenerator
    {
        private const string ProductId = "-//Household Manager//Task Calendar//EN";

        /// <inheritdoc/>
        public CalendarEvent ConvertTaskToEvent(HouseholdTask task, IEnumerable<TaskExecution>? executions = null)
        {
            var calendarEvent = new CalendarEvent
            {
                // Unique identifier for this event
                Uid = $"task-{task.Id}@household-manager.app",

                // Task title as event summary
                Summary = task.Title,

                // Task description with additional metadata
                Description = BuildEventDescription(task, executions),

                // Categories for filtering
                Categories = BuildCategories(task),

                // Priority mapping
                Priority = MapPriority(task.Priority),

                // Creation timestamp
                Created = new CalDateTime(task.CreatedAt),

                // Last modified (use latest execution or creation date)
                LastModified = new CalDateTime(executions?.OrderByDescending(e => e.CompletedAt).FirstOrDefault()?.CompletedAt ?? task.CreatedAt),

                // Status based on completion
                Status = DetermineStatus(task, executions)
            };

            // Set recurrence or single occurrence based on task type
            if (task.Type == TaskType.Regular)
            {
                SetRecurringEvent(calendarEvent, task);
            }
            else if (task.Type == TaskType.OneTime)
            {
                SetSingleEvent(calendarEvent, task);
            }

            return calendarEvent;
        }

        /// <inheritdoc/>
        public string GenerateCalendar(
            IEnumerable<CalendarEvent> events,
            string calendarName,
            string? description = null)
        {
            var calendar = new Ical.Net.Calendar
            {
                ProductId = ProductId,
                Version = "2.0"
            };

            // Set calendar name using X-WR-CALNAME property
            calendar.AddProperty("X-WR-CALNAME", calendarName);

            if (!string.IsNullOrWhiteSpace(description))
            {
                calendar.AddProperty("X-WR-CALDESC", description);
            }

            // Add timezone information
            calendar.AddProperty("X-WR-TIMEZONE", "UTC");

            // Add all events to calendar
            foreach (var evt in events)
            {
                calendar.Events.Add(evt);
            }

            // Serialize to iCalendar format
            var serializer = new CalendarSerializer();
            return serializer.SerializeToString(calendar);
        }

        #region Private Helper Methods

        private string BuildEventDescription(HouseholdTask task, IEnumerable<TaskExecution>? executions)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                parts.Add(task.Description);
                parts.Add(""); // Empty line
            }

            parts.Add($"Room: {task.Room?.Name ?? "Unknown"}");

            if (task.AssignedUser != null)
            {
                parts.Add($"Assigned to: {task.AssignedUser.FullName}");
            }

            // Add completion history
            if (executions != null && executions.Any())
            {
                var validExecutions = executions
                    .Where(e => e.IsCountedForCompletion ?? true)
                    .OrderByDescending(e => e.CompletedAt)
                    .Take(5);

                if (validExecutions.Any())
                {
                    parts.Add("");
                    parts.Add("Recent Completions:");
                    foreach (var execution in validExecutions)
                    {
                        var completedBy = execution.UserId;
                        var completedAt = execution.CompletedAt.ToString("yyyy-MM-dd");
                        parts.Add($"• {completedAt} by {completedBy}");
                    }
                }
            }

            parts.Add("");
            parts.Add("Managed by Household Manager");

            return string.Join("\n", parts);
        }

        private List<string> BuildCategories(HouseholdTask task)
        {
            var categories = new List<string>
            {
                "Household",
                task.Type.ToString(),
                task.Priority.ToString()
            };

            if (task.Room != null)
            {
                categories.Add(task.Room.Name);
            }

            return categories;
        }

        private int MapPriority(TaskPriority priority)
        {
            // iCalendar priority: 1 = highest, 9 = lowest, 0 = undefined
            return priority switch
            {
                TaskPriority.High => 1,
                TaskPriority.Medium => 5,
                TaskPriority.Low => 9,
                _ => 0
            };
        }

        private string DetermineStatus(HouseholdTask task, IEnumerable<TaskExecution>? executions)
        {
            if (!task.IsActive)
            {
                return EventStatus.Cancelled;
            }

            // Check if task is completed this week for regular tasks
            if (task.Type == TaskType.Regular && executions != null)
            {
                var thisWeekStart = GetWeekStarting(DateTime.UtcNow);
                var completedThisWeek = executions.Any(e =>
                    e.WeekStarting == thisWeekStart &&
                    (e.IsCountedForCompletion ?? true));

                return completedThisWeek ? EventStatus.Confirmed : EventStatus.Tentative;
            }

            // For one-time tasks, check if there's any valid execution
            if (task.Type == TaskType.OneTime && executions != null)
            {
                var hasValidExecution = executions.Any(e => e.IsCountedForCompletion ?? true);
                return hasValidExecution ? EventStatus.Confirmed : EventStatus.Tentative;
            }

            return EventStatus.Tentative;
        }

        private void SetRecurringEvent(CalendarEvent calendarEvent, HouseholdTask task)
        {
            DateTime startDate;

            // Regular tasks must have RecurrenceRule
            if (string.IsNullOrWhiteSpace(task.RecurrenceRule))
            {
                throw new ValidationException("Regular tasks must have a recurrence pattern configured");
            }

            var recurrencePattern = new RecurrencePattern(task.RecurrenceRule);

            // Apply RecurrenceEndDate if specified
            if (task.RecurrenceEndDate.HasValue)
            {
                recurrencePattern.Until = new CalDateTime(task.RecurrenceEndDate.Value);
            }

            calendarEvent.RecurrenceRules.Add(recurrencePattern);

            // Determine start date based on recurrence pattern
            if (recurrencePattern.Frequency == FrequencyType.Weekly && recurrencePattern.ByDay?.Any() == true)
            {
                // For weekly tasks, calculate next occurrence of the first specified day
                var firstDay = ConvertIcalDayOfWeekToDotNet(recurrencePattern.ByDay.First().DayOfWeek);
                startDate = CalculateNextOccurrence(firstDay);
            }
            else
            {
                // For other patterns (DAILY, MONTHLY, etc.), start from today or creation date
                startDate = task.CreatedAt > DateTime.UtcNow ? task.CreatedAt : DateTime.UtcNow;
                startDate = startDate.Date;
            }

            // Set as all-day event (no specific time)
            // Use date-only format (YYYYMMDD without time component)
            calendarEvent.Start = new CalDateTime(startDate.Year, startDate.Month, startDate.Day);
            // Don't set End time - this makes it a task without specific duration
        }

        private void SetSingleEvent(CalendarEvent calendarEvent, HouseholdTask task)
        {
            if (task.DueDate.HasValue)
            {
                var dueDate = task.DueDate.Value.Date;

                // Set as all-day event (no specific time)
                // Use date-only format (YYYYMMDD without time component)
                calendarEvent.Start = new CalDateTime(dueDate.Year, dueDate.Month, dueDate.Day);
                // Don't set End time - this makes it a task without specific duration
            }
        }

        private DayOfWeek ConvertDayOfWeek(System.DayOfWeek dayOfWeek)
        {
            // Direct cast as both enums use same values
            return (DayOfWeek)(int)dayOfWeek;
        }

        private System.DayOfWeek ConvertIcalDayOfWeekToDotNet(DayOfWeek icDayOfWeek)
        {
            // Direct cast as both enums use same values
            return (System.DayOfWeek)(int)icDayOfWeek;
        }

        private DateTime CalculateNextOccurrence(System.DayOfWeek targetDay)
        {
            var today = DateTime.UtcNow;
            var daysUntilTarget = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;

            if (daysUntilTarget == 0)
            {
                // If today is the target day, use today
                return today.Date;
            }

            return today.Date.AddDays(daysUntilTarget);
        }

        private DateTime GetWeekStarting(DateTime date)
        {
            // Week starts on Monday
            var diff = (7 + (date.DayOfWeek - System.DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-1 * diff);
        }

        #endregion
    }
}
