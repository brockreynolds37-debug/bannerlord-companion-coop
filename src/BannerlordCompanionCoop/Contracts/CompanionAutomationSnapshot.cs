using System.Collections.Generic;
using BannerlordCompanionCoop.Services;

namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionAutomationSnapshot(
    string? SaveId,
    CompanionMissionJoinScope JoinScope,
    CompanionMissionState State,
    string Summary,
    IReadOnlyList<CompanionSeatOffer> SeatOffers,
    IReadOnlyList<CompanionSeatAssignment> Assignments);
