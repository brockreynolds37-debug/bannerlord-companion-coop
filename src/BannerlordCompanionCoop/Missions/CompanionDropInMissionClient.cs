using System.Collections.Generic;
using System.Collections.ObjectModel;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Services;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionClient : MissionMultiplayerGameModeBaseClient
{
    private readonly List<CompanionSeatOffer> _seatOffers = new();

    public string? RequestedSeatId { get; private set; }

    public string? AssignedHeroStringId { get; private set; }

    public CompanionMissionState MissionState { get; private set; }

    public ReadOnlyCollection<CompanionSeatOffer> SeatOffers => _seatOffers.AsReadOnly();

    public override MultiplayerGameType GameType => MultiplayerGameType.Battle;

    public override bool IsGameModeTactical => false;

    public override bool IsGameModeUsingRoundCountdown => false;

    public override bool IsGameModeUsingGold => false;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();

        // Future client flow:
        // 1. receive seat offers from host
        // 2. pick an available companion seat
        // 3. possess the spawned agent for that hero
    }

    public void ApplyMissionPlan(CompanionMissionPlan plan, string remotePlayerId)
    {
        _seatOffers.Clear();
        _seatOffers.AddRange(plan.SeatOffers);
        MissionState = plan.State;

        CompanionSeatAssignment? assignment = FindAssignmentForRemotePlayer(plan.Assignments, remotePlayerId);
        if (assignment is not null)
        {
            ApplyAssignment(assignment);
        }
    }

    public void RequestSeat(CompanionSeatDefinition seatDefinition)
    {
        RequestedSeatId = seatDefinition.SeatId;
    }

    public CompanionSeatOffer? GetFirstAvailableSeat()
    {
        foreach (CompanionSeatOffer offer in _seatOffers)
        {
            if (offer.AllowGuestControl && !offer.IsReserved)
            {
                return offer;
            }
        }

        return null;
    }

    public void ApplyAssignment(CompanionSeatAssignment assignment)
    {
        RequestedSeatId = assignment.SeatId;
        AssignedHeroStringId = assignment.HeroStringId;
    }

    public override int GetGoldAmount()
    {
        return 0;
    }

    public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
    {
    }

    private static CompanionSeatAssignment? FindAssignmentForRemotePlayer(
        IReadOnlyList<CompanionSeatAssignment> assignments,
        string remotePlayerId)
    {
        foreach (CompanionSeatAssignment assignment in assignments)
        {
            if (assignment.RemotePlayerId == remotePlayerId)
            {
                return assignment;
            }
        }

        return null;
    }
}
