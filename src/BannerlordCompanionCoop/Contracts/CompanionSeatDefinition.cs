namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionSeatDefinition(
    string SeatId,
    string HeroStringId,
    string DisplayName,
    CompanionMissionRole Role,
    bool AllowGuestControl);

