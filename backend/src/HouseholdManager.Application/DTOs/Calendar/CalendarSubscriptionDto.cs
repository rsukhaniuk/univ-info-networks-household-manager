namespace HouseholdManager.Application.DTOs.Calendar
{
    /// <summary>
    /// Information for calendar subscription (CalDAV)
    /// </summary>
    public class CalendarSubscriptionDto
    {
        /// <summary>
        /// The subscription URL for the calendar feed
        /// </summary>
        public string SubscriptionUrl { get; set; } = string.Empty;

        /// <summary>
        /// The household ID this subscription is for
        /// </summary>
        public Guid HouseholdId { get; set; }

        /// <summary>
        /// Human-readable name for the calendar
        /// </summary>
        public string CalendarName { get; set; } = string.Empty;

        /// <summary>
        /// Instructions for subscribing to this calendar
        /// </summary>
        public string Instructions { get; set; } = string.Empty;

        /// <summary>
        /// When this subscription URL was generated
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
