namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatOffer(
    string SeatId,
    string HeroStringId,
    string CharacterStringId,
    string DisplayName,
    CompanionMissionRole Role,
    CompanionMissionJoinScope AllowedJoinScopes,
    bool AllowGuestControl,
    bool IsReserved,
    string? ReservedByRemotePlayerId);
