namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatDefinition(
    string SeatId,
    string HeroStringId,
    string CharacterStringId,
    string DisplayName,
    CompanionMissionRole Role,
    CompanionMissionJoinScope AllowedJoinScopes,
    bool AllowGuestControl);
