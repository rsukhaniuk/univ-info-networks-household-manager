using HouseholdManager.Domain.Entities;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace HouseholdManager.Application.Helpers
{
    /// <summary>
    /// Helper utility for working with iCalendar RRULE
    /// </summary>
    public static class RruleHelper
    {
        /// <summary>
        /// Checks if the RRULE is a weekly recurrence pattern
        /// </summary>
        public static bool IsWeeklyRecurrence(string? rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
                return false;

            try
            {
                var pattern = new RecurrencePattern(rrule);
                return pattern.Frequency == Ical.Net.FrequencyType.Weekly;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts list of weekdays from RRULE BYDAY parameter
        /// Returns empty list if RRULE is not weekly or doesn't have BYDAY
        /// </summary>
        public static List<System.DayOfWeek> ExtractWeekdays(string? rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
                return new List<System.DayOfWeek>();

            try
            {
                var pattern = new RecurrencePattern(rrule);

                if (pattern.Frequency != Ical.Net.FrequencyType.Weekly || pattern.ByDay == null || !pattern.ByDay.Any())
                    return new List<System.DayOfWeek>();

                return pattern.ByDay
                    .Select(wd => ConvertIcalDayOfWeekToDotNet((int)wd.DayOfWeek))
                    .ToList();
            }
            catch
            {
                return new List<System.DayOfWeek>();
            }
        }

        /// <summary>
        /// Creates a weekly RRULE for a specific day of week
        /// </summary>
        public static string CreateWeeklyRrule(System.DayOfWeek day)
        {
            var icDayOfWeek = ConvertDotNetDayOfWeekToIcal(day);
            return $"FREQ=WEEKLY;BYDAY={icDayOfWeek}";
        }

        /// <summary>
        /// Creates a weekly RRULE for multiple days of week
        /// </summary>
        public static string CreateWeeklyRrule(IEnumerable<System.DayOfWeek> days)
        {
            var byDays = days
                .Select(ConvertDotNetDayOfWeekToIcal)
                .ToList();

            if (!byDays.Any())
                throw new ArgumentException("At least one day must be specified", nameof(days));

            return $"FREQ=WEEKLY;BYDAY={string.Join(",", byDays)}";
        }

        /// <summary>
        /// Checks if task can be auto-assigned (must be FREQ=WEEKLY with BYDAY)
        /// </summary>
        public static bool CanAutoAssign(string? rrule)
        {
            if (string.IsNullOrWhiteSpace(rrule))
                return false;

            try
            {
                var pattern = new RecurrencePattern(rrule);
                return pattern.Frequency == Ical.Net.FrequencyType.Weekly
                    && pattern.ByDay != null
                    && pattern.ByDay.Any();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Groups tasks by weekday based on their RecurrenceRule
        /// Only includes tasks that can be auto-assigned (FREQ=WEEKLY with BYDAY)
        /// Tasks with multiple BYDAY values will appear in multiple groups
        /// </summary>
        public static Dictionary<System.DayOfWeek, List<HouseholdTask>> GroupTasksByWeekday(IEnumerable<HouseholdTask> tasks)
        {
            var result = new Dictionary<System.DayOfWeek, List<HouseholdTask>>();

            foreach (var task in tasks)
            {
                if (!CanAutoAssign(task.RecurrenceRule))
                    continue;

                var weekdays = ExtractWeekdays(task.RecurrenceRule);

                foreach (var day in weekdays)
                {
                    if (!result.ContainsKey(day))
                        result[day] = new List<HouseholdTask>();

                    result[day].Add(task);
                }
            }

            return result;
        }

        /// <summary>
        /// Converts iCalendar DayOfWeek to .NET DayOfWeek
        /// WeekDay.DayOfWeek в Ical.Net це int, тому конвертуємо через cast
        /// </summary>
        private static System.DayOfWeek ConvertIcalDayOfWeekToDotNet(int icDayOfWeek)
        {
            // Direct cast as both enums use same values (Sunday=0, Monday=1, etc.)
            return (System.DayOfWeek)icDayOfWeek;
        }

        /// <summary>
        /// Converts .NET DayOfWeek to iCalendar BYDAY string (MO, TU, WE, etc.)
        /// </summary>
        private static string ConvertDotNetDayOfWeekToIcal(System.DayOfWeek day)
        {
            return day switch
            {
                System.DayOfWeek.Sunday => "SU",
                System.DayOfWeek.Monday => "MO",
                System.DayOfWeek.Tuesday => "TU",
                System.DayOfWeek.Wednesday => "WE",
                System.DayOfWeek.Thursday => "TH",
                System.DayOfWeek.Friday => "FR",
                System.DayOfWeek.Saturday => "SA",
                _ => throw new ArgumentException($"Unknown DayOfWeek: {day}")
            };
        }
    }
}
