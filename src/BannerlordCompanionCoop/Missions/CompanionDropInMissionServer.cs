using System;
using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
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

        CompanionModLogger.Info(
            "MissionServer",
            $"Initializing standalone mission server behavior for mission '{Mission.SceneName}'.");
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

    public CompanionCampaignSpectatorSnapshot? LatestCampaignSpectatorSnapshot =>
        BannerlordCompanionCoopSubModule.LatestCampaignSpectatorSnapshot;

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
            CompanionModLogger.Info(
                "MissionServer",
                $"Automation claimed seat '{claim.SeatId}' for remote player '{claim.RemotePlayerId}'.");
        }
        else
        {
            CompanionModLogger.Warn(
                "MissionServer",
                $"Automation failed to claim seat '{claim.SeatId}' for remote player '{claim.RemotePlayerId}'.");
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
            CompanionModLogger.Info("MissionServer", message);
            return true;
        }

        message = $"Seat '{seatId}' could not be claimed for remote player '{remotePlayerId}'.";
        CompanionModLogger.Warn("MissionServer", message);
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
        CompanionModLogger.Info("MissionServer", "Mission transitioned to live state.");
    }

    public bool TryRestorePreferredSeatForPeer(NetworkCommunicator networkPeer)
    {
        if (_coordinator is null)
        {
            return false;
        }

        string remotePlayerId = CompanionRemotePlayerId.FromNetworkPeer(networkPeer);
        bool restored = _coordinator.TryRestorePreferredSeat(remotePlayerId);
        if (!restored)
        {
            return false;
        }

        RefreshPlan();
        CompanionModLogger.Info(
            "MissionServer",
            $"Restored preferred seat for remote player '{remotePlayerId}' before mission sync.");
        return true;
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
            CompanionModLogger.Info(
                "MissionServer",
                $"Released {released} seat reservation(s) for remote player '{remotePlayerId}'.");
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
        if (_latestPlan is not null)
        {
            CompanionModLogger.Info(
                "MissionServer",
                $"Refreshed mission plan scope={_latestPlan.JoinScope}, state={_latestPlan.State}, seatOffers={_latestPlan.SeatOffers.Count}, assignments={_latestPlan.Assignments.Count}.");
        }
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
            CompanionModLogger.Info(
                "MissionServer",
                $"Initialized mission roster from campaign seed '{saveId}' with {companionProfiles.Count} companion(s).");
            return;
        }

        _coordinator.InitializeDebugMission("debug_sandbox_save", CompanionMissionJoinScope.Battles);
        CompanionModLogger.Warn("MissionServer", "Initialized mission roster from debug fallback catalog.");
    }
}
