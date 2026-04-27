using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class DebugSeatCatalog
{
    public static IReadOnlyList<CompanionHeroProfile> CreateDefaultProfiles()
    {
        return new[]
        {
            new CompanionHeroProfile("companion_alayen", "Alayen", CompanionMissionRole.Fighter, 18, false),
            new CompanionHeroProfile("companion_ymira", "Ymira", CompanionMissionRole.Surgeon, 14, false),
            new CompanionHeroProfile("companion_baheshtur", "Baheshtur", CompanionMissionRole.Scout, 16, true),
        };
    }
}
