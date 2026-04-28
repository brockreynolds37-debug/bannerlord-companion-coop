using System;
using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;
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

    public CompanionMissionJoinScope CurrentJoinScope => _latestPlan?.JoinScope ?? CompanionMissionJoinScope.None;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        CompanionMissionJoinScope joinScope = CompanionMissionSceneScopeResolver.ResolveForMission(Mission);
        if (joinScope == CompanionMissionJoinScope.None)
        {
            return;
        }

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
            return;
        }

        _coordinator.InitializeDebugMission("campaign_debug_sandbox_save", joinScope);
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
}
