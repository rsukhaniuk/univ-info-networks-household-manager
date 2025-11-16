namespace HouseholdManager.Models.Enums
{
    /// <summary>
    /// Defines the type of household task
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// Task that repeats weekly on a specific day
        /// </summary>
        Regular,

        /// <summary>
        /// One-time task with a specific due date
        /// </summary>
        OneTime
    }
}
