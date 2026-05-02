using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CompanionSeatRegistry
{
    private readonly Dictionary<string, CompanionSeatDefinition> _definitions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CompanionSeatReservation> _reservations = new(StringComparer.Ordinal);

    public CompanionMissionState State { get; private set; }

    public ReadOnlyCollection<CompanionSeatDefinition> Definitions =>
        _definitions.Values.ToList().AsReadOnly();

    public ReadOnlyCollection<CompanionSeatReservation> Reservations =>
        _reservations.Values.ToList().AsReadOnly();

    public void SetMissionState(CompanionMissionState state)
    {
        State = state;
    }

    public void ReplaceDefinitions(IEnumerable<CompanionSeatDefinition> definitions)
    {
        HashSet<string> activeSeatIds = new(StringComparer.Ordinal);
        _definitions.Clear();

        foreach (CompanionSeatDefinition definition in definitions)
        {
            _definitions[definition.SeatId] = definition;
            activeSeatIds.Add(definition.SeatId);
        }

        string[] staleReservationSeatIds = _reservations.Keys
            .Where(seatId => !activeSeatIds.Contains(seatId))
            .ToArray();

        foreach (string seatId in staleReservationSeatIds)
        {
            _reservations.Remove(seatId);
        }
    }

    public bool TryReserveSeat(string seatId, string remotePlayerId, CompanionMissionJoinScope joinScope)
    {
        if (!_definitions.TryGetValue(seatId, out CompanionSeatDefinition? definition))
        {
            return false;
        }

        if (!definition.AllowGuestControl || !definition.AllowedJoinScopes.Allows(joinScope))
        {
            return false;
        }

        if (_reservations.TryGetValue(seatId, out CompanionSeatReservation? existingSeatReservation))
        {
            if (!string.Equals(existingSeatReservation.RemotePlayerId, remotePlayerId, StringComparison.Ordinal))
            {
                return false;
            }

            _reservations[seatId] = new CompanionSeatReservation(seatId, remotePlayerId, joinScope);
            return true;
        }

        if (TryGetReservationForRemotePlayer(remotePlayerId, out CompanionSeatReservation? existingPlayerReservation))
        {
            _reservations.Remove(existingPlayerReservation!.SeatId);
        }

        _reservations[seatId] = new CompanionSeatReservation(seatId, remotePlayerId, joinScope);
        return true;
    }

    public bool TryGetReservation(string seatId, out CompanionSeatReservation? reservation)
    {
        return _reservations.TryGetValue(seatId, out reservation);
    }

    public bool ReleaseSeat(string seatId)
    {
        return _reservations.Remove(seatId);
    }

    public bool IsSeatReserved(string seatId)
    {
        return _reservations.ContainsKey(seatId);
    }

    public bool TryGetReservationForRemotePlayer(string remotePlayerId, out CompanionSeatReservation? reservation)
    {
        reservation = _reservations.Values.FirstOrDefault(
            value => string.Equals(value.RemotePlayerId, remotePlayerId, StringComparison.Ordinal));

        return reservation is not null;
    }

    public ReadOnlyCollection<CompanionSeatDefinition> GetAvailableSeats()
    {
        List<CompanionSeatDefinition> seats = _definitions.Values
            .Where(definition => definition.AllowGuestControl && !_reservations.ContainsKey(definition.SeatId))
            .OrderBy(definition => definition.DisplayName, StringComparer.Ordinal)
            .ToList();

        return seats.AsReadOnly();
    }

    public IReadOnlyList<CompanionSeatAssignment> BuildAssignments()
    {
        List<CompanionSeatAssignment> assignments = new();

        foreach (KeyValuePair<string, CompanionSeatReservation> pair in _reservations)
        {
            string seatId = pair.Key;
            CompanionSeatReservation reservation = pair.Value;

            if (!_definitions.TryGetValue(seatId, out CompanionSeatDefinition? definition))
            {
                continue;
            }

            assignments.Add(
                new CompanionSeatAssignment(
                    definition.SeatId,
                    definition.HeroStringId,
                    definition.CharacterStringId,
                    definition.DisplayName,
                    reservation.RemotePlayerId,
                    reservation.JoinScope));
        }

        return assignments
            .OrderBy(assignment => assignment.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<CompanionSeatOffer> BuildSeatOffers()
    {
        List<CompanionSeatOffer> offers = new();

        foreach (CompanionSeatDefinition definition in _definitions.Values.OrderBy(definition => definition.DisplayName, StringComparer.Ordinal))
        {
            _reservations.TryGetValue(definition.SeatId, out CompanionSeatReservation? reservation);
            offers.Add(
                new CompanionSeatOffer(
                    definition.SeatId,
                    definition.HeroStringId,
                    definition.CharacterStringId,
                    definition.DisplayName,
                    definition.Role,
                    definition.AllowedJoinScopes,
                    definition.AllowGuestControl,
                    reservation is not null,
                    reservation?.RemotePlayerId));
        }

        return offers;
    }

    public int ReleaseSeatsForRemotePlayer(string remotePlayerId)
    {
        string[] claimedSeatIds = _reservations
            .Where(pair => string.Equals(pair.Value.RemotePlayerId, remotePlayerId, StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .ToArray();

        foreach (string seatId in claimedSeatIds)
        {
            _reservations.Remove(seatId);
        }

        return claimedSeatIds.Length;
    }

    public void ClearReservations()
    {
        _reservations.Clear();
    }
}
