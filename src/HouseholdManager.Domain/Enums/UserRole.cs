using System.Text.Json.Serialization;

namespace HouseholdManager.Domain.Enums
{
    /// <summary>
    /// System role of the user in the platform
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SystemRole
    {
        User,        // Regular user (can create/join households)
        SystemAdmin  // System administrator (has access to all households without membership)
    }

    /// <summary>
    /// User role in a specific household
    /// Applies only to SystemRole.User
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HouseholdRole
    {
        Member,      // Member - can perform tasks, view the plan
        Owner        // Owner - can manage members, create/assign tasks
    }
}
