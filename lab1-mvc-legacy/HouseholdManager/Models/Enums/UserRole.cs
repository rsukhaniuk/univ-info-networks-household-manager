namespace HouseholdManager.Models.Enums
{
    /// <summary>
    /// System role of the user in the platform
    /// </summary>
    public enum SystemRole
    {
        User,        // Regular user (can create/join households)
        SystemAdmin  // System administrator (has access to all households without membership)
    }

    /// <summary>
    /// User role in a specific household
    /// Applies only to SystemRole.User
    /// </summary>
    public enum HouseholdRole
    {
        Member,      // Member - can perform tasks, view the plan
        Owner        // Owner - can manage members, create/assign tasks
    }
}
