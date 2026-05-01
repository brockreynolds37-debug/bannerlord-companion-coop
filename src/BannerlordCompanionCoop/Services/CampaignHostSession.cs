using System;
using System.Collections.Generic;
using System.Linq;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CampaignHostSession
{
    private readonly List<CompanionSeatDefinition> _availableSeats = new();
    private readonly List<CompanionHeroProfile> _availableHeroes = new();
    private readonly Dictionary<string, string> _preferredSeatIdsByRemotePlayerId = new(StringComparer.Ordinal);

    public string? ActiveSaveId { get; private set; }

    public IReadOnlyList<CompanionSeatDefinition> AvailableSeats => _availableSeats;

    public IReadOnlyList<CompanionHeroProfile> AvailableHeroes => _availableHeroes;

    public void Start(string saveId)
    {
        if (!string.Equals(ActiveSaveId, saveId, StringComparison.Ordinal))
        {
            _preferredSeatIdsByRemotePlayerId.Clear();
        }

        ActiveSaveId = saveId;
        _availableSeats.Clear();
        _availableHeroes.Clear();
    }

    public void RememberSeatPreference(string remotePlayerId, string seatId)
    {
        if (string.IsNullOrWhiteSpace(remotePlayerId))
        {
            throw new ArgumentException("Remote player id is required.", nameof(remotePlayerId));
        }

        if (string.IsNullOrWhiteSpace(seatId))
        {
            throw new ArgumentException("Seat id is required.", nameof(seatId));
        }

        _preferredSeatIdsByRemotePlayerId[remotePlayerId] = seatId;
    }

    public bool TryGetPreferredSeatId(string remotePlayerId, out string? seatId)
    {
        return _preferredSeatIdsByRemotePlayerId.TryGetValue(remotePlayerId, out seatId);
    }

    public void PublishSeat(CompanionSeatDefinition seatDefinition)
    {
        if (ActiveSaveId is null)
        {
            throw new InvalidOperationException("Host session must start before seats are published.");
        }

        _availableSeats.Add(seatDefinition);
    }

    public void PublishSeat(CompanionHeroProfile heroProfile, CompanionMissionJoinScope allowedJoinScopes)
    {
        if (ActiveSaveId is null)
        {
            throw new InvalidOperationException("Host session must start before seats are published.");
        }

        _availableHeroes.Add(heroProfile);

        string seatId = $"{heroProfile.HeroStringId}:{allowedJoinScopes.ToSeatScopeToken()}".ToLowerInvariant();
        bool allowGuestControl = !heroProfile.IsWounded && allowedJoinScopes != CompanionMissionJoinScope.None;
        CompanionSeatDefinition seatDefinition = new(
            seatId,
            heroProfile.HeroStringId,
            heroProfile.CharacterStringId,
            heroProfile.DisplayName,
            heroProfile.PreferredRole,
            allowedJoinScopes,
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
            .Where(seat => seat.AllowGuestControl && seat.AllowedJoinScopes.Allows(joinScope))
            .ToArray();

        return new MissionSeatSnapshot(Guid.NewGuid().ToString("N"), ActiveSaveId, joinScope, seats);
    }
}
