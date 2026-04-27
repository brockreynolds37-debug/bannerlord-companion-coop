using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionServer : MissionMultiplayerGameModeBase
{
    private readonly CampaignHostSession _hostSession = new();
    private readonly CompanionSeatRegistry _seatRegistry = new();
    private CompanionMissionCoordinator? _coordinator;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        _coordinator = new CompanionMissionCoordinator(_hostSession, _seatRegistry);
        _coordinator.InitializeDebugMission("debug_sandbox_save", CompanionMissionJoinScope.Battles);

        // Temporary hardcoded claim so the first PC pass can prove seat flow
        // before real network messages exist.
        _coordinator.TryClaimSeat(
            new CompanionSeatClaim(
                "companion_alayen:battles",
                "debug_remote_player_1",
                CompanionMissionJoinScope.Battles));
        _coordinator.BeginMission();
    }

    public CompanionSeatRegistry SeatRegistry => _seatRegistry;

    public CampaignHostSession HostSession => _hostSession;

    public string DebugSummary => _coordinator?.BuildDebugSummary() ?? "state=uninitialized";
}
