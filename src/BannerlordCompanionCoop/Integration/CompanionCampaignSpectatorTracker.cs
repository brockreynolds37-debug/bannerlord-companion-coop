using System;
using System.Collections.Generic;
using BannerlordCompanionCoop.Contracts;
using BannerlordCompanionCoop.Services;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerlordCompanionCoop.Integration;

public sealed class CompanionCampaignSpectatorTracker
{
    private const int MaxRecentEvents = 8;
    private const int SignificantGoldDelta = 50;
    private const float RefreshIntervalSeconds = 0.5f;

    private readonly Queue<string> _recentEvents = new();
    private float _elapsedSinceRefresh;
    private string? _lastEventMessage;
    private string? _lastSettlementName;
    private string? _lastTargetDescription;
    private bool? _lastIsInMapEvent;
    private int? _lastGold;

    public CompanionCampaignSpectatorSnapshot? LatestSnapshot { get; private set; }

    public string? LatestSnapshotJson =>
        LatestSnapshot is null ? null : CompanionCampaignSpectatorProtocol.SerializeSnapshot(LatestSnapshot);

    public void OnApplicationTick(float dt)
    {
        _elapsedSinceRefresh += Math.Max(dt, 0f);
        if (_elapsedSinceRefresh < RefreshIntervalSeconds)
        {
            return;
        }

        _elapsedSinceRefresh = 0f;

        if (!TryBuildSnapshot(out CompanionCampaignSpectatorSnapshot? snapshot))
        {
            Reset();
            return;
        }

        UpdateRecentEvents(snapshot!);
        LatestSnapshot = snapshot! with { RecentEvents = _recentEvents.ToArray() };
    }

    public void Reset()
    {
        _elapsedSinceRefresh = 0f;
        _lastEventMessage = null;
        _lastSettlementName = null;
        _lastTargetDescription = null;
        _lastIsInMapEvent = null;
        _lastGold = null;
        _recentEvents.Clear();
        LatestSnapshot = null;
    }

    private void UpdateRecentEvents(CompanionCampaignSpectatorSnapshot snapshot)
    {
        if (LatestSnapshot is null)
        {
            AddEvent($"Watching {snapshot.HostDisplayName}'s campaign.");
        }

        if (!string.Equals(_lastSettlementName, snapshot.CurrentSettlementName, StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(snapshot.CurrentSettlementName))
            {
                AddEvent($"Entered {snapshot.CurrentSettlementName}.");
            }
            else if (!string.IsNullOrWhiteSpace(_lastSettlementName))
            {
                AddEvent($"Left {_lastSettlementName}.");
            }
        }

        if (_lastIsInMapEvent.HasValue && _lastIsInMapEvent.Value != snapshot.IsInMapEvent)
        {
            if (snapshot.IsInMapEvent)
            {
                AddEvent(
                    string.IsNullOrWhiteSpace(snapshot.NearestSettlementName)
                        ? "Encounter started."
                        : $"Encounter started near {snapshot.NearestSettlementName}.");
            }
            else
            {
                AddEvent("Encounter ended.");
            }
        }

        if (_lastGold.HasValue && _lastGold.Value != snapshot.Gold)
        {
            int goldDelta = snapshot.Gold - _lastGold.Value;
            if (goldDelta >= SignificantGoldDelta)
            {
                AddEvent($"Gained {goldDelta} denars.");
            }
            else if (goldDelta <= -SignificantGoldDelta)
            {
                AddEvent($"Spent {-goldDelta} denars.");
            }
        }

        if (!snapshot.IsInSettlement
            && !snapshot.IsInMapEvent
            && !string.IsNullOrWhiteSpace(snapshot.TargetDescription)
            && !string.Equals(_lastTargetDescription, snapshot.TargetDescription, StringComparison.Ordinal))
        {
            AddEvent($"Now heading toward {snapshot.TargetDescription}.");
        }

        _lastSettlementName = snapshot.CurrentSettlementName;
        _lastTargetDescription = snapshot.TargetDescription;
        _lastIsInMapEvent = snapshot.IsInMapEvent;
        _lastGold = snapshot.Gold;
    }

    private void AddEvent(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.Equals(_lastEventMessage, message, StringComparison.Ordinal))
        {
            return;
        }

        _recentEvents.Enqueue(message);
        _lastEventMessage = message;

        while (_recentEvents.Count > MaxRecentEvents)
        {
            _recentEvents.Dequeue();
        }
    }

    private static bool TryBuildSnapshot(out CompanionCampaignSpectatorSnapshot? snapshot)
    {
        Campaign? campaign = Campaign.Current;
        MobileParty? mainParty = MobileParty.MainParty;
        Hero? mainHero = Hero.MainHero;
        if (campaign is null || mainParty is null || mainHero is null || mainParty.Party is null)
        {
            snapshot = null;
            return false;
        }

        string hostDisplayName = mainHero.Name?.ToString() ?? "Host";
        string? currentSettlementName = ToDisplayName(mainParty.CurrentSettlement);
        string? nearestSettlementName = currentSettlementName ?? FindNearestSettlementName(mainParty);
        string? targetDescription = DescribeTarget(mainParty, currentSettlementName, nearestSettlementName);
        string? factionName = mainParty.MapFaction?.Name?.ToString();
        int gold = mainHero.Gold;
        int partySize = mainParty.Party.NumberOfHealthyMembers;
        float foodDaysRemaining = mainParty.GetNumDaysForFoodToLast();
        bool isInSettlement = !string.IsNullOrWhiteSpace(currentSettlementName);
        bool isInMapEvent = mainParty.MapEvent is not null;

        var mapPosition = mainParty.GetPosition2D;

        snapshot = new CompanionCampaignSpectatorSnapshot(
            campaign.UniqueGameId,
            hostDisplayName,
            BuildSummary(mainParty, currentSettlementName, nearestSettlementName, targetDescription, isInSettlement, isInMapEvent),
            factionName,
            currentSettlementName,
            nearestSettlementName,
            targetDescription,
            gold,
            partySize,
            foodDaysRemaining,
            mapPosition.x,
            mapPosition.y,
            isInSettlement,
            isInMapEvent,
            Array.Empty<string>());

        return true;
    }

    private static string BuildSummary(
        MobileParty mainParty,
        string? currentSettlementName,
        string? nearestSettlementName,
        string? targetDescription,
        bool isInSettlement,
        bool isInMapEvent)
    {
        if (isInMapEvent)
        {
            return string.IsNullOrWhiteSpace(nearestSettlementName)
                ? "In an active encounter."
                : $"In an active encounter near {nearestSettlementName}.";
        }

        if (isInSettlement && !string.IsNullOrWhiteSpace(currentSettlementName))
        {
            return $"Inside {currentSettlementName}.";
        }

        if (!string.IsNullOrWhiteSpace(targetDescription) && !string.IsNullOrWhiteSpace(nearestSettlementName))
        {
            return mainParty.IsMoving
                ? $"Travelling near {nearestSettlementName} toward {targetDescription}."
                : $"Waiting near {nearestSettlementName} while focused on {targetDescription}.";
        }

        if (!string.IsNullOrWhiteSpace(nearestSettlementName))
        {
            return mainParty.IsMoving
                ? $"Travelling near {nearestSettlementName}."
                : $"Waiting near {nearestSettlementName}.";
        }

        return mainParty.IsMoving
            ? "Travelling on the campaign map."
            : "Waiting on the campaign map.";
    }

    private static string? DescribeTarget(
        MobileParty mainParty,
        string? currentSettlementName,
        string? nearestSettlementName)
    {
        MobileParty? moveTargetParty = mainParty.MoveTargetParty;
        if (moveTargetParty is not null && moveTargetParty != mainParty)
        {
            return ToDisplayName(moveTargetParty);
        }

        if (mainParty.IsMoving && !string.IsNullOrWhiteSpace(nearestSettlementName))
        {
            return nearestSettlementName;
        }

        return currentSettlementName;
    }

    private static string? FindNearestSettlementName(MobileParty mainParty)
    {
        Settlement? nearestSettlement = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (Settlement settlement in Settlement.FindAll(static settlement => settlement.IsActive))
        {
            float distanceSquared = settlement.GetPosition2D.DistanceSquared(mainParty.GetPosition2D);
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestSettlement = settlement;
            }
        }

        return ToDisplayName(nearestSettlement);
    }

    private static string? ToDisplayName(object? value)
    {
        return value switch
        {
            null => null,
            Settlement settlement => settlement.Name?.ToString(),
            MobileParty party => party.Name?.ToString(),
            _ => value.ToString(),
        };
    }
}
