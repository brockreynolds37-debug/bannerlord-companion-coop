namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatAssignment(
    string SeatId,
    string HeroStringId,
    string CharacterStringId,
    string DisplayName,
    string RemotePlayerId,
    CompanionMissionJoinScope JoinScope);
