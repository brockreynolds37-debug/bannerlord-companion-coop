using System;
using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CampaignHostSession
{
    private readonly List<CompanionSeatDefinition> _availableSeats = new();

    public string? ActiveSaveId { get; private set; }

    public IReadOnlyList<CompanionSeatDefinition> AvailableSeats => _availableSeats;

    public void Start(string saveId)
    {
        ActiveSaveId = saveId;
        _availableSeats.Clear();
    }

    public void PublishSeat(CompanionSeatDefinition seatDefinition)
    {
        if (ActiveSaveId is null)
        {
            throw new InvalidOperationException("Host session must start before seats are published.");
        }

        _availableSeats.Add(seatDefinition);
    }
}

