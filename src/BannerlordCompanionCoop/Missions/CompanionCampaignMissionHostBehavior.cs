using System;
using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
using BannerlordCompanionCoop.Integration;
using BannerlordCompanionCoop.Networking;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionCampaignMissionHostBehavior : MissionLogic, ICompanionMissionHost
{
    private readonly CampaignHostSession _hostSession = new();
    private readonly CompanionSeatRegistry _seatRegistry = new();
    private CompanionMissionCoordinator? _coordinator;
    private CompanionMissionPlan? _latestPlan;

    public event Action<CompanionMissionPlan?>? MissionPlanChanged;

    public CompanionMissionPlan? LatestPlan => _latestPlan;

    public CompanionCampaignSpectatorSnapshot? LatestCampaignSpectatorSnapshot =>
        BannerlordCompanionCoopSubModule.LatestCampaignSpectatorSnapshot;

    public CompanionMissionJoinScope CurrentJoinScope => _latestPlan?.JoinScope ?? CompanionMissionJoinScope.None;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        CompanionMissionJoinScope joinScope = CompanionMissionSceneScopeResolver.ResolveForMission(Mission);
        if (joinScope == CompanionMissionJoinScope.None)
        {
            return;
        }

        CompanionModLogger.Info(
            "CampaignHost",
            $"Initializing campaign mission host for mission '{Mission.SceneName}' with scope '{joinScope}'.");
        _coordinator = new CompanionMissionCoordinator(_hostSession, _seatRegistry);
        InitializeMissionRoster(joinScope);
        RefreshPlan();
    }

    public bool TryClaimSeatForPeer(NetworkCommunicator networkPeer, string seatId, out string message)
    {
        if (_coordinator is null)
        {
            message = "Campaign mission host is not initialized.";
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
            CompanionModLogger.Info("CampaignHost", message);
            return true;
        }

        message = $"Seat '{seatId}' could not be claimed for remote player '{remotePlayerId}'.";
        CompanionModLogger.Warn("CampaignHost", message);
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
        CompanionModLogger.Info("CampaignHost", "Campaign mission transitioned to live state.");
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
            "CampaignHost",
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
                "CampaignHost",
                $"Released {released} seat reservation(s) for remote player '{remotePlayerId}'.");
        }

        return released;
    }

    private void InitializeMissionRoster(CompanionMissionJoinScope joinScope)
    {
        if (_coordinator is null)
        {
            return;
        }

        if (CampaignCompanionRosterProvider.TryCreateMissionSeed(
            out string saveId,
            out IReadOnlyList<CompanionHeroProfile> companionProfiles))
        {
            _coordinator.InitializeMission(saveId, companionProfiles, joinScope);
            CompanionModLogger.Info(
                "CampaignHost",
                $"Initialized campaign mission roster from save '{saveId}' with {companionProfiles.Count} companion(s).");
            return;
        }

        _coordinator.InitializeDebugMission("campaign_debug_sandbox_save", joinScope);
        CompanionModLogger.Warn("CampaignHost", "Initialized campaign mission roster from debug fallback catalog.");
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
                "CampaignHost",
                $"Refreshed mission plan scope={_latestPlan.JoinScope}, state={_latestPlan.State}, seatOffers={_latestPlan.SeatOffers.Count}, assignments={_latestPlan.Assignments.Count}.");
        }
        MissionPlanChanged?.Invoke(_latestPlan);
    }
}
