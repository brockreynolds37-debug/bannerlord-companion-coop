namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionHeroProfile(
    string HeroStringId,
    string CharacterStringId,
    string DisplayName,
    CompanionMissionRole PreferredRole,
    int Level,
    bool IsWounded);
