namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatClaim(
    string SeatId,
    string RemotePlayerId,
    CompanionMissionJoinScope JoinScope);
