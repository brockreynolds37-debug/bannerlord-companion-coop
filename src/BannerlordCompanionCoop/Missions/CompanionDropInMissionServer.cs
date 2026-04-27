using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionServer : MissionMultiplayerGameModeBase
{
    private readonly CompanionSeatRegistry _seatRegistry = new();

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        // This is the future bridge point where the host campaign session will
        // publish which companion heroes are eligible for guest control.
        _seatRegistry.SetMissionState(CompanionMissionState.WaitingForHostSeatAssignments);
    }

    public CompanionSeatRegistry SeatRegistry => _seatRegistry;
}

