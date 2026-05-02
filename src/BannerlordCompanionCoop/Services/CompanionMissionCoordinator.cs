using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CompanionMissionCoordinator
{
    private readonly CampaignHostSession _hostSession;
    private readonly CompanionSeatRegistry _seatRegistry;
    private readonly List<CompanionSeatAssignment> _assignments = new();
    private MissionSeatSnapshot? _activeSnapshot;

    public CompanionMissionCoordinator(CampaignHostSession hostSession, CompanionSeatRegistry seatRegistry)
    {
        _hostSession = hostSession ?? throw new ArgumentNullException(nameof(hostSession));
        _seatRegistry = seatRegistry ?? throw new ArgumentNullException(nameof(seatRegistry));
    }

    public ReadOnlyCollection<CompanionSeatAssignment> Assignments => _assignments.AsReadOnly();

    public string? ActiveSaveId => _activeSnapshot?.SaveId ?? _hostSession.ActiveSaveId;

    public CompanionMissionJoinScope ActiveJoinScope => _activeSnapshot?.JoinScope ?? CompanionMissionJoinScope.None;

    public CompanionMissionState State => _seatRegistry.State;

    public bool HasActiveMission => _activeSnapshot is not null;

    public void InitializeMission(
        string saveId,
        IEnumerable<CompanionHeroProfile> companionProfiles,
        CompanionMissionJoinScope joinScope)
    {
        if (companionProfiles is null)
        {
            throw new ArgumentNullException(nameof(companionProfiles));
        }

        _hostSession.Start(saveId);

        foreach (CompanionHeroProfile profile in companionProfiles.Where(IsSeatPublishable))
        {
            _hostSession.PublishSeat(profile, CompanionMissionJoinScope.AllSupportedScenes);
        }

        PublishSeatsForMission(joinScope);
    }

    public void InitializeDebugMission(string saveId, CompanionMissionJoinScope joinScope)
    {
        InitializeMission(saveId, DebugSeatCatalog.CreateDefaultProfiles(), joinScope);
    }

    public void PublishSeatsForMission(CompanionMissionJoinScope joinScope)
    {
        _activeSnapshot = _hostSession.BuildMissionSnapshot(joinScope);
        _seatRegistry.ReplaceDefinitions(_activeSnapshot.Seats);
        _seatRegistry.SetMissionState(CompanionMissionState.WaitingForGuestSelections);
        _assignments.Clear();
    }

    public bool TryClaimSeat(CompanionSeatClaim claim)
    {
        if (!_seatRegistry.TryReserveSeat(claim.SeatId, claim.RemotePlayerId, claim.JoinScope))
        {
            return false;
        }

        _hostSession.RememberSeatPreference(claim.RemotePlayerId, claim.SeatId);
        RebuildAssignments();
        return true;
    }

    public bool TryRestorePreferredSeat(string remotePlayerId)
    {
        if (_activeSnapshot is null
            || string.IsNullOrWhiteSpace(remotePlayerId)
            || !_hostSession.TryGetPreferredSeatId(remotePlayerId, out string? preferredSeatId)
            || string.IsNullOrWhiteSpace(preferredSeatId))
        {
            return false;
        }

        if (_seatRegistry.TryGetReservationForRemotePlayer(remotePlayerId, out _))
        {
            return false;
        }

        string seatId = preferredSeatId!;
        if (!_seatRegistry.TryReserveSeat(seatId, remotePlayerId, _activeSnapshot.JoinScope))
        {
            return false;
        }

        RebuildAssignments();
        return true;
    }

    public void BeginMission()
    {
        _seatRegistry.SetMissionState(CompanionMissionState.SpawningPlayers);
        RebuildAssignments();
        _seatRegistry.SetMissionState(CompanionMissionState.MissionLive);
    }

    public void EndMission()
    {
        _seatRegistry.SetMissionState(CompanionMissionState.MissionEnded);
    }

    public CompanionMissionPlan BuildMissionPlan()
    {
        if (_activeSnapshot is null)
        {
            throw new InvalidOperationException("Mission seats must be published before a mission plan is built.");
        }

        return new CompanionMissionPlan(
            _activeSnapshot.MissionInstanceId,
            _activeSnapshot.SaveId,
            _activeSnapshot.JoinScope,
            _seatRegistry.State,
            _seatRegistry.BuildSeatOffers(),
            Assignments);
    }

    public CompanionMissionPlan? TryBuildMissionPlan()
    {
        return _activeSnapshot is null ? null : BuildMissionPlan();
    }

    public int ReleaseRemotePlayer(string remotePlayerId)
    {
        int releasedCount = _seatRegistry.ReleaseSeatsForRemotePlayer(remotePlayerId);

        if (releasedCount > 0)
        {
            RebuildAssignments();
        }

        return releasedCount;
    }

    public string BuildDebugSummary()
    {
        if (_activeSnapshot is null)
        {
            return $"state={_seatRegistry.State}; snapshot=none; seats={_seatRegistry.Definitions.Count}; claims={_seatRegistry.Reservations.Count}; assignments={_assignments.Count}";
        }

        return $"state={_seatRegistry.State}; save={_activeSnapshot.SaveId}; scope={_activeSnapshot.JoinScope}; seats={_seatRegistry.Definitions.Count}; claims={_seatRegistry.Reservations.Count}; assignments={_assignments.Count}";
    }

    private void RebuildAssignments()
    {
        _assignments.Clear();
        _assignments.AddRange(_seatRegistry.BuildAssignments());
    }

    private static bool IsSeatPublishable(CompanionHeroProfile profile)
    {
        return !string.IsNullOrWhiteSpace(profile.HeroStringId)
            && !string.IsNullOrWhiteSpace(profile.CharacterStringId)
            && !string.IsNullOrWhiteSpace(profile.DisplayName);
    }
}
