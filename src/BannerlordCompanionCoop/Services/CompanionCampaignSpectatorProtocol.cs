using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class CompanionCampaignSpectatorProtocol
{
    public static CompanionCampaignSpectatorSnapshot DeserializeSnapshot(string json)
    {
        CompanionCampaignSpectatorSnapshotDto dto =
            CompanionJsonSerializer.Deserialize<CompanionCampaignSpectatorSnapshotDto>(json);

        return new CompanionCampaignSpectatorSnapshot(
            dto.SaveId,
            dto.HostDisplayName ?? string.Empty,
            dto.Summary ?? string.Empty,
            dto.FactionName,
            dto.CurrentSettlementName,
            dto.NearestSettlementName,
            dto.TargetDescription,
            dto.Gold,
            dto.PartySize,
            dto.FoodDaysRemaining,
            dto.MapPositionX,
            dto.MapPositionY,
            dto.IsInSettlement,
            dto.IsInMapEvent,
            (dto.RecentEvents ?? new List<string>()).ToArray());
    }

    public static string SerializeSnapshot(CompanionCampaignSpectatorSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        CompanionCampaignSpectatorSnapshotDto dto = new()
        {
            SaveId = snapshot.SaveId,
            HostDisplayName = snapshot.HostDisplayName,
            Summary = snapshot.Summary,
            FactionName = snapshot.FactionName,
            CurrentSettlementName = snapshot.CurrentSettlementName,
            NearestSettlementName = snapshot.NearestSettlementName,
            TargetDescription = snapshot.TargetDescription,
            Gold = snapshot.Gold,
            PartySize = snapshot.PartySize,
            FoodDaysRemaining = snapshot.FoodDaysRemaining,
            MapPositionX = snapshot.MapPositionX,
            MapPositionY = snapshot.MapPositionY,
            IsInSettlement = snapshot.IsInSettlement,
            IsInMapEvent = snapshot.IsInMapEvent,
            RecentEvents = new List<string>(snapshot.RecentEvents),
        };

        return CompanionJsonSerializer.Serialize(dto);
    }

    [DataContract]
    private sealed class CompanionCampaignSpectatorSnapshotDto
    {
        [DataMember(Name = "saveId", Order = 1, EmitDefaultValue = false)]
        public string? SaveId { get; set; }

        [DataMember(Name = "hostDisplayName", Order = 2)]
        public string? HostDisplayName { get; set; }

        [DataMember(Name = "summary", Order = 3)]
        public string? Summary { get; set; }

        [DataMember(Name = "factionName", Order = 4, EmitDefaultValue = false)]
        public string? FactionName { get; set; }

        [DataMember(Name = "currentSettlementName", Order = 5, EmitDefaultValue = false)]
        public string? CurrentSettlementName { get; set; }

        [DataMember(Name = "nearestSettlementName", Order = 6, EmitDefaultValue = false)]
        public string? NearestSettlementName { get; set; }

        [DataMember(Name = "targetDescription", Order = 7, EmitDefaultValue = false)]
        public string? TargetDescription { get; set; }

        [DataMember(Name = "gold", Order = 8)]
        public int Gold { get; set; }

        [DataMember(Name = "partySize", Order = 9)]
        public int PartySize { get; set; }

        [DataMember(Name = "foodDaysRemaining", Order = 10)]
        public float FoodDaysRemaining { get; set; }

        [DataMember(Name = "mapPositionX", Order = 11)]
        public float MapPositionX { get; set; }

        [DataMember(Name = "mapPositionY", Order = 12)]
        public float MapPositionY { get; set; }

        [DataMember(Name = "isInSettlement", Order = 13)]
        public bool IsInSettlement { get; set; }

        [DataMember(Name = "isInMapEvent", Order = 14)]
        public bool IsInMapEvent { get; set; }

        [DataMember(Name = "recentEvents", Order = 15)]
        public List<string> RecentEvents { get; set; } = new();
    }
}
