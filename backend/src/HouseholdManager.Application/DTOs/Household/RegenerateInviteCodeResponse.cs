namespace HouseholdManager.Application.DTOs.Household
{
    /// <summary>
    /// Response DTO for regenerate invite code operation
    /// </summary>
    public class RegenerateInviteCodeResponse
    {
        /// <summary>
        /// New invite code
        /// </summary>
        public Guid InviteCode { get; set; }

        /// <summary>
        /// Date and time when the invite code expires (UTC)
        /// </summary>
        public DateTime? InviteCodeExpiresAt { get; set; }
    }
}
