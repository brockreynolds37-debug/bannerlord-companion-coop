using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class DebugSeatCatalog
{
    public static IReadOnlyList<CompanionHeroProfile> CreateDefaultProfiles()
    {
        return new[]
        {
            new CompanionHeroProfile("imperial_infantryman", "imperial_infantryman", "Imperial Infantryman", CompanionMissionRole.Fighter, 18, false),
            new CompanionHeroProfile("khuzait_spearman", "khuzait_spearman", "Khuzait Spearman", CompanionMissionRole.Scout, 14, false),
            new CompanionHeroProfile("sturgian_warrior", "sturgian_warrior", "Sturgian Warrior", CompanionMissionRole.Surgeon, 16, true),
        };
    }
}
