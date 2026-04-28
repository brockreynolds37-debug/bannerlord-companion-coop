using System;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public sealed class CompanionCampaignSpectatorSession
{
    public CompanionCampaignSpectatorSnapshot? LatestSnapshot { get; private set; }

    public DateTime? LastUpdatedUtc { get; private set; }

    public bool HasSnapshot => LatestSnapshot is not null;

    public string Summary => LatestSnapshot?.Summary ?? "No campaign spectator snapshot is available.";

    public void ApplySnapshot(CompanionCampaignSpectatorSnapshot snapshot)
    {
        LatestSnapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        LastUpdatedUtc = DateTime.UtcNow;
    }

    public void ApplySnapshotJson(string snapshotJson)
    {
        ApplySnapshot(CompanionCampaignSpectatorProtocol.DeserializeSnapshot(snapshotJson));
    }

    public void Clear()
    {
        LatestSnapshot = null;
        LastUpdatedUtc = null;
    }

    public string BuildDebugSummary()
    {
        CompanionCampaignSpectatorSnapshot? snapshot = LatestSnapshot;
        if (snapshot is null)
        {
            return "campaign spectator session: empty";
        }

        return $"campaign spectator session: host={snapshot.HostDisplayName}; save={snapshot.SaveId}; summary={snapshot.Summary}";
    }
}
