using BannerlordCompanionCoop.Missions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Modes;

public sealed class CompanionDropInGameMode : MissionBasedMultiplayerGameMode
{
    public CompanionDropInGameMode(string gameType) : base(gameType)
    {
    }

    public override void StartMultiplayerGame(string scene)
    {
        MissionState.OpenNew(
            "CompanionDropIn",
            new MissionInitializerRecord(scene),
            missionController => new MissionBehavior[]
            {
                MissionLobbyComponent.CreateBehavior(),
                new CompanionDropInMissionServer(),
                new CompanionDropInMissionClient(),
                new MultiplayerTimerComponent(),
                new MissionLobbyEquipmentNetworkComponent(),
                new MultiplayerTeamSelectComponent(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new MultiplayerGameNotificationsComponent(),
                new MissionOptionsComponent(),
                new MultiplayerPreloadHelper(),
            });
    }
}

