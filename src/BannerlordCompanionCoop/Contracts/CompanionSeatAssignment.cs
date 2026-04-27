namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatAssignment(
    string SeatId,
    string HeroStringId,
    string DisplayName,
    string RemotePlayerId,
    CompanionMissionJoinScope JoinScope);
