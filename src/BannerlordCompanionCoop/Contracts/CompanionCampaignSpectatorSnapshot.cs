using System.Collections.Generic;

namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionCampaignSpectatorSnapshot(
    string? SaveId,
    string HostDisplayName,
    string Summary,
    string? FactionName,
    string? CurrentSettlementName,
    string? NearestSettlementName,
    string? TargetDescription,
    int Gold,
    int PartySize,
    float FoodDaysRemaining,
    float MapPositionX,
    float MapPositionY,
    bool IsInSettlement,
    bool IsInMapEvent,
    IReadOnlyList<string> RecentEvents);
