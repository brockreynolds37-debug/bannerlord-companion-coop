using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CampaignHostSession
{
    private readonly List<CompanionSeatDefinition> _availableSeats = new();
    private readonly List<CompanionHeroProfile> _availableHeroes = new();

    public string? ActiveSaveId { get; private set; }

    public IReadOnlyList<CompanionSeatDefinition> AvailableSeats => _availableSeats;

    public IReadOnlyList<CompanionHeroProfile> AvailableHeroes => _availableHeroes;

    public void Start(string saveId)
    {
        ActiveSaveId = saveId;
        _availableSeats.Clear();
        _availableHeroes.Clear();
    }

    public void PublishSeat(CompanionSeatDefinition seatDefinition)
    {
        if (ActiveSaveId is null)
        {
            throw new InvalidOperationException("Host session must start before seats are published.");
        }

        _availableSeats.Add(seatDefinition);
    }

    public void PublishSeat(CompanionHeroProfile heroProfile, CompanionMissionJoinScope joinScope)
    {
        if (ActiveSaveId is null)
        {
            throw new InvalidOperationException("Host session must start before seats are published.");
        }

        _availableHeroes.Add(heroProfile);

        string scopeSuffix = joinScope.ToString();
        string seatId = $"{heroProfile.HeroStringId}:{scopeSuffix}".ToLowerInvariant();
        bool allowGuestControl = !heroProfile.IsWounded;
        CompanionSeatDefinition seatDefinition = new(
            seatId,
            heroProfile.HeroStringId,
            heroProfile.DisplayName,
            heroProfile.PreferredRole,
            joinScope,
            allowGuestControl);

        _availableSeats.Add(seatDefinition);
    }

    public MissionSeatSnapshot BuildMissionSnapshot(CompanionMissionJoinScope joinScope)
    {
        if (ActiveSaveId is null)
        {
            throw new InvalidOperationException("Host session must start before mission snapshots are built.");
        }

        CompanionSeatDefinition[] seats = _availableSeats
            .Where(seat => seat.AllowGuestControl && seat.JoinScope.Allows(joinScope))
            .ToArray();

        return new MissionSeatSnapshot(ActiveSaveId, joinScope, seats);
    }
}
