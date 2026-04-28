using System;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CompanionAutomationBridge
{
    private readonly CompanionMissionCoordinator _coordinator;

    public CompanionAutomationBridge(CompanionMissionCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    public CompanionAutomationSnapshot BuildSnapshot()
    {
        CompanionMissionPlan? plan = _coordinator.TryBuildMissionPlan();

        return new CompanionAutomationSnapshot(
            _coordinator.ActiveSaveId,
            _coordinator.ActiveJoinScope,
            _coordinator.State,
            _coordinator.BuildDebugSummary(),
            plan?.SeatOffers ?? Array.Empty<CompanionSeatOffer>(),
            plan?.Assignments ?? Array.Empty<CompanionSeatAssignment>());
    }

    public CompanionAutomationResult Execute(CompanionAutomationCommand command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        bool success;
        string message;

        switch (command.Kind)
        {
            case CompanionAutomationCommandKind.GetSnapshot:
                success = true;
                message = "Snapshot refreshed.";
                break;
            case CompanionAutomationCommandKind.InitializeDebugMission:
                success = TryInitializeDebugMission(command, out message);
                break;
            case CompanionAutomationCommandKind.PublishSeatsForMission:
                success = TryPublishSeatsForMission(command, out message);
                break;
            case CompanionAutomationCommandKind.ClaimSeat:
                success = TryClaimSeat(command, out message);
                break;
            case CompanionAutomationCommandKind.ReleaseRemotePlayer:
                success = TryReleaseRemotePlayer(command, out message);
                break;
            case CompanionAutomationCommandKind.BeginMission:
                success = TryBeginMission(out message);
                break;
            case CompanionAutomationCommandKind.EndMission:
                success = TryEndMission(out message);
                break;
            default:
                success = false;
                message = $"Unsupported automation command kind '{command.Kind}'.";
                break;
        }

        return new CompanionAutomationResult(
            command.CommandId,
            command.Kind,
            success,
            message,
            BuildSnapshot());
    }

    private bool TryInitializeDebugMission(CompanionAutomationCommand command, out string message)
    {
        if (!command.JoinScope.HasValue)
        {
            message = "InitializeDebugMission requires a join scope.";
            return false;
        }

        string saveId = string.IsNullOrWhiteSpace(command.SaveId)
            ? "debug_sandbox_save"
            : command.SaveId;

        _coordinator.InitializeDebugMission(saveId, command.JoinScope.Value);
        message = $"Debug mission initialized for save '{saveId}' with scope '{command.JoinScope.Value}'.";
        return true;
    }

    private bool TryPublishSeatsForMission(CompanionAutomationCommand command, out string message)
    {
        if (!_coordinator.HasActiveMission)
        {
            message = "Cannot publish seats before a host session is initialized.";
            return false;
        }

        if (!command.JoinScope.HasValue)
        {
            message = "PublishSeatsForMission requires a join scope.";
            return false;
        }

        _coordinator.PublishSeatsForMission(command.JoinScope.Value);
        message = $"Published mission seats for scope '{command.JoinScope.Value}'.";
        return true;
    }

    private bool TryClaimSeat(CompanionAutomationCommand command, out string message)
    {
        if (!_coordinator.HasActiveMission)
        {
            message = "Cannot claim a seat before a mission has been published.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.SeatId) || string.IsNullOrWhiteSpace(command.RemotePlayerId))
        {
            message = "ClaimSeat requires both seatId and remotePlayerId.";
            return false;
        }

        CompanionMissionJoinScope joinScope = command.JoinScope ?? _coordinator.ActiveJoinScope;
        bool claimed = _coordinator.TryClaimSeat(
            new CompanionSeatClaim(command.SeatId, command.RemotePlayerId, joinScope));

        message = claimed
            ? $"Seat '{command.SeatId}' claimed for remote player '{command.RemotePlayerId}'."
            : $"Seat '{command.SeatId}' could not be claimed for remote player '{command.RemotePlayerId}'.";

        return claimed;
    }

    private bool TryReleaseRemotePlayer(CompanionAutomationCommand command, out string message)
    {
        if (string.IsNullOrWhiteSpace(command.RemotePlayerId))
        {
            message = "ReleaseRemotePlayer requires remotePlayerId.";
            return false;
        }

        int released = _coordinator.ReleaseRemotePlayer(command.RemotePlayerId);
        bool success = released > 0;
        message = success
            ? $"Released {released} seat claim(s) for remote player '{command.RemotePlayerId}'."
            : $"No seat claims were active for remote player '{command.RemotePlayerId}'.";

        return success;
    }

    private bool TryBeginMission(out string message)
    {
        if (!_coordinator.HasActiveMission)
        {
            message = "Cannot begin mission before a mission has been published.";
            return false;
        }

        _coordinator.BeginMission();
        message = "Mission state advanced to live.";
        return true;
    }

    private bool TryEndMission(out string message)
    {
        if (!_coordinator.HasActiveMission)
        {
            message = "Cannot end mission before a mission has been published.";
            return false;
        }

        _coordinator.EndMission();
        message = "Mission state advanced to ended.";
        return true;
    }
}
