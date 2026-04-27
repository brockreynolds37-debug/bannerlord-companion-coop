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
        _definitions.Clear();

        foreach (CompanionSeatDefinition definition in definitions)
        {
            _definitions[definition.SeatId] = definition;
        }
    }

    public bool TryReserveSeat(string seatId, string remotePlayerId, CompanionMissionJoinScope joinScope)
    {
        if (!_definitions.TryGetValue(seatId, out CompanionSeatDefinition? definition))
        {
            return false;
        }

        if (!definition.AllowGuestControl || _reservations.ContainsKey(seatId))
        {
            return false;
        }

        _reservations[seatId] = new CompanionSeatReservation(seatId, remotePlayerId, joinScope);
        return true;
    }

    public bool ReleaseSeat(string seatId)
    {
        return _reservations.Remove(seatId);
    }

    public bool IsSeatReserved(string seatId)
    {
        return _reservations.ContainsKey(seatId);
    }
}

