using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using BannerlordCompanionCoop.Contracts;

namespace BannerlordCompanionCoop.Services;

public static class CompanionAutomationProtocol
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static CompanionAutomationCommand DeserializeCommand(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Command payload cannot be empty.", nameof(json));
        }

        CompanionAutomationCommand? command = JsonSerializer.Deserialize<CompanionAutomationCommand>(json, JsonOptions);
        if (command is null)
        {
            throw new InvalidOperationException("Command payload could not be deserialized.");
        }

        return command;
    }

    public static string SerializeSnapshot(CompanionAutomationSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static string SerializeResult(CompanionAutomationResult result)
    {
        return JsonSerializer.Serialize(result, JsonOptions);
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
