using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionClient : MissionMultiplayerGameModeBaseClient
{
    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        // Future client flow:
        // 1. receive seat offers from host
        // 2. pick an available companion seat
        // 3. possess the spawned agent for that hero
    }
}

