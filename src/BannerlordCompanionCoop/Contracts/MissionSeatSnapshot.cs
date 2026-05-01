using System.Collections.Generic;

namespace BannerlordCompanionCoop.Contracts;

public sealed record MissionSeatSnapshot(
    string MissionInstanceId,
    string SaveId,
    CompanionMissionJoinScope JoinScope,
    IReadOnlyList<CompanionSeatDefinition> Seats);
