using BannerlordCompanionCoop.Contracts;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Integration;

public static class CompanionMissionSceneScopeResolver
{
    public static CompanionMissionJoinScope ResolveForMission(Mission? mission)
    {
        if (mission is null)
        {
            return CompanionMissionJoinScope.None;
        }

        if (mission.IsFieldBattle || mission.IsSiegeBattle || mission.IsNavalBattle || mission.IsSallyOutBattle)
        {
            return CompanionMissionJoinScope.Battles;
        }

        return CompanionMissionJoinScope.None;
    }
}
