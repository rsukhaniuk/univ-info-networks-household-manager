using HouseholdManager.Models;
using HouseholdManager.Models.Enums;
using HouseholdManager.Repositories.Interfaces;
using HouseholdManager.Services.Interfaces;

namespace HouseholdManager.Services.Implementations
{
    /// <summary>
    /// Implementation of household service with business logic
    /// </summary>
    public class HouseholdService : IHouseholdService
    {
        private readonly IHouseholdRepository _householdRepository;
        private readonly IHouseholdMemberRepository _memberRepository;
        private readonly ILogger<HouseholdService> _logger;

        public HouseholdService(
            IHouseholdRepository householdRepository,
            IHouseholdMemberRepository memberRepository,
            ILogger<HouseholdService> logger)
        {
            _householdRepository = householdRepository;
            _memberRepository = memberRepository;
            _logger = logger;
        }

    }
}
