using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionServer : MissionMultiplayerGameModeBase
{
    private readonly CampaignHostSession _hostSession = new();
    private readonly CompanionSeatRegistry _seatRegistry = new();
    private CompanionMissionCoordinator? _coordinator;
    private CompanionAutomationBridge? _automationBridge;
    private CompanionMissionPlan? _latestPlan;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        _coordinator = new CompanionMissionCoordinator(_hostSession, _seatRegistry);
        _automationBridge = new CompanionAutomationBridge(_coordinator);
        _coordinator.InitializeDebugMission("debug_sandbox_save", CompanionMissionJoinScope.Battles);
        RefreshPlan();
    }

    public CompanionSeatRegistry SeatRegistry => _seatRegistry;

    public CampaignHostSession HostSession => _hostSession;

    public CompanionAutomationBridge? AutomationBridge => _automationBridge;

    public string DebugSummary => _coordinator?.BuildDebugSummary() ?? "state=uninitialized";

    public CompanionMissionPlan? LatestPlan => _latestPlan;

    public override bool IsGameModeUsingOpposingTeams => false;

    public override bool IsGameModeHidingAllAgentVisuals => false;

    public override MultiplayerGameType GetMissionType()
    {
        return MultiplayerGameType.Battle;
    }

    public CompanionAutomationSnapshot? BuildAutomationSnapshot()
    {
        return _automationBridge?.BuildSnapshot();
    }

    public string? BuildAutomationSnapshotJson()
    {
        CompanionAutomationSnapshot? snapshot = BuildAutomationSnapshot();
        return snapshot is null ? null : CompanionAutomationProtocol.SerializeSnapshot(snapshot);
    }

    public CompanionAutomationResult? ExecuteAutomationCommand(CompanionAutomationCommand command)
    {
        if (_automationBridge is null)
        {
            return null;
        }

        CompanionAutomationResult result = _automationBridge.Execute(command);
        RefreshPlan();
        return result;
    }

    public string? ExecuteAutomationCommandJson(string commandJson)
    {
        if (_automationBridge is null)
        {
            return null;
        }

        CompanionAutomationCommand command = CompanionAutomationProtocol.DeserializeCommand(commandJson);
        CompanionAutomationResult result = _automationBridge.Execute(command);
        RefreshPlan();
        return CompanionAutomationProtocol.SerializeResult(result);
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

        _latestPlan = _coordinator.TryBuildMissionPlan();
    }
}
