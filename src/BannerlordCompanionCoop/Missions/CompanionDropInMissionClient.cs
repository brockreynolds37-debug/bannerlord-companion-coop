using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Diagnostics;
using BannerlordCompanionCoop.Networking;
using BannerlordCompanionCoop.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerlordCompanionCoop.Missions;

public sealed class CompanionDropInMissionClient : MissionMultiplayerGameModeBaseClient
{
    private const InputKey RequestCompanionControlHotKey = InputKey.O;
    private readonly List<CompanionSeatOffer> _seatOffers = new();
    private bool _hasShownBattleHint;
    private string? _lastAutoRequestKey;
    private string? _preferredHeroStringId;
    private string? _preferredSeatId;
    private string? _lastSpectatorEventMessage;
    private string? _lastSpectatorSummaryMessage;

    public string? RequestedSeatId { get; private set; }

    public string? AssignedHeroStringId { get; private set; }

    public string? AssignedCharacterStringId { get; private set; }

    public string? LastSeatClaimMessage { get; private set; }

    public bool? LastSeatClaimSucceeded { get; private set; }

    public string? LocalRemotePlayerId { get; private set; }

    public CompanionMissionJoinScope ActiveJoinScope { get; private set; }

    public CompanionMissionState MissionState { get; private set; }

    public CompanionCampaignSpectatorSnapshot? CampaignSpectatorSnapshot { get; private set; }

    public ReadOnlyCollection<CompanionSeatOffer> SeatOffers => _seatOffers.AsReadOnly();

    public override MultiplayerGameType GameType => MultiplayerGameType.Battle;

    public override bool IsGameModeTactical => false;

    public override bool IsGameModeUsingRoundCountdown => false;

    public override bool IsGameModeUsingGold => false;

    public override void OnBehaviorInitialize()
    {
        base.OnBehaviorInitialize();
        CompanionModLogger.Info(
            "MissionClient",
            $"Initialized mission client behavior for mission '{Mission.SceneName}' (networkClient={GameNetwork.IsClient}).");
    }

    public void ApplyMissionPlan(CompanionMissionPlan plan, string remotePlayerId)
    {
        _seatOffers.Clear();
        _seatOffers.AddRange(plan.SeatOffers);
        LocalRemotePlayerId = remotePlayerId;
        ActiveJoinScope = plan.JoinScope;
        MissionState = plan.State;
        AssignedHeroStringId = null;
        AssignedCharacterStringId = null;

        CompanionSeatAssignment? assignment = FindAssignmentForRemotePlayer(plan.Assignments, remotePlayerId);
        RequestedSeatId = ResolveRequestedSeatId(plan, remotePlayerId, RequestedSeatId);
        if (assignment is not null)
        {
            ApplyAssignment(assignment);
        }

        CompanionModLogger.Info(
            "MissionClient",
            $"Applied mission plan mission='{plan.MissionInstanceId}', remotePlayer='{remotePlayerId}', scope={plan.JoinScope}, state={plan.State}, seatOffers={plan.SeatOffers.Count}, assignments={plan.Assignments.Count}, requestedSeat='{RequestedSeatId ?? "none"}'.");
        ShowBattleHintIfNeeded();
        TryAutoRequestSeat(plan);
    }

    public bool RequestSeat(CompanionSeatDefinition seatDefinition)
    {
        CompanionMissionJoinScope joinScope = ActiveJoinScope == CompanionMissionJoinScope.None
            ? seatDefinition.AllowedJoinScopes
            : ActiveJoinScope;

        return RequestSeat(seatDefinition.SeatId, joinScope);
    }

    public bool RequestSeat(CompanionSeatOffer seatOffer)
    {
        return RequestSeat(seatOffer.SeatId, ActiveJoinScope);
    }

    public bool RequestSeat(string seatId, CompanionMissionJoinScope joinScope)
    {
        RequestedSeatId = seatId;
        LastSeatClaimSucceeded = null;
        LastSeatClaimMessage = null;

        if (joinScope == CompanionMissionJoinScope.None)
        {
            LastSeatClaimSucceeded = false;
            LastSeatClaimMessage = "Cannot request a companion seat before a mission scope is active.";
            CompanionModLogger.Warn(
                "MissionClient",
                $"Rejected local seat request for seat '{seatId}' because no active mission scope was available.");
            return false;
        }

        CompanionMissionNetworkBehavior? networkBehavior = Mission.GetMissionBehavior<CompanionMissionNetworkBehavior>();
        bool requested = networkBehavior is not null && networkBehavior.RequestSeatClaim(seatId);
        if (!requested && RequestedSeatId == seatId)
        {
            RequestedSeatId = null;
        }

        CompanionModLogger.Info(
            "MissionClient",
            $"Seat request for seat '{seatId}' in scope '{joinScope}' returned {requested}.");
        return requested;
    }

    public CompanionSeatOffer? GetFirstAvailableSeat()
    {
        foreach (CompanionSeatOffer offer in _seatOffers)
        {
            if (offer.AllowGuestControl && !offer.IsReserved)
            {
                return offer;
            }
        }

        return null;
    }

    public void ApplyAssignment(CompanionSeatAssignment assignment)
    {
        RequestedSeatId = assignment.SeatId;
        AssignedHeroStringId = assignment.HeroStringId;
        AssignedCharacterStringId = assignment.CharacterStringId;
        LastSeatClaimSucceeded = true;
        _preferredSeatId = assignment.SeatId;
        _preferredHeroStringId = assignment.HeroStringId;
        _lastAutoRequestKey = null;
        CompanionModLogger.Info(
            "MissionClient",
            $"Applied assignment seat='{assignment.SeatId}', hero='{assignment.HeroStringId}', character='{assignment.CharacterStringId}'.");
    }

    public void ApplySeatClaimResult(string seatId, bool success, string message)
    {
        LastSeatClaimSucceeded = success;
        LastSeatClaimMessage = message;
        CompanionModLogger.Info(
            "MissionClient",
            $"Seat claim result seat='{seatId}', success={success}, message='{message}'.");
        ShowStatus(message);

        if (!success && RequestedSeatId == seatId)
        {
            RequestedSeatId = null;
        }
    }

    public void ApplyCampaignSpectatorSnapshot(CompanionCampaignSpectatorSnapshot snapshot)
    {
        CampaignSpectatorSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        BannerlordCompanionCoopSubModule.ApplyRemoteCampaignSpectatorSnapshot(snapshot);
        CompanionModLogger.Info(
            "MissionClient",
            $"Applied campaign spectator snapshot summary='{snapshot.Summary}', recentEvents={snapshot.RecentEvents.Count}.");
        ShowCampaignSpectatorContext(snapshot);
    }

    public void ClearCampaignSpectatorSnapshot()
    {
        CampaignSpectatorSnapshot = null;
        _lastSpectatorEventMessage = null;
        _lastSpectatorSummaryMessage = null;
        BannerlordCompanionCoopSubModule.ClearRemoteCampaignSpectatorSnapshot();
        CompanionModLogger.Info("MissionClient", "Cleared campaign spectator snapshot.");
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);

        if (!ShouldHandleCompanionControlHotkeys())
        {
            return;
        }

        ShowBattleHintIfNeeded();

        if (!Input.IsKeyPressed(RequestCompanionControlHotKey))
        {
            return;
        }

        CompanionSeatOffer? seatOffer = FindSeatOfferNearCamera();
        if (seatOffer is null)
        {
            CompanionModLogger.Warn("MissionClient", "Companion control hotkey pressed, but no nearby eligible seat was found.");
            ShowStatus("Move the camera near an available companion, then press O to take control.");
            return;
        }

        if (!RequestSeat(seatOffer))
        {
            CompanionModLogger.Warn(
                "MissionClient",
                $"Companion control hotkey request failed for seat '{seatOffer.SeatId}' ({seatOffer.DisplayName}).");
            ShowStatus(LastSeatClaimMessage ?? $"Could not request control of {seatOffer.DisplayName}.");
            return;
        }

        CompanionModLogger.Info(
            "MissionClient",
            $"Companion control hotkey requested seat '{seatOffer.SeatId}' ({seatOffer.DisplayName}).");
        ShowStatus($"Requested control of {seatOffer.DisplayName}.");
    }

    public override int GetGoldAmount()
    {
        return 0;
    }

    public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
    {
    }

    private static CompanionSeatAssignment? FindAssignmentForRemotePlayer(
        IReadOnlyList<CompanionSeatAssignment> assignments,
        string remotePlayerId)
    {
        foreach (CompanionSeatAssignment assignment in assignments)
        {
            if (assignment.RemotePlayerId == remotePlayerId)
            {
                return assignment;
            }
        }

        return null;
    }

    private CompanionSeatOffer? FindSeatOfferNearCamera()
    {
        if (Mission.Scene is null)
        {
            return null;
        }

        var cameraPosition = Mission.Scene.LastFinalRenderCameraPosition;
        CompanionSeatOffer? nearestOffer = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (CompanionSeatOffer seatOffer in _seatOffers)
        {
            if (!CanRequestControl(seatOffer))
            {
                continue;
            }

            Agent? matchingAgent = FindMatchingAgent(seatOffer);
            if (matchingAgent is null)
            {
                continue;
            }

            float distanceSquared = matchingAgent.Position.DistanceSquared(cameraPosition);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestOffer = seatOffer;
            }
        }

        return nearestOffer;
    }

    private Agent? FindMatchingAgent(CompanionSeatOffer seatOffer)
    {
        foreach (Agent agent in Mission.AllAgents)
        {
            if (IsSelectableCompanionAgent(agent, seatOffer))
            {
                return agent;
            }
        }

        return null;
    }

    private bool CanRequestControl(CompanionSeatOffer seatOffer)
    {
        if (!seatOffer.AllowGuestControl || !seatOffer.AllowedJoinScopes.Allows(ActiveJoinScope))
        {
            return false;
        }

        return !seatOffer.IsReserved
            || string.Equals(seatOffer.ReservedByRemotePlayerId, LocalRemotePlayerId, StringComparison.Ordinal);
    }

    private bool ShouldHandleCompanionControlHotkeys()
    {
        return GameNetwork.IsClient
            && GameNetwork.MyPeer is not null
            && !GameNetwork.MyPeer.IsServerPeer
            && Mission.Scene is not null
            && ActiveJoinScope.Allows(CompanionMissionJoinScope.Battles)
            && MissionState != CompanionMissionState.MissionEnded
            && _seatOffers.Count > 0;
    }

    private void ShowBattleHintIfNeeded()
    {
        if (_hasShownBattleHint || !ShouldHandleCompanionControlHotkeys())
        {
            return;
        }

        _hasShownBattleHint = true;
        ShowStatus("Press O near a companion to request control during battle.");
    }

    private static bool IsSelectableCompanionAgent(Agent agent, CompanionSeatOffer seatOffer)
    {
        return agent.IsHuman
            && agent.State == AgentState.Active
            && AgentMatchesSeat(agent, seatOffer);
    }

    private static bool AgentMatchesSeat(Agent agent, CompanionSeatOffer seatOffer)
    {
        return Matches(agent.Character?.StringId, seatOffer.CharacterStringId)
            || Matches(TryGetHeroStringId(agent), seatOffer.HeroStringId)
            || Matches(agent.Name, seatOffer.DisplayName);
    }

    private static string? TryGetHeroStringId(Agent agent)
    {
        return agent.Character is CharacterObject characterObject
            ? characterObject.HeroObject?.StringId
            : null;
    }

    private static bool Matches(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left)
            && !string.IsNullOrWhiteSpace(right)
            && string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private void TryAutoRequestSeat(CompanionMissionPlan plan)
    {
        if (!ShouldAutoRequestSeat())
        {
            return;
        }

        CompanionSeatOffer? preferredSeat = FindPreferredSeatOffer();
        CompanionSeatOffer? autoSelectedSeat = preferredSeat ?? GetFirstAvailableSeat();
        if (autoSelectedSeat is null)
        {
            return;
        }

        string autoRequestKey = $"{plan.MissionInstanceId}|{autoSelectedSeat.SeatId}";
        if (string.Equals(_lastAutoRequestKey, autoRequestKey, StringComparison.Ordinal))
        {
            return;
        }

        if (!RequestSeat(autoSelectedSeat))
        {
            CompanionModLogger.Warn(
                "MissionClient",
                $"Automatic seat request failed for seat '{autoSelectedSeat.SeatId}' ({autoSelectedSeat.DisplayName}).");
            return;
        }

        _lastAutoRequestKey = autoRequestKey;
        string strategy = preferredSeat is not null ? "preferred" : "first-available";
        CompanionModLogger.Info(
            "MissionClient",
            $"Automatically requested {strategy} seat '{autoSelectedSeat.SeatId}' ({autoSelectedSeat.DisplayName}).");
        ShowStatus($"Requesting {autoSelectedSeat.DisplayName} for this mission.");
    }

    private CompanionSeatOffer? FindPreferredSeatOffer()
    {
        foreach (CompanionSeatOffer seatOffer in _seatOffers)
        {
            if (!CanRequestControl(seatOffer))
            {
                continue;
            }

            if (Matches(seatOffer.SeatId, _preferredSeatId)
                || Matches(seatOffer.HeroStringId, _preferredHeroStringId))
            {
                return seatOffer;
            }
        }

        return null;
    }

    private bool ShouldAutoRequestSeat()
    {
        if (!GameNetwork.IsClient || GameNetwork.MyPeer is null || GameNetwork.MyPeer.IsServerPeer)
        {
            return false;
        }

        if (MissionState == CompanionMissionState.MissionEnded
            || !string.IsNullOrWhiteSpace(AssignedHeroStringId)
            || !string.IsNullOrWhiteSpace(RequestedSeatId)
            || _seatOffers.Count == 0)
        {
            return false;
        }

        return ActiveJoinScope != CompanionMissionJoinScope.None;
    }

    private static string? ResolveRequestedSeatId(
        CompanionMissionPlan plan,
        string remotePlayerId,
        string? requestedSeatId)
    {
        if (string.IsNullOrWhiteSpace(requestedSeatId))
        {
            return null;
        }

        foreach (CompanionSeatOffer seatOffer in plan.SeatOffers)
        {
            if (Matches(seatOffer.SeatId, requestedSeatId)
                && string.Equals(seatOffer.ReservedByRemotePlayerId, remotePlayerId, StringComparison.Ordinal))
            {
                return seatOffer.SeatId;
            }
        }

        return null;
    }

    private void ShowCampaignSpectatorContext(CompanionCampaignSpectatorSnapshot snapshot)
    {
        string summaryMessage = $"Host campaign: {snapshot.Summary}";
        if (!string.Equals(_lastSpectatorSummaryMessage, summaryMessage, StringComparison.Ordinal))
        {
            _lastSpectatorSummaryMessage = summaryMessage;
            ShowStatus(summaryMessage);
        }

        string? latestEvent = snapshot.RecentEvents.Count > 0
            ? snapshot.RecentEvents[snapshot.RecentEvents.Count - 1]
            : null;

        if (string.IsNullOrWhiteSpace(latestEvent)
            || string.Equals(_lastSpectatorEventMessage, latestEvent, StringComparison.Ordinal))
        {
            return;
        }

        string latestEventMessage = latestEvent!;
        _lastSpectatorEventMessage = latestEventMessage;
        ShowStatus(latestEventMessage);
    }

    private static void ShowStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        InformationManager.DisplayMessage(new InformationMessage(message));
    }
}
