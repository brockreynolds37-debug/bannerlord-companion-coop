namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatOffer(
    string SeatId,
    string HeroStringId,
    string DisplayName,
    CompanionMissionRole Role,
    CompanionMissionJoinScope JoinScope,
    bool AllowGuestControl,
    bool IsReserved,
    string? ReservedByRemotePlayerId);
