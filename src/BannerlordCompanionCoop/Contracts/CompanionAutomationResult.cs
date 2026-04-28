namespace BannerlordCompanionCoop.Contracts;

public sealed record CompanionAutomationResult(
    string CommandId,
    CompanionAutomationCommandKind Kind,
    bool Success,
    string Message,
    CompanionAutomationSnapshot Snapshot);
