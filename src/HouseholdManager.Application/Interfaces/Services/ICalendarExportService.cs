using HouseholdManager.Application.DTOs.Calendar;

namespace HouseholdManager.Application.Interfaces.Services
{
    /// <summary>
    /// Service for exporting household tasks to iCalendar format
    /// </summary>
    public interface ICalendarExportService
    {
        /// <summary>
        /// Export all active tasks for a household to iCalendar format
        /// </summary>
        /// <param name="householdId">The household ID</param>
        /// <param name="userId">The user requesting the export (for authorization)</param>
        /// <param name="startDate">Optional start date for filtering tasks</param>
        /// <param name="endDate">Optional end date for filtering tasks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iCalendar content as string</returns>
        Task<string> ExportHouseholdTasksAsync(
            Guid householdId,
            string userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Export a single task to iCalendar format
        /// </summary>
        /// <param name="taskId">The task ID</param>
        /// <param name="userId">The user requesting the export (for authorization)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>iCalendar content as string</returns>
        Task<string> ExportTaskAsync(Guid taskId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate a calendar subscription URL for a household
        /// </summary>
        /// <param name="householdId">The household ID</param>
        /// <param name="userId">The user requesting the URL (for authorization)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Calendar subscription information</returns>
        Task<CalendarSubscriptionDto> GetSubscriptionUrlAsync(Guid householdId, string userId, CancellationToken cancellationToken = default);
    }
}
