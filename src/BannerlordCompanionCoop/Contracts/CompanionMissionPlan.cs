using System.Collections.Generic;
using BannerlordCompanionCoop.Services;

namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionMissionPlan(
    string SaveId,
    CompanionMissionJoinScope JoinScope,
    CompanionMissionState State,
    IReadOnlyList<CompanionSeatOffer> SeatOffers,
    IReadOnlyList<CompanionSeatAssignment> Assignments);
