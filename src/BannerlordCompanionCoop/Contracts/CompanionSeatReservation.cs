namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatReservation(
    string SeatId,
    string RemotePlayerId,
    CompanionMissionJoinScope JoinScope);

