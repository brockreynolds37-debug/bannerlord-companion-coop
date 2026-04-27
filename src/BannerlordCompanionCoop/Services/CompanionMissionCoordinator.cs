using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CompanionMissionCoordinator
{
    private readonly CampaignHostSession _hostSession;
    private readonly CompanionSeatRegistry _seatRegistry;
    private readonly List<CompanionSeatAssignment> _assignments = new();

    public CompanionMissionCoordinator(CampaignHostSession hostSession, CompanionSeatRegistry seatRegistry)
    {
        _hostSession = hostSession ?? throw new ArgumentNullException(nameof(hostSession));
        _seatRegistry = seatRegistry ?? throw new ArgumentNullException(nameof(seatRegistry));
    }

    public ReadOnlyCollection<CompanionSeatAssignment> Assignments => _assignments.AsReadOnly();

    public void InitializeDebugMission(string saveId, CompanionMissionJoinScope joinScope)
    {
        _hostSession.Start(saveId);

        foreach (CompanionHeroProfile profile in DebugSeatCatalog.CreateDefaultProfiles())
        {
            _hostSession.PublishSeat(profile, joinScope);
        }

        PublishSeatsForMission(joinScope);
    }

    public void PublishSeatsForMission(CompanionMissionJoinScope joinScope)
    {
        MissionSeatSnapshot snapshot = _hostSession.BuildMissionSnapshot(joinScope);
        _seatRegistry.ReplaceDefinitions(snapshot.Seats);
        _seatRegistry.SetMissionState(CompanionMissionState.WaitingForGuestSelections);
        _assignments.Clear();
    }

    public bool TryClaimSeat(CompanionSeatClaim claim)
    {
        if (!_seatRegistry.TryReserveSeat(claim.SeatId, claim.RemotePlayerId, claim.JoinScope))
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

    public string BuildDebugSummary()
    {
        return $"state={_seatRegistry.State}; seats={_seatRegistry.Definitions.Count}; claims={_seatRegistry.Reservations.Count}; assignments={_assignments.Count}";
    }

    private void RebuildAssignments()
    {
        _assignments.Clear();
        _assignments.AddRange(_seatRegistry.BuildAssignments());
    }
}
