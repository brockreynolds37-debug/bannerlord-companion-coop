using BannerlordCompanionCoop.Missions;
using BannerlordCompanionCoop.Networking;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Modes;

public sealed class CompanionDropInGameMode : MultiplayerGameMode
{
    public CompanionDropInGameMode(string gameType) : base(gameType)
    {
    }

    public override void JoinCustomGame(TaleWorlds.MountAndBlade.Diamond.JoinGameData joinGameData)
    {
        LobbyGameStateCustomGameClient lobbyGameStateCustomGameClient = Game.Current.GameStateManager.CreateState<LobbyGameStateCustomGameClient>();
        lobbyGameStateCustomGameClient.SetStartingParameters(
            NetworkMain.GameClient,
            joinGameData.GameServerProperties.Address,
            joinGameData.GameServerProperties.Port,
            joinGameData.PeerIndex,
            joinGameData.SessionKey);

        Game.Current.GameStateManager.PushState(lobbyGameStateCustomGameClient, 0);
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
                new CompanionBattlePossessionBehavior(),
                new CompanionMissionNetworkBehavior(),
                new MultiplayerTimerComponent(),
                new MissionLobbyEquipmentNetworkComponent(),
                new MultiplayerTeamSelectComponent(),
                new MissionBoundaryPlacer(),
                new MissionBoundaryCrossingHandler(),
                new MultiplayerGameNotificationsComponent(),
                new TaleWorlds.MountAndBlade.Source.Missions.MissionOptionsComponent(),
            });
    }
}
