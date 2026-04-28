using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class CompanionMissionJoinScopeExtensions
{
    public static bool Allows(this CompanionMissionJoinScope allowedScopes, CompanionMissionJoinScope requestedScope)
    {
        if (allowedScopes == CompanionMissionJoinScope.None || requestedScope == CompanionMissionJoinScope.None)
        {
            return false;
        }

        return (allowedScopes & requestedScope) == requestedScope;
    }

    public static string ToSeatScopeToken(this CompanionMissionJoinScope allowedScopes)
    {
        if (allowedScopes == CompanionMissionJoinScope.None)
        {
            return "none";
        }

        if (allowedScopes == CompanionMissionJoinScope.AllSupportedScenes)
        {
            return "all_supported_scenes";
        }

        return string.Join(
            "_",
            GetOrderedFlags(allowedScopes));
    }

    private static string[] GetOrderedFlags(CompanionMissionJoinScope allowedScopes)
    {
        List<string> flags = new();

        if (allowedScopes.HasFlag(CompanionMissionJoinScope.Battles))
        {
            flags.Add("battles");
        }

        if (allowedScopes.HasFlag(CompanionMissionJoinScope.TownScenes))
        {
            flags.Add("town_scenes");
        }

        if (allowedScopes.HasFlag(CompanionMissionJoinScope.Raids))
        {
            flags.Add("raids");
        }

        if (allowedScopes.HasFlag(CompanionMissionJoinScope.Hideouts))
        {
            flags.Add("hideouts");
        }

        return flags.ToArray();
    }
}
