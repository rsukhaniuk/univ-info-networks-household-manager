namespace HouseholdManager.Extensions
{
    /// <summary>
    /// Extension methods for DateTime to handle UTC to Local conversions
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts UTC DateTime to local time with custom format.
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value to convert.</param>
        /// <param name="format">The custom date and time format string.</param>
        /// <returns>The formatted local date and time string.</returns>
        public static string ToLocalString(this DateTime utcDateTime, string format = "MMM dd, yyyy HH:mm")
        {
            return utcDateTime.ToLocalTime().ToString(format);
        }

        /// <summary>
        /// Converts UTC DateTime to local date only (long format).
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value to convert.</param>
        /// <returns>The formatted local date string (long format).</returns>
        public static string ToLocalDateLong(this DateTime utcDateTime)
        {
            return utcDateTime.ToLocalTime().ToString("MMMM dd, yyyy");
        }

        /// <summary>
        /// Converts UTC DateTime to local date only (short format).
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value to convert.</param>
        /// <returns>The formatted local date string (short format).</returns>
        public static string ToLocalDateShort(this DateTime utcDateTime)
        {
            return utcDateTime.ToLocalTime().ToString("MMM dd, yyyy");
        }

        /// <summary>
        /// Converts UTC DateTime to local time only.
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value to convert.</param>
        /// <returns>The formatted local time string.</returns>
        public static string ToLocalTimeString(this DateTime utcDateTime)
        {
            return utcDateTime.ToLocalTime().ToString("HH:mm");
        }

        /// <summary>
        /// Converts UTC DateTime to full local date and time.
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value to convert.</param>
        /// <returns>The formatted local date and time string.</returns>
        public static string ToLocalDateTimeString(this DateTime utcDateTime)
        {
            return utcDateTime.ToLocalTime().ToString("MMM dd, yyyy HH:mm");
        }

        /// <summary>
        /// Returns relative time string (e.g., "2 hours ago").
        /// </summary>
        /// <param name="utcDateTime">The UTC DateTime value to convert.</param>
        /// <returns>The relative time string (e.g., "2 hours ago").</returns>
        public static string ToRelativeTime(this DateTime utcDateTime)
        {
            var localTime = utcDateTime.ToLocalTime();
            var timeSpan = DateTime.Now - localTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} week{((int)(timeSpan.TotalDays / 7) == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago";

            return localTime.ToString("MMM dd, yyyy");
        }

        // Nullable DateTime overloads

        /// <summary>
        /// Converts nullable UTC DateTime to local string (returns default text if null).
        /// </summary>
        /// <param name="utcDateTime">The nullable UTC DateTime value to convert.</param>
        /// <param name="format">The custom date and time format string.</param>
        /// <param name="nullText">The text to return if the value is null.</param>
        /// <returns>The formatted local date and time string, or default text if null.</returns>
        public static string ToLocalString(this DateTime? utcDateTime, string format = "MMM dd, yyyy HH:mm", string nullText = "Never")
        {
            return utcDateTime.HasValue
                ? utcDateTime.Value.ToLocalTime().ToString(format)
                : nullText;
        }

        /// <summary>
        /// Converts nullable UTC DateTime to local date only (returns default text if null).
        /// </summary>
        /// <param name="utcDateTime">The nullable UTC DateTime value to convert.</param>
        /// <param name="nullText">The text to return if the value is null.</param>
        /// <returns>The formatted local date string (short format), or default text if null.</returns>
        public static string ToLocalDateShort(this DateTime? utcDateTime, string nullText = "No date")
        {
            return utcDateTime.HasValue
                ? utcDateTime.Value.ToLocalTime().ToString("MMM dd, yyyy")
                : nullText;
        }

        /// <summary>
        /// Converts nullable UTC DateTime to relative time (returns default text if null).
        /// </summary>
        /// <param name="utcDateTime">The nullable UTC DateTime value to convert.</param>
        /// <param name="nullText">The text to return if the value is null.</param>
        /// <returns>The relative time string, or default text if null.</returns>
        public static string ToRelativeTime(this DateTime? utcDateTime, string nullText = "Never")
        {
            return utcDateTime.HasValue
                ? utcDateTime.Value.ToRelativeTime()
                : nullText;
        }
    }
}
