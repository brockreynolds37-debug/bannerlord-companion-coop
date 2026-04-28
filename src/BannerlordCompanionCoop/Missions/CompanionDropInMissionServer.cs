using System;
using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Integration;
using BannerlordCompanionCoop.Networking;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionServer : MissionMultiplayerGameModeBase, ICompanionMissionHost
{
    private readonly CampaignHostSession _hostSession = new();
    private readonly CompanionSeatRegistry _seatRegistry = new();
    private CompanionMissionCoordinator? _coordinator;
    private CompanionAutomationBridge? _automationBridge;
    private CompanionMissionPlan? _latestPlan;

    public event Action<CompanionMissionPlan?>? MissionPlanChanged;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        if (!GameNetwork.IsServer)
        {
            return;
        }

        _coordinator = new CompanionMissionCoordinator(_hostSession, _seatRegistry);
        _automationBridge = new CompanionAutomationBridge(_coordinator);
        InitializeMissionRoster();
        RefreshPlan();
    }

    public CompanionSeatRegistry SeatRegistry => _seatRegistry;

    public CampaignHostSession HostSession => _hostSession;

    public CompanionAutomationBridge? AutomationBridge => _automationBridge;

    public string DebugSummary => _coordinator?.BuildDebugSummary() ?? "state=uninitialized";

    public CompanionMissionPlan? LatestPlan => _latestPlan;

    public CompanionMissionJoinScope CurrentJoinScope => _latestPlan?.JoinScope ?? CompanionMissionJoinScope.None;

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

    public bool TryClaimSeatForPeer(NetworkCommunicator networkPeer, string seatId, out string message)
    {
        if (_coordinator is null)
        {
            message = "Mission server is not initialized.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(seatId))
        {
            message = "Seat claim requires a seat id.";
            return false;
        }

        if (CurrentJoinScope == CompanionMissionJoinScope.None)
        {
            message = "Mission seats are not currently published.";
            return false;
        }

        string remotePlayerId = CompanionRemotePlayerId.FromNetworkPeer(networkPeer);
        bool claimed = _coordinator.TryClaimSeat(
            new CompanionSeatClaim(seatId, remotePlayerId, CurrentJoinScope));

        if (claimed)
        {
            RefreshPlan();
            message = $"Seat '{seatId}' claimed for remote player '{remotePlayerId}'.";
            return true;
        }

        message = $"Seat '{seatId}' could not be claimed for remote player '{remotePlayerId}'.";
        return false;
    }

    public void EnsureMissionStarted()
    {
        if (_coordinator is null)
        {
            return;
        }

        if (_coordinator.State == CompanionMissionState.MissionLive || _coordinator.State == CompanionMissionState.MissionEnded)
        {
            return;
        }

        _coordinator.BeginMission();
        RefreshPlan();
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
            MissionPlanChanged?.Invoke(_latestPlan);
            return;
        }

        _latestPlan = _coordinator.TryBuildMissionPlan();
        MissionPlanChanged?.Invoke(_latestPlan);
    }

    private void InitializeMissionRoster()
    {
        if (_coordinator is null)
        {
            return;
        }

        if (CampaignCompanionRosterProvider.TryCreateMissionSeed(
            out string saveId,
            out IReadOnlyList<CompanionHeroProfile> companionProfiles))
        {
            _coordinator.InitializeMission(saveId, companionProfiles, CompanionMissionJoinScope.Battles);
            return;
        }

        _coordinator.InitializeDebugMission("debug_sandbox_save", CompanionMissionJoinScope.Battles);
    }
}
