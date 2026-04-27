using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionServer : MissionMultiplayerGameModeBase
{
    private readonly CampaignHostSession _hostSession = new();
    private readonly CompanionSeatRegistry _seatRegistry = new();
    private CompanionMissionCoordinator? _coordinator;
    private CompanionMissionPlan? _latestPlan;

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
        RefreshPlan();
    }

    public CompanionSeatRegistry SeatRegistry => _seatRegistry;

    public CampaignHostSession HostSession => _hostSession;

    public string DebugSummary => _coordinator?.BuildDebugSummary() ?? "state=uninitialized";

    public CompanionMissionPlan? LatestPlan => _latestPlan;

    public override bool IsGameModeUsingOpposingTeams => false;

    public override bool IsGameModeHidingAllAgentVisuals => false;

    public override MultiplayerGameType GetMissionType()
    {
        return MultiplayerGameType.Battle;
    }

    public bool TryClaimSeatForRemotePlayer(CompanionSeatClaim claim)
    {
        if (_coordinator is null)
        {
            return false;
        }

        bool claimed = _coordinator.TryClaimSeat(claim);

        if (claimed)
        {
            RefreshPlan();
        }

        return claimed;
    }

    public int ReleaseRemotePlayer(string remotePlayerId)
    {
        if (_coordinator is null)
        {
            return 0;
        }

        int released = _coordinator.ReleaseRemotePlayer(remotePlayerId);

        if (released > 0)
        {
            RefreshPlan();
        }

        return released;
    }

    private void RefreshPlan()
    {
        if (_coordinator is null)
        {
            _latestPlan = null;
            return;
        }

        _latestPlan = _coordinator.BuildMissionPlan();
    }
}
