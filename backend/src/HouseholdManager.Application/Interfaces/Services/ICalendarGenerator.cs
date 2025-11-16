using HouseholdManager.Domain.Entities;
using Ical.Net;
using Ical.Net.CalendarComponents;

namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Low-level interface for generating iCalendar objects
    /// </summary>
    public interface ICalendarGenerator
    {
        /// <summary>
        /// Convert a household task to an iCalendar event
        /// </summary>
        /// <param name="task">The household task</param>
        /// <param name="executions">Optional execution history for the task</param>
        /// <returns>iCalendar VEvent</returns>
        CalendarEvent ConvertTaskToEvent(HouseholdTask task, IEnumerable<TaskExecution>? executions = null);

        /// <summary>
        /// Generate a complete iCalendar document from multiple events
        /// </summary>
        /// <param name="events">Collection of calendar events</param>
        /// <param name="calendarName">Name of the calendar</param>
        /// <param name="description">Description of the calendar</param>
        /// <returns>Serialized iCalendar string</returns>
        string GenerateCalendar(
            IEnumerable<CalendarEvent> events,
            string calendarName,
            string? description = null);
    }
}
