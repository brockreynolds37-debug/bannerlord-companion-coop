namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionAutomationCommand(
    string CommandId,
    CompanionAutomationCommandKind Kind,
    string? SaveId = null,
    string? SeatId = null,
    string? RemotePlayerId = null,
    CompanionMissionJoinScope? JoinScope = null);
