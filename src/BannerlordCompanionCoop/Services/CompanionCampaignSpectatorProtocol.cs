using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class CompanionCampaignSpectatorProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static CompanionCampaignSpectatorSnapshot DeserializeSnapshot(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Spectator snapshot payload cannot be empty.", nameof(json));
        }

        CompanionCampaignSpectatorSnapshot? snapshot =
            JsonSerializer.Deserialize<CompanionCampaignSpectatorSnapshot>(json, JsonOptions);

        if (snapshot is null)
        {
            throw new InvalidOperationException("Spectator snapshot payload could not be deserialized.");
        }

        return snapshot;
    }

    public static string SerializeSnapshot(CompanionCampaignSpectatorSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
