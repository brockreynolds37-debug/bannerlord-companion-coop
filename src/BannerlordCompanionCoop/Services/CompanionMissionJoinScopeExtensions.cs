using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class CompanionMissionJoinScopeExtensions
{
    public static bool Allows(this CompanionMissionJoinScope allowedScope, CompanionMissionJoinScope requestedScope)
    {
        if (allowedScope == CompanionMissionJoinScope.None || requestedScope == CompanionMissionJoinScope.None)
        {
            return false;
        }

        return allowedScope == CompanionMissionJoinScope.AllSupportedScenes || allowedScope == requestedScope;
    }
}
